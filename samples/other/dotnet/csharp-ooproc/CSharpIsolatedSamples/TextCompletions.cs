using System.ComponentModel;
using Functions.Worker.Extensions.OpenAI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OpenAI.ObjectModels.ResponseModels;

namespace CSharpIsolatedSamples;

/// <summary>
/// These samples show how to use the OpenAI Completions APIs. For more details on the Completions APIs, see
/// https://platform.openai.com/docs/guides/completion.
/// </summary>
public static class TextCompletions
{
    public record CreateRequest(string Instructions);
    public record EmbeddingsRequest(string RawText, string FilePath);

    public record SemanticSearchRequest(string Prompt);
    /// <summary>
    /// This sample demonstrates the "templating" pattern, where the function takes a parameter
    /// and embeds it into a text prompt, which is then sent to the OpenAI completions API.
    /// </summary>
    [Function(nameof(WhoIs))]
    public static string WhoIs(
        [HttpTrigger(AuthorizationLevel.Anonymous, Route = "whois/{name}")] HttpRequestData req,
        [TextCompletionInput("Who is {name}?", Model = "gpt-3.5-turbo-instruct")] CompletionCreateResponse response)
    {
        return response.Choices[0].Text;
    }

    /// <summary>
    /// This sample takes a prompt as input, sends it directly to the OpenAI completions API, and results the 
    /// response as the output.
    /// </summary>
    [Function(nameof(GenericCompletion))]
    public static IActionResult GenericCompletion(
        [HttpTrigger(AuthorizationLevel.Function, "post")] PromptPayload payload,
        [TextCompletionInput("{Prompt}")] CompletionCreateResponse response,
        ILogger log)
    {
        if (!response.Successful)
        {
            Error error = response.Error ?? new Error() { MessageObject = "OpenAI returned an unspecified error" };
            return new ObjectResult(error) { StatusCode = 500 };
        }

        log.LogInformation("Prompt = {prompt}, Response = {response}", payload.Prompt, response);
        string text = response.Choices[0].Text;
        return new OkObjectResult(text);
    }

    [Function(nameof(CreateChatBot))]
    public static async Task<CreateChatBotOutputType> CreateChatBot(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "chats/{chatId}")] CreateRequest req,
            string chatId,
            FunctionContext  context)
    {
        var responseJson = new { chatId };
        return new CreateChatBotOutputType
        { 
            HttpResponse = new ObjectResult(responseJson) { StatusCode = 202 },
            ChatBotCreateRequest = new ChatBotCreateRequest2(chatId, req.Instructions),

        };
    }

    public class CreateChatBotOutputType
    {

        [ChatBotCreateOutput()]
        public ChatBotCreateRequest2 ChatBotCreateRequest { get; set; }

        public IActionResult HttpResponse { get; set; }
    }

    /*
    [Function("PromptEmail")]
    public static IActionResult PromptEmail(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearchInput("KustoConnectionString", "Documents", Query = "{Prompt}")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
    */

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

    public record PromptPayload(string Prompt);
}