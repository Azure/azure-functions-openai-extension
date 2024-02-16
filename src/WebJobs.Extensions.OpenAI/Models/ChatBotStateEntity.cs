// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Data.Tables;

namespace WebJobs.Extensions.OpenAI.Models;

/// <summary>
/// The ChatBotStateEntity class represents the state of a chat bot to interact with Table Storage.
/// </summary>
class ChatBotStateEntity : ITableEntity
{
    // WARNING: Changing this value is a breaking change!
    internal const string FixedRowKeyValue = "state";

    public ChatBotStateEntity(string partitionKey)
    {
        this.PartitionKey = partitionKey;
        this.RowKey = FixedRowKeyValue;
        this.CreatedAt = DateTime.UtcNow;
        this.LastUpdatedAt = DateTime.UtcNow;
        this.Exists = true;
    }

    public ChatBotStateEntity(TableEntity entity)
    {
        this.PartitionKey = entity.PartitionKey;
        this.RowKey = entity.RowKey;
        this.Timestamp = entity.Timestamp;
        this.ETag = entity.ETag;
        this.CreatedAt = DateTime.SpecifyKind(entity.GetDateTime(nameof(this.CreatedAt)).GetValueOrDefault(), DateTimeKind.Utc);
        this.LastUpdatedAt = DateTime.SpecifyKind(entity.GetDateTime(nameof(this.LastUpdatedAt)).GetValueOrDefault(), DateTimeKind.Utc);
        this.TotalMessages = entity.GetInt32(nameof(this.TotalMessages)).GetValueOrDefault();
        this.TotalTokens = entity.GetInt32(nameof(this.TotalTokens)).GetValueOrDefault();
        this.Exists = entity.GetBoolean(nameof(this.Exists)).GetValueOrDefault();
    }

    // TODO: Confirm whether this is necessary
    //// public ChatBotStateEntity() { }

    /// <summary>
    /// Partition key.
    /// </summary>
    public string PartitionKey { get; set; }

    /// <summary>
    /// Row key.
    /// </summary>
    public string RowKey { get; set; }

    /// <summary>
    /// Gets if chatbot exists or not.
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// Gets when chat bot was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets when chatbot was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets total messages in chat bot.
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Gets total tokens.
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Gets timestamp of table entity.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets ETag of table entity.
    /// </summary>
    public ETag ETag { get; set; }
}
