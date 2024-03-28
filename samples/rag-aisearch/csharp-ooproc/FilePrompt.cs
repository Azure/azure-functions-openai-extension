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

namespace SemanticAISearchEmbeddings;

public static class FilePrompt
{
    public class EmbeddingsRequest
    {
        [JsonPropertyName("FilePath")]
        public string? FilePath { get; set; }
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
        [EmbeddingsInput("{FilePath}", InputType.FilePath, Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] EmbeddingsContext embeddings)
    {
        using StreamReader reader = new(req.Body);
        string request = await reader.ReadToEndAsync();

        EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

        if (requestBody == null)
        {
            throw new ArgumentException("Invalid request body. Make sure that you pass in {\"filePath\": value } as the request body.");
        }

        string title = Path.GetFileNameWithoutExtension(requestBody.FilePath);

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
        [SemanticSearchOutput("AISearchEndpoint", "openai-index", CredentialSettingName = "SearchAPIKey", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")]
        public SearchableDocument SearchableDocument { get; set; }

        public HttpResponseData? HttpResponse { get; set; }
    }

    [Function("PromptFile")]
    public static IActionResult PromptFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearchInput("AISearchEndpoint", "openai-index", CredentialSettingName = "SearchAPIKey", Query = "{Prompt}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
