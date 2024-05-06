// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;

/// <summary>
/// Input binding attribute for converting function trigger input into OpenAI embeddings.
/// </summary>
/// <remarks>
/// More information on OpenAI embeddings can be found at
/// https://platform.openai.com/docs/guides/embeddings/what-are-embeddings.
/// </remarks>
[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public class EmbeddingsBaseAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingsBaseAttribute"/> class with the specified input.
    /// </summary>
    /// <param name="input">The input source containing the data to generate embeddings for.</param>
    /// <param name="inputType">The type of the input.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <c>null</c>.</exception>
    public EmbeddingsBaseAttribute(string input, InputType inputType)
    {
        this.Input = input ?? throw new ArgumentNullException(nameof(input));
        this.InputType = inputType;
    }

    /// <summary>
    /// Gets or sets the ID of the model to use.
    /// </summary>
    /// <remarks>
    /// Changing the default embeddings model is a breaking change, since any changes will be stored in a vector database for lookup. Changing the default model can cause the lookups to start misbehaving if they don't match the data that was previously ingested into the vector database.
    /// </remarks>
    [AutoResolve]
    public string Model { get; set; } = OpenAIModels.DefaultEmbeddingsModel;

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
    [AutoResolve]
    public string Input { get; }

    /// <summary>
    /// Gets the type of the input.
    /// </summary>
    public InputType InputType { get; }
}
