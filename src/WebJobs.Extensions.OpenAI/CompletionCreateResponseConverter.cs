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

class CompletionCreateResponseConverter :
    IAsyncConverter<TextCompletionAttribute, CompletionCreateResponse>,
    IAsyncConverter<TextCompletionAttribute, string>
{
    readonly IOpenAIService service;
    readonly ILogger logger;

    public CompletionCreateResponseConverter(IOpenAIService service, ILogger logger)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Intended for use with .NET in-proc functions
    Task<CompletionCreateResponse> IAsyncConverter<TextCompletionAttribute, CompletionCreateResponse>.ConvertAsync(
        TextCompletionAttribute attribute,
        CancellationToken cancellationToken)
    {
        return this.ConvertCoreAsync(attribute, cancellationToken);
    }

    // Intended for use with out-of-proc functions
    async Task<string> IAsyncConverter<TextCompletionAttribute, string>.ConvertAsync(
        TextCompletionAttribute attribute,
        CancellationToken cancellationToken)
    {
        CompletionCreateResponse response = await this.ConvertCoreAsync(attribute, cancellationToken);
        return JsonSerializer.Serialize(response);
    }

    async Task<CompletionCreateResponse> ConvertCoreAsync(
        TextCompletionAttribute attribute,
        CancellationToken cancellationToken)
    {
        CompletionCreateRequest request = attribute.BuildRequest();
        this.logger.LogInformation("Sending OpenAI completion request: {request}", request);

        CompletionCreateResponse response = await this.service.Completions.CreateCompletion(
            request,
            modelId: null,
            cancellationToken);
        this.logger.LogInformation("Received OpenAI completino response: {response}", response);

        return response;
    }
}
