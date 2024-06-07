// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBSearch;

sealed class CosmosDBSearchProvider : ISearchProvider
{
    readonly IConfiguration configuration;
    readonly ILogger logger;
    readonly IOptions cosmosDBSearchConfigOptions;
    readonly int vectorSearchDimensions = 1536;
    readonly int numLists = 1;
    const string defaultSearchIndexName = "openai-index";
    const string databaseName = "openai-database";

    public string Name { get; set; } = "CosmosDBSearch";

    internal class OutputDocument
    {
        public OutputDocument(string title, string text)
        {
            this.Title = title;
            this.Text = text;
        }

        [BsonElement("text")]
        public string Text { get; set; }

        [BsonElement("title")]
        public string Title { get; set; }
    }

    /// <summary>
    /// Initializes CosmosDB search provider.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="cosmosDBSearchConfigOptions">Cosmos DB search config options.</param>
    /// <exception cref="ArgumentNullException">Throws ArgumentNullException if logger factory is null.</exception>
    public CosmosDBSearchProvider(IConfiguration configuration, ILoggerFactory loggerFactory, IOptions<CosmosDBSearchConfigOptions> cosmosDBSearchConfigOptions)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        int value = cosmosDBSearchConfigOptions.Value.VectorSearchDimensions;
        if (value < 2 || value > 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(CosmosDBSearchConfigOptions.VectorSearchDimensions), value, "Vector search dimensions must be between 2 and 2000");
        }

        this.cosmosDBSearchConfigOptions = cosmosDBSearchConfigOptions;
        this.logger = loggerFactory.CreateLogger<CosmosDBSearchProvider>();

    }

    /// <summary>
    /// Add a document to the search index.
    /// </summary>
    /// <param name="document">The searchable document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns a task that completes when the document is successfully saved.</returns>
    public async Task AddDocumentAsync(SearchableDocument document, CancellationToken cancellationToken)
    {
        if (document.ConnectionInfo is null)
        {
            throw new ArgumentNullException(nameof(document.ConnectionInfo));
        }
        string connectionString = this.configuration.GetValue<string>(document.ConnectionInfo.ConnectionName);

        MongoClient mongoClient = this.GetMongoClient(connectionString, document.ConnectionInfo.ConnectionName);

        this.CreateVectorIndexIfNotExists(mongoClient, document.ConnectionInfo.CollectionName ?? defaultSearchIndexName);

        await this.UpsertVectorAsync(mongoClient, document);
    }

    /// <summary>
    /// Search for documents using the provided request.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <returns>Search Response.</returns>
    /// <exception cref="ArgumentException">Throws argument exception if query or embeddings is null.</exception>
    /// <exception cref="InvalidOperationException">Throws the invalid operation exception if search result response is null.</exception>
    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        if (request.Embeddings.IsEmpty)
        {
            throw new ArgumentException("Embeddings must be provided.");
        }

        if (request.ConnectionInfo is null)
        {
            throw new ArgumentNullException(nameof(request.ConnectionInfo));
        }

        string connectionString = this.configuration.GetValue<string>(request.ConnectionInfo.ConnectionName);
        MongoClient mongoClient = this.GetMongoClient(connectionString, request.ConnectionInfo.ConnectionName);

        try
        {
            IMongoCollection<BsonDocument> collection = mongoClient.GetDatabase(databaseName).GetCollection<BsonDocument>(request.ConnectionInfo.CollectionName ?? defaultSearchIndexName);
            // Search Azure Cosmos DB for MongoDB vCore collection for similar embeddings and project fields.
            BsonDocument[] pipeline = [];
            switch (this.cosmosDBSearchConfigOptions.Value.Kind)
            {
                case CosmosDBVectorSearchType.VectorIVF:
                    pipeline = this.GetVectorIVFSearchPipeline(request);
                    break;
                case CosmosDBVectorSearchType.VectorHNSW:
                    pipeline = this.GetVectorHNSWSearchPipeline(request);
                    break;
            }

            IAsyncCursor<BsonDocument> cursor = await collection.AggregateAsync<BsonDocument>(pipeline);
            List<BsonDocument> documents = await cursor.ToListAsync();

            List<OutputDocument> resultFromDocuments = documents.ToList().ConvertAll(bsonDocument => BsonSerializer.Deserialize<OutputDocument>(bsonDocument));

            List<SearchResult> searchResults = new();

            foreach (OutputDocument doc in resultFromDocuments)
            {
                searchResults.Add(new SearchResult(doc.Title, doc.Text));
            }

            SearchResponse response = new(searchResults);
            return response;

        }
        catch (MongoException ex)
        {
            this.logger.LogError(ex, "CosmosDBSearchProvider:SearchAsync error");
            throw;
        }
    }

    public void CreateVectorIndexIfNotExists(MongoClient client, string collectionName)
    {
        try
        {
            BsonDocument vectorIndexDefinition = new BsonDocument();
            switch (this.cosmosDBSearchConfigOptions.Value.Kind)
            {
                case AzureCosmosDBVectorSearchType.VectorIVF:
                    vectorIndexDefinition = this.GetIndexDefinitionVectorIVF(collectionName);
                    break;
                case AzureCosmosDBVectorSearchType.VectorHNSW:
                    vectorIndexDefinition = this.GetIndexDefinitionVectorHNSW(collectionName);
                    break;
            }

            //Find if vector index exists in vectors collection
            using IAsyncCursor<BsonDocument> indexCursor = client.GetDatabase(databaseName).GetCollection<BsonDocument>(collectionName).Indexes.List();
            bool vectorIndexExists = indexCursor.ToList().Any(x => x["name"] == "vectorSearchIndex");
            if (!vectorIndexExists)
            {
                BsonDocumentCommand<BsonDocument> command = new(vectorIndexDefinition);

                BsonDocument result = client.GetDatabase(databaseName).RunCommand(command);
                if (result["ok"] != 1)
                {
                    this.logger.LogError("CreateIndex failed with response: " + result.ToJson());
                }
            }

        }
        catch (MongoException ex)
        {
            this.logger.LogError(ex, "CosmosDBSearchProvider:CreateVectorIndexIfNotExists error");
            throw;
        }

    }

    async Task UpsertVectorAsync(MongoClient client, SearchableDocument document)
    {
        List<BsonDocument> list = new();
        for (int i = 0; i < document.Embeddings?.Response?.Data.Count; i++)
        {
            BsonDocument vectorDocument = new()
            {
                { "id", Guid.NewGuid().ToString("N") },
                { "text", document.Embeddings.Request.Input![i] },
                { "title", Path.GetFileNameWithoutExtension(document.Title) },
                { "embedding",  new BsonArray(document.Embeddings.Response.Data[i].Embedding.ToArray().Select(e => new BsonDouble(Convert.ToDouble(e))))},
                { "timestamp", DateTime.UtcNow }
            };
            list.Add(vectorDocument);
        }

        try
        {
            // Insert the documents into the collection
            await client.GetDatabase(databaseName).GetCollection<BsonDocument>(document.ConnectionInfo?.CollectionName ?? defaultSearchIndexName).InsertManyAsync(list);

            this.logger.LogInformation("""
                        Indexed {Count} sections
                        """,
                        list.Count);
        }
        catch (MongoException ex)
        {
            this.logger.LogError(ex, "CosmosDBSearchProvider:UpsertVectorAsync error");
            throw;
        }

    }

    MongoClient GetMongoClient(string connectionString, string connectionStringName)
    {
        // TODO: Add suport for managed identity
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"""
                No CosmosDB mongo connection string named '{connectionStringName}' was found.
                It's required to be specified as an app setting or environment variable.
                """);
        }
        return new MongoClient(connectionString);
    }

    private BsonDocument[] GetVectorIVFSearchPipeline(SearchRequest request) {
        return new BsonDocument[]
            {
                new()
                {
                    {
                        "$search", new BsonDocument
                        {
                            {
                                "cosmosSearch", new BsonDocument
                                {
                                    { "vector", !(request.Embeddings.IsEmpty) ? new BsonArray(request.Embeddings.ToArray()) : new BsonArray()},
                                    { "path", "embedding" },
                                    { "k", request.MaxResults }
                                }
                            },
                            { "returnStoredSource", true }
                        }
                    }
                },
                new()
                {
                    {
                        "$project", new BsonDocument
                        {
                            { "embedding", 0 },
                            { "_id", 0 },
                            { "id", 0 },
                            { "timestamp", 0 },
                            {"similarityScore": { "$meta": "searchScore" }},
                        }
                    }
                }
            };
    }

    private BsonDocument[] GetVectorHNSWSearchPipeline(SearchRequest request) {
        return new BsonDocument[]
            {
                new()
                {
                    {
                        "$search", new BsonDocument
                        {
                            {
                                "cosmosSearch", new BsonDocument
                                {
                                    { "vector", !(request.Embeddings.IsEmpty) ? new BsonArray(request.Embeddings.ToArray()) : new BsonArray()},
                                    { "path", "embedding" },
                                    { "k", request.MaxResults },
                                    { "efSearch", this.cosmosDBSearchConfigOptions.EfSearch }
                                }
                            },
                            { "returnStoredSource", true }
                        }
                    }
                },
                new()
                {
                    {
                        "$project", new BsonDocument
                        {
                            { "embedding", 0 },
                            { "_id", 0 },
                            { "id", 0 },
                            { "timestamp", 0 },
                            {"similarityScore": { "$meta": "searchScore" }},
                        }
                    }
                }
            };
    }

    private BsonDocument GetIndexDefinitionVectorIVF(string collectionName)
    {
        return new BsonDocument
        {
            { "createIndexes", collectionName },
            {
                "indexes", new BsonArray
                {
                    new BsonDocument
                    {
                        { "name", "vectorSearchIndex" },
                        { "key", new BsonDocument { { "embedding", "cosmosSearch" } } },
                        { "cosmosSearchOptions", new BsonDocument
                            {
                                { "kind", this.cosmosDBSearchConfigOptions.Value.Kind.GetCustomName() },
                                { "numLists", this.cosmosDBSearchConfigOptions.Value.NumLists },
                                { "similarity", this.cosmosDBSearchConfigOptions.Value.Similarity.GetCustomName() },
                                { "dimensions", this.cosmosDBSearchConfigOptions.Value.Dimensions }
                            }
                        }
                    }
                }
            }
        };
    }

    private BsonDocument GetIndexDefinitionVectorHNSW(string collectionName)
    {
        return new BsonDocument
        {
            { "createIndexes", collectionName },
            {
                "indexes", new BsonArray
                {
                    new BsonDocument
                    {
                        { "name",  "vectorSearchIndex" },
                        { "key", new BsonDocument { { "embedding", "cosmosSearch" } } },
                        {
                            "cosmosSearchOptions", new BsonDocument
                            {
                                { "kind", this.cosmosDBSearchConfigOptions.Value.Kind.GetCustomName() },
                                { "m", this.cosmosDBSearchConfigOptions.Value.NumberOfConnections },
                                { "efConstruction", this.cosmosDBSearchConfigOptions.Value.EfConstruction },
                                { "similarity", this.cosmosDBSearchConfigOptions.Value.Similarity.GetCustomName() },
                                { "dimensions", this.cosmosDBSearchConfigOptions.Value.Dimensions }
                            }
                        }
                    }
                }
            }
        };
    }
}
