// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;

/// <summary>
/// Binding attribute for semantic search (input bindings).
/// </summary>
public sealed class SemanticSearchInputAttribute : InputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSearchInputAttribute"/> class with the specified connection
    /// and collection names.
    /// </summary>
    /// <param name="searchConnectionName">
    /// The name of an app setting or environment variable which contains a connection string value.
    /// </param>
    /// <param name="collection">The name of the collection or table to search or store.</param>
    /// <param name="aiConnectionName">The name of the configuration section for AI service connectivity settings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="collection"/> or <paramref name="searchConnectionName"/> are null.
    /// </exception>
    public SemanticSearchInputAttribute(string searchConnectionName, string collection, string aiConnectionName = "")
    {
        this.SearchConnectionName = searchConnectionName ?? throw new ArgumentNullException(nameof(searchConnectionName));
        this.Collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.AIConnectionName = aiConnectionName;
    }

    /// <summary>
    /// Gets or sets the name of the configuration section for AI service connectivity settings.
    /// </summary>
    /// <remarks>
    /// This property specifies the name of the configuration section that contains connection details for the AI service.
    /// 
    /// For Azure OpenAI:
    /// - If specified, looks for "Endpoint" and "Key" values in this configuration section
    /// - If not specified or the section doesn't exist, falls back to environment variables:
    ///   AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_KEY
    /// - For user-assigned managed identity authentication, this property is required
    /// 
    /// For OpenAI:
    /// - For OpenAI service (non-Azure), set the OPENAI_API_KEY environment variable.
    /// </remarks>
    public string AIConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the name of an app setting or environment variable which contains a connection string value.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string SearchConnectionName { get; set; }

    /// <summary>
    /// The name of the collection or table to search.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string Collection { get; set; }

    /// <summary>
    /// Gets or sets the semantic query text to use for searching.
    /// This property is only used for the semantic search input binding.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string? Query { get; set; }

    /// <summary>
    /// Gets or sets the ID of the model to use for embeddings.
    /// The default value is "text-embedding-ada-002".
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string EmbeddingsModel { get; set; } = OpenAIModels.DefaultEmbeddingsModel;

    /// <summary>
    /// Gets or sets the name of the Large Language Model to invoke for chat responses.
    /// The default value is "gpt-3.5-turbo".
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string ChatModel { get; set; } = OpenAIModels.DefaultChatModel;

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
    /// Gets or sets the sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output
    /// more random, while lower values like 0.2 will make it more focused and deterministic.
    /// </summary>
    /// <remarks>
    /// It's generally recommend to use this or <see cref="this.TopP"/> but not both.
    /// </remarks>
    public string? Temperature { get; set; } = "0.5";

    /// <summary>
    /// Gets or sets an alternative to sampling with temperature, called nucleus sampling, where the model considers
    /// the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10%
    /// probability mass are considered.
    /// </summary>
    /// <remarks>
    /// It's generally recommend to use this or <see cref="this.Temperature"/> but not both.
    /// </remarks>
    public string? TopP { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to output in the completion. Default is 2048.
    /// </summary>
    /// <remarks>
    /// The token count of your prompt plus max_tokens cannot exceed the model's context length.
    /// Most models have a context length of 2048 tokens (except for the newest models, which support 4096).
    /// </remarks>
    public string? MaxTokens { get; set; } = "2048";
}
