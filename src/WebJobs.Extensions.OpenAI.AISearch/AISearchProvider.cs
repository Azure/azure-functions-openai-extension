// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Azure;
using Azure.AI.OpenAI;
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
    // We create a separate client object for each connection string we see.
    readonly ConcurrentDictionary<string, SearchIndexClient> searchIndexClients = new();
    readonly ConcurrentDictionary<string, SearchClient> searchClients = new();
    readonly INameResolver nameResolver;
    readonly ILogger logger;
    const string defaultSearchIndexName = "openai-index";

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

    public async Task AddDocumentAsync(SearchableDocument document, CancellationToken cancellationToken)
    {
        string endpoint = this.nameResolver.Resolve(document.ConnectionInfo!.ConnectionName);
        string key = this.nameResolver.Resolve(document.ConnectionInfo!.ApiKey);

        SearchIndexClient searchIndexClient;
        SearchClient searchClient;
        if (string.IsNullOrEmpty(key))
        {
            searchIndexClient = new SearchIndexClient(new Uri(endpoint), new DefaultAzureCredential());
            searchClient = new SearchClient(new Uri(endpoint), document.ConnectionInfo!.CollectionName ?? defaultSearchIndexName, new DefaultAzureCredential());
        }
        else
        {
            searchIndexClient = new SearchIndexClient(new Uri(endpoint), new AzureKeyCredential(key));
            searchClient = new SearchClient(new Uri(endpoint), document.ConnectionInfo!.CollectionName ?? defaultSearchIndexName, new AzureKeyCredential(key));
        }

        this.searchIndexClients.GetOrAdd(document.ConnectionInfo!.ConnectionName, searchIndexClient);
        this.searchClients.GetOrAdd(document.ConnectionInfo!.ConnectionName, searchClient);

        await this.CreateIndexIfDoesntExist(searchIndexClient, document.ConnectionInfo!.CollectionName ?? defaultSearchIndexName);

        await this.IndexSectionsAsync(searchClient, document);
    }

    public Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        throw new NotImplementedException();
    }

    async Task CreateIndexIfDoesntExist(SearchIndexClient searchIndexClient, string searchIndexName)
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

        await searchIndexClient.CreateIndexAsync(index);
    }

    async Task IndexSectionsAsync(SearchClient searchClient, SearchableDocument document)
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
                IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch);
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
            IndexDocumentsResult result = await searchClient.IndexDocumentsAsync(batch);
            int succeeded = result.Results.Count(r => r.Succeeded);
            string message = $"Indexed {batch.Actions.Count} sections, {succeeded} succeeded";
            this.logger.LogDebug(message);
        }
    }

}
