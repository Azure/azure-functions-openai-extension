// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

#pragma warning disable CS0618 // Approved for use by this extension
[Binding(TriggerHandlesReturnValue = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AssistantSkillTriggerAttribute : Attribute
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

    // TODO: Consider making the function description another trigger type to make it work across all languages
    /// <summary>
    /// Gets or sets a JSON description of the function parameter, which is provided to the LLM.
    /// If no description is provided, the description will be autogenerated.
    /// </summary>
    /// <remarks>
    /// For more information on the syntax of the parameter description JSON, see the OpenAI API documentation:
    /// https://platform.openai.com/docs/api-reference/chat/create#chat-create-tools.
    /// </remarks>
    public string? ParameterDescriptionJson { get; set; }
}