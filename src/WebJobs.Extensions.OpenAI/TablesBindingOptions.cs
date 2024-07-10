// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Azure.Core;
using Azure.Data.Tables;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;


public class TablesBindingOptions
{
    public string? ConnectionString { get; set; }

    public Uri? ServiceUri { get; set; }

    public TokenCredential? Credential { get; set; }

    public TableClientOptions? TableClientOptions { get; set; }

    internal TableServiceClient? Client;

    internal virtual TableServiceClient CreateClient()
    {
        if (Client is not null)
        {
            return Client;
        }

        if (ServiceUri is not null && Credential is not null)
        {
            Client = new TableServiceClient(ServiceUri, Credential, TableClientOptions);
        }
        else
        {
            Client = new TableServiceClient(ConnectionString, TableClientOptions);
        }

        return Client;
    }
}

