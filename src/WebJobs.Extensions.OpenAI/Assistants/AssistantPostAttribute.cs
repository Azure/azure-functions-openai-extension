// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AssistantPostAttribute : Attribute
{
    public AssistantPostAttribute(string id, string userMessage)
    {
        this.Id = id;
        this.UserMessage = userMessage;
    }

    /// <summary>
    /// Gets the ID of the assistant to update.
    /// </summary>
    [AutoResolve]
    public string Id { get; }

    /// <summary>
    /// Gets or sets the OpenAI chat model to use.
    /// </summary>
    /// <remarks>
    /// When using Azure OpenAI, then should be the name of the model <em>deployment</em>.
    /// </remarks>
    [AutoResolve]
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the user message to OpenAI.
    /// </summary>
    [AutoResolve]
    public string UserMessage { get; }

    /// <summary>
    /// Configuration section name for the table settings for chat storage.
    /// </summary>
    public string? ChatStorageConnectionSetting { get; set; }

    /// <summary>
    /// Table collection name for chat storage.
    /// </summary>
    public string CollectionName { get; set; } = "ChatState";
}