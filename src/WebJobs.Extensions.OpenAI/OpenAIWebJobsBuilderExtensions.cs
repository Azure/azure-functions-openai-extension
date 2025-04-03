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

        // Register the client for Azure Open AI
        string? azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        string? azureOpenAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");

        if (azureOpenAIEndpoint != null && !string.IsNullOrEmpty(azureOpenAIKey))
        {
            RegisterAzureOpenAIClient(builder.Services, azureOpenAIEndpoint, azureOpenAIKey);
        }
        else if (azureOpenAIEndpoint != null)
        {
            RegisterAzureOpenAIADAuthClient(builder.Services, azureOpenAIEndpoint);
        }
        else if (!string.IsNullOrEmpty(openAIKey))
        {
            RegisterOpenAIClient(builder.Services, openAIKey);
        }
        else
        {
            throw new InvalidOperationException("Must set AZURE_OPENAI_ENDPOINT or OPENAI_API_KEY environment variables.");
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

        return builder;
    }

    static void RegisterAzureOpenAIClient(IServiceCollection services, string azureOpenAIEndpoint, string azureOpenAIKey)
    {
        services.AddAzureOpenAIClient(azureOpenAIEndpoint, azureOpenAIKey);
    }

    static void RegisterAzureOpenAIADAuthClient(IServiceCollection services, string azureOpenAIEndpoint)
    {
        services.AddAzureOpenAIClientWithDefaultAzureCredential(azureOpenAIEndpoint);
    }

    static void RegisterOpenAIClient(IServiceCollection services, string openAIKey)
    {
        services.AddOpenAIClient(openAIKey);
    }
}
