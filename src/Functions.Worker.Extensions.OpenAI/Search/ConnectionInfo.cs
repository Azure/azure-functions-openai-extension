// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;
public class ConnectionInfo
{
    public ConnectionInfo(string ConnectionName, string CollectionName)
    {
        this.ConnectionName = ConnectionName;
        this.CollectionName = CollectionName;
    }

    public string ConnectionName { get; set; }
    public string CollectionName { get; set; }
}
