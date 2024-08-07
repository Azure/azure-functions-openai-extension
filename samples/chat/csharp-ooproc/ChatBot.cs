using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
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
        CreateRequest? createRequestBody;
        try
        {
            using StreamReader reader = new(req.Body);

            string request = await reader.ReadToEndAsync();

            createRequestBody = JsonSerializer.Deserialize<CreateRequest>(request);

        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid request body. Make sure that you pass in {\"instructions\": value } as the request body.", ex.Message);
        }

        return new CreateChatBotOutput
        {
            HttpResponse = new ObjectResult(responseJson) { StatusCode = 201 },
            ChatBotCreateRequest = new AssistantCreateRequest(chatId, createRequestBody?.Instructions)
            {
                ChatStorageConnectionSetting = "AzureWebJobsStorage",
                CollectionName = "SampleChatState"
            },
        };
    }

    public class CreateChatBotOutput
    {
        [AssistantCreateOutput()]
        public AssistantCreateRequest? ChatBotCreateRequest { get; set; }

        [HttpResult]
        public IActionResult? HttpResponse { get; set; }
    }

    [Function(nameof(PostUserResponse))]
    public static async Task<IActionResult> PostUserResponse(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chats/{chatId}")] HttpRequestData req,
        string chatId,
        [AssistantPostInput("{chatId}", "{Query.message}", Model = "%CHAT_MODEL_DEPLOYMENT_NAME%")] AssistantState state)
    {
        return new OkObjectResult(state.RecentMessages.LastOrDefault()?.Content ?? "No response returned.");
    }

    [Function(nameof(GetChatState))]
    public static async Task<IActionResult> GetChatState(
       [HttpTrigger(AuthorizationLevel.Function, "get", Route = "chats/{chatId}")] HttpRequestData req,
       string chatId,
       [AssistantQueryInput("{chatId}", TimestampUtc = "{Query.timestampUTC}")] AssistantState state,
       FunctionContext context)
    {
        return new OkObjectResult(state);
    }
}