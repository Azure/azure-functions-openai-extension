// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;

namespace WebJobs.Extensions.OpenAI;

/// <summary>
/// Binding target for the <see cref="EmbeddingsAttribute"/>.
/// </summary>
/// <param name="Request">The embeddings request that was sent to OpenAI.</param>
/// <param name="Response">The embeddings response that was received from OpenAI.</param>
public record EmbeddingsContext(EmbeddingCreateRequest Request, EmbeddingCreateResponse Response)
{
    /// <summary>
    /// Gets the number of embeddings that were returned in the response.
    /// </summary>
    public int Count => this.Response.Data.Count;
}
