// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

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

        // Register the WebJobs extension, which enables the bindings.
        builder.AddExtension<OpenAIExtension>();
        
        // Service objects that will be used by the extension
        builder.Services.AddSingleton<TextCompletionConverter>();
        builder.Services.AddSingleton<EmbeddingsConverter>();
        builder.Services.AddSingleton<EmbeddingsStoreConverter>();
        builder.Services.AddSingleton<SemanticSearchConverter>();
        builder.Services.AddSingleton<AssistantBindingConverter>();

        builder.Services.AddOptions<OpenAIConfigOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                config.GetSection("azurefunctionsjobhost:extensions:openai").Bind(options);
            });

        builder.Services.AddSingleton<IAssistantService, DefaultAssistantService>();
        builder.Services
            .AddSingleton<AssistantSkillManager>()
            .AddSingleton<IAssistantSkillInvoker>(p => p.GetRequiredService<AssistantSkillManager>());
        builder.Services.AddSingleton<AssistantSkillTriggerBindingProvider>();

        builder.Services.AddAzureClientsCore(); // Adds AzureComponentFactory

        builder.Services.AddSingleton<OpenAIClientFactory>();

        return builder;
    }
}
