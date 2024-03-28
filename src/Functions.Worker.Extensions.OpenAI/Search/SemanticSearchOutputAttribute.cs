// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;

/// <summary>
/// Binding attribute for semantic document storage (output bindings).
/// </summary>
public class SemanticSearchOutputAttribute : OutputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSearchInputAttribute"/> class with the specified connection
    /// and collection names.
    /// </summary>
    /// <param name="connectionName">
    /// The name of an app setting or environment variable which contains a connection string value.
    /// </param>
    /// <param name="collection">The name of the collection or table to search or store.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="collection"/> or <paramref name="connectionName"/> are null.
    /// </exception>
    public SemanticSearchOutputAttribute(string connectionName, string collection)
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

    /// <summary>
    /// The name of the app setting or environment variable containing the required credentials 
    /// for authenticating with the search provider. See the documentation for the search provider
    /// extension to know what format the underlying credential value requires.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string? CredentialSettingName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the model to use for embeddings.
    /// The default value is "text-embedding-3-small".
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string EmbeddingsModel { get; set; } = OpenAIModels.DefaultEmbeddingsModel;
}
