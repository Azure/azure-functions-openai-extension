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

/// <summary>
/// Assistant Create Request.
/// </summary>
public class AssistantCreateRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantCreateRequest"/> class.
    /// </summary>
    public AssistantCreateRequest()
    {
        // For deserialization
        this.Id = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantCreateRequest"/> class.
    /// </summary>
    /// <param name="id">The identifier.</param>
    public AssistantCreateRequest(string id)
    {
        this.Id = id;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantCreateRequest"/> class.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="instructions">The instructions.</param>
    public AssistantCreateRequest(string id, string? instructions)
    {
        this.Id = id;

        if (!string.IsNullOrWhiteSpace(instructions))
        {
            this.Instructions = instructions;
        }
    }

    /// <summary>
    /// The identifier of the assistant to create.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The instructions that are provided to assistant to follow.
    /// </summary>
    public string Instructions { get; set; } = "You are a helpful assistant.";
}