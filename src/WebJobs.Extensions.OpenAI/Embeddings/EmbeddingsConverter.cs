// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;


namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;

class EmbeddingsConverter :
    IAsyncConverter<EmbeddingsAttribute, EmbeddingsContext>,
    IAsyncConverter<EmbeddingsAttribute, string>
{
    readonly OpenAIClient openAIClient;
    readonly ILogger logger;
    // Note: we need this converter as Azure.AI.OpenAI does not support System.Text.Json serialization since their constructors are internal
    static readonly JsonSerializerOptions options = new()
    {
        Converters = { new EmbeddingsJsonConverter() }
    };

    public EmbeddingsConverter(OpenAIClient openAIClient, ILoggerFactory loggerFactory)
    {
        this.openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
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
        EmbeddingsOptions request = attribute.BuildRequest();
        this.logger.LogInformation("Sending OpenAI embeddings request: {request}", request);
        Response<Embeddings> response = await this.openAIClient.GetEmbeddingsAsync(request, cancellationToken);
        this.logger.LogInformation("Received OpenAI embeddings response: {response}", response);

        return new EmbeddingsContext(request, response);
    }
}
