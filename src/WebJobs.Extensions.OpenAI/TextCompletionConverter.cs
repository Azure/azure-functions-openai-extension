// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;

namespace WebJobs.Extensions.OpenAI;

class TextCompletionConverter :
    IAsyncConverter<TextCompletionAttribute, CompletionCreateResponse>,
    IAsyncConverter<TextCompletionAttribute, string>
{
    readonly IOpenAIServiceProvider serviceProvider;
    readonly ILogger logger;

    public TextCompletionConverter(IOpenAIServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.logger = loggerFactory?.CreateLogger<TextCompletionConverter>() ?? throw new ArgumentNullException(nameof(loggerFactory));
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
        return JsonConvert.SerializeObject(response);
    }

    async Task<CompletionCreateResponse> ConvertCoreAsync(
        TextCompletionAttribute attribute,
        CancellationToken cancellationToken)
    {
        CompletionCreateRequest request = attribute.BuildRequest();
        this.logger.LogInformation("Sending OpenAI completion request: {request}", request);

        IOpenAIService service = this.serviceProvider.GetService(attribute.Model);
        CompletionCreateResponse response = await service.Completions.CreateCompletion(
            request,
            modelId: null,
            cancellationToken);
        this.logger.LogInformation("Received OpenAI completion response: {response}", response);

        if (attribute.ThrowOnError && response.Error is not null)
        {
            throw new InvalidOperationException(
                $"OpenAI returned an error of type '{response.Error.Type}': {response.Error.Message}");
        }

        return response;
    }
}
