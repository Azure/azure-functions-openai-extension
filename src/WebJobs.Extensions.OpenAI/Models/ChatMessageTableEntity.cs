// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Data.Tables;

namespace WebJobs.Extensions.OpenAI.Models;

/// <summary>
/// The ChatMessageTableEntity class represents each chat message to interact with table storage.
/// </summary>
class ChatMessageTableEntity : ITableEntity
{
    /// <summary>
    /// Partition key.
    /// </summary>
    public string PartitionKey { get; set; }

    /// <summary>
    /// Row key.
    /// </summary>
    public string RowKey { get; set; }

    /// <summary>
    /// Chat message that will be stored in the table.
    /// </summary>
    public string ChatMessage { get; set; }

    /// <summary>
    /// Role of who sent message.
    /// </summary>
    public string Role { get; set; }

    // <summary>
    /// Gets timestamp of table entity.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    // <summary>
    /// Gets ETag of table entity.
    /// </summary>
    public ETag ETag { get; set; }

    // <summary>
    /// Gets when chat message was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

}
