// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;

/// <summary>
/// Assistant create request which is used to create a assistant.
/// </summary>
public class AssistantCreateRequest
{
    public AssistantCreateRequest()
    {
        // For deserialization
        this.Id = string.Empty;
    }

    public AssistantCreateRequest(string id)
    {
        this.Id = id;
    }

    public AssistantCreateRequest(string id, string? instructions)
    {
        this.Id = id;

        if (!string.IsNullOrWhiteSpace(instructions))
        {
            this.Instructions = instructions;
        }
    }

    /// <summary>
    /// Gets the ID of the assistant to create.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Instructions that are provided to assistant to follow.
    /// </summary>
    public string Instructions { get; set; } = "You are a helpful assistant.";

    /// <summary>
    /// Configuration section name for the table settings for chat storage.
    /// </summary>
    public string? ChatStorageConnectionSetting { get; set; }

    /// <summary>
    /// Table collection name for chat storage.
    /// </summary>
    public string CollectionName { get; set; } = "ChatState";

    /// <summary>
    /// When true, preserves existing chat history and only updates the system instructions.
    /// </summary>
    public bool PreserveChatHistory { get; set; }
}
