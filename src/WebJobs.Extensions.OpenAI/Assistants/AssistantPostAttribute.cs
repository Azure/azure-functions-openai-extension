// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AssistantPostAttribute : Attribute
{
    public AssistantPostAttribute(string id)
    {
        this.Id = id;
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
}

public record AssistantPostRequest(string UserMessage)
{
    public string Id { get; set; } = string.Empty;

    public string? Model { get; set; }
}