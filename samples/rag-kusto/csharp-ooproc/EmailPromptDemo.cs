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

namespace SemanticSearchEmbeddings;

public class EmailPromptDemo
{
    public class EmbeddingsRequest
    {
        [JsonPropertyName("URL")]
        public string? URL { get; set; }

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
    [Function("IngestEmail")]
    public async Task<HttpResponseData> IngestEmail(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        [EmbeddingsStoreInput("{URL}", InputType.URL, "{Title}", "KustoConnectionString", "Documents", Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] EmbeddingsContext embeddings)
    {
        using StreamReader reader = new(req.Body);
        string request = await reader.ReadToEndAsync();

        EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

        if (requestBody == null || requestBody.URL == null || requestBody.Title == null)
        {
            throw new ArgumentException("Invalid request body. Make sure that you pass in {\"URL\": value , \"Title\": value} as the request body.");
        }

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { status = "success", requestBody.Title, chunks = embeddings.Count });
        return response;
    }

    [Function("PromptEmail")]
    public IActionResult PromptEmail(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearchInput("KustoConnectionString", "Documents", Query = "{Prompt}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
