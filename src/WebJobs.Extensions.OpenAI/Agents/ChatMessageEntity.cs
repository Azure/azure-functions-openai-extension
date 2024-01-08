// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;
public class ChatMessageEntity
{
    public ChatMessageEntity(string content, string role, string? toolCallId = null, string? functionName = null)
    {
        this.Content = content;
        this.Role = role;
        this.ToolCallId = toolCallId;
        this.FunctionName = functionName;
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
    /// Gets or sets the ID of the tool call resolved by the provided content.
    /// </summary>
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Gets or sets the name of the function that was called to produce output.
    /// </summary>
    public string? FunctionName { get; set; }    
}
