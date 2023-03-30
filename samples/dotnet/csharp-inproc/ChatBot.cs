// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using OpenAI.GPT3.ObjectModels.RequestModels;
using WebJobs.Extensions.OpenAI.DurableTask;

namespace CSharpInProcSamples;

/// <summary>
/// The ChatBot sample allows you to create chat bots with a specified set of initial instructions.
/// </summary>
public static class ChatBot
{
    const string UserResponseEvent = "UserResponse";

    /// <summary>
    /// This HTTP trigger function is used to create a new chat bot instance. It takes the initial instructions
    /// from the content of the HTTP request. The instructions are passed to the ChatBot orchestration function as
    /// an input parameter.
    /// </summary>
    [FunctionName(nameof(CreateChatBot))]
    public static async Task<IActionResult> CreateChatBot(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chats")] HttpRequest req,
        [DurableClient] IDurableOrchestrationClient starter)
    {
        using StreamReader streamReader = new(req.Body);
        string instructions = await streamReader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(instructions))
        {
            return new BadRequestObjectResult(new { error = "No instructions provided" });
        }

        string chatId = "chat-" + Guid.NewGuid().ToString("N")[..8];
        string instanceId = await starter.StartNewAsync(
            nameof(ChatBotOrchestration),
            instanceId: chatId,
            input: instructions.Trim());

        return MakeAcceptedHttpResponse(req, chatId);
    }

    /// <summary>
    /// This HTTP trigger function is used to receive prompts from the user and add them to the chat context.
    /// </summary>
    [FunctionName(nameof(GetChatStatus))]
    public static async Task<IActionResult> GetChatStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "chats/{chatId}")] HttpRequest req,
        string chatId,
        [DurableClient] IDurableOrchestrationClient client)
    {
        DurableOrchestrationStatus status = await client.GetStatusAsync(instanceId: chatId);
        if (!TryEnsureChatIsAvailable(status, out IActionResult? unavailableResult))
        {
            return unavailableResult;
        }

        return new OkObjectResult(new
        {
            assistantMessage = status.CustomStatus,
            lastUpdated = status.LastUpdatedTime.ToString("s"),
        });
    }

    /// <summary>
    /// This HTTP trigger function is used to receive prompts from the user and add them to the chat context.
    /// </summary>
    [FunctionName(nameof(PostUserResponse))]
    public static async Task<IActionResult> PostUserResponse(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chats/{chatId}")] HttpRequest req,
        string chatId,
        [DurableClient] IDurableOrchestrationClient client)
    {
        DurableOrchestrationStatus status = await client.GetStatusAsync(instanceId: chatId);
        if (!TryEnsureChatIsAvailable(status, out IActionResult? unavailableResult))
        {
            return unavailableResult;
        }

        using StreamReader streamReader = new(req.Body);
        string userResponse = await streamReader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(userResponse))
        {
            return new BadRequestObjectResult(new { error = "No user response provided" });
        }

        await client.RaiseEventAsync(chatId, eventName: UserResponseEvent, eventData: userResponse);
        return MakeAcceptedHttpResponse(req, chatId);
    }

    static IActionResult MakeAcceptedHttpResponse(HttpRequest req, string chatId)
    {
        // Add a location header so that clients can easily discover the endpoint for interacting with this chat.
        string location = new Uri(req.GetDisplayUrl()).GetLeftPart(UriPartial.Path);
        if (!location.EndsWith(chatId))
        {
            location = location.TrimEnd('/') + "/" + chatId;
        }

        return new AcceptedResult(location, new { message = "Request accepted", chatId, location });
    }

    static bool TryEnsureChatIsAvailable(
        DurableOrchestrationStatus status,
        [NotNullWhen(false)] out IActionResult? unavailableResponse)
    {
        if (status == null)
        {
            unavailableResponse = new NotFoundObjectResult(new { message = "Chat not found" });
            return false;
        }
        else if (status.RuntimeStatus == OrchestrationRuntimeStatus.Pending)
        {
            unavailableResponse = new OkObjectResult(new { message = "Waiting for chat session to begin" });
            return false;
        }
        else if (status.RuntimeStatus != OrchestrationRuntimeStatus.Running)
        {
            unavailableResponse = new OkObjectResult(new
            {
                message = "Chat session is no longer active",
                status = status.RuntimeStatus.ToString(),
                output = status.Output
            });
            return false;
        }

        unavailableResponse = null;
        return true;
    }

    /// <summary>
    /// This orchestration works by first calling an activity to get the initial
    /// response from ChatGPT. Afterwards, it runs in a loop and keeps track
    /// of the chat history in a list. The loops ends when a 24-hour timeout
    /// expires or when ChatGPT returns an error message (which is expected when
    /// we've exceeded the max tokens for a conversation).
    /// </summary>
    [FunctionName(nameof(ChatBotOrchestration))]
    public static async Task ChatBotOrchestration(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        static async Task SessionLoop(IDurableOrchestrationContext context, string message, Task timeoutTask)
        {
            // Chat history is stored locally in memory and passed to the activity function for each iteration.
            // This is required because ChatGPT is largely stateless and otherwise won't remember previous replies.
            // The first message is a system message that instructs the bot about how it should behave.
            List<ChatMessage> chatHistory = new(capacity: 100) { ChatMessage.FromSystem(message) };

            while (!timeoutTask.IsCompleted)
            {
                // Get the next prompt from ChatGPT. We save it into custom status so that a client can query it
                // and display it to the end user in an appropriate format.
                string assistantMessage = await context.GetChatCompletionAsync(chatHistory);
                chatHistory.Add(ChatMessage.FromAssistant(assistantMessage));
                context.SetCustomStatus(assistantMessage);

                // Wait for the user to respond. This is done by listening for an external event of a well-known name.
                // The payload of the external event is a message to add to the chat history.
                message = await context.WaitForExternalEvent<string>(name: "UserResponse");
                chatHistory.Add(ChatMessage.FromUser(message));
            }
        }

        // Create a timer that expires after 24 hours, which will be used to terminate the session loop.
        using CancellationTokenSource cts = new();
        Task timeoutTask = context.CreateTimer(context.CurrentUtcDateTime.AddHours(24), cts.Token);

        // Start the session loop. The loop will end when the timeout expires or if some other input causes the
        // session loop to end on its own.
        string message = context.GetInput<string>();
        Task sessionTask = SessionLoop(context, message, timeoutTask);
        await Task.WhenAny(timeoutTask, sessionTask);
        cts.Cancel();
    }
}
