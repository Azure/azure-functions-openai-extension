// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Extensions;
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
            // Common configuration.
            // The Betalgo SDK has a default API version, but we support overriding it using an environment variable.
            string? apiVersion = Environment.GetEnvironmentVariable("OPENAI_API_VERSION");
            if (!string.IsNullOrEmpty(apiVersion))
            {
                settings.ApiVersion = apiVersion;
            }

            // Try Azure connection first, which is preferred for privacy
            string? azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            if (!string.IsNullOrEmpty(azureOpenAIEndpoint))
            {
                // Azure OpenAI configuration
                settings.ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")!;
                settings.ProviderType = ProviderType.Azure;
                settings.BaseDomain = azureOpenAIEndpoint;
                settings.DeploymentId = "placeholder"; // dummy value - this will be replaced at runtime
            }
            else
            {
                // Public OpenAI configuration
                settings.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                settings.Organization = Environment.GetEnvironmentVariable("OPENAI_ORGANIZATION_ID");
            }

            if (string.IsNullOrEmpty(settings.ApiKey))
            {
                throw new InvalidOperationException("Must set OPENAI_API_KEY or AZURE_OPENAI_KEY environment variable.");
            }
        });

        // Register the WebJobs extension, which enables the bindings.
        builder.AddExtension<OpenAIExtension>();

        // Service objects that will be used by the extension
        builder.Services.AddSingleton<IOpenAIServiceProvider, DefaultOpenAIServiceProvider>();
        builder.Services.AddSingleton<TextCompletionConverter>();
        builder.Services.AddSingleton<EmbeddingsConverter>();
        builder.Services.AddSingleton<SemanticSearchConverter>();
        builder.Services.AddSingleton<ChatBotBindingConverter>();
        builder.Services.AddSingleton<IChatBotService, DefaultChatBotService>();

        return builder;
    }
}
