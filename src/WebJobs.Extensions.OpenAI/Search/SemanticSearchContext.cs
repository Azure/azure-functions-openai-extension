// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;

/// <summary>
/// Input binding target for the <see cref="SemanticSearchAttribute"/>.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
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
    /// Embeddings context that contains embedings request and response from OpenAI for searchable document.
    /// </summary>
    [JsonProperty("embeddings")]
    public EmbeddingsContext Embeddings { get; }

    /// <summary>
    /// Chat response from the chat completions request.
    /// </summary>
    [JsonProperty("chat")]
    public ChatCompletions Chat { get; }

    /// <summary>
    /// Gets the latest response message from the OpenAI Chat API.
    /// </summary>
    [JsonProperty("response")]
    public string Response => this.Chat.Choices.Last().Message.Content;
}
