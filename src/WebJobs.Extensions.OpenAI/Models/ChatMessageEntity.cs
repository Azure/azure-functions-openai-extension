// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;

public class ChatMessageEntity
{
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
    /// Gets or sets the role of the chat agent.
    /// </summary>
    public string Role { get; set; }
}
