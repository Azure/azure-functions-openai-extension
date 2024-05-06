// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;
using Microsoft.Azure.Functions.Worker.Http;

namespace AssistantSample;

/// <summary>
/// Defines HTTP APIs for interacting with assistants.
/// </summary>
static class AssistantApis
{
    public class AssistantPostRequest
    {
        [JsonPropertyName("message")]
        public string? UserMessage { get; set; }
    }

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
    public static async Task<HttpResponseData> PostUserQuery(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "assistants/{assistantId}")] HttpRequestData req,
        string assistantId,
        [AssistantPostInput("{assistantId}", "{message}", Model = "%CHAT_MODEL_DEPLOYMENT_NAME%")] AssistantState state)
    {
        using StreamReader reader = new(req.Body);
        string request = await reader.ReadToEndAsync();

        AssistantPostRequest? requestBody = JsonSerializer.Deserialize<AssistantPostRequest>(request);

        if (requestBody is null || string.IsNullOrEmpty(requestBody.UserMessage))
        {
            HttpResponseData badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid request body. Make sure that you pass in {\"message\": value } as the request body.");
            return badResponse;
        }

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(state);
        return response;
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
