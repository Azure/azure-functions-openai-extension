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
        [JsonPropertyName("Url")]
        public string? Url { get; set; }
    }

    public class SemanticSearchRequest
    {
        [JsonPropertyName("Prompt")]
        public string? Prompt { get; set; }
    }

    [Function("IngestEmail")]
    public async Task<EmbeddingsStoreOutputResponse> IngestEmail(
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

        HttpResponseData response = req.CreateResponse(HttpStatusCode.Created);

        return new EmbeddingsStoreOutputResponse
        {
            HttpResponse = response,
            SearchableDocument = new SearchableDocument(filename)
        };
    }
    public class EmbeddingsStoreOutputResponse
    {
        [EmbeddingsStoreOutput("{url}", InputType.Url, "KustoConnectionString", "Documents", Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")]
        public SearchableDocument? SearchableDocument { get; set; }

        public HttpResponseData? HttpResponse { get; set; }
    }

    [Function("PromptEmail")]
    public IActionResult PromptEmail(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearchInput("KustoConnectionString", "Documents", Query = "{Prompt}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
