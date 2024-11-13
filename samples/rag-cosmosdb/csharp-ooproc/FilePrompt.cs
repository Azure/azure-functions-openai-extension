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
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class SemanticSearchRequest
    {
        [JsonPropertyName("prompt")]
        public string? Prompt { get; set; }
    }

    [Function("IngestFile")]
    public static async Task<EmbeddingsStoreOutputResponse> IngestFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        ArgumentNullException.ThrowIfNull(req);

        using StreamReader reader = new(req.Body);
        string request = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(request))
        {
            throw new ArgumentException("Request body is empty.");
        }

        EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

        if (string.IsNullOrWhiteSpace(requestBody?.Url))
        {
            throw new ArgumentException("Invalid request body. Make sure that you pass in {\"url\": value } as the request body.");
        }

        if (!Uri.TryCreate(requestBody.Url, UriKind.Absolute, out Uri? uri))
        {
            throw new ArgumentException("Invalid Url format.");
        }

        string filename = Path.GetFileName(uri.AbsolutePath);

        return new EmbeddingsStoreOutputResponse
        {
            HttpResponse = new OkObjectResult(new { status = HttpStatusCode.OK }),
            SearchableDocument = new SearchableDocument(filename)
        };
    }

    public class EmbeddingsStoreOutputResponse
    {
        [EmbeddingsStoreOutput("{url}", InputType.Url, "CosmosDBMongoVCoreConnectionString", "openai-index", Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")]
        public required SearchableDocument SearchableDocument { get; init; }

        public IActionResult? HttpResponse { get; set; }
    }

    [Function("PromptFile")]
    public static IActionResult PromptFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearchInput("CosmosDBMongoVCoreConnectionString", "openai-index", Query = "{prompt}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
