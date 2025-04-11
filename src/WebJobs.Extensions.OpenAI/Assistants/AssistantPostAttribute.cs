// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AssistantPostAttribute : AssistantBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantPostAttribute"/> class.
    /// </summary>
    /// <param name="id">The assistant identifier.</param>
    /// <param name="userMessage">The user message.</param>
    /// <param name="aiConnectionName">The name of the configuration section for AI service connectivity settings.</param>
    public AssistantPostAttribute(string id, string userMessage, string aiConnectionName = "") : base(aiConnectionName)
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
    [AutoResolve]
    public string CollectionName { get; set; } = "ChatState";
}