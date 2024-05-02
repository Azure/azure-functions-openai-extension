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
    IAsyncConverter<EmbeddingsStoreAttribute, EmbeddingsContext>,
    IAsyncConverter<EmbeddingsStoreAttribute, string>
{
    readonly OpenAISDK.OpenAIClient openAIClient;
    readonly ILogger logger;
    readonly ISearchProvider? searchProvider;

    // Note: we need this converter as Azure.AI.OpenAI does not support System.Text.Json serialization since their constructors are internal
    static readonly JsonSerializerOptions options = new()
    {
        Converters = { new EmbeddingsContextConverter() }
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

    Task<EmbeddingsContext> IAsyncConverter<EmbeddingsStoreAttribute, EmbeddingsContext>.ConvertAsync(
        EmbeddingsStoreAttribute attribute,
        CancellationToken cancellationToken)
    {
        return this.ConvertCoreAsync(attribute, cancellationToken);
    }

    async Task<string> IAsyncConverter<EmbeddingsStoreAttribute, string>.ConvertAsync(
        EmbeddingsStoreAttribute input,
        CancellationToken cancellationToken)
    {
        EmbeddingsContext response = await this.ConvertCoreAsync(input, cancellationToken);
        return JsonSerializer.Serialize(response, options);
    }

    async Task<EmbeddingsContext> ConvertCoreAsync(
        EmbeddingsStoreAttribute attribute,
        CancellationToken cancellationToken)
    {
        ConnectionInfo connectionInfo = new(attribute.ConnectionName, attribute.Collection);

        if (string.IsNullOrEmpty(connectionInfo.ConnectionName))
        {
            throw new InvalidOperationException("No connection string information was provided.");
        }
        else if (string.IsNullOrEmpty(connectionInfo.CollectionName))
        {
            throw new InvalidOperationException("No collection name information was provided.");
        }
        if (this.searchProvider == null)
        {
            throw new InvalidOperationException(
                "No search provider is configured. Search providers are configured in the host.json file. For .NET apps, the appropriate nuget package must also be added to the app's project file.");
        }

        OpenAISDK.EmbeddingsOptions request = EmbeddingsHelper.BuildRequest(attribute.MaxOverlap, attribute.MaxChunkLength, attribute.Model, attribute.InputType, attribute.Input);
        this.logger.LogInformation("Sending OpenAI embeddings store request: {request}", request);
        Response<OpenAISDK.Embeddings> response = await this.openAIClient.GetEmbeddingsAsync(request, cancellationToken);
        this.logger.LogInformation("Received OpenAI embeddings store response: {response}", response);

        EmbeddingsContext embeddingsContext = new(request, response);
        SearchableDocument item = new(attribute.Title, embeddingsContext)
        {
            ConnectionInfo = connectionInfo
        };
        await this.searchProvider.AddDocumentAsync(item, cancellationToken);
        return embeddingsContext;
    }
}
