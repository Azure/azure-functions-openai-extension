// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Azure;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAISDK = Azure.AI.OpenAI;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
class EmbeddingsStoreConverter :
    IAsyncConverter<EmbeddingsStoreAttribute, IAsyncCollector<SearchableDocument>>
{
    readonly OpenAISDK.OpenAIClient openAIClient;
    readonly ILogger logger;
    readonly ISearchProvider? searchProvider;

    // Note: we need this converter as Azure.AI.OpenAI does not support System.Text.Json serialization since their constructors are internal
    static readonly JsonSerializerOptions options = new()
    {
        Converters = { new EmbeddingsContextConverter(), new SearchableDocumentJsonConverter() }
    };

    public EmbeddingsStoreConverter(OpenAISDK.OpenAIClient openAIClient,
        ILoggerFactory loggerFactory,
        IEnumerable<ISearchProvider> searchProviders,
        IOptions<OpenAIConfigOptions> openAiConfigOptions)
    {
        this.openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
        this.logger = loggerFactory?.CreateLogger<EmbeddingsStoreConverter>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        openAiConfigOptions.Value.SearchProvider.TryGetValue("type", out object value);
        this.searchProvider = searchProviders?
            .FirstOrDefault(x => string.Equals(x.Name, value?.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    public Task<IAsyncCollector<SearchableDocument>> ConvertAsync(EmbeddingsStoreAttribute input, CancellationToken cancellationToken)
    {
        if (this.searchProvider == null)
        {
            throw new InvalidOperationException(
                "No search provider is configured. Search providers are configured in the host.json file. For .NET apps, the appropriate nuget package must also be added to the app's project file.");
        }
        IAsyncCollector<SearchableDocument> collector = new SemanticDocumentCollector(input, this.searchProvider, this.openAIClient, this.logger);
        return Task.FromResult(collector);
    }

    // Called by the host when processing binding requests from out-of-process workers.
    internal SearchableDocument ToSearchableDocument(string? json)
    {
        this.logger.LogDebug("Creating searchable document from JSON string: {Text}", json);
        SearchableDocument document = JsonSerializer.Deserialize<SearchableDocument>(json, options);
        return document ?? throw new ArgumentException("Invalid search request.");
    }

    sealed class SemanticDocumentCollector : IAsyncCollector<SearchableDocument>
    {
        readonly EmbeddingsStoreAttribute attribute;
        readonly ISearchProvider searchProvider;
        readonly OpenAISDK.OpenAIClient openAIClient;
        readonly ILogger logger;

        public SemanticDocumentCollector(EmbeddingsStoreAttribute attribute, ISearchProvider searchProvider, OpenAISDK.OpenAIClient openAIClient, ILogger logger)
        {
            this.attribute = attribute;
            this.searchProvider = searchProvider;
            this.openAIClient = openAIClient;
            this.logger = logger;
        }

        public async Task AddAsync(SearchableDocument item, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(this.attribute.ConnectionName))
            {
                throw new InvalidOperationException("No connection string information was provided.");
            }
            else if (string.IsNullOrEmpty(this.attribute.Collection))
            {
                throw new InvalidOperationException("No collection name information was provided.");
            }

            // Get embeddings from OpenAI
            OpenAISDK.EmbeddingsOptions request = await EmbeddingsHelper.BuildRequest(this.attribute.MaxOverlap, this.attribute.MaxChunkLength, this.attribute.Model, this.attribute.InputType, this.attribute.Input);
            this.logger.LogInformation("Sending OpenAI embeddings request to deployment: {deploymentName}", request.DeploymentName);
            Response<OpenAISDK.Embeddings> response = await this.openAIClient.GetEmbeddingsAsync(request, cancellationToken);
            EmbeddingsContext embeddingsContext = new(request, response);
            this.logger.LogInformation("Received OpenAI embeddings of count: {count}", embeddingsContext.Count);

            // Add document to the embed store
            item.Embeddings = embeddingsContext;
            item.ConnectionInfo = new ConnectionInfo(this.attribute.ConnectionName, this.attribute.Collection);
            this.logger.LogInformation("Adding document to the embed store.");
            await this.searchProvider.AddDocumentAsync(item, cancellationToken);
            this.logger.LogInformation("Finished adding document to the embed store.");
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
