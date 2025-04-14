// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;

/// <summary>
/// Chat Message Entity which contains the content of the message, the role of the chat agent, and the name of the calling function if applicable.
/// </summary>
public class AssistantMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantMessage"/> class.
    /// </summary>
    /// <param name="content">The content of the message.</param>
    /// <param name="role">The role of the chat agent.</param>
    /// <param name="toolCalls">The tool calls.</param>
    public AssistantMessage(string content, string role, string toolCalls)
    {
        this.Content = content;
        this.Role = role;
        this.ToolCalls = toolCalls;
    }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the role of the chat agent.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; }

    /// <summary>
    /// Gets or sets the tool calls.
    /// </summary>
    [JsonPropertyName("toolCalls")]
    public string ToolCalls { get; set; }
}
