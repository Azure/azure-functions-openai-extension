﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

/// <summary>
/// Input binding attribute for capturing OpenAI completions in function executions.
/// </summary>
[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class TextCompletionAttribute : AssistantBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextCompletionAttribute"/> class with the specified text prompt.
    /// </summary>
    /// <param name="prompt">The prompt to generate completions for, encoded as a string.</param>
    public TextCompletionAttribute(string prompt)
    {
        this.Prompt = string.IsNullOrEmpty(prompt)
            ? throw new ArgumentException("Input cannot be null or empty.", nameof(prompt))
            : prompt;
    }

    /// <summary>
    /// Gets or sets the prompt to generate completions for, encoded as a string.
    /// </summary>
    [AutoResolve]
    public string Prompt { get; }
}
