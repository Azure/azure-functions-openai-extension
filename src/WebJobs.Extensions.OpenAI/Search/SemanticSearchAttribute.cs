// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.WebJobs.Description;
using OpenAI.GPT3.ObjectModels;

namespace WebJobs.Extensions.OpenAI.Search;

/// <summary>
/// Binding attribute for semantic search (input bindings) and semantic document storage (output bindings).
/// </summary>
[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public class SemanticSearchAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSearchAttribute"/> class with the specified connection
    /// and collection names.
    /// </summary>
    /// <param name="connectionName">
    /// The name of an app setting or environment variable which contains a connection string value.
    /// </param>
    /// <param name="collection">The name of the collection or table to search or store.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="collection"/> or <paramref name="connectionName"/> are null.
    /// </exception>
    public SemanticSearchAttribute(string connectionName, string collection)
    {
        this.ConnectionName = connectionName ?? throw new ArgumentNullException(nameof(connectionName));
        this.Collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    /// <summary>
    /// Gets or sets the name of an app setting or environment variable which contains a connection string value.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    [AutoResolve]
    public string ConnectionName { get; set; }

    /// <summary>
    /// The name of the collection or table to search.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    [AutoResolve]
    public string Collection { get; set; }

    /// <summary>
    /// Gets or sets the semantic query text to use for searching.
    /// This property is only used for the semantic search input binding.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    [AutoResolve]
    public string? Query { get; set; }

    /// <summary>
    /// Gets or sets the ID of the model to use for embeddings.
    /// The default value is "text-embedding-ada-002".
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    [AutoResolve]
    public string EmbeddingsModel { get; set; } = Models.TextEmbeddingAdaV2;

    /// <summary>
    /// Gets or sets the name of the Large Language Model to invoke for chat responses.
    /// The default value is "gpt-3.5-turbo".
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    [AutoResolve]
    public string ChatModel { get; set; } = Models.ChatGpt3_5Turbo;

    /// <summary>
    /// Gets or sets the system prompt to use for prompting the large language model.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The system prompt will be appended with knowledge that is fetched as a result of the <see cref="Query"/>.
    /// The combined prompt will then be sent to the OpenAI Chat API.
    /// </para><para>
    /// This property supports binding expressions.
    /// </para>
    /// </remarks>
    [AutoResolve]
    public string SystemPrompt { get; set; } = """
        You are a helpful assistant. You are responding to requests from a user about internal emails and documents.
        You can and should refer to the internal documents to help respond to requests. If a user makes a request that's
        not covered by the internal emails and documents, explain that you don't know the answer or that you don't have
        access to the information.

        The following is a list of documents that you can refer to when answering questions. The documents are in the format
        [filename]: [text] and are separated by newlines. If you answer a question by referencing any of the documents,
        please cite the document in your answer. For example, if you answer a question by referencing info.txt,
        you should add "Reference: info.txt" to the end of your answer on a separate line.

        """;

    /// <summary>
    /// Gets or sets the number of knowledge items to inject into the <see cref="SystemPrompt"/>.
    /// </summary>
    public int MaxKnowledgeCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether the binding should throw if there is an error calling the OpenAI
    /// endpoint.
    /// </summary>
    /// <remarks>
    /// The default value is <c>true</c>. Set this to <c>false</c> to handle errors manually in the function code.
    /// </remarks>
    public bool ThrowOnError { get; set; } = true;
}
