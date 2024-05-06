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

    public class AssistantPostRequest
    {
        [JsonPropertyName("message")]
        public string? UserMessage { get; set; }
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

    [Function(nameof(PostUserResponse))]
    public static async Task<HttpResponseData> PostUserResponse(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chats/{chatId}")] HttpRequestData req,
        string chatId,
        [AssistantPostInput("{chatId}", "{message}", Model = "%CHAT_MODEL_DEPLOYMENT_NAME%")] AssistantState state)
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