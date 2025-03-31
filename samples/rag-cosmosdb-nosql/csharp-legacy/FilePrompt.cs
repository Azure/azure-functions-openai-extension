// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;

namespace CosmosDBNoSQLSearchLegacy;

public static class FilePrompt
{
    public record EmbeddingsRequest(string Url);
    public record SemanticSearchRequest(string Prompt);

    [FunctionName("IngestFile")]
    public static async Task<IActionResult> IngestFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] EmbeddingsRequest req,
        [EmbeddingsStore("{url}", InputType.Url, "CosmosDBNoSql", "openai-index", Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")]
        IAsyncCollector<SearchableDocument> output)
    {
        if (string.IsNullOrWhiteSpace(req.Url))
        {
            throw new ArgumentException("Invalid request body. Make sure that you pass in {\"url\": value } as the request body.");
        }

        if (!Uri.TryCreate(req.Url, UriKind.Absolute, out Uri? uri))
        {
            return new BadRequestResult();
        }

        string title = Path.GetFileName(uri.AbsolutePath);

        await output.AddAsync(new SearchableDocument(title));
        return new OkObjectResult(new { status = "success", title });
    }

    [FunctionName("PromptFile")]
    public static IActionResult PromptFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearch("CosmosDBNoSql", "openai-index", Query = "{Prompt}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
