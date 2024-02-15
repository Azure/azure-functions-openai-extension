// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;

namespace AssistantSample;

/// <summary>
/// Defines HTTP APIs for interacting with assistants.
/// </summary>
static class AssistantApis
{
    /// <summary>
    /// HTTP PUT function that creates a new assistant chat bot with the specified ID.
    /// </summary>
    [FunctionName(nameof(CreateAssistant))]
    public static async Task<IActionResult> CreateAssistant(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "assistants/{assistantId}")] HttpRequest req,
        string assistantId,
        [ChatBotCreate] IAsyncCollector<ChatBotCreateRequest> createRequests)
    {
        string instructions =
            """
            Don't make assumptions about what values to plug into functions.
            Ask for clarification if a user request is ambiguous.
            """;

        await createRequests.AddAsync(new ChatBotCreateRequest(assistantId, instructions));
        var responseJson = new { assistantId };
        return new ObjectResult(responseJson) { StatusCode = 202 };
    }

    /// <summary>
    /// HTTP POST function that sends user prompts to the assistant chat bot.
    /// </summary>
    [FunctionName(nameof(PostUserQuery))]
    public static async Task<IActionResult> PostUserQuery(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "assistants/{assistantId}")] HttpRequest req,
        string assistantId,
        [ChatBotPost("{assistantId}")] ICollector<ChatBotPostRequest> newMessages)
    {
        string userMessage = await req.ReadAsStringAsync();
        if (string.IsNullOrEmpty(userMessage))
        {
            return new BadRequestObjectResult(new { message = "Request body is empty" });
        }

        newMessages.Add(new ChatBotPostRequest(userMessage));
        return new AcceptedResult();
    }

    /// <summary>
    /// HTTP GET function that queries the conversation history of the assistant chat bot.
    /// </summary>
    [FunctionName(nameof(GetChatState))]
    public static ChatBotState GetChatState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assistants/{assistantId}")] HttpRequest req,
        string assistantId,
        [ChatBotQuery("{assistantId}", TimestampUtc = "{Query.timestampUTC}")] ChatBotState state)
    {
        return state;
    }
}
