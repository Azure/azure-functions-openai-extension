// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AssistantQueryAttribute : Attribute
{
    public AssistantQueryAttribute(string id)
    {
        this.Id = id;
    }

    /// <summary>
    /// Gets the ID of the assistant to query.
    /// </summary>
    [AutoResolve]
    public string Id { get; }

    /// <summary>
    /// Gets or sets the timestamp of the earliest message in the chat history to fetch.
    /// The timestamp should be in ISO 8601 format - for example, 2023-08-01T00:00:00Z.
    /// </summary>
    [AutoResolve]
    public string TimestampUtc { get; set; } = string.Empty;
}