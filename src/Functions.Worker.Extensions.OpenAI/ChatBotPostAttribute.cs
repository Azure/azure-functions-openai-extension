// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
namespace Functions.Worker.Extensions.OpenAI;

[AttributeUsage(AttributeTargets.Parameter| AttributeTargets.Property)]
public class ChatBotPostAttribute : Attribute
{
    public ChatBotPostAttribute(string id)
    {
        this.Id = id;
    }

    /// <summary>
    /// Gets the ID of the chat bot to update.
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
