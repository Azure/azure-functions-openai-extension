// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Functions.Worker.Extensions.OpenAI;

/// <summary>
/// Chat bot post request which is used to relay post requests.
/// </summary>
public class ChatBotPostRequest
{
    /// <summary>
    /// Gets user message that user has entered for chatbot to respond to.
    /// </summary>
    public string UserMessage { get; set; }

    /// <summary>
    /// Gets the ID of the chat bot to update.
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

