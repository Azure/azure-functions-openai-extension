// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
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
    readonly ILogger logger;
    readonly IOptions<CosmosDBSearchConfigOptions> cosmosDBSearchConfigOptions;
    readonly ConcurrentDictionary<string, MongoClient> cosmosDBClients = new();
<<<<<<< HEAD
<<<<<<< HEAD
    string databaseName = "";
    string collectionName = "";
    string indexName = "";
=======
    readonly string databaseName = "openai-functions-database";
    readonly string collectionName = "openai-functions-collection";
    readonly string indexName = "openai-functions-index";
    readonly MongoClient cosmosClient;
>>>>>>> 94c2ade (resolving comments)
=======
    string databaseName = "openai-functions-database";
    string collectionName = "openai-functions-collection";
    string indexName = "openai-functions-index";
>>>>>>> 3eafd96 (Formatting)

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
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="cosmosDBSearchConfigOptions">Cosmos DB search config options.</param>
    /// <exception cref="ArgumentNullException">Throws ArgumentNullException if logger factory is null.</exception>
    public CosmosDBSearchProvider(
        ILoggerFactory loggerFactory,
        IOptions<CosmosDBSearchConfigOptions> cosmosDBSearchConfigOptions
    )
    {
        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        int value = cosmosDBSearchConfigOptions.Value.VectorSearchDimensions;
        if (value < 2 || value > 2000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CosmosDBSearchConfigOptions.VectorSearchDimensions),
                value,
                "Vector search dimensions must be between 2 and 2000"
            );
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
    public async Task AddDocumentAsync(
        SearchableDocument document,
        CancellationToken cancellationToken
    )
    {
<<<<<<< HEAD
<<<<<<< HEAD
        MongoClient cosmosClient = this.cosmosDBClients.GetOrAdd(
            document.ConnectionInfo!.ConnectionName,
            _ => this.CreateMongoClient(document.ConnectionInfo.ConnectionName)
        );

        this.databaseName = this.cosmosDBSearchConfigOptions.Value.DatabaseName;
        this.collectionName = document.ConnectionInfo.CollectionName;
        this.indexName = this.cosmosDBSearchConfigOptions.Value.IndexName;
        this.CreateVectorIndexIfNotExists(cosmosClient);

        await this.UpsertVectorAsync(cosmosClient, document);
    }

    MongoClient CreateMongoClient(string connectionName)
=======
        this.cosmosClient = cosmosDBClients.GetOrAdd(
=======
        MongoClient cosmosClient = cosmosDBClients.GetOrAdd(
>>>>>>> 3eafd96 (Formatting)
            document.ConnectionInfo!.ConnectionName,
            _ => CreateMongoClient(document.ConnectionInfo.ConnectionName)
        );

        this.databaseName = document.ConnectionInfo.DatabaseName;
        this.collectionName = document.ConnectionInfo.CollectionName;
        this.indexName = cosmosDBSearchConfigOptions.Value.IndexName;
        this.CreateVectorIndexIfNotExists(cosmosClient);

        await this.UpsertVectorAsync(cosmosClient, document);
    }

    MongoClient CreateMongoClient(String connectionName)
