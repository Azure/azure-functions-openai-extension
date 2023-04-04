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

partial class OpenAIExtension
{
    class EmbeddingsConverter :
        IAsyncConverter<EmbeddingsAttribute, EmbeddingCreateResponse>,
        IAsyncConverter<EmbeddingsAttribute, string>
    {
        readonly IOpenAIService service;
        readonly ILogger logger;

        public EmbeddingsConverter(IOpenAIService service, ILogger logger)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        Task<EmbeddingCreateResponse> IAsyncConverter<EmbeddingsAttribute, EmbeddingCreateResponse>.ConvertAsync(
            EmbeddingsAttribute attribute,
            CancellationToken cancellationToken)
        {
            return this.ConvertCoreAsync(attribute, cancellationToken);
        }

        async Task<string> IAsyncConverter<EmbeddingsAttribute, string>.ConvertAsync(
            EmbeddingsAttribute input,
            CancellationToken cancellationToken)
        {
            EmbeddingCreateResponse response = await this.ConvertCoreAsync(input, cancellationToken);
            return JsonSerializer.Serialize(response);
        }

        async Task<EmbeddingCreateResponse> ConvertCoreAsync(
            EmbeddingsAttribute attribute,
            CancellationToken cancellationToken)
        {
            EmbeddingCreateRequest request = attribute.BuildRequest();
            this.logger.LogInformation("Sending OpenAI embeddings request: {request}", request);
            EmbeddingCreateResponse response = await this.service.Embeddings.CreateEmbedding(
                request,
                cancellationToken);
            this.logger.LogInformation("Received OpenAI embeddings response: {response}", response);
            return response;
        }
    }
}
