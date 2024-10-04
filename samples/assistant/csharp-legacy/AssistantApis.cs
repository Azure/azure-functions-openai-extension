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

        await createRequests.AddAsync(new AssistantCreateRequest(assistantId, instructions));
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
        [AssistantPost("{assistantId}", "{Query.message}")] AssistantState updatedState)
    {
        return new OkObjectResult(updatedState.RecentMessages.LastOrDefault()?.Content ?? "No response returned.");
    }

    /// <summary>
    /// HTTP GET function that queries the conversation history of the assistant chat bot.
    /// </summary>
    [FunctionName(nameof(GetChatState))]
    public static AssistantState GetChatState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assistants/{assistantId}")] HttpRequest req,
        string assistantId,
        [AssistantQuery("{assistantId}", TimestampUtc = "{Query.timestampUTC}")] AssistantState state)
    {
        return state;
    }
}