// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;

/// <summary>
/// Connection info object.
/// </summary>
public class ConnectionInfo
{
    /// <summary>
    /// Connection info containing connection name, collection name, and credentials.
    /// </summary>
    /// <param name="connectionName">
    /// The name of an app setting or environment variable which contains a connection string value.
    /// </param>
    /// <param name="databaseName">
    /// The name of the database which has the collection.</param>
    /// </param>
    /// <param name="collectionName">
    /// The name of the collection or table to search or store.</param>
    /// </param>
    /// <param name="credentials">
    /// Credentials for authenticating with the search provider.</param>
    /// </param>
    public ConnectionInfo(
        string connectionName,
        string databaseName,
        string collectionName,
        string? credentials
    )
    {
        this.ConnectionName = connectionName;
        this.DatabaseName = databaseName;
        this.CollectionName = collectionName;
        this.Credentials = credentials;
    }

    /// <summary>
    /// Gets or sets the name of an app setting or environment variable which contains a connection string value.
    /// </summary>
    public string ConnectionName { get; set; }

    /// <summary>
    /// The name of the database which has the collection.
    /// </summary>
    public string DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection or table to search.
    /// </summary>
    public string CollectionName { get; set; }

    /// <summary>
    /// The name of the app setting or environment variable containing the required credentials
    /// for authenticating with the search provider. See the documentation for the search provider
    /// extension to know what format the underlying credential value requires.
    /// </summary>
    public string? Credentials { get; set; }
}
