// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.GPT3.Extensions;
using WebJobs.Extensions.OpenAI.Agents;
using WebJobs.Extensions.OpenAI.Search;

namespace WebJobs.Extensions.OpenAI;

/// <summary>
/// Extension methods for registering the OpenAI webjobs extension.
/// </summary>
public static class OpenAIWebJobsBuilderExtensions
{
    /// <summary>
    /// Registers OpenAI bindings with the WebJobs host.
    /// </summary>
    /// <param name="builder">The WebJobs builder.</param>
    /// <returns>Returns the <paramref name="builder"/> reference to support fluent-style configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is <c>null</c>.</exception>
    public static IWebJobsBuilder AddOpenAIBindings(this IWebJobsBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        // Register the OpenAI service, which we depend on.
        builder.Services.AddOpenAIService(settings =>
        {
            // TODO: Find a more generic way to configure these.
            settings.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            settings.Organization = Environment.GetEnvironmentVariable("OPENAI_ORGANIZATION_ID");

            if (settings.ApiKey == null)
            {
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
            }
        });

        // Register the WebJobs extension, which enables the bindings.
        builder.AddExtension<OpenAIExtension>();

        // Service objects that will be used by the extension
        builder.Services.AddSingleton<TextCompletionConverter>();
        builder.Services.AddSingleton<EmbeddingsConverter>();
        builder.Services.AddSingleton<SemanticSearchConverter>();
        builder.Services.AddSingleton<ChatBotBindingConverter>();
        builder.Services.AddSingleton<IChatBotService, DefaultChatBotService>();

        return builder;
    }
}
