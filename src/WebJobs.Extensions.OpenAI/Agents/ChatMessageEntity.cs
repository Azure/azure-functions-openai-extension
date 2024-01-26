// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Azure;
using Azure.Data.Tables;
using OpenAI.ObjectModels.RequestModels;

namespace WebJobs.Extensions.OpenAI.Agents;
public class ChatMessageEntity: ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string ChatMessage { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

}
