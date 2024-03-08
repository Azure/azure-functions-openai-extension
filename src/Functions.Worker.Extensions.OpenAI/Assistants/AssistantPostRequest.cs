// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;

/// <summary>
/// Assistant post request which is used to relay post requests.
/// </summary>
public class AssistantPostRequest
{
    /// <summary>
    /// Gets user message that user has entered for assistant to respond to.
    /// </summary>
    public string? UserMessage { get; set; }

    /// <summary>
    /// Gets the ID of the assistant to update.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OpenAI chat model to use.
    /// </summary>
    /// <remarks>
    /// When using Azure OpenAI, then should be the name of the model <em>deployment</em>.
    /// </remarks>
    public string? Model { get; set; }
}

