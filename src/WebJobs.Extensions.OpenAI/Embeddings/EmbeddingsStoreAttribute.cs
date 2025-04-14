// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class EmbeddingsStoreAttribute : EmbeddingsBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingsStoreAttribute"/> class with the specified connection
    /// and collection names.
    /// </summary>
    /// <param name="input">The input source containing the data to generate embeddings for
    /// and is interpreted based on the value for <paramref name="inputType"/>.</param>
    /// <param name="inputType">The type of the input.</param>
    /// <param name="storeConnectionName">
    /// The name of an app setting or environment variable which contains a connection string value.
    /// </param>
    /// <param name="collection">The name of the collection or table to search or store.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="input"/> or <paramref name="collection"/> or <paramref name="connectionName"/> are null.
    /// </exception>
    public EmbeddingsStoreAttribute(string input, InputType inputType, string storeConnectionName, string collection) : base(input, inputType)
    {
        this.StoreConnectionName = storeConnectionName ?? throw new ArgumentNullException(nameof(storeConnectionName));
        this.Collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    /// <summary>
    /// Gets or sets the name of an app setting or environment variable which contains a connection string value for embedding store.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string StoreConnectionName { get; set; }

    /// <summary>
    /// The name of the collection or table to search.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string Collection { get; set; }
}
