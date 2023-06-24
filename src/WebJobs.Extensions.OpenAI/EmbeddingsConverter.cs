// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace WebJobs.Extensions.OpenAI;

class EmbeddingsConverter :
    IAsyncConverter<EmbeddingsAttribute, EmbeddingsContext>,
    IAsyncConverter<EmbeddingsAttribute, string>
{
    readonly IOpenAIService service;
    readonly ILogger logger;

    public EmbeddingsConverter(IOpenAIService service, ILoggerFactory loggerFactory)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
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
        return JsonSerializer.Serialize(response);
    }

    async Task<EmbeddingsContext> ConvertCoreAsync(
        EmbeddingsAttribute attribute,
        CancellationToken cancellationToken)
    {
        EmbeddingCreateRequest request = attribute.BuildRequest();
        this.logger.LogInformation("Sending OpenAI embeddings request: {request}", request);
        EmbeddingCreateResponse response = await this.service.Embeddings.CreateEmbedding(
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
