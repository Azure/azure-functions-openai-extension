// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Data.Tables;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

/// <summary>
/// Table Binding Options.
/// </summary>
public class TableBindingOptions
{
    /// <summary>
    /// Connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Table Service Uri.
    /// </summary>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Token Credential.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Table Client Options.
    /// </summary>
    public TableClientOptions? TableClientOptions { get; set; }

    internal TableServiceClient? Client;

    internal virtual TableServiceClient CreateClient()
    {
        if (this.Client is not null)
        {
            return this.Client;
        }

        if (this.ServiceUri is not null && this.Credential is not null)
        {
            this.Client = new TableServiceClient(this.ServiceUri, this.Credential, this.TableClientOptions);
        }
        else
        {
            this.Client = new TableServiceClient(this.ConnectionString, this.TableClientOptions);
        }

        return this.Client;
    }
}

