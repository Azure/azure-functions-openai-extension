// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;
using Microsoft.Extensions.Logging;

namespace SemanticAISearchEmbeddings;

public class FilePrompt
{
    readonly ILogger _logger;

    public FilePrompt(ILoggerFactory loggerFactory)
    {
        this._logger = loggerFactory.CreateLogger<FilePrompt>();
    }

    [Function("IngestFile")]
    public async Task<EmbeddingsStoreOutputResponse> IngestFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] EmbeddingsRequest body 
    )
    {

        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            var jsonBody = await reader.ReadToEndAsync();
            this._logger.LogInformation(jsonBody); // Log the JSON body
            // deserialize the JSON body into the EmbeddingsRequest object
            try
            {
                body = JsonSerializer.Deserialize<EmbeddingsRequest>(jsonBody);
            }
            catch (JsonException ex)
            {
                this._logger.LogInformation($"Message: {ex.Message}");
                this._logger.LogInformation($"StackTrace: {ex.StackTrace}");
            }
        }
        if (body == null || body.Url == null)
        {
            throw new ArgumentException(
                "Invalid request body. Make sure that you pass in {\"Url\": value } as the request body."
            );
        }

        Uri uri = new(body.Url);
        string filename = Path.GetFileName(uri.AbsolutePath);

        return new EmbeddingsStoreOutputResponse
        {
            HttpResponse = new OkObjectResult("Ingested file"),
            SearchableDocument = new SearchableDocument(filename)
        };
    }

    [Function("PromptFile")]
    public IActionResult PromptFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearchInput(
            "AISearchEndpoint",
            "openai-index",
            Query = "{Prompt}",
            ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%",
            EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%"
        )]
            SemanticSearchContext result
    )
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }

    public class EmbeddingsStoreOutputResponse
    {
        [EmbeddingsStoreOutput(
            "{Url}",
            InputType.Url,
            "AISearchEndpoint",
            "openai-index",
            Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%"
        )]
        public required SearchableDocument SearchableDocument { get; init; }

        [HttpResult]
        public IActionResult? HttpResponse { get; set; }
    }

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
}
