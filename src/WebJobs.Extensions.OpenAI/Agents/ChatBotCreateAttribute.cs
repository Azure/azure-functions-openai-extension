// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace WebJobs.Extensions.OpenAI.Agents;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public class ChatBotCreateAttribute : Attribute
{
    // No configuration needed
}

public class ChatBotCreateRequest
{
    public ChatBotCreateRequest()
    {
        // For deserialization
        this.Id = string.Empty;
    }

    public ChatBotCreateRequest(string id)
    {
        this.Id = id;
    }

    public ChatBotCreateRequest(string id, string? instructions)
    {
        this.Id = id;
    
        if (!string.IsNullOrWhiteSpace(instructions))
        {
            this.Instructions = instructions;
        }
    }

    public string Id { get; set; }
    public string Instructions { get; set; } = "You are a helpful chat bot.";
    public DateTime? ExpiresAt { get; set; }
}