// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using OpenAI.Chat;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

/// <summary>
/// Binding attribute for the OpenAI Assistant extension. This attribute is used to specify the configuration
/// settings for the OpenAI Assistant when used in a function parameter. It allows to set various chat completion options.
/// </summary>
[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public class AssistantBaseAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the Large Language Model to invoke for chat responses.
    /// The default value is "gpt-3.5-turbo".
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    [AutoResolve]
    public string ChatModel { get; set; } = OpenAIModels.DefaultChatModel;

    /// <summary>
    /// Gets or sets the name of the configuration section for AI service connectivity settings.
    /// </summary>
    /// <remarks>
    /// This property specifies the name of the configuration section that contains connection details for the AI service.
    /// 
    /// For Azure OpenAI:
    /// - If specified, looks for "Endpoint" and "Key" values in this configuration section
    /// - If not specified or the section doesn't exist, falls back to environment variables:
    ///   AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_KEY
    /// - For user-assigned managed identity authentication, configuration section is required
    /// 
    /// For OpenAI:
    /// - For OpenAI service (non-Azure), set the OPENAI_API_KEY environment variable.
    /// </remarks>
    public string AIConnectionName { get; set; } = "";

    /// <summary>
    /// Gets or sets the sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output
    /// more random, while lower values like 0.2 will make it more focused and deterministic.
    /// </summary>
    /// <remarks>
    /// It's generally recommend to use this or <see cref="this.TopP"/> but not both.
    /// </remarks>
    [AutoResolve]
    public string? Temperature { get; set; } = "0.5";

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
    /// Gets or sets the maximum number of tokens to output in the completion. Default value = 100.
    /// </summary>
    /// <remarks>
    /// The token count of your prompt plus max_tokens cannot exceed the model's context length.
    /// Most models have a context length of 2048 tokens (except for the newest models, which support 4096).
    /// </remarks>
    [AutoResolve]
    public string? MaxTokens { get; set; } = "100";

    internal ChatCompletionOptions BuildRequest()
    {
        ChatCompletionOptions request = new();
        if (int.TryParse(this.MaxTokens, out int maxTokens))
        {
            request.MaxOutputTokenCount = maxTokens;
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
