// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

/// <summary>
/// Chat Message Entity which contains the content of the message and the role of the chat agent (“system”, “user”, "assistant", "function" or "tool").
/// </summary>
public class ChatMessageEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageEntity"/> class.
    /// </summary>
    /// <param name="content">The content of the message.</param>
    /// <param name="role">The role of the chat agent (“system”, “user”, "assistant", "function" or "tool").</param>
    public ChatMessageEntity(string content, string role)
    {
        this.Content = content;
        this.Role = role;
    }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the role of the chat agent (“system”, “user”, "assistant", "function" or "tool").
    /// </summary>
    public string Role { get; set; }
}
