// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;

/// <summary>
/// Searchable document which contains the title and embeddings context.
/// </summary>
public class SearchableDocument
{
    /// <summary>
    /// Searchable document containing the title and embeddings context.
    /// </summary>
    /// <param name="title">
    /// Title of the searchable document.
    /// </param>
    /// <param name="embeddingsContext">
    /// The embeddings context associated with the searchable document.
    /// </param>
    public SearchableDocument(string title, EmbeddingsContext embeddingsContext)
    {
        this.Title = title;
        this.EmbeddingsContext = embeddingsContext;
    }

    /// <summary>
    /// Connection info for the searchable document.
    /// </summary>
    public ConnectionInfo? ConnectionInfo { get; set; }

    /// <summary>
    /// Title of the searchable document.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Embeddings context that contains embedings request and response from OpenAI for searchable document.
    /// </summary>
    public EmbeddingsContext EmbeddingsContext { get; }
}
