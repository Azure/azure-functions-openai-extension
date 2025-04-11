// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

public class OpenAIClientFactory
{
    readonly IConfiguration configuration;
    readonly AzureComponentFactory azureComponentFactory;
    readonly ILogger logger;
    readonly ConcurrentDictionary<string, AzureOpenAIClient> azureOpenAIclients = new();
    readonly ConcurrentDictionary<string, OpenAIClient> openAIClients = new();
    readonly ConcurrentDictionary<string, (ChatClient, string, string)> chatClients = new(); // key is ai endpoint, model
    readonly ConcurrentDictionary<string, (EmbeddingClient, string, string)> embeddingClients = new(); // key is ai endpoint, model
    string aiEndpoint = string.Empty;

    public OpenAIClientFactory(
        IConfiguration configuration,
        AzureComponentFactory azureComponentFactory,
        ILoggerFactory loggerFactory)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.azureComponentFactory = azureComponentFactory ?? throw new ArgumentNullException(nameof(azureComponentFactory));
        this.logger = loggerFactory?.CreateLogger<OpenAIClientFactory>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public ChatClient GetChatClient(string aiConnectionName, string model)
    {
        HasOpenAIKey(out bool hasOpenAIKey, out string openAIKey);
        ChatClient chatClient;
        (chatClient, string endpoint, string chatModel) = this.chatClients.GetOrAdd(
           hasOpenAIKey ? "OpenAI" : aiConnectionName,
           name =>
           {
               if (!hasOpenAIKey)
               {
                   AzureOpenAIClient azureOpenAIClient = this.CreateClientFromConfigSection(aiConnectionName);
                   return (azureOpenAIClient.GetChatClient(model), this.aiEndpoint, model);
               }
               else
               {
                   OpenAIClient openAIClient = this.CreateOpenAIClient(openAIKey);
                   return (openAIClient.GetChatClient(model), this.aiEndpoint, model);
               }
           });

        return chatClient;
    }

    public EmbeddingClient GetEmbeddingClient(string aiConnectionName, string model)
    {
        HasOpenAIKey(out bool hasOpenAIKey, out string openAIKey);
        EmbeddingClient embeddingClient;
        (embeddingClient, string endpoint, string embeddingModel) = this.embeddingClients.GetOrAdd(
           hasOpenAIKey ? "OpenAI" : aiConnectionName,
           name =>
           {
               if (!hasOpenAIKey)
               {
                   AzureOpenAIClient azureOpenAIClient = this.CreateClientFromConfigSection(aiConnectionName);
                   return (azureOpenAIClient.GetEmbeddingClient(model), this.aiEndpoint, model);
               }
               else
               {
                   OpenAIClient openAIClient = this.CreateOpenAIClient(openAIKey);
                   return (openAIClient.GetEmbeddingClient(model), this.aiEndpoint, model);
               }
           });

        return embeddingClient;
    }

    static void HasOpenAIKey(out bool hasOpenAIKey, out string openAIKey)
    {
        hasOpenAIKey = false;
        openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(openAIKey))
        {
            hasOpenAIKey = true;
        }
    }

    AzureOpenAIClient CreateClientFromConfigSection(string aiConnectionName)
    {
        IConfigurationSection section = this.configuration.GetSection(aiConnectionName);

        if (!section.Exists())
        {
            this.logger.LogInformation($"Configuration section for Azure OpenAI not found, trying fallback to environment variables - AZURE_OPENAI_ENDPOINT and/or AZURE_OPENAI_KEY");
        }

        this.aiEndpoint = section?.GetValue<string>("Endpoint") ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? azureOpenAIKey = section?.GetValue<string>("Key") ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");

        if (!string.IsNullOrEmpty(this.aiEndpoint))
        {
            this.logger.LogInformation($"Using Azure OpenAI endpoint: {this.aiEndpoint}");
            if (!string.IsNullOrEmpty(azureOpenAIKey))
            {
                this.logger.LogInformation($"Authenticating using Azure OpenAI Key.");
                return this.CreateAzureOpenAIClient(this.aiEndpoint, azureOpenAIKey);
            }
            else
            {
                this.logger.LogInformation($"Authenticating using Azure OpenAI TokenCredential.");

                TokenCredential tokenCredential = section.Exists() ?
                    this.azureComponentFactory.CreateTokenCredential(section) :
                    new DefaultAzureCredential();
                return this.CreateAzureOpenAIClientWithTokenCredential(this.aiEndpoint, tokenCredential);
            }
        }

        string errorMessage = $"Configuration section '{aiConnectionName}' is missing required 'Endpoint' or 'Key' values.";
        this.logger.LogError(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }

    AzureOpenAIClient CreateAzureOpenAIClient(string endpoint, string apiKey)
    {
        string key = $"{endpoint}-{apiKey}";
        return this.azureOpenAIclients.GetOrAdd(key, _ => new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey)));
    }

    AzureOpenAIClient CreateAzureOpenAIClientWithTokenCredential(string endpoint, TokenCredential tokenCredential)
    {
        return this.azureOpenAIclients.GetOrAdd(endpoint, _ => new AzureOpenAIClient(new Uri(endpoint), tokenCredential));
    }

    OpenAIClient CreateOpenAIClient(string openAIKey)
    {
        this.logger.LogInformation($"Authenticating using OpenAI Key.");
        return this.openAIClients.GetOrAdd(openAIKey, _ => new OpenAIClient(openAIKey));
    }
}