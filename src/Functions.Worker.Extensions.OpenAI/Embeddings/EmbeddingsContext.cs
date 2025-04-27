// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using OpenAI.Embeddings;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;

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
