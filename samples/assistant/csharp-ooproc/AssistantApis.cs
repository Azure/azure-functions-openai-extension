﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;
using Microsoft.Azure.Functions.Worker.Http;

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
    [Function(nameof(CreateAssistant))]
    public static async Task<CreateChatBotOutput> CreateAssistant(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "assistants/{assistantId}")] HttpRequestData req,
        string assistantId)
    {
        string instructions =
           """
            Don't make assumptions about what values to plug into functions.
            Ask for clarification if a user request is ambiguous.
            """;

        using StreamReader reader = new(req.Body);

        string request = await reader.ReadToEndAsync();


        return new CreateChatBotOutput
        {
            HttpResponse = new ObjectResult(new { assistantId }) { StatusCode = 201 },
            ChatBotCreateRequest = new AssistantCreateRequest(assistantId, instructions)
            {
                ChatStorageConnectionSetting = DefaultChatStorageConnectionSetting,
                CollectionName = DefaultCollectionName,
            },
        };
    }

    public class CreateChatBotOutput
    {
        [AssistantCreateOutput()]
        public AssistantCreateRequest? ChatBotCreateRequest { get; set; }

        [HttpResult]
        public IActionResult? HttpResponse { get; set; }
    }

    /// <summary>
    /// HTTP POST function that sends user prompts to the assistant chat bot.
    /// </summary>
    [Function(nameof(PostUserQuery))]
    public static IActionResult PostUserQuery(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "assistants/{assistantId}")] HttpRequestData req,
        string assistantId,
        [AssistantPostInput("{assistantId}", "{Query.message}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", ChatStorageConnectionSetting = DefaultChatStorageConnectionSetting, CollectionName = DefaultCollectionName)] AssistantState state)
    {
        return new OkObjectResult(state.RecentMessages.Any() ? state.RecentMessages[state.RecentMessages.Count - 1].Content : "No response returned.");
    }

    /// <summary>
    /// HTTP GET function that queries the conversation history of the assistant chat bot.
    /// </summary>
    [Function(nameof(GetChatState))]
    public static IActionResult GetChatState(
       [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assistants/{assistantId}")] HttpRequestData req,
       string assistantId,
       [AssistantQueryInput("{assistantId}", TimestampUtc = "{Query.timestampUTC}", ChatStorageConnectionSetting = DefaultChatStorageConnectionSetting, CollectionName = DefaultCollectionName)] AssistantState state)
    {
        return new OkObjectResult(state);
    }
}
