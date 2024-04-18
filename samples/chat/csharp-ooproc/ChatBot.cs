using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;

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
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "chats/{chatId}")] HttpRequestData req,
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
    public static async Task<PostResponseOutput> PostUserResponse(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chats/{chatId}")] HttpRequestData req,
        string chatId)
    {
        string? userMessage = await req.ReadAsStringAsync();
        if (string.IsNullOrEmpty(userMessage))
        {
            HttpResponseData badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Request body is empty");
            return new PostResponseOutput { HttpResponse = badResponse };
        }

        HttpResponseData response = req.CreateResponse(HttpStatusCode.Created);

        return new PostResponseOutput
        {
            HttpResponse = response,
            ChatBotPostRequest = new AssistantPostRequest { UserMessage = userMessage, Id = chatId }
        };
    }

    public class PostResponseOutput
    {
        [AssistantPostOutput("{chatId}", Model = "%CHAT_MODEL_DEPLOYMENT_NAME%")]
        public AssistantPostRequest? ChatBotPostRequest { get; set; }

        public HttpResponseData? HttpResponse { get; set; }
    }

    [Function(nameof(GetChatState))]
    public static async Task<HttpResponseData> GetChatState(
       [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chats/{chatId}")] HttpRequestData req,
       string chatId,
       [AssistantQueryInput("{chatId}", TimestampUtc = "{Query.timestampUTC}")] AssistantState state,
       FunctionContext context)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(state);
        return response;
    }
}