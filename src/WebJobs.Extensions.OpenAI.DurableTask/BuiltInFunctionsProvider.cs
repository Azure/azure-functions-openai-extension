// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Script.Description;
using Newtonsoft.Json.Linq;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace WebJobs.Extensions.OpenAI.DurableTask;

/// <summary>
/// Class that defines all the built-in functions for executing CNCF Serverless Workflows.
/// IMPORTANT: Renaming methods in this class is a breaking change!
/// </summary>
class BuiltInFunctionsProvider : IFunctionProvider
{
    /// <inheritdoc/>
    Task<ImmutableArray<FunctionMetadata>> IFunctionProvider.GetFunctionMetadataAsync() =>
        Task.FromResult(this.GetFunctionMetadata().ToImmutableArray());

    // TODO: Not sure what this is for...
    /// <inheritdoc/>
    ImmutableDictionary<string, ImmutableArray<string>> IFunctionProvider.FunctionErrors =>
        new Dictionary<string, ImmutableArray<string>>().ToImmutableDictionary();

    /// <summary>
    /// Calls ChatGPT with the specified <paramref name="chatHistory"/> and returns the response message.
    /// </summary>
    /// <param name="chatHistory">The full conversation chat history.</param>
    /// <param name="service">
    /// The dependency-injected <see cref="IOpenAIService"/> object to use for invoking ChatGPT.
    /// </param>
    /// <returns>Returns the response from ChatGPT, which can be appended to the chat history.</returns>
    /// <exception cref="ApplicationException">Throw if ChatGPT returns an error.</exception>
    [FunctionName(nameof(GetNextChatBotResponse))]
    public static async Task<string> GetNextChatBotResponse(
        [ActivityTrigger] IDurableActivityContext context,
        [OpenAIService] IOpenAIService service)
    {
        List<ChatMessage> chatHistory = context.GetInput<List<ChatMessage>>();
        ChatCompletionCreateRequest request = new()
        {
            Messages = chatHistory,
            Model = Models.ChatGpt3_5Turbo
        };

        ChatCompletionCreateResponse response = await service.ChatCompletion.CreateCompletion(request);
        if (!response.Successful)
        {
            // Throwing an exception will cause the orchestration to retry based on its configured retry policy.
            // TODO: Check for a non-retriable error and return a different kind of result.
            Error error = response.Error ?? new Error() { Message = "Unspecified error" };
            throw new ApplicationException($"The {request.Model} engine returned an error: {error}");
        }

        // We don't normally expect more than one message, but just in case we get multiple messages,
        // return all of them separated by two newlines.
        string replyMessage = string.Join(
            Environment.NewLine + Environment.NewLine,
            response.Choices.Select(choice => choice.Message.Content));
        return replyMessage;
    }

    internal static string GetBuiltInFunctionName(string functionName)
    {
        return $"OpenAI::{functionName}";
    }

    /// <summary>
    /// Returns an enumeration of all the function triggers defined in this class.
    /// </summary>
    IEnumerable<FunctionMetadata> GetFunctionMetadata()
    {
        foreach (MethodInfo method in this.GetType().GetMethods())
        {
            if (method.GetCustomAttribute<FunctionNameAttribute>() is not FunctionNameAttribute)
            {
                // Not an Azure Function definition
                continue;
            }

            FunctionMetadata metadata = new()
            {
                // NOTE: We always use the method name and ignore the function name
                Name = GetBuiltInFunctionName(method.Name),
                ScriptFile = $"assembly:{method.ReflectedType.Assembly.FullName}",
                EntryPoint = $"{method.ReflectedType.FullName}.{method.Name}",
                Language = "DotNetAssembly",
            };

            // Scan the parameters for binding attributes and add them to the bindings collection
            // so that we can register them with the Functions runtime.
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (parameter.GetCustomAttribute<OrchestrationTriggerAttribute>() is not null)
                {
                    // NOTE: We assume each orchestrator function in this file defines the parameter name as "context".
                    metadata.Bindings.Add(BindingMetadata.Create(new JObject(
                        new JProperty("type", "orchestrationTrigger"),
                        new JProperty("name", "context"))));
                }
                else if (parameter.GetCustomAttribute<ActivityTriggerAttribute>() is not null)
                {
                    // NOTE: We assume each activity function in this file binds to IDurableActivityContext
                    //       and defines the parameter name as "context".
                    metadata.Bindings.Add(BindingMetadata.Create(new JObject(
                        new JProperty("type", "activityTrigger"),
                        new JProperty("name", "context"))));
                }
                else if (parameter.GetCustomAttribute<OpenAIServiceAttribute>() is not null)
                {
                    // NOTE: We assume each OpenAI service function in this file defines the parameter name as "service".
                    metadata.Bindings.Add(BindingMetadata.Create(new JObject(
                        new JProperty("type", "openAIService"),
                        new JProperty("name", "service"),
                        new JProperty("direction", "in"))));
                }
            }

            yield return metadata;
        }
    }
}
