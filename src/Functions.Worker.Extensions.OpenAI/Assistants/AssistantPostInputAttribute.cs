// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;

/// <summary>
/// Assistant post input attribute which is used to update the assistant.
/// </summary>
public sealed class AssistantPostInputAttribute : InputBindingAttribute
{
    public AssistantPostInputAttribute(string id, string UserMessage)
    {
        this.Id = id;
        this.UserMessage = UserMessage;
    }

    /// <summary>
    /// Gets the ID of the assistant to update.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the OpenAI chat model to use.
    /// </summary>
    /// <remarks>
    /// When using Azure OpenAI, then should be the name of the model <em>deployment</em>.
    /// </remarks>
    public string? Model { get; set; }

    /// <summary>
    /// Gets user message that user has entered for assistant to respond to.
    /// </summary>
    public string UserMessage { get; }

    /// <summary>
    /// Configuration section name for the table settings for chat storage.
    /// </summary>
    public string? ChatStorageConnectionSetting { get; set; }

    /// <summary>
    /// Table collection name for chat storage.
    /// </summary>
    public string CollectionName { get; set; } = "SampleChatState";
}
