// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

class EmbeddingsConverter :
    IAsyncConverter<EmbeddingsAttribute, EmbeddingsContext>,
    IAsyncConverter<EmbeddingsAttribute, string>
{
    readonly OpenAIClient openAIClient;
    readonly ILogger logger;

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
        return JsonConvert.SerializeObject(response);
    }

    async Task<EmbeddingsContext> ConvertCoreAsync(
        EmbeddingsAttribute attribute,
        CancellationToken cancellationToken)
    {
        EmbeddingsOptions request;
        Response<Embeddings> response;
        try
        {
            request = attribute.BuildRequest();
            this.logger.LogInformation("Sending OpenAI embeddings request: {request}", request);
            response = await this.openAIClient.GetEmbeddingsAsync(request, cancellationToken);
            this.logger.LogInformation("Received OpenAI embeddings response: {response}", response);
        }
        catch (Exception ex) when (attribute.ThrowOnError)
        {
            this.logger.LogError(ex, "Error invoking OpenAI embeddings API");
            throw new InvalidOperationException(
                $"OpenAI returned an error: {ex.Message}");
        }

        return new EmbeddingsContext(request, response);
    }
}
