// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Functions.Worker.Extensions.OpenAI;

/// <summary>
/// Chat Message Entity which contains the content of the message and the role of the chat agent.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessage"/> class.
    /// </summary>
    /// <param name="content">The content of the message.</param>
    /// <param name="role">The role of the chat agent.</param>
    public ChatMessage(string content, string role)
    {
        this.Content = content;
        this.Role = role;
    }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the role of the chat agent.
    /// </summary>
    public string Role { get; set; }
}
