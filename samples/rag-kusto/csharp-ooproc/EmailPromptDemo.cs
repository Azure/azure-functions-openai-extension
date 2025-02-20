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
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class SemanticSearchRequest
    {
        [JsonPropertyName("prompt")]
        public string? Prompt { get; set; }
    }

    [Function("IngestEmail")]
    public async Task<EmbeddingsStoreOutputResponse> IngestEmail(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        using StreamReader reader = new(req.Body);
        string request = await reader.ReadToEndAsync();

        EmbeddingsStoreOutputResponse badRequestResponse = new()
        {
            HttpResponse = new BadRequestResult(),
            SearchableDocument = new SearchableDocument(string.Empty)
        };

        if (string.IsNullOrWhiteSpace(request))
        {
            return badRequestResponse;
        }

        EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

        if (string.IsNullOrWhiteSpace(requestBody?.Url))
        {
            throw new ArgumentException("Invalid request body. Make sure that you pass in {\"url\": value } as the request body.");
        }

        if (!Uri.TryCreate(requestBody.Url, UriKind.Absolute, out Uri? uri))
        {
            return badRequestResponse;
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
        [EmbeddingsStoreOutput("{url}", InputType.Url, "KustoConnectionString", "Documents", Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")]
        public required SearchableDocument SearchableDocument { get; init; }

        [HttpResult]
        public IActionResult? HttpResponse { get; set; }
    }

    [Function("PromptEmail")]
    public IActionResult PromptEmail(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearchInput("KustoConnectionString", "Documents", Query = "{prompt}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
