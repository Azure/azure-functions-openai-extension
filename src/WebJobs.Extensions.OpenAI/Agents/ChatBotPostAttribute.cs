// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace WebJobs.Extensions.OpenAI.Agents;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public class ChatBotPostAttribute : Attribute
{
    public ChatBotPostAttribute(string id)
    {
        this.Id = id;
    }

    /// <summary>
    /// Gets the ID of the chat bot to update.
    /// </summary>
    [AutoResolve]
    public string Id { get; }
}

public record ChatBotPostRequest(string UserMessage)
{
    public string Id { get; set; } = string.Empty;
}