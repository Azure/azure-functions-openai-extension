// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

namespace AssistantSample;

/// <summary>
/// Defines HTTP APIs for interacting with assistants.
/// </summary>
static class AssistantApis
{
    const string DefaultChatStorageConnectionSetting = "AzureWebJobsStorage";
    const string DefaultCollectionName = "ChatState";

    /// <summary>
    /// HTTP PUT function that creates a new assistant chat bot with the specified ID.
    /// </summary>
    [FunctionName(nameof(CreateAssistant))]
    public static async Task<IActionResult> CreateAssistant(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "assistants/{assistantId}")] HttpRequest req,
        string assistantId,
        [AssistantCreate] IAsyncCollector<AssistantCreateRequest> createRequests)
    {
        string instructions =
            """
            Don't make assumptions about what values to plug into functions.
            Ask for clarification if a user request is ambiguous.
            """;
        AssistantCreateRequest assistantCreateRequest = new(assistantId, instructions)
        {
            ChatStorageConnectionSetting = DefaultChatStorageConnectionSetting,
            CollectionName = DefaultCollectionName,
        };
        await createRequests.AddAsync(assistantCreateRequest);
        var responseJson = new { assistantId };
        return new ObjectResult(responseJson) { StatusCode = 202 };
    }

    /// <summary>
    /// HTTP POST function that sends user prompts to the assistant chat bot.
    /// </summary>
    [FunctionName(nameof(PostUserQuery))]
    public static IActionResult PostUserQuery(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "assistants/{assistantId}")] HttpRequest req,
        string assistantId,
        [AssistantPost("{assistantId}", "{Query.message}", Model = "%CHAT_MODEL_DEPLOYMENT_NAME%", ChatStorageConnectionSetting = DefaultChatStorageConnectionSetting, CollectionName = DefaultCollectionName)] AssistantState updatedState)
    {
        return new OkObjectResult(updatedState.RecentMessages.Any() ? updatedState.RecentMessages[updatedState.RecentMessages.Count - 1].Content : "No response returned.");
    }

    /// <summary>
    /// HTTP GET function that queries the conversation history of the assistant chat bot.
    /// </summary>
    [FunctionName(nameof(GetChatState))]
    public static AssistantState GetChatState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assistants/{assistantId}")] HttpRequest req,
        string assistantId,
        [AssistantQuery("{assistantId}", TimestampUtc = "{Query.timestampUTC}", ChatStorageConnectionSetting = DefaultChatStorageConnectionSetting, CollectionName = DefaultCollectionName)] AssistantState state)
    {
        return state;
    }
}