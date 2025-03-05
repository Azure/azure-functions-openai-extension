// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;
using Azure.Data.Tables;
using OpenAI.Chat;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

/// <summary>
/// The ChatMessageTableEntity class represents each chat message to interact with table storage.
/// </summary>
class ChatMessageTableEntity : ITableEntity
{
    // WARNING: Changing this is a breaking change!
    internal const string RowKeyPrefix = "msg-";

    public ChatMessageTableEntity(
        string partitionKey,
        int messageIndex,
        string content,
        ChatMessageRole role,
        string? name = null)
    {
        this.PartitionKey = partitionKey;
        this.RowKey = GetRowKey(messageIndex);
        this.Content = content;
        this.Role = role.ToString();
        this.Name = name;
        this.CreatedAt = DateTime.UtcNow;
    }

    public ChatMessageTableEntity(TableEntity entity)
    {
        this.PartitionKey = entity.PartitionKey;
        this.RowKey = entity.RowKey;
        this.Timestamp = entity.Timestamp;
        this.ETag = entity.ETag;
        this.Content = entity.GetString(nameof(this.Content));
        this.Role = entity.GetString(nameof(this.Role));
        this.Name = entity.GetString(nameof(this.Name));
        this.CreatedAt = DateTime.SpecifyKind(entity.GetDateTime(nameof(this.CreatedAt)).GetValueOrDefault(), DateTimeKind.Utc);
    }

    /// <summary>
    /// Partition key.
    /// </summary>
    public string PartitionKey { get; set; }

    /// <summary>
    /// Row key.
    /// </summary>
    public string RowKey { get; set; }

    /// <summary>
    /// For chat messages, this is the chat content. For function calls, this is the function return value.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Name of the function, if applicable.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Role of who sent message.
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// Gets timestamp of table entity.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets ETag of table entity.
    /// </summary>
    public ETag ETag { get; set; }

    /// <summary>
    /// Gets when table entity was created at.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // WARNING: Changing this is a breaking change!
    static string GetRowKey(int messageNumber)
    {
        // Example msg-001B
        return string.Concat(RowKeyPrefix, messageNumber.ToString("X4"));
    }
}
