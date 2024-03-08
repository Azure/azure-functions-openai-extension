// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embedding;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;
public class SemanticSearchContext
{
    /// <summary>
    /// Input binding target for the <see cref="SemanticSearchAttribute"/>.
    /// </summary>
    /// <param name="Embeddings">The embeddings context associated with the semantic search.</param>
    /// <param name="Chat">The chat response from the large language model.</param>
    public SemanticSearchContext(EmbeddingsContext Embeddings, Response<ChatCompletions> Chat)
    {
        this.Embeddings = Embeddings;
        this.Chat = Chat;
        
    }

    public EmbeddingsContext Embeddings { get; }

    public Response<ChatCompletions> Chat { get; }

    
    /// <summary>
    /// Gets the latest response message from the OpenAI Chat API.
    /// </summary>
    public string Response => this.Chat.Value.Choices.Last().Message.Content;
 }
