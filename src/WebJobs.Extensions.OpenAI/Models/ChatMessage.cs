// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

/// <summary>
/// Chat Message Entity which contains the content of the message, the role of the chat agent, and the name of the calling function if applicable.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessage"/> class.
    /// </summary>
    /// <param name="content">The content of the message.</param>
    /// <param name="role">The role of the chat agent.</param>
    public ChatMessage(string content, string role, string? name)
    {
        this.Content = content;
        this.Role = role;
        this.Name = name;
    }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the role of the chat agent.
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// Gets or sets the name of the calling function if applicable.
    /// </summary>
    public string? Name { get; set; }
}
