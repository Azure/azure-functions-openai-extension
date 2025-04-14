// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;

/// <summary>
/// Assistant post input attribute which is used to update the assistant.
/// </summary>
public sealed class AssistantPostInputAttribute : InputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantPostInputAttribute"/> class.
    /// </summary>
    /// <param name="id">The assistant identifier.</param>
    /// <param name="userMessage">The user message.</param>
    public AssistantPostInputAttribute(string id, string userMessage)
    {
        this.Id = id;
        this.UserMessage = userMessage;
    }

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
    /// - For user-assigned managed identity authentication, this property is required
    /// 
    /// For OpenAI:
    /// - For OpenAI service (non-Azure), set the OPENAI_API_KEY environment variable.
    /// </remarks>
    public string AIConnectionName { get; set; } = "";

    /// <summary>
    /// Gets the ID of the assistant to update.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the OpenAI chat model to use.
    /// </summary>
    /// <remarks>
    /// When using Azure OpenAI, then should be the name of the model <em>deployment</em>.
    /// </remarks>
    public string? ChatModel { get; set; }

    /// <summary>
    /// Gets user message that user has entered for assistant to respond to.
    /// </summary>
    public string UserMessage { get; }

    /// <summary>
    /// Configuration section name for the table settings for chat storage.
    /// </summary>
    public string? ChatStorageConnectionSetting { get; set; }

    /// <summary>
    /// Table collection name for chat storage.
    /// </summary>
    public string CollectionName { get; set; } = "ChatState";

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
    /// Gets or sets the maximum number of tokens to output in the completion. Default is 100.
    /// </summary>
    /// <remarks>
    /// The token count of your prompt plus max_tokens cannot exceed the model's context length.
    /// Most models have a context length of 2048 tokens (except for the newest models, which support 4096).
    /// </remarks>
    public string? MaxTokens { get; set; } = "100";
}
