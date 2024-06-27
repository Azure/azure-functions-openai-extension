using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;
using Microsoft.Azure.Functions.Worker.Http;

namespace ChatBot;

/// <summary>
/// The ChatBot sample allows you to create chat bots with a specified set of initial instructions.
/// </summary>
public static class ChatBot
{
    public class CreateRequest
    {
        [JsonPropertyName("instructions")]
        public string? Instructions { get; set; }
    }

    [Function(nameof(CreateChatBot))]
    public static async Task<CreateChatBotOutput> CreateChatBot(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "chats/{chatId}")] HttpRequestData req,
        string chatId)
    {
        var responseJson = new { chatId };

        using StreamReader reader = new(req.Body);

        string request = await reader.ReadToEndAsync();

        CreateRequest? createRequestBody = JsonSerializer.Deserialize<CreateRequest>(request);

        if (createRequestBody == null)
        {
            throw new ArgumentException("Invalid request body. Make sure that you pass in {\"instructions\": value } as the request body.");
        }

        HttpResponseData response = req.CreateResponse();
        await response.WriteAsJsonAsync(responseJson, HttpStatusCode.Created);

        return new CreateChatBotOutput
        {
            HttpResponse = response,
            ChatBotCreateRequest = new AssistantCreateRequest(chatId, createRequestBody.Instructions),
        };
    }

    public class CreateChatBotOutput
    {
        [AssistantCreateOutput()]
        public AssistantCreateRequest? ChatBotCreateRequest { get; set; }

        public HttpResponseData? HttpResponse { get; set; }
    }

    // GPT 4o
    [Function(nameof(PostUserResponseGpt4o))]
    public static async Task<HttpResponseData> PostUserResponseGpt4o(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chats/{chatId}/gpt4o")] HttpRequestData req,
    string chatId,
    [AssistantPostInput("{chatId}", "{Query.message}", Model = "gpt-4o")] AssistantState state)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);

        response.Headers.Add("Content-Type", "text/plain");
        await response.WriteStringAsync(state.RecentMessages.LastOrDefault()?.Content ?? "No response returned.");
        return response;
    }

    // Phi
    [Function(nameof(PostUserResponsePhi))]
    public static async Task<HttpResponseData> PostUserResponsePhi(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chats/{chatId}/phi")] HttpRequestData req,
        string chatId,
        [AssistantPostInput("{chatId}", "{Query.message}", Model = "phi3-medium-4k")] AssistantState state)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);

        response.Headers.Add("Content-Type", "text/plain");
        await response.WriteStringAsync(state.RecentMessages.LastOrDefault()?.Content ?? "No response returned.");
        return response;
    }

    // Mistral
    [Function(nameof(PostUserResponseMistral))]
    public static async Task<HttpResponseData> PostUserResponseMistral(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chats/{chatId}/mistral")] HttpRequestData req,
    string chatId,
    [AssistantPostInput("{chatId}", "{Query.message}", Model = "mixtral-8x7b")] AssistantState state)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);

        response.Headers.Add("Content-Type", "text/plain");
        await response.WriteStringAsync(state.RecentMessages.LastOrDefault()?.Content ?? "No response returned.");
        return response;
    }

    [Function(nameof(GetChatState))]
    public static async Task<HttpResponseData> GetChatState(
       [HttpTrigger(AuthorizationLevel.Function, "get", Route = "chats/{chatId}")] HttpRequestData req,
       string chatId,
       [AssistantQueryInput("{chatId}", TimestampUtc = "{Query.timestampUTC}")] AssistantState state,
       FunctionContext context)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(state);
        return response;
    }
}