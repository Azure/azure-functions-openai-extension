// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;

namespace WebJobs.Extensions.OpenAI.Models;

/// <summary>
/// The ChatBotStateEntity class represents the state of a chat bot to interact with Table Storage.
/// </summary>
class ChatBotStateEntity : ITableEntity
{
    /// <summary>
    /// Partition key.
    /// </summary>
    public string PartitionKey { get; set; }

    /// <summary>
    /// Row key.
    /// </summary>
    public string RowKey { get; set; }

    // <summary>
    /// Gets if chatbot exists or not.
    /// </summary>
    public bool Exists { get; set; }

    // <summary>
    /// Gets when chat bot was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // <summary>
    /// Gets when chatbot was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    // <summary>
    /// Gets total messages in chat bot.
    /// </summary>
    public int TotalMessages { get; set; }

    // <summary>
    /// Gets total tokens.
    /// </summary>
    public int TotalTokens { get; set; }

    // <summary>
    /// Gets timestamp of table entity.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    // <summary>
    /// Gets ETag of table entity.
    /// </summary>
    public ETag ETag { get; set; }
}
