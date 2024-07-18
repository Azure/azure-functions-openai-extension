// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBNoSqlSearch;

sealed class CosmosDBSearchProvider : ISearchProvider
{
    readonly IConfiguration configuration;
    readonly ILogger logger;
    readonly IOptions<CosmosDBNoSqlSearchConfigOptions> cosmosDBNoSqlSearchConfigOptions;
    readonly ConcurrentDictionary<string, CosmosClient> cosmosDBClients = new();
    string databaseName = "openai-functions-database";
    string collectionName = "openai-functions-collection";

    public string Name { get; set; } = "CosmosDBNoSqlSearch";

    /// <summary>
    /// Initializes CosmosDB search provider.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="cosmosDBNoSqlSearchConfigOptions">Cosmos DB No Sql search config options.</param>
    /// <exception cref="ArgumentNullException">Throws ArgumentNullException if logger factory is null.</exception>
    public CosmosDBSearchProvider(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        IOptions<CosmosDBNoSqlSearchConfigOptions> cosmosDBNoSqlSearchConfigOptions
    )
    {
        this.configuration =
            configuration ?? throw new ArgumentNullException(nameof(configuration));
        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }
        this.cosmosDBNoSqlSearchConfigOptions = cosmosDBNoSqlSearchConfigOptions;
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
        CosmosClient cosmosClient = cosmosDBClients.GetOrAdd(
            document.ConnectionInfo!.ConnectionName,
            _ => CreateCosmosClient(document.ConnectionInfo!)
        );

