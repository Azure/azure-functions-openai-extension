// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;


// TODO: Update this package to be 0.8.0-alpha when the next version of the OpenAI SDK is released.
[assembly: ExtensionInformation("Microsoft.Azure.WebJobs.Extensions.OpenAI", "0.7.0-alpha")]


namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI;

/// <summary>
/// Input binding attribute for capturing OpenAI completions in function executions.
/// </summary>
public sealed class TextCompletionInputAttribute : InputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextCompletionInputAttribute"/> class with the specified text prompt.
    /// </summary>
    /// <param name="prompt">The prompt to generate completions for, encoded as a string.</param>
    public TextCompletionInputAttribute(string prompt)
    {
        this.Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
    }

    /// <summary>
    /// Gets or sets the prompt to generate completions for, encoded as a string.
    /// </summary>
    public string Prompt { get; }

    /// <summary>
    /// Gets or sets the ID of the model to use.
    /// </summary>
    public string Model { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// Gets or sets the sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output
    /// more random, while lower values like 0.2 will make it more focused and deterministic.
    /// </summary>
    /// <remarks>
    /// It's generally recommend to use this or <see cref="this.TopP"/> but not both.
    /// </remarks>
    public string? Temperature { get; set; } = "0.5";

    /// <summary>
    /// Gets or sets an alternative to sampling with temperature, called nucleus sampling, where the model considers
    /// the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10%
    /// probability mass are considered.
    /// </summary>
    /// <remarks>
    /// It's generally recommend to use this or <see cref="this.Temperature"/> but not both.
    /// </remarks>
    public string? TopP { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate in the completion.
    /// </summary>
    /// <remarks>
    /// The token count of your prompt plus max_tokens cannot exceed the model's context length.
    /// Most models have a context length of 2048 tokens (except for the newest models, which support 4096).
    /// </remarks>
    public string? MaxTokens { get; set; } = "100";
}