>>>>>>> 94c2ade (resolving comments)
    {
        MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionName);
        settings.ApplicationName = this.cosmosDBSearchConfigOptions.Value.ApplicationName;
        return new MongoClient(settings);
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

        try
        {
<<<<<<< HEAD
<<<<<<< HEAD
            MongoClient cosmosClient = this.cosmosDBClients.GetOrAdd(
                request.ConnectionInfo.ConnectionName,
                _ => this.CreateMongoClient(request.ConnectionInfo.ConnectionName)
=======
            MongoClient cosmosClient = cosmosDBClients.GetOrAdd(
                request.ConnectionInfo.ConnectionName,
                _ => CreateMongoClient(request.ConnectionInfo.ConnectionName)
>>>>>>> 3eafd96 (Formatting)
            );

            IMongoCollection<BsonDocument> collection = cosmosClient
                .GetDatabase(this.databaseName)
<<<<<<< HEAD
=======
            IMongoCollection<BsonDocument> collection = this
                .mongoClient.GetDatabase(this.databaseName)
>>>>>>> 94c2ade (resolving comments)
=======
>>>>>>> 3eafd96 (Formatting)
                .GetCollection<BsonDocument>(this.collectionName);
            // Search Azure Cosmos DB for MongoDB vCore collection for similar embeddings and project fields.
            BsonDocument[]? pipeline = null;
            switch (this.cosmosDBSearchConfigOptions.Value.Kind)
            {
                case "vector-ivf":
                    pipeline = this.GetVectorIVFSearchPipeline(request);
                    break;
                case "vector-hnsw":
                    pipeline = this.GetVectorHNSWSearchPipeline(request);
                    break;
            }

            IAsyncCursor<BsonDocument> cursor = await collection.AggregateAsync<BsonDocument>(
                pipeline
            );
            List<BsonDocument> documents = await cursor.ToListAsync();

            List<OutputDocument> resultFromDocuments = documents
                .ToList()
                .ConvertAll(bsonDocument =>
                    BsonSerializer.Deserialize<OutputDocument>(bsonDocument)
                );

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

<<<<<<< HEAD
<<<<<<< HEAD
    void CreateVectorIndexIfNotExists(MongoClient cosmosClient)
=======
    void CreateVectorIndexIfNotExists()
>>>>>>> 94c2ade (resolving comments)
=======
    void CreateVectorIndexIfNotExists(MongoClient cosmosClient)
>>>>>>> 3eafd96 (Formatting)
    {
        try
        {
            //Find if vector index exists in vectors collection
            using IAsyncCursor<BsonDocument> indexCursor = cosmosClient
                .GetDatabase(this.databaseName)
                .GetCollection<BsonDocument>(this.collectionName)
                .Indexes.List();
            bool vectorIndexExists = indexCursor.ToList().Any(x => x["name"] == this.indexName);
            if (!vectorIndexExists)
            {
                BsonDocument vectorIndexDefinition = new BsonDocument();
                switch (this.cosmosDBSearchConfigOptions.Value.Kind)
                {
                    case "vector-ivf":
<<<<<<< HEAD
<<<<<<< HEAD
                        vectorIndexDefinition = this.GetIndexDefinitionVectorIVF();
                        break;
                    case "vector-hnsw":
                        vectorIndexDefinition = this.GetIndexDefinitionVectorHNSW();
=======
                        vectorIndexDefinition = this.GetIndexDefinitionVectorIVF(
                            this.collectionName
                        );
                        break;
                    case "vector-hnsw":
                        vectorIndexDefinition = this.GetIndexDefinitionVectorHNSW(
                            this.collectionName
                        );
>>>>>>> ad8a4dd (Updating readme file)
=======
                        vectorIndexDefinition = this.GetIndexDefinitionVectorIVF();
                        break;
                    case "vector-hnsw":
                        vectorIndexDefinition = this.GetIndexDefinitionVectorHNSW();
>>>>>>> 94c2ade (resolving comments)
                        break;
                }

                BsonDocumentCommand<BsonDocument> command = new(vectorIndexDefinition);
<<<<<<< HEAD
<<<<<<< HEAD
                BsonDocument result = cosmosClient
                    .GetDatabase(this.databaseName)
=======
                BsonDocument result = this
                    .mongoClient.GetDatabase(this.databaseName)
>>>>>>> 94c2ade (resolving comments)
=======
                BsonDocument result = cosmosClient
                    .GetDatabase(this.databaseName)
>>>>>>> 3eafd96 (Formatting)
                    .RunCommand(command);
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

<<<<<<< HEAD
<<<<<<< HEAD
    async Task UpsertVectorAsync(MongoClient cosmosClient, SearchableDocument document)
    {
<<<<<<< HEAD
=======
    async Task UpsertVectorAsync(SearchableDocument document)
=======
    async Task UpsertVectorAsync(MongoClient cosmosClient, SearchableDocument document)
>>>>>>> 3eafd96 (Formatting)
    {
>>>>>>> 94c2ade (resolving comments)
        List<BsonDocument> list = new();
        for (int i = 0; i < document.Embeddings?.Response?.Data.Count; i++)
        {
            BsonDocument vectorDocument =
                new()
                {
                    { "id", Guid.NewGuid().ToString("N") },
                    {
<<<<<<< HEAD
                        this.cosmosDBSearchConfigOptions.Value.TextKey,
=======
                        this.cosmosDBSearchConfigOptions.TextKey,
>>>>>>> 94c2ade (resolving comments)
                        document.Embeddings.Request.Input![i]
                    },
                    { "title", Path.GetFileNameWithoutExtension(document.Title) },
                    {
<<<<<<< HEAD
                        this.cosmosDBSearchConfigOptions.Value.EmbeddingKey,
=======
                        this.cosmosDBSearchConfigOptions.EmbeddingKey,
>>>>>>> 94c2ade (resolving comments)
                        new BsonArray(
                            document
                                .Embeddings.Response.Data[i]
                                .Embedding.ToArray()
                                .Select(e => new BsonDouble(Convert.ToDouble(e)))
                        )
                    },
                    { "timestamp", DateTime.UtcNow }
                };
            list.Add(vectorDocument);
        }

        try
        {
            // Insert the documents into the collection
<<<<<<< HEAD
<<<<<<< HEAD
            await cosmosClient
                .GetDatabase(this.databaseName)
=======
            await this
                .mongoClient.GetDatabase(this.databaseName)
>>>>>>> 94c2ade (resolving comments)
=======
            await cosmosClient
                .GetDatabase(this.databaseName)
>>>>>>> 3eafd96 (Formatting)
                .GetCollection<BsonDocument>(this.collectionName)
                .InsertManyAsync(list);

            this.logger.LogInformation(
                """
                Indexed {Count} sections
                """,
                list.Count
            );
        }
        catch (MongoException ex)
        {
            this.logger.LogError(ex, "CosmosDBSearchProvider:UpsertVectorAsync error");
            throw;
        }
<<<<<<< HEAD
=======
        BsonDocument[] pipeline = new BsonDocument[]
        {
            new()
            {
                {
                    "$search",
                    new BsonDocument
                    {
                        {
                            "cosmosSearch",
                            new BsonDocument
                            {
                                {
                                    "vector",
                                    !(request.Embeddings.IsEmpty)
                                        ? new BsonArray(request.Embeddings.ToArray())
                                        : new BsonArray()
                                },
                                { "path", this.cosmosDBSearchConfigOptions.Value.EmbeddingKey },
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
                    "$project",
                    new BsonDocument
                    {
                        { "embedding", 0 },
                        { "_id", 0 },
                        { "id", 0 },
                        { "timestamp", 0 }
                    }
                }
            }
        };
        return pipeline;
>>>>>>> 539fb8c (Fixing test cases)
=======
>>>>>>> 94c2ade (resolving comments)
    }

    BsonDocument[] GetVectorIVFSearchPipeline(SearchRequest request)
    {
        BsonDocument[] pipeline = new BsonDocument[]
        {
            new()
            {
                {
                    "$search",
                    new BsonDocument
                    {
                        {
                            "cosmosSearch",
                            new BsonDocument
                            {
                                {
                                    "vector",
                                    !(request.Embeddings.IsEmpty)
                                        ? new BsonArray(request.Embeddings.ToArray())
                                        : new BsonArray()
                                },
                                { "path", this.cosmosDBSearchConfigOptions.Value.EmbeddingKey },
<<<<<<< HEAD
                                { "k", request.MaxResults }
=======
                                { "k", request.MaxResults },
                                { "efSearch", this.cosmosDBSearchConfigOptions.Value.EfSearch }
>>>>>>> 539fb8c (Fixing test cases)
                            }
                        },
                        { "returnStoredSource", true }
                    }
                }
            },
            new()
            {
                {
                    "$project",
                    new BsonDocument
                    {
                        { "embedding", 0 },
                        { "_id", 0 },
                        { "id", 0 },
                        { "timestamp", 0 }
                    }
                }
            }
        };
        return pipeline;
<<<<<<< HEAD
    }

    BsonDocument[] GetVectorHNSWSearchPipeline(SearchRequest request)
    {
        BsonDocument[] pipeline = new BsonDocument[]
        {
            new()
            {
                {
                    "$search",
                    new BsonDocument
                    {
                        {
                            "cosmosSearch",
                            new BsonDocument
                            {
                                {
                                    "vector",
                                    !(request.Embeddings.IsEmpty)
                                        ? new BsonArray(request.Embeddings.ToArray())
                                        : new BsonArray()
                                },
                                { "path", this.cosmosDBSearchConfigOptions.Value.EmbeddingKey },
                                { "k", request.MaxResults },
                                { "efSearch", this.cosmosDBSearchConfigOptions.Value.EfSearch }
                            }
                        },
                        { "returnStoredSource", true }
                    }
                }
            },
            new()
            {
                {
                    "$project",
                    new BsonDocument
                    {
                        { "embedding", 0 },
                        { "_id", 0 },
                        { "id", 0 },
                        { "timestamp", 0 }
                    }
                }
            }
        };
        return pipeline;
=======
>>>>>>> 539fb8c (Fixing test cases)
    }

    BsonDocument GetIndexDefinitionVectorIVF()
    {
        return new BsonDocument
        {
            { "createIndexes", this.collectionName },
            {
                "indexes",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "name", this.indexName },
                        {
                            "key",
                            new BsonDocument
                            {
                                {
                                    this.cosmosDBSearchConfigOptions.Value.EmbeddingKey,
                                    "cosmosSearch"
                                }
                            }
                        },
                        {
                            "cosmosSearchOptions",
                            new BsonDocument
                            {
                                { "kind", this.cosmosDBSearchConfigOptions.Value.Kind },
                                { "numLists", this.cosmosDBSearchConfigOptions.Value.NumLists },
                                { "similarity", this.cosmosDBSearchConfigOptions.Value.Similarity },
                                {
                                    "dimensions",
                                    this.cosmosDBSearchConfigOptions.Value.VectorSearchDimensions
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    BsonDocument GetIndexDefinitionVectorHNSW()
    {
        return new BsonDocument
        {
            { "createIndexes", this.collectionName },
            {
                "indexes",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "name", this.indexName },
                        {
                            "key",
                            new BsonDocument
                            {
                                {
                                    this.cosmosDBSearchConfigOptions.Value.EmbeddingKey,
                                    "cosmosSearch"
                                }
                            }
                        },
                        {
                            "cosmosSearchOptions",
                            new BsonDocument
                            {
                                { "kind", this.cosmosDBSearchConfigOptions.Value.Kind },
                                { "m", this.cosmosDBSearchConfigOptions.Value.NumberOfConnections },
                                {
                                    "efConstruction",
                                    this.cosmosDBSearchConfigOptions.Value.EfConstruction
                                },
                                { "similarity", this.cosmosDBSearchConfigOptions.Value.Similarity },
                                {
                                    "dimensions",
                                    this.cosmosDBSearchConfigOptions.Value.VectorSearchDimensions
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
