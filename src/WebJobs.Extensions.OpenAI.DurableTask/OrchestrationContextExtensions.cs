// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace WebJobs.Extensions.OpenAI.DurableTask;

/// <summary>
/// Static class defining OpenAI-specific extension methods for <see cref="IDurableOrchestrationContext"/>.
/// </summary>
public static class OrchestrationContextExtensions
{
    /// <summary>
    /// Extension method that calls OpenAI to get the next chat bot response.
    /// </summary>
    /// <param name="context">The current orchestration context.</param>
    /// <param name="chatHistory">The current chat history</param>
    /// <param name="retryOptions">Any retry options for handling failures.</param>
    /// <returns>Returns a Task that resolves to the chat response received from the GPT API.</returns>
    public static async Task<string> GetChatCompletionAsync(
        this IDurableOrchestrationContext context,
        IReadOnlyCollection<ChatMessage> chatHistory,
        RetryOptions? retryOptions = null)
    {
        string activityFunctionName = BuiltInFunctionsProvider.GetBuiltInFunctionName(
            nameof(BuiltInFunctionsProvider.GetNextChatBotResponse));

        string botResponseMessage;
        if (retryOptions != null)
        {
            botResponseMessage = await context.CallActivityWithRetryAsync<string>(
                activityFunctionName,
                retryOptions,
                chatHistory);
        }
        else
        {
            botResponseMessage = await context.CallActivityAsync<string>(
                activityFunctionName,
                chatHistory);
        }

        return botResponseMessage;
    }
}
