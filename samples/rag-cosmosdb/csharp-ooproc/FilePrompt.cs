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
        [JsonPropertyName("URL")]
        public string? URL { get; set; }
    }

    public class SemanticSearchRequest
    {
        [JsonPropertyName("Prompt")]
        public string? Prompt { get; set; }
    }

    // REVIEW: There are several assumptions about how the Embeddings binding and the SemanticSearch bindings
    //         work together. We should consider creating a higher-level of abstraction for this.
    [Function("IngestFile")]
    public static async Task<SemanticSearchOutputResponse> IngestFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        [EmbeddingsInput("{URL}", InputType.URL, Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] EmbeddingsContext embeddings)
    {
        using StreamReader reader = new(req.Body);
        string request = await reader.ReadToEndAsync();

        EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

        if (requestBody == null || requestBody.URL == null)
        {
            throw new ArgumentException("Invalid request body. Make sure that you pass in {\"URL\": value } as the request body.");
        }

        Uri uri = new(uriString: requestBody.URL);
        string title = Path.GetFileNameWithoutExtension(uri.LocalPath);

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { status = "success", title, chunks = embeddings.Count });

        return new SemanticSearchOutputResponse
        {
            HttpResponse = response,
            SearchableDocument = new SearchableDocument(title, embeddings)
        };
    }

    public class SemanticSearchOutputResponse
    {
        [SemanticSearchOutput("CosmosDBMongoVCoreConnectionString", "openai-index", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")]
        public SearchableDocument? SearchableDocument { get; set; }

        public HttpResponseData? HttpResponse { get; set; }
    }

    [Function("PromptFile")]
    public static IActionResult PromptFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearchInput("CosmosDBMongoVCoreConnectionString", "openai-index", Query = "{Prompt}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
