// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Logging;

namespace WebJobs.Extensions.OpenAI.AISearch;
sealed class AISearchProvider : ISearchProvider
{
    readonly INameResolver nameResolver;
    readonly ILogger logger;
    const string defaultSearchIndexName = "openai-index";

    /// <summary>
    /// Initializes AI Search provider.
    /// </summary>
    /// <param name="nameResolver">The name resolver.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <exception cref="ArgumentNullException">Throws ArgumentNullException if logger factory is null.</exception>
    public AISearchProvider(INameResolver nameResolver, ILoggerFactory loggerFactory)
    {
        // TODO: Use IConfiguration instead of INameResolver to get the connection string.
        //       Otherwise, we might have problems if users inject their own configuration source.
        this.nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));

        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        this.logger = loggerFactory.CreateLogger<AISearchProvider>();
    }

    /// <summary>
    /// Add a document to the search index.
    /// </summary>
    /// <param name="document">The searchable document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns a task that completes when the document is successfully saved.</returns>
    public async Task AddDocumentAsync(SearchableDocument document, CancellationToken cancellationToken)
    {
        string endpoint = this.nameResolver.Resolve(document.ConnectionInfo!.ConnectionName);
        string key = this.nameResolver.Resolve(document.ConnectionInfo!.ApiKey);

        SearchIndexClient searchIndexClient = this.GetSearchIndexClient(endpoint, key);
        SearchClient searchClient = this.GetSearchClient(endpoint, key, document.ConnectionInfo!.CollectionName ?? defaultSearchIndexName);

        await this.CreateIndexIfDoesntExist(searchIndexClient, document.ConnectionInfo!.CollectionName ?? defaultSearchIndexName, cancellationToken);

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

        SearchOptions searchOptions = new()
        {
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new()
            {
                SemanticConfigurationName = "default",
                QueryCaption = new(QueryCaptionType.Extractive),
            },
            Size = request.MaxResults,
        };

        // Use vector search if embeddings are provided.
        if (!request.Embeddings.IsEmpty)
        {
            var vectorQuery = new VectorizedQuery(request.Embeddings)
            {
                KNearestNeighborsCount = request?.MaxResults ?? 3,
            };
            vectorQuery.Fields.Add("embeddings");
            searchOptions.VectorSearch = new();
            searchOptions.VectorSearch.Queries.Add(vectorQuery);
        }

        string endpoint = this.nameResolver.Resolve(request.ConnectionInfo!.ConnectionName);
        string key = this.nameResolver.Resolve(request.ConnectionInfo!.ApiKey);
        SearchClient searchClient = this.GetSearchClient(endpoint, key, request.ConnectionInfo!.CollectionName ?? defaultSearchIndexName);
        
        Response<SearchResults<SearchDocument>> searchResultResponse = await searchClient.SearchAsync<SearchDocument>(
            request.Query, searchOptions);
        if (searchResultResponse.Value is null)
        {
            throw new InvalidOperationException("Failed to get search result from AI Search.");
        }

        SearchResults<SearchDocument> searchResult = searchResultResponse.Value;

        List<SearchResult> results = new(capacity: request.MaxResults);
        foreach (SearchResult<SearchDocument> doc in searchResult.GetResults())
        {
            doc.Document.TryGetValue("title", out object? titleValue);
            string? contentValue;
            try
            {
                IEnumerable<string> docs = doc.SemanticSearch.Captions.Select(c => c.Text);
                contentValue = string.Join(" . ", docs);

                // Use below if dont want to use semantic captions
                //doc.Document.TryGetValue("content", out object? text);
                //contentValue = (string)text;

            }
            catch (ArgumentNullException)
            {
                contentValue = null;
            }

            if (titleValue is string title && contentValue is string text)
            {
                text = text.Replace('\r', ' ').Replace('\n', ' ');
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
            if (page.Values.Any(indexName => indexName == searchIndexName))
            {
                this.logger.LogDebug("Search index - {searchIndexName} already exists", searchIndexName);
                return;
            }
        }

        string vectorSearchConfigName = "my-vector-config";
        string vectorSearchProfile = "my-vector-profile";
        var index = new SearchIndex(searchIndexName)
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
                    VectorSearchDimensions = 1536,
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
                        }
                    })
                }
            }
        };

        await searchIndexClient.CreateIndexAsync(index, cancellationToken);
    }

    async Task IndexSectionsAsync(SearchClient searchClient, SearchableDocument document, CancellationToken cancellationToken = default)
    {
        var iteration = 0;
        var batch = new IndexDocumentsBatch<SearchDocument>();
        for (int i = 0; i < document.Embeddings.Response.Value.Data.Count; i++)
        {
            batch.Actions.Add(new IndexDocumentsAction<SearchDocument>(
                IndexActionType.MergeOrUpload,
                new SearchDocument
                {
                    ["id"] = Guid.NewGuid().ToString("N"),
                    ["text"] = document.Embeddings.Request.Input![i],
                    ["title"] = Path.GetFileNameWithoutExtension(document.Title),
                    ["embeddings"] = document.Embeddings.Response.Value.Data[i].Embedding.ToArray() ?? Array.Empty<float>(),
                    ["timestamp"] = DateTime.UtcNow
                }));
            iteration++;
            if (iteration % 1_000 is 0)
            {
                // Every one thousand documents, batch create.
                IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
                int succeeded = result.Results.Count(r => r.Succeeded);
                this.logger.LogDebug("""
                        Indexed {Count} sections, {Succeeded} succeeded
                        """,
                        batch.Actions.Count,
                        succeeded);

                batch = new();
            }
        }

        if (batch is { Actions.Count: > 0 })
        {
            // Any remaining documents, batch create.
            IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
            int succeeded = result.Results.Count(r => r.Succeeded);
            string message = $"Indexed {batch.Actions.Count} sections, {succeeded} succeeded";
            this.logger.LogDebug(message);
        }
    }

    SearchIndexClient GetSearchIndexClient(string endpoint, string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return new SearchIndexClient(new Uri(endpoint), new DefaultAzureCredential());
        }
        else
        {
            return new SearchIndexClient(new Uri(endpoint), new AzureKeyCredential(key));
        }
    }

    SearchClient GetSearchClient(string endpoint, string key, string searchIndexName)
    {
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
