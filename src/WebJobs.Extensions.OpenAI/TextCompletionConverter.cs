// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Chat;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

class TextCompletionConverter :
    IAsyncConverter<TextCompletionAttribute, TextCompletionResponse>,
    IAsyncConverter<TextCompletionAttribute, string>
{
    readonly OpenAIClientFactory openAIClientFactory;
    readonly ILogger logger;

    public TextCompletionConverter(OpenAIClientFactory openAIClientFactory, ILoggerFactory loggerFactory)
    {
        this.openAIClientFactory = openAIClientFactory ?? throw new ArgumentNullException(nameof(openAIClientFactory));
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
        ChatCompletionOptions options = attribute.BuildRequest();
        this.logger.LogInformation("Sending OpenAI completion request with prompt: {request}", attribute.Prompt);

        IList<ChatMessage> chatMessages = new List<ChatMessage>()
        {
            new UserChatMessage(attribute.Prompt)
        };

        ClientResult<ChatCompletion> response = await this.openAIClientFactory.GetChatClient(
            attribute.AIConnectionName,
            attribute.ChatModel).CompleteChatAsync(chatMessages, options, cancellationToken: cancellationToken);

        string text = string.Join(
            Environment.NewLine + Environment.NewLine,
            response.Value.Content[0].Text);
        TextCompletionResponse textCompletionResponse = new(text, response.Value.Usage.TotalTokenCount);
        return textCompletionResponse;
    }
}
