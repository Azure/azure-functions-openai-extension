// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;

namespace WebJobs.Extensions.OpenAI;

class EmbeddingsConverter :
    IAsyncConverter<EmbeddingsAttribute, EmbeddingsContext>,
    IAsyncConverter<EmbeddingsAttribute, string>
{
    readonly IOpenAIServiceProvider serviceProvider;
    readonly ILogger logger;

    public EmbeddingsConverter(IOpenAIServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
        IOpenAIService service = this.serviceProvider.GetService(attribute.Model);

        EmbeddingCreateRequest request = attribute.BuildRequest();
        this.logger.LogInformation("Sending OpenAI embeddings request: {request}", request);
        EmbeddingCreateResponse response = await service.Embeddings.CreateEmbedding(
            request,
            cancellationToken);
        this.logger.LogInformation("Received OpenAI embeddings response: {response}", response);

        if (attribute.ThrowOnError && response.Error is not null)
        {
            throw new InvalidOperationException(
                $"OpenAI returned an error of type '{response.Error.Type}': {response.Error.Message}");
        }

        return new EmbeddingsContext(request, response);
    }
}
