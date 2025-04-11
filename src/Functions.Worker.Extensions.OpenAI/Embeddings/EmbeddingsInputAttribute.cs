﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;

public class EmbeddingsInputAttribute : InputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingsInputAttribute"/> class with the specified input.
    /// </summary>
    /// <param name="input">The input source containing the data to generate embeddings for.</param>
    /// <param name="inputType">The type of the input.</param>
    /// <param name="aiConnectionName">The name of the configuration section for AI service connectivity settings.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <c>null</c>.</exception>
    public EmbeddingsInputAttribute(string input, InputType inputType, string aiConnectionName = "")
    {
        this.Input = input ?? throw new ArgumentNullException(nameof(input));
        this.InputType = inputType;
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
    /// Gets or sets the ID of the model to use.
    /// </summary>
    /// <remarks>
    /// Changing the default embeddings model is a breaking change, since any changes will be stored in a vector database for lookup. Changing the default model can cause the lookups to start misbehaving if they don't match the data that was previously ingested into the vector database.
    /// </remarks>
    public string EmbeddingsModel { get; set; } = OpenAIModels.DefaultEmbeddingsModel;

    /// <summary>
    /// Gets or sets the maximum number of characters to chunk the input into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At the time of writing, the maximum input tokens allowed for second-generation input embedding models
    /// like <c>text-embedding-ada-002</c> is 8191. 1 token is ~4 chars in English, which translates to roughly 32K 
    /// characters of English input that can fit into a single chunk.
    /// </para>
    /// </remarks>
    public int MaxChunkLength { get; set; } = 8 * 1024; // REVIEW: Is 8K a good default?

    /// <summary>
    /// Gets or sets the maximum number of characters to overlap between chunks.
    /// </summary>
    public int MaxOverlap { get; set; } = 128;

    /// <summary>
    /// Gets the input to generate embeddings for.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Gets the type of the input.
    /// </summary>
    public InputType InputType { get; }
}
