// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

/// <summary>
/// Binding target for the <see cref="EmbeddingsAttribute"/>.
/// </summary>
/// <param name="Request">The embeddings request that was sent to OpenAI.</param>
/// <param name="Response">The embeddings response that was received from OpenAI.</param>
public record EmbeddingsContext(EmbeddingsOptions Request, Response<Embeddings> Response)
{
    /// <summary>
    /// Gets the number of embeddings that were returned in the response.
    /// </summary>
    public int Count => this.Response.Value?.Data?.Count ?? 0;
}
