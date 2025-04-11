// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

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
public sealed class EmbeddingsAttribute : EmbeddingsBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingsAttribute"/> class with the specified input.
    /// </summary>
    /// <param name="input">The input source containing the data to generate embeddings for.</param>
    /// <param name="inputType">The type of the input.</param>
    /// <param name="aiConnectionName">The name of the configuration section for AI service connectivity settings.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <c>null</c>.</exception>
    public EmbeddingsAttribute(string input, InputType inputType, string aiConnectionName = "") : base(input, inputType, aiConnectionName)
    {
    }
}
