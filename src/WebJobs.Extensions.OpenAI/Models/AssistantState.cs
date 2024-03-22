// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

/// <summary>
/// Assistant State.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class AssistantState
{
    public AssistantState(
        string Id,
        bool Exists,
        DateTime CreatedAt,
        DateTime LastUpdatedAt,
        int TotalMessages,
        int TotalTokens,
        IReadOnlyList<ChatMessage> RecentMessages)
    {
        this.Id = Id;
        this.Exists = Exists;
        this.CreatedAt = CreatedAt;
        this.LastUpdatedAt = LastUpdatedAt;
        this.TotalMessages = TotalMessages;
        this.TotalTokens = TotalTokens;
        this.RecentMessages = RecentMessages;
    }

    /// <summary>
    /// Gets the ID of the assistant.
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets if assistant exists.
    /// </summary>
    [JsonProperty("exists")]
    public bool Exists { get; set; }

    /// <summary>
    /// Gets timestamp of when assistant is created.
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets timestamp of when assistant is last updated.
    /// </summary>
    [JsonProperty("lastUpdatedAt")]
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets number of total messages for assistant.
    /// </summary>
    [JsonProperty("totalMessages")]
    public int TotalMessages { get; set; }

    /// <summary>
    /// Gets number of total tokens for assistant.
    /// </summary>
    [JsonProperty("totalTokens")]
    public int TotalTokens { get; set; }

    /// <summary>
    /// Gets a list of the recent messages from the assistant.
    /// </summary>
    [JsonProperty("recentMessages")]
    public IReadOnlyList<ChatMessage> RecentMessages { get; set; } = Array.Empty<ChatMessage>();
}
