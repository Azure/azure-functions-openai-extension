// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;
using Microsoft.Azure.Functions.Worker.Http;

namespace CosmosDBSearchEmbeddings;

public static class FilePrompt
{
    public class EmbeddingsRequest
    {
        [JsonPropertyName("Url")]
        public string? Url { get; set; }
    }

    public class SemanticSearchRequest
    {
        [JsonPropertyName("Prompt")]
        public string? Prompt { get; set; }
    }

    [Function("IngestFile")]
    public static async Task<EmbeddingsStoreOutputResponse> IngestFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        using StreamReader reader = new(req.Body);
        string request = await reader.ReadToEndAsync();

        EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

        if (requestBody == null || requestBody.Url == null)
        {
            throw new ArgumentException("Invalid request body. Make sure that you pass in {\"Url\": value } as the request body.");
        }

        Uri uri = new(requestBody.Url);
        string filename = Path.GetFileName(uri.AbsolutePath);

        IActionResult result = new OkObjectResult(new { status = HttpStatusCode.OK });

        return new EmbeddingsStoreOutputResponse
        {
            HttpResponse = result,
            SearchableDocument = new SearchableDocument(filename)
        };
    }

    public class EmbeddingsStoreOutputResponse
    {
        [EmbeddingsStoreOutput("{Url}", InputType.Url, "CosmosDBMongoVCoreConnectionString", "openai-index", Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")]
        public required SearchableDocument SearchableDocument { get; init; }

        public IActionResult? HttpResponse { get; set; }
    }

    [Function("PromptFile")]
    public static IActionResult PromptFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearchInput("CosmosDBMongoVCoreConnectionString", "openai-index", Query = "{Prompt}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
