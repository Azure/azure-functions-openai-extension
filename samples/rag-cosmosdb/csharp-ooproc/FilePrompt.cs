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

        [JsonPropertyName("Title")]
        public string? Title { get; set; }
    }

    public class SemanticSearchRequest
    {
        [JsonPropertyName("Prompt")]
        public string? Prompt { get; set; }
    }

    // REVIEW: There are several assumptions about how the Embeddings binding and the SemanticSearch bindings
    //         work together. We should consider creating a higher-level of abstraction for this.
    [Function("IngestFile")]
    public static async Task<HttpResponseData> IngestFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        [EmbeddingsStoreInput("{Url}", InputType.Url, "{Title}", "CosmosDBMongoVCoreConnectionString", "openai-index", Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] EmbeddingsContext embeddings)
    {
        using StreamReader reader = new(req.Body);
        string request = await reader.ReadToEndAsync();

        EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

        if (requestBody == null || requestBody.Url == null || requestBody.Title == null)
        {
            throw new ArgumentException("Invalid request body. Make sure that you pass in {\"Url\": value , \"Title\": value} as the request body.");
        }

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { status = "success", requestBody.Title, chunks = embeddings.Count });
        return response;
    }

    [Function("PromptFile")]
    public static IActionResult PromptFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearchInput("CosmosDBMongoVCoreConnectionString", "openai-index", Query = "{Prompt}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
