// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;


// TODO: Move this somewhere else
[assembly: ExtensionInformation("CGillum.WebJobs.Extensions.OpenAI", "0.3.1-alpha")]


namespace Functions.Worker.Extensions.OpenAI;

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
    public string Model { get; set; } = "text-davinci-003";

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

    /// <summary>
    /// Gets or sets a value indicating whether the binding should throw if there is an error calling the OpenAI
    /// endpoint.
    /// </summary>
    /// <remarks>
    /// The default value is <c>true</c>. Set this to <c>false</c> to handle errors manually in the function code.
    /// </remarks>
    public bool ThrowOnError { get; set; } = true;
}
