﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.AzureAISearch;

sealed class AzureAISearchProvider : ISearchProvider
{
    readonly IConfiguration configuration;
    readonly ILogger logger;
    readonly bool isSemanticSearchEnabled = false;
    readonly bool useSemanticCaptions = false;
    readonly int vectorSearchDimensions = 1536;
    readonly string searchAPIKeySetting = "SearchAPIKey";
    const string defaultSearchIndexName = "openai-index";
    const string vectorSearchConfigName = "openai-vector-config";
    const string vectorSearchProfile = "openai-vector-profile";

    public string Name { get; set; } = "AzureAISearch";

    /// <summary>
    /// Initializes AI Search provider.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <exception cref="ArgumentNullException">Throws ArgumentNullException if logger factory is null.</exception>
    public AzureAISearchProvider(IConfiguration configuration, ILoggerFactory loggerFactory, IOptions<AzureAISearchConfigOptions> azureAiSearchConfigOptions)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        this.isSemanticSearchEnabled = azureAiSearchConfigOptions.Value.IsSemanticSearchEnabled;
        this.useSemanticCaptions = azureAiSearchConfigOptions.Value.UseSemanticCaptions;
        this.searchAPIKeySetting = azureAiSearchConfigOptions.Value.SearchAPIKeySetting ?? this.searchAPIKeySetting;
        int value = azureAiSearchConfigOptions.Value.VectorSearchDimensions;
        if (value < 2 || value > 3072)
        {
            throw new ArgumentOutOfRangeException(nameof(AzureAISearchConfigOptions.VectorSearchDimensions), value, "Vector search dimensions must be between 2 and 3072");
        }

        this.vectorSearchDimensions = value;

        this.logger = loggerFactory.CreateLogger<AzureAISearchProvider>();
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
        string endpoint = this.configuration.GetValue<string>(document.ConnectionInfo.ConnectionName);

        SearchIndexClient searchIndexClient = this.GetSearchIndexClient(endpoint);
        SearchClient searchClient = this.GetSearchClient(endpoint, document.ConnectionInfo.CollectionName ?? defaultSearchIndexName);

        await this.CreateIndexIfDoesntExist(searchIndexClient, document.ConnectionInfo.CollectionName ?? defaultSearchIndexName, cancellationToken);

        await this.IndexSectionsAsync(searchClient, document, cancellationToken);
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
        if (request.Query is null && request.Embeddings.IsEmpty)
        {
            throw new ArgumentException("Either query or embeddings must be provided");
        }

        if (request.ConnectionInfo is null)
        {
            throw new ArgumentNullException(nameof(request.ConnectionInfo));
        }

        string endpoint = this.configuration.GetValue<string>(request.ConnectionInfo.ConnectionName);
        SearchClient searchClient = this.GetSearchClient(endpoint, request.ConnectionInfo.CollectionName ?? defaultSearchIndexName);

        SearchOptions searchOptions = this.isSemanticSearchEnabled
            ? new SearchOptions
            {
                QueryType = SearchQueryType.Semantic,
                SemanticSearch = new()
                {
                    SemanticConfigurationName = "default",
                    QueryCaption = new(this.useSemanticCaptions
                        ? QueryCaptionType.Extractive
                        : QueryCaptionType.None),
                },
                Size = request.MaxResults,
            }
            : new SearchOptions
            {
                Size = request.MaxResults,
            };

        // Use vector search if embeddings are provided.
        if (!request.Embeddings.IsEmpty)
        {
            VectorizedQuery vectorQuery = new(request.Embeddings)
            {
                // Use a higher K value for semantic search to get better results.
                KNearestNeighborsCount = this.isSemanticSearchEnabled ? Math.Max(50, request.MaxResults) : request.MaxResults,
            };
            vectorQuery.Fields.Add("embeddings");
            searchOptions.VectorSearch = new();
            searchOptions.VectorSearch.Queries.Add(vectorQuery);
        }

        Response<SearchResults<SearchDocument>> searchResultResponse = await searchClient.SearchAsync<SearchDocument>(
            request.Query, searchOptions);
        if (searchResultResponse.Value is null)
        {
            throw new InvalidOperationException($"Failed to get search result from Azure AI Search instance: {searchClient.ServiceName} and index: {searchClient.IndexName}");
        }

        SearchResults<SearchDocument> searchResult = searchResultResponse.Value;

