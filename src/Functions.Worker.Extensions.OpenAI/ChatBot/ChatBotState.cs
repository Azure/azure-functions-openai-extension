// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Functions.Worker.Extensions.OpenAI.ChatBot;

/// <summary>
/// Chat bot state.
/// </summary>
public class ChatBotState
{
    /// <summary>
    /// Gets the ID of the chat bot.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets if chat bot exists.
    /// </summary>
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    /// <summary>
    /// Gets status of chat bot. Options are Uninitialzied, Active, or Expired.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Gets timestamp of when chat bot is created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets timestamp of when chat bot is last updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedAt")]
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets number of total messages for chat bot.
    /// </summary>
    [JsonPropertyName("totalMessages")]
    public int TotalMessages { get; set; }

    /// <summary>
    /// Gets number of total tokens for chatbot.
    /// </summary>
    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; }

    /// <summary>
    /// Gets a list of the recent messages from the chatbot.
    /// </summary>
    [JsonPropertyName("recentMessages")]
    public IReadOnlyList<ChatMessage> RecentMessages { get; set; } = Array.Empty<ChatMessage>();
}
