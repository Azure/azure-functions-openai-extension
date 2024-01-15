// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

class TextCompletionConverter :
    IAsyncConverter<TextCompletionAttribute, Response<Completions>>,
    IAsyncConverter<TextCompletionAttribute, string>
{
    readonly OpenAIClient openAIClient;
    readonly ILogger logger;

    public TextCompletionConverter(OpenAIClient openAIClient, ILoggerFactory loggerFactory)
    {
        this.openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
        this.logger = loggerFactory?.CreateLogger<TextCompletionConverter>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    // Intended for use with .NET in-proc functions
    Task<Response<Completions>> IAsyncConverter<TextCompletionAttribute, Response<Completions>>.ConvertAsync(
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
        Response<Completions> response = await this.ConvertCoreAsync(attribute, cancellationToken);
        return JsonConvert.SerializeObject(response);
    }

    async Task<Response<Completions>> ConvertCoreAsync(
        TextCompletionAttribute attribute,
        CancellationToken cancellationToken)
    {
        CompletionsOptions options;
        Response<Completions> response;
        try
        {
            options = attribute.BuildRequest();
            this.logger.LogInformation("Sending OpenAI completion request: {request}", options);

            response = await this.openAIClient.GetCompletionsAsync(
                options,
                cancellationToken);
            this.logger.LogInformation("Received OpenAI completion response: {response}", response);
        }
        catch (Exception ex) when (attribute.ThrowOnError)
        {
            this.logger.LogError(ex, "Error invoking OpenAI completions API");
            throw new InvalidOperationException(
                               $"OpenAI returned an error: {ex.Message}");
        }

        return response;
    }
}
