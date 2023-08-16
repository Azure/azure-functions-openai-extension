// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using WebJobs.Extensions.OpenAI;

namespace CSharpInProcSamples;

/// <summary>
/// Examples of working with OpenAI embeddings.
/// </summary>
public class EmbeddingsGenerator
{
    public record EmbeddingsRequest(string RawText, string FilePath);

    /// <summary>
    /// Example showing how to use the <see cref="EmbeddingsAttribute"/> input binding to generate embeddings 
    /// for a raw text string.
    /// </summary>
    [FunctionName(nameof(GenerateEmbeddings_Http_Request))]
    public static void GenerateEmbeddings_Http_Request(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings")] EmbeddingsRequest req,
        [Embeddings("{RawText}", InputType.RawText)] EmbeddingsContext embeddings,
        ILogger logger)
    {
        logger.LogInformation(
            "Received {count} embedding(s) for input text containing {length} characters.",
            embeddings.Response.Data.Count,
            req.RawText.Length);

        // TODO: Store the embeddings into a database or other storage.
    }

    /// <summary>
    /// Example showing how to use the <see cref="EmbeddingsAttribute"/> input binding to generate embeddings
    /// for text contained in a file on the file system.
    /// </summary>
    [FunctionName(nameof(GetEmbeddings_Http_FilePath))]
    public static void GetEmbeddings_Http_FilePath(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings-from-file")] EmbeddingsRequest req,
        [Embeddings("{FilePath}", InputType.FilePath, MaxChunkLength = 512)] EmbeddingsContext embeddings,
        ILogger logger)
    {
        logger.LogInformation(
            "Received {count} embedding(s) for input file '{path}'.",
            embeddings.Response.Data.Count,
            req.FilePath);

        // TODO: Store the embeddings into a database or other storage.
    }
}
