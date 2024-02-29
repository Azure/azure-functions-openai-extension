// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;

/// <summary>
/// Assistant post output attribute which is used to update the assistant.
/// </summary>
public class AssistantPostOutputAttribute : OutputBindingAttribute
{
    public AssistantPostOutputAttribute(string id)
    {
        this.Id = id;
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
}