        var databaseProperties = this.cosmosDBNoSqlSearchConfigOptions.Value.DatabaseProperties;
        databaseProperties.DatabaseName = document.ConnectionInfo!.DatabaseName;
        var databaseResponse = await this
            .cosmosClient.CreateDatabaseIfNotExistsAsync(
                databaseProperties,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var containerProperties = this.cosmosDBNoSqlSearchConfigOptions.Value.ContainerProperties;
        containerProperties.CollectionName = document.ConnectionInfo!.CollectionName;
        containerProperties.VectorEmbeddingPolicy = this.cosmosDBNoSqlSearchConfigOptions
            .Value
            .VectorEmbeddingPolicy;
        containerProperties.IndexingPolicy = this.cosmosDBNoSqlSearchConfigOptions
            .Value
            .IndexingPolicy;
        var containerResponse = await databaseResponse
            .Database.CreateContainerIfNotExistsAsync(
                containerProperties,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        await this.UpsertVectorAsync(cosmosClient, document, cancellationToken);
    }

    CosmosClient CreateCosmosClient(ConnectionInfo connectionInfo)
    {
        return CosmosClient(
            connectionInfo.ConnectionName,
            new CosmosClientOptions
            {
                ApplicationName = this.cosmosDBNoSqlSearchConfigOptions.ApplicationName,
                Serializer = new CosmosSystemTextJsonSerializer(JsonSerializerOptions.Default),
            }
        );
    }

    async Task UpsertVectorAsync(
        CosmosClient cosmosClient,
        SearchableDocument document,
        CancellationToken cancellationToken
    )
    {
        try
        {
            for (int i = 0; i < document.Embeddings?.Response?.Data.Count; i++)
            {
                MemoryRecordWithId record = new MemoryRecordWithId(document, i);
                var result = await this
                    .cosmosClient.GetDatabase(document.ConnectionInfo!.DatabaseName)
                    .GetContainer(document.ConnectionInfo!.CollectionName)
                    .UpsertItemAsync(
                        record,
                        new PartitionKey(record.Id),
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
            }
        }
        catch (CosmosException ex)
        {
            this.logger.LogError(ex, "CosmosDBNoSqlSearchProvider: UpsertVectorAsync error");
            throw;
        }
    }

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

        CosmosClient cosmosClient = cosmosDBClients.GetOrAdd(
            request.ConnectionInfo!.ConnectionName,
            _ => CreateCosmosClient(request.ConnectionInfo!)
        );

        try
        {
            string query = "SELECT ";

            // If limit_offset_clause is not specified, add TOP clause
            if (
                cosmosDBNoSqlSearchConfigOptions.Value.PreFilters == null
                || cosmosDBNoSqlSearchConfigOptions.Value.PreFilters["limit_offset_clause"] == null
            )
            {
                query += "TOP @limit";
            }
            query +=
                "x.id,x.text,x.title,x.timestamp,VectorDistance(@embeddingKey, @embedding) AS SimilarityScore FROM x";

            //# Add where_clause if specified
            if (
                cosmosDBNoSqlSearchConfigOptions.Value.PreFilters != null
                || cosmosDBNoSqlSearchConfigOptions.Value.PreFilters["where_clause"] != null
            )
            {
                query += " @whereClause";
            }
            query += "ORDER BY VectorDistance(@embeddingKey, @embedding)";

            // Add limit_offset_clause if specified
            if (
                cosmosDBNoSqlSearchConfigOptions.Value.PreFilters != null
                || cosmosDBNoSqlSearchConfigOptions.Value.PreFilters["limit_offset_clause"] != null
            )
            {
                query += " @limitOffsetClause";
            }

            var queryDefinition = new QueryDefinition(query);
            queryDefinition.WithParameter(
                "@embeddingKey",
                cosmosDBNoSqlSearchConfigOptions.Value.EmbeddingKey
            );
            queryDefinition.WithParameter("@embedding", embedding);
            queryDefinition.WithParameter("@limit", limit);
            queryDefinition.WithParameter(
                "@whereClause",
                cosmosDBNoSqlSearchConfigOptions.Value.PreFilters["where_clause"]
            );
            queryDefinition.WithParameter(
                "@limitOffsetClause",
                cosmosDBNoSqlSearchConfigOptions.Value.PreFilters["limit_offset_clause"]
            );

            var feedIterator = this
                ._cosmosClient.GetDatabase(this._databaseName)
                .GetContainer(collectionName)
                .GetItemQueryIterator<MemoryRecordWithSimilarityScore>(queryDefinition);

            List<SearchResult> searchResults = new();
            while (feedIterator.HasMoreResults)
            {
                foreach (
                    var memoryRecord in await feedIterator
                        .ReadNextAsync(cancellationToken)
                        .ConfigureAwait(false)
                )
                {
                    searchResults.Add(new SearchResult(memoryRecord.Title, memoryRecord.Text));
                }
            }
            SearchResponse response = new(searchResults);
        }
        catch
        {
            this.logger.LogError(ex, "CosmosDBnoSqlSearchProvider:SearchAsync error");
            throw;
        }
    }

    /// <summary>
    /// Creates a new record with a similarity score.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="text"></param>
    /// <param name="embedding"></param>
    /// <param name="title"></param>
    /// <param name="timestamp"></param>
    internal class MemoryRecordWithSimilarityScore(
        string id,
        string text,
        ReadOnlyMemory<float> embedding,
        string title,
        DateTimeOffset? timestamp = null
    )
    {
        /// <summary>
        /// The similarity score returned.
        /// </summary>
        public double SimilarityScore { get; set; }
    }

    /// <summary>
    /// Creates a new record that also serializes an "id" property.
    /// </summary>
    internal class MemoryRecordWithId
    {
        string id;
        string text;
        ReadOnlyMemory<float> embedding;
        string title;
        DateTimeOffset? timestamp;

        /// <summary>
        /// Creates a new record that also serializes an "id" property.
        /// </summary>
        public MemoryRecordWithId(SearchableDocument document, int dataId)
        {
            this.id = Guid.NewGuid().ToString("N");
            this.text = document.Embeddings.Request.Input![dataId];
            this.title = Path.GetFileNameWithoutExtension(document.Title);
            this.embedding = document.Embeddings.Response.Data[dataId].Embedding.ToArray();
            this.timestamp = DateTime.UtcNow;
        }
    }
}
