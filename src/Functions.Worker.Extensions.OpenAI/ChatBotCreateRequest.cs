// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Functions.Worker.Extensions.OpenAI;

/// <summary>
/// Chat bot create request which is used to create a chat bot.
/// </summary>
public class ChatBotCreateRequest
{
    public ChatBotCreateRequest()
    {
        // For deserialization
        this.Id = string.Empty;
    }

    public ChatBotCreateRequest(string id)
    {
        this.Id = id;
    }

    public ChatBotCreateRequest(string id, string? instructions)
    {
        this.Id = id;

        if (!string.IsNullOrWhiteSpace(instructions))
        {
            this.Instructions = instructions;
        }
    }

    /// <summary>
    /// Gets the ID of the chat bot to create.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Instructions that are provided to chat bot to follow.
    /// </summary>
    public string Instructions { get; set; } = "You are a helpful chat bot.";

    /// <summary>
    /// Gets time when chat bot request is set to expire.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
