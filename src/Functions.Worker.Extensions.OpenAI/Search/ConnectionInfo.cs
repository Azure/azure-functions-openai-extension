// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;

/// <summary>
/// Connection info containing connection name, collection name, and credentials.
/// </summary>
public class ConnectionInfo
{
    public ConnectionInfo(string ConnectionName, string CollectionName, string? Credentials)
    {
        this.ConnectionName = ConnectionName;
        this.CollectionName = CollectionName;
        this.Credentials = Credentials;
    }

    public string ConnectionName { get; set; }
    public string CollectionName { get; set; }
    public string? Credentials { get; set; }
}
