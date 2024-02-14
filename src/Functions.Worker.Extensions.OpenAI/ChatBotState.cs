// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Functions.Worker.Extensions.OpenAI;

/// <summary>
/// Chat bot state.
/// </summary>
public class ChatBotState
{
    /// <summary>
    /// Gets the ID of the chat bot.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets if chat bot exists.
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// Gets status of chat bot. Options are Uninitialzied, Active, or Expired.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Gets timestamp of when chat bot is created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets timestamp of when chat bot is last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets number of total messages for chat bot.
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Gets number of total tokens for chatbot.
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Gets a list of the recent messages from the chatbot.
    /// </summary>
    public IReadOnlyList<ChatMessageEntity> RecentMessages { get; set; }
}
