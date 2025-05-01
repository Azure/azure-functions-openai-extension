// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
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
    public string CosmosDBConnectionSetting = "CosmosDBNoSqlConnectionString";
    const string endpointSettingSuffix = "Endpoint";

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
        this.CosmosDBConnectionSetting =
            document.ConnectionInfo?.ConnectionName ?? this.CosmosDBConnectionSetting;

        // Retrieve an existing Cosmos client or create a new one if it's not already in the cache.
        CosmosClient cosmosClient = this.GetCosmosClient();

        DatabaseResponse databaseResponse = await cosmosClient
            .CreateDatabaseIfNotExistsAsync(
                this.cosmosDBNoSqlSearchConfigOptions.Value.DatabaseName,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        VectorEmbeddingPolicy vectorEmbeddingPolicy = new(
            new Collection<Embedding>
            {
                new()
                {
                    DataType = this.cosmosDBNoSqlSearchConfigOptions.Value.VectorDataType,
                    Dimensions = this.cosmosDBNoSqlSearchConfigOptions.Value.VectorDimensions,
                    DistanceFunction = this.cosmosDBNoSqlSearchConfigOptions
                        .Value
                        .VectorDistanceFunction,
                    Path = this.cosmosDBNoSqlSearchConfigOptions.Value.EmbeddingKey,
                }
            }
        );

        IndexingPolicy indexingPolicy = new()
        {
            VectorIndexes = new Collection<VectorIndexPath>
            {
                new()
                {
                    Path = this.cosmosDBNoSqlSearchConfigOptions.Value.EmbeddingKey,
                    Type = this.cosmosDBNoSqlSearchConfigOptions.Value.VectorIndexType,
                },
            },
        };

        // Create a container if not exists.
        ContainerProperties containerProperties = new(
            document.ConnectionInfo!.CollectionName,
            "/id"
        )
        {
            VectorEmbeddingPolicy = vectorEmbeddingPolicy,
            IndexingPolicy = indexingPolicy,
        };

        await databaseResponse
            .Database.CreateContainerIfNotExistsAsync(
                containerProperties,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        await this.UpsertVectorAsync(cosmosClient, document, cancellationToken);
    }

    CosmosClient GetCosmosClient()
    {
        CosmosClient cosmosClient = this.cosmosDBClients.GetOrAdd(
            this.CosmosDBConnectionSetting,
            _ => this.CreateCosmosClient()
        );
        return cosmosClient;
    }

    CosmosClient CreateCosmosClient()
    {
        IConfigurationSection cosmosConfigSection = this.configuration.GetSection(
            this.CosmosDBConnectionSetting
        );
        if (cosmosConfigSection.Exists())
        {
            string cosmosAccountUri = cosmosConfigSection[endpointSettingSuffix];

            if (!string.IsNullOrEmpty(cosmosAccountUri))
            {
                this.logger.LogInformation(
                    "Using Managed Identity for Cosmos DB No SQL Connection."
                );
                TokenCredential credential = new DefaultAzureCredential();

                var serializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                    IncludeFields = true
                };
                // custom converter here
                serializerOptions.Converters.Add(new ReadOnlyMemoryFloatConverter());
                return new CosmosClient(
                    cosmosAccountUri,
                    credential,
                    new CosmosClientOptions
                    {
                        ApplicationName = this.cosmosDBNoSqlSearchConfigOptions
                            .Value
                            .ApplicationName,
                        Serializer = new CosmosSystemTextJsonSerializer(serializerOptions)
                    }
                );
            }
        }

        string connectionString = this.configuration.GetValue<string>(
            this.CosmosDBConnectionSetting
        );
        if (!string.IsNullOrEmpty(connectionString))
        {
            this.logger.LogInformation("Using Connection String for Cosmos DB No SQL connection.");

            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                IncludeFields = true
            };
            // custom converter here
            serializerOptions.Converters.Add(new ReadOnlyMemoryFloatConverter());
            return new CosmosClient(
                connectionString,
                new CosmosClientOptions
                {
                    ApplicationName = this.cosmosDBNoSqlSearchConfigOptions.Value.ApplicationName,
                    Serializer = new CosmosSystemTextJsonSerializer(serializerOptions)
                }
            );
        }

        // Throw exception if no valid configuration is found
        string errorMessage =
            $"Configuration section or endpoint '{this.CosmosDBConnectionSetting}' does not exist.";
        this.logger.LogError(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }

    async Task UpsertVectorAsync(
        CosmosClient cosmosClient,
        SearchableDocument document,
        CancellationToken cancellationToken
    )
    {
        try
        {
            for (int i = 0; i < document.Embeddings?.Response?.Count; i++)
            {
                MemoryRecordWithId record = new(
                    Guid.NewGuid().ToString("N"),
                    document.Embeddings?.Request![i] ?? string.Empty,
                    Path.GetFileNameWithoutExtension(document.Title),
                    document.Embeddings?.Response[i].ToFloats().ToArray() ?? Array.Empty<float>(),
                    DateTime.UtcNow
                );

                await cosmosClient
                    .GetDatabase(this.cosmosDBNoSqlSearchConfigOptions.Value.DatabaseName)
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

        this.CosmosDBConnectionSetting =
            request.ConnectionInfo.ConnectionName ?? this.CosmosDBConnectionSetting;

        // Create cosmos client if not exists in the cache.
        CosmosClient cosmosClient = this.GetCosmosClient();

        try
        {
            string vectorPropertyName = this.cosmosDBNoSqlSearchConfigOptions.Value.EmbeddingKey.TrimStart('/');
            // If limit_offset_clause is not specified, add TOP clause
            string query = "SELECT ";
            if (
                string.IsNullOrWhiteSpace(
                    this.cosmosDBNoSqlSearchConfigOptions.Value.LimitOffsetFilterClause
                )
            )
            {
                query += "TOP @limit ";
            }
            query +=
                $"c.id,c.text,c.title,c.timestamp,VectorDistance(c.{vectorPropertyName}, @embedding) AS SimilarityScore FROM c";

            //# Add where_clause if specified
            if (
                !string.IsNullOrWhiteSpace(
                    this.cosmosDBNoSqlSearchConfigOptions.Value.WhereFilterClause
                )
            )
            {
                query += $" {this.cosmosDBNoSqlSearchConfigOptions.Value.WhereFilterClause}";
            }
            query += $" ORDER BY VectorDistance(c.{vectorPropertyName}, @embedding)";

            // Add limit_offset_clause if specified
            if (
                !string.IsNullOrWhiteSpace(
                    this.cosmosDBNoSqlSearchConfigOptions.Value.LimitOffsetFilterClause
                )
            )
            {
                query += $" {this.cosmosDBNoSqlSearchConfigOptions.Value.LimitOffsetFilterClause}";
            }

            var queryDefinition = new QueryDefinition(query);
            queryDefinition.WithParameter("@limit", request.MaxResults);
            queryDefinition.WithParameter("@embedding", request.Embeddings);
            FeedIterator<MemoryRecordWithSimilarityScore> feedIterator = cosmosClient
                .GetDatabase(this.cosmosDBNoSqlSearchConfigOptions.Value.DatabaseName)
                .GetContainer(request.ConnectionInfo!.CollectionName)
                .GetItemQueryIterator<MemoryRecordWithSimilarityScore>(
                    queryDefinition,
                    requestOptions: new QueryRequestOptions { PopulateIndexMetrics = true, }
                );

            List<SearchResult> searchResults = new();
            while (feedIterator.HasMoreResults)
            {
                foreach (
                    MemoryRecordWithSimilarityScore memoryRecord in await feedIterator.ReadNextAsync()
                )
                {
                    searchResults.Add(new SearchResult(memoryRecord.Title, memoryRecord.Text));
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
        public string Id { get; set; }
        public string Text { get; set; }
        public string Title { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// The similarity score returned.
        /// </summary>
        public double SimilarityScore { get; set; }

        public MemoryRecordWithSimilarityScore(
            string id,
            string text,
            string title,
            ReadOnlyMemory<float> embedding,
            DateTimeOffset? timestamp,
            double SimilarityScore
        )
        {
            this.Id = id;
            this.Text = text;
            this.Title = title;
            this.Embedding = embedding;
            this.Timestamp = timestamp;
            this.SimilarityScore = SimilarityScore;
        }
    }

    /// <summary>
    /// Creates a new record that also serializes an "id" property.
    /// </summary>
    internal class MemoryRecordWithId
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Title { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Creates a new record that also serializes an "id" property.
        /// </summary>
        public MemoryRecordWithId(
            string id,
            string text,
            string title,
            ReadOnlyMemory<float> embedding,
            DateTimeOffset? timestamp
        )
        {
            this.Id = id;
            this.Text = text;
            this.Title = title;
            this.Embedding = embedding;
            this.Timestamp = timestamp;
        }
    }

    class ReadOnlyMemoryFloatConverter : JsonConverter<ReadOnlyMemory<float>>
    {
        public override ReadOnlyMemory<float> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            float[]? floatArray = JsonSerializer.Deserialize<float[]>(ref reader, options);
            return floatArray;
        }

        public override void Write(
            Utf8JsonWriter writer,
            ReadOnlyMemory<float> value,
            JsonSerializerOptions options
        )
        {
            JsonSerializer.Serialize(writer, value.ToArray(), options);
        }
    }
}
