// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
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
    readonly Func<Uri, AzureKeyCredential, AzureOpenAIClient> azureKeyClientFactory;
    readonly Func<Uri, TokenCredential, AzureOpenAIClient> azureTokenClientFactory;
    readonly Func<string, OpenAIClient> openAIClientFactory;

    readonly ConcurrentDictionary<string, AzureOpenAIClient> connectionClients = new();
    readonly ConcurrentDictionary<string, OpenAIClient> openAIClients = new();

    public OpenAIClientFactory(
        IConfiguration configuration,
        AzureComponentFactory azureComponentFactory,
        ILoggerFactory loggerFactory)
        : this(
            configuration,
            azureComponentFactory,
            loggerFactory,
            static (endpoint, credential) => new AzureOpenAIClient(endpoint, credential),
            static (endpoint, credential) => new AzureOpenAIClient(endpoint, credential))
    {
    }

    internal OpenAIClientFactory(
        IConfiguration configuration,
        AzureComponentFactory azureComponentFactory,
        ILoggerFactory loggerFactory,
        Func<Uri, AzureKeyCredential, AzureOpenAIClient> azureKeyClientFactory,
        Func<Uri, TokenCredential, AzureOpenAIClient> azureTokenClientFactory,
        Func<string, OpenAIClient>? openAIClientFactory = null)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.azureComponentFactory = azureComponentFactory ?? throw new ArgumentNullException(nameof(azureComponentFactory));
        this.logger = loggerFactory?.CreateLogger<OpenAIClientFactory>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        this.azureKeyClientFactory = azureKeyClientFactory ?? throw new ArgumentNullException(nameof(azureKeyClientFactory));
        this.azureTokenClientFactory = azureTokenClientFactory ?? throw new ArgumentNullException(nameof(azureTokenClientFactory));
        this.openAIClientFactory = openAIClientFactory ?? (static key => new OpenAIClient(key));
    }

    public ChatClient GetChatClient(string aiConnectionName, string model)
    {
        HasOpenAIKey(out bool hasOpenAIKey, out string openAIKey);

        return hasOpenAIKey
            ? this.CreateOpenAIClient(openAIKey).GetChatClient(model)
            : this.GetAzureOpenAIClient(aiConnectionName).GetChatClient(model);
    }

    public EmbeddingClient GetEmbeddingClient(string aiConnectionName, string model)
    {
        HasOpenAIKey(out bool hasOpenAIKey, out string openAIKey);

        return hasOpenAIKey
            ? this.CreateOpenAIClient(openAIKey).GetEmbeddingClient(model)
            : this.GetAzureOpenAIClient(aiConnectionName).GetEmbeddingClient(model);
    }

    AzureOpenAIClient GetAzureOpenAIClient(string aiConnectionName) =>
        this.connectionClients.GetOrAdd(
            aiConnectionName,
            static (name, factory) => factory.CreateClientFromConfigSection(name),
            this);

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

        string aiEndpoint = section?.GetValue<string>("Endpoint") ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? string.Empty;
        string? azureOpenAIKey = section?.GetValue<string>("Key") ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");

        if (!string.IsNullOrEmpty(aiEndpoint))
        {
            this.logger.LogInformation($"Using Azure OpenAI endpoint: {aiEndpoint}");
            if (!string.IsNullOrEmpty(azureOpenAIKey))
            {
                this.logger.LogInformation($"Authenticating using Azure OpenAI Key.");
                return this.azureKeyClientFactory(new Uri(aiEndpoint), new AzureKeyCredential(azureOpenAIKey));
            }
            else
            {
                this.logger.LogInformation($"Authenticating using Azure OpenAI TokenCredential.");
                TokenCredential tokenCredential = section.Exists() ?
                    this.azureComponentFactory.CreateTokenCredential(section) :
                    new DefaultAzureCredential();
                return this.azureTokenClientFactory(new Uri(aiEndpoint), tokenCredential);
            }
        }

        string errorMessage = $"Configuration section '{aiConnectionName}' is missing required 'Endpoint' and/or 'Key' values.";
        this.logger.LogError(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }

    OpenAIClient CreateOpenAIClient(string openAIKey)
    {
        return this.openAIClients.GetOrAdd(
            ComputeSha256Hex(openAIKey),
            _ =>
            {
                this.logger.LogInformation($"Authenticating using OpenAI Key.");
                return this.openAIClientFactory(openAIKey);
            });
    }

    static string ComputeSha256Hex(string value)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }
}
