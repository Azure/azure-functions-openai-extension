// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

class TextCompletionConverter :
    IAsyncConverter<TextCompletionAttribute, TextCompletionResponse>,
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
    Task<TextCompletionResponse> IAsyncConverter<TextCompletionAttribute, TextCompletionResponse>.ConvertAsync(
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
        TextCompletionResponse response = await this.ConvertCoreAsync(attribute, cancellationToken);
        return JsonConvert.SerializeObject(response);
    }

    async Task<TextCompletionResponse> ConvertCoreAsync(
        TextCompletionAttribute attribute,
        CancellationToken cancellationToken)
    {
        ChatCompletionsOptions options = attribute.BuildRequest();
        this.logger.LogInformation("Sending OpenAI completion request: {request}", options);

        Response<ChatCompletions> response = await this.openAIClient.GetChatCompletionsAsync(
            options,
            cancellationToken);

        string text = string.Join(
            Environment.NewLine + Environment.NewLine,
            response.Value.Choices.Select(choice => choice.Message.Content));
        TextCompletionResponse textCompletionResponse = new(text, response.Value.Usage.TotalTokens);
        
        return textCompletionResponse;
    }
}
