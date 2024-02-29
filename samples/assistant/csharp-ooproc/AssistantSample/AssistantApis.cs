// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AssistantSample;

/// <summary>
/// Defines HTTP APIs for interacting with assistants.
/// </summary>
static class AssistantApis
{

    /// <summary>
    /// HTTP PUT function that creates a new assistant chat bot with the specified ID.
    /// </summary>
    [Function(nameof(CreateAssistant))]
    public static async Task<CreateChatBotOutput> CreateAssistant(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "assistants/{assistantId}")] HttpRequestData req,
        string assistantId)
    {
        var responseJson = new { assistantId };

        string instructions =
           """
            Don't make assumptions about what values to plug into functions.
            Ask for clarification if a user request is ambiguous.
            """;

        using StreamReader reader = new(req.Body);

        string request = await reader.ReadToEndAsync();

        HttpResponseData response = req.CreateResponse();
        await response.WriteAsJsonAsync(responseJson, HttpStatusCode.Created);

        return new CreateChatBotOutput
        {
            HttpResponse = response,
            ChatBotCreateRequest = new AssistantCreateRequest(assistantId, instructions),
        };
    }

    public class CreateChatBotOutput
    {
        [AssistantCreateOutput()]
        public AssistantCreateRequest? ChatBotCreateRequest { get; set; }

        public HttpResponseData? HttpResponse { get; set; }
    }

    /// <summary>
    /// HTTP POST function that sends user prompts to the assistant chat bot.
    /// </summary>
    [Function(nameof(PostUserQuery))]
    public static async Task<PostResponseOutput> PostUserQuery(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "assistants/{assistantId}")] HttpRequestData req,
        string assistantId)
    {
        string? userMessage = await req.ReadAsStringAsync();
        if (string.IsNullOrEmpty(userMessage))
        {
            HttpResponseData badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Request body is empty");
            return new PostResponseOutput { HttpResponse = badResponse };
        }

        HttpResponseData response = req.CreateResponse(HttpStatusCode.Accepted);

        return new PostResponseOutput
        {
            HttpResponse = response,
            ChatBotPostRequest = new AssistantPostRequest { UserMessage = userMessage, Id = assistantId }
        };
    }

    public class PostResponseOutput
    {
        [AssistantPostOutput("{assistantId}", Model = "gpt-3.5-turbo")]
        public AssistantPostRequest? ChatBotPostRequest { get; set; }

        public HttpResponseData? HttpResponse { get; set; }
    }

    /// <summary>
    /// HTTP GET function that queries the conversation history of the assistant chat bot.
    /// </summary>
    [Function(nameof(GetChatState))]
    public static async Task<HttpResponseData> GetChatState(
       [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assistants/{assistantId}")] HttpRequestData req,
       string assistantId,
       [AssistantQueryInput("{assistantId}", TimestampUtc = "{Query.timestampUTC}")] AssistantState state)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(state);
        return response;
    }
}