        List<SearchResult> results = new(capacity: request.MaxResults);
        foreach (SearchResult<SearchDocument> doc in searchResult.GetResults())
        {
            doc.Document.TryGetValue("title", out object? titleValue);
            string? contentValue;

            if (this.useSemanticCaptions)
            {
                IEnumerable<string> docs = doc.SemanticSearch.Captions.Select(c => c.Text);
                contentValue = string.Join(" . ", docs);
            }
            else
            {
                doc.Document.TryGetValue("text", out object? content);
                contentValue = (string)content;
            }

            if (titleValue is string title && contentValue is string text)
            {
                results.Add(new SearchResult(title, text));
            }
        }

        SearchResponse response = new(results);
        return response;
    }

    async Task CreateIndexIfDoesntExist(SearchIndexClient searchIndexClient, string searchIndexName, CancellationToken cancellationToken = default)
    {
        AsyncPageable<string> indexNames = searchIndexClient.GetIndexNamesAsync();
        await foreach (Page<string> page in indexNames.AsPages())
        {
            if (page.Values.Any(indexName => string.Equals(indexName, searchIndexName, StringComparison.OrdinalIgnoreCase)))
            {
                this.logger.LogDebug("Search index - {searchIndexName} already exists", searchIndexName);
                return;
            }
        }

        SearchIndex index = new(searchIndexName)
        {
            VectorSearch = new()
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(vectorSearchConfigName)
                },
                Profiles =
                {
                    new VectorSearchProfile(vectorSearchProfile, vectorSearchConfigName)
                }
            },
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("text") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
                new SimpleField("title", SearchFieldDataType.String) { IsFacetable = true },
                new SearchField("embeddings", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    VectorSearchDimensions = this.vectorSearchDimensions,
                    IsSearchable = true,
                    VectorSearchProfileName = vectorSearchProfile,
                },
                new SimpleField("timestamp", SearchFieldDataType.DateTimeOffset) { IsFacetable = true }
            },
            SemanticSearch = new()
            {
                Configurations =
                {
                    new SemanticConfiguration("default", new()
                    {
                        ContentFields =
                        {
                            new SemanticField("text")
                        },
                        TitleField = new SemanticField("title")
                    })
                }
            }
        };

        await searchIndexClient.CreateIndexAsync(index, cancellationToken);
    }

    async Task IndexSectionsAsync(SearchClient searchClient, SearchableDocument document, CancellationToken cancellationToken = default)
    {
        int iteration = 0;
        IndexDocumentsBatch<SearchDocument> batch = new();
        for (int i = 0; i < document.Embeddings?.Response?.Data.Count; i++)
        {
            batch.Actions.Add(new IndexDocumentsAction<SearchDocument>(
                IndexActionType.MergeOrUpload,
                new SearchDocument
                {
                    ["id"] = Guid.NewGuid().ToString("N"),
                    ["text"] = document.Embeddings.Request.Input![i],
                    ["title"] = Path.GetFileNameWithoutExtension(document.Title),
                    ["embeddings"] = document.Embeddings.Response.Data[i].Embedding.ToArray() ?? Array.Empty<float>(),
                    ["timestamp"] = DateTime.UtcNow
                }));
            iteration++;
            if (iteration % 1_000 is 0)
            {
                // Every one thousand documents, batch create.
                await this.IndexDocumentsBatchAsync(searchClient, batch, cancellationToken);

                batch = new();
            }
        }

        if (batch is { Actions.Count: > 0 })
        {
            // Any remaining documents, batch create.
            await this.IndexDocumentsBatchAsync(searchClient, batch, cancellationToken);
        }
    }

    async Task IndexDocumentsBatchAsync(SearchClient searchClient, IndexDocumentsBatch<SearchDocument> batch, CancellationToken cancellationToken)
    {
        IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
        int succeeded = result.Results.Count(r => r.Succeeded);
        this.logger.LogInformation("""
                        Indexed {Count} sections, {Succeeded} succeeded
                        """,
                batch.Actions.Count,
                succeeded);
    }

    SearchIndexClient GetSearchIndexClient(string endpoint)
    {
        string? key = this.configuration.GetValue<string>(this.searchAPIKeySetting);
        if (string.IsNullOrEmpty(key))
        {
            return new SearchIndexClient(new Uri(endpoint), new DefaultAzureCredential());
        }
        else
        {
            return new SearchIndexClient(new Uri(endpoint), new AzureKeyCredential(key));
        }
    }

    SearchClient GetSearchClient(string endpoint, string searchIndexName)
    {
        string? key = this.configuration.GetValue<string>(this.searchAPIKeySetting);
        SearchClient searchClient;
        if (string.IsNullOrEmpty(key))
        {
            searchClient = new SearchClient(new Uri(endpoint), searchIndexName, new DefaultAzureCredential());
        }
        else
        {
            searchClient = new SearchClient(new Uri(endpoint), searchIndexName, new AzureKeyCredential(key));
        }

        return searchClient;
    }
}
