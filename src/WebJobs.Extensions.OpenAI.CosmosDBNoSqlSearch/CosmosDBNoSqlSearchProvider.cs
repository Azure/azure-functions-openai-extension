// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBNoSqlSearch;

sealed class CosmosDBNoSqlSearchProvider : ISearchProvider
{
    readonly IConfiguration configuration;
    readonly ILogger logger;
    readonly IOptions<CosmosDBNoSqlSearchConfigOptions> cosmosDBNoSqlSearchConfigOptions;
    readonly ConcurrentDictionary<string, CosmosClient> cosmosDBClients = new();

    public string Name { get; set; } = "CosmosDBNoSqlSearch";

    /// <summary>
    /// Initializes CosmosDB search provider.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="cosmosDBNoSqlSearchConfigOptions">Cosmos DB No Sql search config options.</param>
    /// <exception cref="ArgumentNullException">Throws ArgumentNullException if logger factory is null.</exception>
    public CosmosDBNoSqlSearchProvider(
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
        this.logger = loggerFactory.CreateLogger<CosmosDBNoSqlSearchProvider>();
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
        // Create cosmos client if not exists in the cache.
        CosmosClient cosmosClient = cosmosDBClients.GetOrAdd(
            document.ConnectionInfo!.ConnectionName,
            _ => CreateCosmosClient(document.ConnectionInfo!)
        );

        // Create a database if not exists.
        DatabaseResponse databaseResponse = await cosmosClient
            .CreateDatabaseIfNotExistsAsync(
                this.cosmosDBNoSqlSearchConfigOptions.Value.DatabaseName,
                this.cosmosDBNoSqlSearchConfigOptions.Value.DatabaseThroughput,
                this.cosmosDBNoSqlSearchConfigOptions.Value.DatabaseRequestOptions,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        // Create a container if not exists.
        ContainerProperties containerProperties = this.cosmosDBNoSqlSearchConfigOptions
            .Value
            .ContainerProperties;
        containerProperties.VectorEmbeddingPolicy = this.cosmosDBNoSqlSearchConfigOptions
            .Value
            .VectorEmbeddingPolicy;
        containerProperties.IndexingPolicy = this.cosmosDBNoSqlSearchConfigOptions
            .Value
            .IndexingPolicy;
        ContainerResponse containerResponse = await databaseResponse
            .Database.CreateContainerIfNotExistsAsync(
                containerProperties,
                this.cosmosDBNoSqlSearchConfigOptions.Value.ContainerThroughput,
                this.cosmosDBNoSqlSearchConfigOptions.Value.ContainerRequestOptions,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        await this.UpsertVectorAsync(cosmosClient, document, cancellationToken);
    }

    CosmosClient CreateCosmosClient(ConnectionInfo connectionInfo)
    {
        return new CosmosClient(
            connectionInfo.ConnectionName,
            new CosmosClientOptions
            {
                ApplicationName = this.cosmosDBNoSqlSearchConfigOptions.Value.ApplicationName
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
                var result = await cosmosClient
                    .GetDatabase(this.cosmosDBNoSqlSearchConfigOptions.Value.DatabaseName)
                    .GetContainer(document.ConnectionInfo!.CollectionName)
                    .UpsertItemAsync(
                        record,
                        new PartitionKey(record.id),
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
            queryDefinition.WithParameter("@embedding", request.Embeddings);
            queryDefinition.WithParameter("@limit", request.MaxResults);
            queryDefinition.WithParameter(
                "@whereClause",
                cosmosDBNoSqlSearchConfigOptions.Value.PreFilters["where_clause"]
            );
            queryDefinition.WithParameter(
                "@limitOffsetClause",
                cosmosDBNoSqlSearchConfigOptions.Value.PreFilters["limit_offset_clause"]
            );

            var feedIterator = cosmosClient
                .GetDatabase(this.cosmosDBNoSqlSearchConfigOptions.Value.DatabaseName)
                .GetContainer(request.ConnectionInfo!.CollectionName)
                .GetItemQueryIterator<MemoryRecordWithSimilarityScore>(queryDefinition);

            List<SearchResult> searchResults = new();
            while (feedIterator.HasMoreResults)
            {
                foreach (var memoryRecord in await feedIterator.ReadNextAsync())
                {
                    searchResults.Add(new SearchResult(memoryRecord.title, memoryRecord.text));
                }
            }
            SearchResponse response = new(searchResults);
            return response;
        }
        catch (CosmosException ex)
        {
            this.logger.LogError(ex, "CosmosDBnoSqlSearchProvider:SearchAsync error");
            throw;
        }
    }

    /// <summary>
    /// Creates a new record with a similarity score.
    /// </summary>
    internal class MemoryRecordWithSimilarityScore
    {
        public string id { get; set; }
        public string text { get; set; }
        public ReadOnlyMemory<float> embedding { get; set; }
        public string title { get; set; }
        public DateTimeOffset? timestamp { get; set; }

        /// <summary>
        /// The similarity score returned.
        /// </summary>
        public double SimilarityScore { get; set; }

        public MemoryRecordWithSimilarityScore(
            string id,
            string text,
            string title,
            ReadOnlyMemory<float> embedding,
            DateTimeOffset timestamp,
            double SimilarityScore
        )
        {
            this.id = id;
            this.text = text;
            this.title = title;
            this.embedding = embedding;
            this.timestamp = timestamp;
            this.SimilarityScore = SimilarityScore;
        }
    }

    /// <summary>
    /// Creates a new record that also serializes an "id" property.
    /// </summary>
    internal class MemoryRecordWithId
    {
        public string id { get; set; }
        public string text { get; set; }
        public ReadOnlyMemory<float> embedding { get; set; }
        public string title { get; set; }
        public DateTimeOffset? timestamp { get; set; }

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
