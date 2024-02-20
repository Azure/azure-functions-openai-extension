using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Functions.Worker.Extensions.OpenAI;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CSharpIsolatedSamples;

/// <summary>
/// The ChatBot sample allows you to create chat bots with a specified set of initial instructions.
/// </summary>
public static class ChatBotIsolated
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

        HttpResponseData response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(responseJson);

        return new CreateChatBotOutput
        {
            HttpResponse = response,
            ChatBotCreateRequest = new ChatBotCreateRequest(chatId, createRequestBody.Instructions),
        };
    }

    public class CreateChatBotOutput
    {
        [ChatBotCreateOutput()]
        public ChatBotCreateRequest? ChatBotCreateRequest { get; set; }

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
            ChatBotPostRequest = new ChatBotPostRequest { UserMessage = userMessage, Id = chatId }
        };
    }

    public class PostResponseOutput
    {
        [ChatBotPostOutput("{chatId}", Model = "gpt-3.5-turbo")]
        public ChatBotPostRequest? ChatBotPostRequest { get; set; }

        public HttpResponseData? HttpResponse { get; set; }
    }

    [Function(nameof(GetChatState))]
    public static async Task<HttpResponseData> GetChatState(
       [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chats/{chatId}")] HttpRequestData req,
       string chatId,
       [ChatBotQueryInput("{chatId}", TimestampUtc = "{Query.timestampUTC}")] ChatBotState state,
       FunctionContext context)
    {
        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(state);
        return response;
    }
}