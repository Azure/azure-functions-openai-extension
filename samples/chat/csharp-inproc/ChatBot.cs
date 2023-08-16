// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using WebJobs.Extensions.OpenAI.Agents;

namespace ChatBotSample;

/// <summary>
/// The ChatBot sample allows you to create chat bots with a specified set of initial instructions.
/// </summary>
public static class ChatBot
{
    public record CreateRequest(string Instructions);

    [FunctionName(nameof(CreateChatBot))]
    public static async Task<IActionResult> CreateChatBot(
        [HttpTrigger(AuthorizationLevel.Anonymous , "put", Route = "chats/{chatId}")] CreateRequest req,
        string chatId,
        [ChatBotCreate] IAsyncCollector<ChatBotCreateRequest> createRequests)
    {
        await createRequests.AddAsync(new ChatBotCreateRequest(chatId, req.Instructions));
        var responseJson = new { chatId };
        return new ObjectResult(responseJson) { StatusCode = 202 };
    }

    [FunctionName(nameof(GetChatState))]
    public static ChatBotState GetChatState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chats/{chatId}")] HttpRequest req,
        string chatId,
        [ChatBotQuery("{chatId}", TimestampUtc = "{Query.timestampUTC}")] ChatBotState state)
    {
        return state;
    }

    [FunctionName(nameof(PostUserResponse))]
    public static async Task<IActionResult> PostUserResponse(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chats/{chatId}")] HttpRequest req,
        string chatId,
        [ChatBotPost("{chatId}")] ICollector<ChatBotPostRequest> newMessages)
    {
        string userMessage = await req.ReadAsStringAsync();
        if (string.IsNullOrEmpty(userMessage))
        {
            return new BadRequestObjectResult(new { message = "Request body is empty" });
        }
        
        newMessages.Add(new ChatBotPostRequest(userMessage));
        return new AcceptedResult();
    }
}
