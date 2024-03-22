// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Data.Tables;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

/// <summary>
/// The AssistantStateEntity class represents the state of a assistant to interact with Table Storage.
/// </summary>
class AssistantStateEntity : ITableEntity
{
    // WARNING: Changing this value is a breaking change!
    internal const string FixedRowKeyValue = "state";

    public AssistantStateEntity(string partitionKey)
    {
        this.PartitionKey = partitionKey;
        this.RowKey = FixedRowKeyValue;
        this.CreatedAt = DateTime.UtcNow;
        this.LastUpdatedAt = DateTime.UtcNow;
        this.Exists = true;
    }

    public AssistantStateEntity(TableEntity entity)
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

    /// <summary>
    /// Partition key.
    /// </summary>
    public string PartitionKey { get; set; }

    /// <summary>
    /// Row key.
    /// </summary>
    public string RowKey { get; set; }

    /// <summary>
    /// Gets if assistant exists or not.
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// Gets when assistant was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets when assistant was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets total messages in assistant.
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
