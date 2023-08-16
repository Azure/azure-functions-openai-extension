// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using WebJobs.Extensions.OpenAI;
using WebJobs.Extensions.OpenAI.Search;

namespace CSharpInProcSamples.Demos;

public static class EmailPromptDemo
{
    public record EmbeddingsRequest(string FilePath);
    public record SemanticSearchRequest(string Prompt);

    // REVIEW: There are several assumptions about how the Embeddings binding and the SemanticSearch bindings
    //         work together. We should consider creating a higher-level of abstraction for this.
    [FunctionName("IngestEmail")]
    public static async Task<IActionResult> IngestEmail_Better(
        [HttpTrigger(AuthorizationLevel.Function, "post")] EmbeddingsRequest req,
        [Embeddings("{FilePath}", InputType.FilePath)] EmbeddingsContext embeddings,
        [SemanticSearch("KustoConnectionString", "Documents")] IAsyncCollector<SearchableDocument> output)
    {
        string title = Path.GetFileNameWithoutExtension(req.FilePath);
        await output.AddAsync(new SearchableDocument(title, embeddings));
        return new OkObjectResult(new { status = "success", title, chunks = embeddings.Count });
    }

    [FunctionName("PromptEmail")]
    public static IActionResult PromptEmail(
        [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
        [SemanticSearch("KustoConnectionString", "Documents", Query = "{Prompt}")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
