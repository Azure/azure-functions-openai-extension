﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;

/// <summary>
/// Interface for search store providers to implement.
/// </summary>
public interface ISearchProvider
{
    /// <summary>
    /// Adds a document to a search provider index.
    /// </summary>
    /// <param name="document">The document metadata.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Returns a task that completes when the document is successfully saved.</returns>
    Task AddDocumentAsync(SearchableDocument document, CancellationToken cancellationToken);

    /// <summary>
    /// Performs a semantic search for documents in a search provider index.
    /// </summary>
    /// <param name="request">Information about the search.</param>
    /// <returns>The results of the search.</returns>
    Task<SearchResponse> SearchAsync(SearchRequest request);
}

public record SearchableDocument(
    string Title,
    EmbeddingsContext Embeddings)
{
    public ConnectionInfo? ConnectionInfo { get; set; }
}

public record ConnectionInfo(string ConnectionName, string CollectionName, string? Credentials);

public record SearchRequest(
    string Query,
    ReadOnlyMemory<float> Embeddings,
    int MaxResults,
    ConnectionInfo ConnectionInfo);

public record SearchResponse(IReadOnlyList<SearchResult> OrderedResults);
