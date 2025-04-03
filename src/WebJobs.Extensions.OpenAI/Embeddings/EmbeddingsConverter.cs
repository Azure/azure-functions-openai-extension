// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;

class EmbeddingsConverter :
    IAsyncConverter<EmbeddingsAttribute, EmbeddingsContext>,
    IAsyncConverter<EmbeddingsAttribute, string>
{
    readonly EmbeddingClient embeddingClient;
    readonly ILogger logger;

    // Note: we need this converter as Azure.AI.OpenAI does not support System.Text.Json serialization since their constructors are internal
    static readonly JsonSerializerOptions options = new()
    {
        Converters = { new EmbeddingsContextConverter(), new SearchableDocumentJsonConverter() }
    };

    public EmbeddingsConverter(AzureOpenAIClient azureOpenAIClient, ILoggerFactory loggerFactory)
    {
        // ToDo: Handle the deployment name retrieval better
        this.embeddingClient = azureOpenAIClient.GetEmbeddingClient("embedding") ?? throw new ArgumentNullException(nameof(azureOpenAIClient));
        this.logger = loggerFactory?.CreateLogger<EmbeddingsConverter>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    Task<EmbeddingsContext> IAsyncConverter<EmbeddingsAttribute, EmbeddingsContext>.ConvertAsync(
        EmbeddingsAttribute attribute,
        CancellationToken cancellationToken)
    {
        return this.ConvertCoreAsync(attribute, cancellationToken);
    }

    async Task<string> IAsyncConverter<EmbeddingsAttribute, string>.ConvertAsync(
        EmbeddingsAttribute input,
        CancellationToken cancellationToken)
    {
        EmbeddingsContext response = await this.ConvertCoreAsync(input, cancellationToken);
        return JsonSerializer.Serialize(response, options);
    }

    async Task<EmbeddingsContext> ConvertCoreAsync(
        EmbeddingsAttribute attribute,
        CancellationToken cancellationToken)
    {
        List<string> input = await EmbeddingsHelper.BuildRequest(attribute.MaxOverlap, attribute.MaxChunkLength, attribute.Model, attribute.InputType, attribute.Input);
        this.logger.LogInformation("Sending OpenAI embeddings request");
        ClientResult<OpenAIEmbeddingCollection> response = await this.embeddingClient.GenerateEmbeddingsAsync(input);
        this.logger.LogInformation("Received OpenAI embeddings count: {response}", response.Value.Count);

        return new EmbeddingsContext(input, response);
    }
}
