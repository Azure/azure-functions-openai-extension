// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embedding;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;

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
