// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;

namespace SemanticAISearchEmbeddings;

public static class FilePrompt
{
    public record EmbeddingsRequest(string FilePath);
    public record SemanticSearchRequest(string Prompt);

    // REVIEW: There are several assumptions about how the Embeddings binding and the SemanticSearch bindings
    //         work together. We should consider creating a higher-level of abstraction for this.
    [FunctionName("IngestFile")]
    public static async Task<IActionResult> IngestFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] EmbeddingsRequest req,
        [Embeddings("{FilePath}", InputType.FilePath, Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] EmbeddingsContext embeddings,
        [SemanticSearch("AISearchEndpoint", "openai-index", CredentialSettingName = "SearchAPIKey", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] IAsyncCollector<SearchableDocument> output)
    {
        string title = Path.GetFileNameWithoutExtension(req.FilePath);
        await output.AddAsync(new SearchableDocument(title, embeddings));
        return new OkObjectResult(new { status = "success", title, chunks = embeddings.Count });
    }

    [FunctionName("PromptFile")]
    public static IActionResult PromptFile(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearch("AISearchEndpoint", "openai-index", CredentialSettingName = "SearchAPIKey", Query = "{Prompt}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
