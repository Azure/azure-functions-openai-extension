using System.ComponentModel;
using Functions.Worker.Extensions.OpenAI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.ObjectModels.ResponseModels;

namespace CSharpIsolatedSamples;

/// <summary>
/// These samples show how to use the OpenAI Completions APIs. For more details on the Completions APIs, see
/// https://platform.openai.com/docs/guides/completion.
/// </summary>
public static class ChatBotIsolated
{
    [Serializable]
    public class CreateRequest
    {
        [JsonProperty("instructions")]
        public string? Instructions { get; set; }
    }

    [Function(nameof(CreateChatBot))]
    public static async Task<CreateChatBotOutputType> CreateChatBot(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "chats/{chatId}")] HttpRequestData req,
            string chatId,
            FunctionContext context)
    {
        var responseJson = new { chatId };
        var request = await new StreamReader(req.Body).ReadToEndAsync();

        var createRequestBody = JsonConvert.DeserializeObject<CreateRequest>(request);
        return new CreateChatBotOutputType
        {
            HttpResponse = new ObjectResult(responseJson) { StatusCode = 202 },
            ChatBotCreateRequest = new ChatBotCreateRequest2(chatId, createRequestBody.Instructions),

        };
    }

    public class CreateChatBotOutputType
    {

        [ChatBotCreateOutput()]
        public ChatBotCreateRequest2 ChatBotCreateRequest { get; set; }

        public IActionResult HttpResponse { get; set; }
    }

    [Function(nameof(PostUserResponse))]
    public static async Task<MyOutputType> PostUserResponse(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chats/{chatId}")] HttpRequestData req,
        string chatId)
    {
        string userMessage = await req.ReadAsStringAsync();
        if (string.IsNullOrEmpty(userMessage))
        {
            return new MyOutputType { HttpResponse = new BadRequestObjectResult(new { message = "Request body is empty" }) };
        }

        return new MyOutputType
        {
            HttpResponse = new AcceptedResult(),
            ChatBotPostRequest = new ChatBotPostRequest2 { UserMessage = userMessage, Id = chatId }
        };
    }

    public class MyOutputType
    {
        [ChatBotPostOutput("{chatId}")]
        public ChatBotPostRequest2 ChatBotPostRequest { get; set; }

        public IActionResult HttpResponse { get; set; }
    }

    [Function(nameof(GetChatState))]
    public static ChatBotState2 GetChatState(
       [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chats/{chatId}")] HttpRequest req,
       string chatId,
       [ChatBotQueryInput("{chatId}", TimestampUtc = "{Query.timestampUTC}")] ChatBotState2 state,
       FunctionContext context)
    {
        return state;
    }
}