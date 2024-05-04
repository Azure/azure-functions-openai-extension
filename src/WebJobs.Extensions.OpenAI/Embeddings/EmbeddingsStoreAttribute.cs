// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class EmbeddingsStoreAttribute : EmbeddingsBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingsStoreAttribute"/> class with the specified connection
    /// and collection names.
    /// </summary>
    /// <param name="input">The input source containing the data to generate embeddings for.</param>
    /// <param name="inputType">The type of the input.</param>
    /// <param name="connectionName">
    /// The name of an app setting or environment variable which contains a connection string value.
    /// </param>
    /// <param name="collection">The name of the collection or table to search or store.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="input"/> or <paramref name="collection"/> or <paramref name="connectionName"/> are null.
    /// </exception>
    public EmbeddingsStoreAttribute(string input, InputType inputType, string connectionName, string collection) : base(input, inputType)
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
    public string ConnectionName { get; set; }

    /// <summary>
    /// The name of the collection or table to search.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string Collection { get; set; }
}
