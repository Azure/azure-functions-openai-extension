// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;

/// <summary>
/// Assistant query input attribute which is used query the Assistant to get current state.
/// </summary>
public sealed class AssistantQueryInputAttribute : InputBindingAttribute
{
    public AssistantQueryInputAttribute(string id)
    {
        this.Id = id;
    }

    /// <summary>
    /// Gets the ID of the Assistant to query.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the timestamp of the earliest message in the chat history to fetch.
    /// The timestamp should be in ISO 8601 format - for example, 2023-08-01T00:00:00Z.
    /// </summary>
    public string TimestampUtc { get; set; } = string.Empty;

    /// <summary>
    /// Configuration section name for the table settings for chat storage.
    /// </summary>
    public string? ChatStorageConnectionSetting { get; set; }

    /// <summary>
    /// Table collection name for chat storage.
    /// </summary>
    public string CollectionName { get; set; } = "SampleChatState";
}
