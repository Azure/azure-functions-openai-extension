// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;

/// <summary>
/// Input binding target for semantic search.
/// </summary>
public class SemanticSearchContext
{
    /// <summary>
    /// Input binding target for the <see cref="SemanticSearchAttribute"/>.
    /// </summary>
    /// <param name="Embeddings">The embeddings context associated with the semantic search.</param>
    /// <param name="Chat">The chat response from the large language model.</param>
    public SemanticSearchContext(EmbeddingsContext Embeddings, ChatCompletions Chat)
    {
        this.Embeddings = Embeddings;
        this.Chat = Chat;
        
    }

    /// <summary>
    /// Gets the embeddings context associated with the semantic search.
    /// </summary>
    [JsonPropertyName("embeddings")]
    public EmbeddingsContext Embeddings { get; }

    /// <summary>
    /// Gets the chat response from the large language model.
    /// </summary>
    [JsonPropertyName("chat")]
    public ChatCompletions Chat { get; }


    /// <summary>
    /// Gets the latest response message from the OpenAI Chat API.
    /// </summary>
    [JsonPropertyName("response")]
    public string Response => this.Chat.Choices.Last().Message.Content;
 }
