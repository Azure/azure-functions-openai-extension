// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Functions.Worker.Extensions.OpenAI;
public class ChatBotCreateRequest2
{
    public ChatBotCreateRequest2()
    {
        // For deserialization
        this.Id = string.Empty;
    }

    public ChatBotCreateRequest2(string id)
    {
        this.Id = id;
    }

    public ChatBotCreateRequest2(string id, string? instructions)
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
