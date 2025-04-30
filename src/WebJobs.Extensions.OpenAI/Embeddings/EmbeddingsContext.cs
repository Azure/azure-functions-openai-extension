// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using OpenAI.Embeddings;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;

/// <summary>
/// Binding target for the <see cref="EmbeddingsAttribute"/>.
/// </summary>
/// <param name="Request">The embeddings request that was sent to OpenAI.</param>
/// <param name="Response">The embeddings response that was received from OpenAI.</param>
public class EmbeddingsContext
{
    public EmbeddingsContext(IList<string> Request, OpenAIEmbeddingCollection? Response)
    {
        this.Request = Request;
        this.Response = Response;
    }

    /// <summary>
    /// Embeddings request sent to OpenAI.
    /// </summary>
    public IList<string> Request { get; set; }

    /// <summary>
    /// Embeddings response from OpenAI.
    /// </summary>
    public OpenAIEmbeddingCollection? Response { get; set; }

    /// <summary>
    /// Gets the number of embeddings that were returned in the response.
    /// </summary>
    public int Count => this.Response?.Count ?? 0;
}
