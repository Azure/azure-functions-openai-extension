// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.WebJobs.Description;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace WebJobs.Extensions.OpenAI;

/// <summary>
/// Input binding attribute for capturing OpenAI completions in function executions.
/// </summary>
[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class OpenAICompletionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAICompletionAttribute"/> class with the specified text prompt.
    /// </summary>
    /// <param name="prompt">The prompt to generate completions for, encoded as a string.</param>
    public OpenAICompletionAttribute(string prompt)
    {
        this.Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
    }

    /// <summary>
    /// Gets or sets the prompt to generate completions for, encoded as a string.
    /// </summary>
    [AutoResolve]
    public string Prompt { get; }

    /// <summary>
    /// Gets or sets the ID of the model to use.
    /// </summary>
    [AutoResolve]
    public string Model { get; set; } = "text-davinci-003";

    /// <summary>
    /// Gets or sets the sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output
    /// more random, while lower values like 0.2 will make it more focused and deterministic.
    /// </summary>
    /// <remarks>
    /// It's generally recommend to use this or <see cref="this.TopP"/> but not both.
    /// </remarks>
    [AutoResolve]
    public string? Temperature { get; set; } = "0.9";

    /// <summary>
    /// Gets or sets an alternative to sampling with temperature, called nucleus sampling, where the model considers
    /// the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10%
    /// probability mass are considered.
    /// </summary>
    /// <remarks>
    /// It's generally recommend to use this or <see cref="this.Temperature"/> but not both.
    /// </remarks>
    [AutoResolve]
    public string? TopP { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate in the completion.
    /// </summary>
    /// <remarks>
    /// The token count of your prompt plus max_tokens cannot exceed the model's context length.
    /// Most models have a context length of 2048 tokens (except for the newest models, which support 4096).
    /// </remarks>
    [AutoResolve]
    public string? MaxTokens { get; set; }

    internal CompletionCreateRequest BuildRequest()
    {
        CompletionCreateRequest request = new()
        {
            Prompt = this.Prompt
        };

        if (this.Model is not null)
        {
            request.Model = this.Model;
        }

        if (int.TryParse(this.MaxTokens, out int maxTokens))
        {
            request.MaxTokens = maxTokens;
        }

        if (float.TryParse(this.Temperature, out float temperature))
        {
            request.Temperature = temperature;
        }

        if (float.TryParse(this.TopP, out float topP))
        {
            request.TopP = topP;
        }

        return request;
    }
}
