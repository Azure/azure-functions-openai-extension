// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;

/// <summary>
/// Assistant state.
/// </summary>
public class AssistantState
{
    /// <summary>
    /// Gets the ID of the assistant.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets if assistant exists.
    /// </summary>
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    /// <summary>
    /// Gets status of assistant. Options are Uninitialzied, Active, or Expired.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Gets timestamp of when assistant is created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets timestamp of when assistant is last updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedAt")]
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets number of total messages for assistant.
    /// </summary>
    [JsonPropertyName("totalMessages")]
    public int TotalMessages { get; set; }

    /// <summary>
    /// Gets number of total tokens for assistant.
    /// </summary>
    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; }

    /// <summary>
    /// Gets a list of the recent messages from the assistant.
    /// </summary>
    [JsonPropertyName("recentMessages")]
    public IReadOnlyList<AssistantMessage> RecentMessages { get; set; } = Array.Empty<AssistantMessage>();
}
