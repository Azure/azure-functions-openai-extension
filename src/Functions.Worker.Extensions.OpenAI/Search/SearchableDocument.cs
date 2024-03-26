// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;

/// <summary>
/// Searchable document containing the title and embeddings context.
/// </summary>
public class SearchableDocument
{
    public SearchableDocument(string title, EmbeddingsContext embeddingsContext)
    {
        this.Title = title;
        this.EmbeddingsContext = embeddingsContext;
    }
    public ConnectionInfo? ConnectionInfo { get; set; }
    public string Title { get; }
    public EmbeddingsContext EmbeddingsContext { get; }
}
