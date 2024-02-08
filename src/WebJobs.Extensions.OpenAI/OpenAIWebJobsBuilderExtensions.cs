// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Azure;
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
        Uri? azureOpenAIEndpoint = GetAzureOpenAIEndpoint();
        string? azureOpenAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        string? openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

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
        builder.Services.AddSingleton<SemanticSearchConverter>();
        builder.Services.AddSingleton<ChatBotBindingConverter>();
        builder.Services.AddSingleton<IChatBotService, DefaultChatBotService>();
        builder.Services
            .AddSingleton<AssistantSkillManager>()
            .AddSingleton<IAssistantSkillInvoker>(p => p.GetRequiredService<AssistantSkillManager>());
        builder.Services.AddSingleton<AssistantSkillTriggerBindingProvider>();
        return builder;
    }

    static Uri? GetAzureOpenAIEndpoint()
    {
        if (Uri.TryCreate(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"), UriKind.Absolute, out var uri))
        {
            return uri;
        }

        return null;
    }

    static void RegisterAzureOpenAIClient(IServiceCollection services, Uri azureOpenAIEndpoint, string azureOpenAIKey)
    {
        services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddOpenAIClient(azureOpenAIEndpoint, new AzureKeyCredential(azureOpenAIKey));
        });
    }

    static void RegisterAzureOpenAIADAuthClient(IServiceCollection services, Uri azureOpenAIEndpoint)
    {
        var managedIdentityClient = new OpenAIClient(azureOpenAIEndpoint, new DefaultAzureCredential());
        services.AddSingleton<OpenAIClient>(managedIdentityClient);
    }

    static void RegisterOpenAIClient(IServiceCollection services, string openAIKey)
    {
        var openAIClient = new OpenAIClient(openAIKey);
        services.AddSingleton<OpenAIClient>(openAIClient);
    }
}
