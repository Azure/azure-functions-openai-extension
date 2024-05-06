﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;

public sealed class AssistantSkillTriggerAttribute : TriggerBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantSkillTriggerAttribute"/> class with the specified function
    /// description.
    /// </summary>
    /// <param name="functionDescription">A description of the assistant function, which is provided to the model.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="functionDescription"/> is <c>null</c>.</exception>
    public AssistantSkillTriggerAttribute(string functionDescription)
    {
        this.FunctionDescription = functionDescription ?? throw new ArgumentNullException(nameof(functionDescription));
    }

    /// <summary>
    /// Gets or sets the name of the function to be invoked by the assistant.
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Gets the description of the assistant function, which is provided to the LLM.
    /// </summary>
    public string FunctionDescription { get; }

    /// <summary>
    /// Gets or sets a JSON description of the function parameter, which is provided to the LLM.
    /// If no description is provided, the description will be autogenerated.
    /// </summary>
    /// <remarks>
    /// For more information on the syntax of the parameter description JSON, see the OpenAI API documentation:
    /// https://platform.openai.com/docs/api-reference/chat/create#chat-create-tools.
    /// </remarks>
    public string? ParameterDescriptionJson { get; set; }

    /// <summary>
    /// Gets or sets the OpenAI chat model to use.
    /// </summary>
    /// <remarks>
    /// When using Azure OpenAI, then should be the name of the model <em>deployment</em>.
    /// </remarks>
    public string Model { get; set; } = OpenAIModels.DefaultChatModel;
}