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

namespace ChatBotSample;

/// <summary>
/// The ChatBot sample allows you to create chat bots with a specified set of initial instructions.
/// </summary>
public static class ChatBot
{
    const string DefaultChatStorageConnectionSetting = "AzureWebJobsStorage";
    const string DefaultCollectionName = "ChatState";

    public record CreateRequest(string Instructions);

    [FunctionName(nameof(CreateChatBot))]
    public static async Task<IActionResult> CreateChatBot(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "chats/{chatId}")] CreateRequest req,
        string chatId,
        [AssistantCreate] IAsyncCollector<AssistantCreateRequest> createRequests)
    {
        AssistantCreateRequest assistantCreateRequest = new(chatId, req.Instructions)
        {
            ChatStorageConnectionSetting = DefaultChatStorageConnectionSetting,
            CollectionName = DefaultCollectionName,
        };
        await createRequests.AddAsync(assistantCreateRequest);
        var responseJson = new { chatId };
        return new ObjectResult(responseJson) { StatusCode = 201 };
    }

    [FunctionName(nameof(GetChatState))]
    public static AssistantState GetChatState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chats/{chatId}")] HttpRequest req,
        string chatId,
        [AssistantQuery("{chatId}", TimestampUtc = "{Query.timestampUTC}", ChatStorageConnectionSetting = DefaultChatStorageConnectionSetting, CollectionName = DefaultCollectionName)] AssistantState state)
    {
        return state;
    }

    [FunctionName(nameof(PostUserResponse))]
    public static async Task<IActionResult> PostUserResponse(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chats/{chatId}")] HttpRequest req,
        string chatId,
        [AssistantPost("{chatId}", "{Query.message}", Model = "%CHAT_MODEL_DEPLOYMENT_NAME%", ChatStorageConnectionSetting = DefaultChatStorageConnectionSetting, CollectionName = DefaultCollectionName)] AssistantState updatedState)
    {
        return new OkObjectResult(updatedState.RecentMessages.Any() ? updatedState.RecentMessages[updatedState.RecentMessages.Count - 1].Content : "No response returned.");
    }
}
