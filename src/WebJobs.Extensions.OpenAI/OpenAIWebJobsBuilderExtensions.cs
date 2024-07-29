// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
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

        // Add AzureComponentFactory to the services
        builder.Services.AddAzureClientsCore();

        // Register the client for Azure Open AI
        string? openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        IConfigurationRoot configuration = new ConfigurationBuilder()
           .AddEnvironmentVariables()
           .Build();

        IConfigurationSection azureOpenAIConfigSection = configuration.GetSection("AZURE_OPENAI");
        if (azureOpenAIConfigSection.Exists())
        {
            builder.Services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddOpenAIClient(azureOpenAIConfigSection);
            });
        }
        else if (!string.IsNullOrEmpty(openAIKey))
        {
            RegisterOpenAIClient(builder.Services, openAIKey);
        }
        else
        {
            throw new InvalidOperationException("Must set AZUREOPENAI configuration section (with Endpoint, Key or Credentials) or OPENAI_API_KEY environment variables.");
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
        return builder;
    }

    static void RegisterOpenAIClient(IServiceCollection services, string openAIKey)
    {
        var openAIClient = new OpenAIClient(openAIKey);
        services.AddSingleton<OpenAIClient>(openAIClient);
    }
}
