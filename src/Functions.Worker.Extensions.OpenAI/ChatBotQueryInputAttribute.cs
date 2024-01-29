// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Functions.Worker.Extensions.OpenAI;

/// <summary>
/// Chat bot query input attribute which is used query the chatbot to get current state.
/// </summary>
public sealed class ChatBotQueryInputAttribute : InputBindingAttribute
{
    public ChatBotQueryInputAttribute(string id)
    {
        this.Id = id;
    }

    /// <summary>
    /// Gets the ID of the chat bot to query.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the timestamp of the earliest message in the chat history to fetch.
    /// The timestamp should be in ISO 8601 format - for example, 2023-08-01T00:00:00Z.
    /// </summary>
    public string TimestampUtc { get; set; } = string.Empty;
}
