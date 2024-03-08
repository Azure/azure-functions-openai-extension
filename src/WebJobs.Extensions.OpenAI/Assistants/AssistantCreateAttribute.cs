// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public class AssistantCreateAttribute : Attribute
{
    // No configuration needed
}

public class AssistantCreateRequest
{
    public AssistantCreateRequest()
    {
        // For deserialization
        this.Id = string.Empty;
    }

    public AssistantCreateRequest(string id)
    {
        this.Id = id;
    }

    public AssistantCreateRequest(string id, string? instructions)
    {
        this.Id = id;

        if (!string.IsNullOrWhiteSpace(instructions))
        {
            this.Instructions = instructions;
        }
    }

    public string Id { get; set; }
    public string Instructions { get; set; } = "You are a helpful assistant.";
    public DateTime? ExpiresAt { get; set; }
}