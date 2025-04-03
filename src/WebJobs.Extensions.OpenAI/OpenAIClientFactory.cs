// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

public class OpenAIClientFactory
{
    static readonly ConcurrentDictionary<string, AzureOpenAIClient> _azureOpenAIclients = new();
    static readonly ConcurrentDictionary<string, OpenAIClient> _openAIClients = new();

    public static AzureOpenAIClient CreateAzureOpenAIClient(string endpoint, string apiKey)
    {
        string key = $"{endpoint}-{apiKey}";
        return _azureOpenAIclients.GetOrAdd(key, _ => new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey)));
    }

    public static AzureOpenAIClient CreateAzureOpenAIClientWithDefaultAzureCredential(string endpoint)
    {
        return _azureOpenAIclients.GetOrAdd(endpoint, _ => new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential()));
    }

    public static OpenAIClient CreateOpenAIClient(string apiKey)
    {
        return _openAIClients.GetOrAdd(apiKey, _ => new OpenAIClient(apiKey));
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureOpenAIClient(
        this IServiceCollection services,
        string endpoint,
        string apiKey)
    {
        services.AddSingleton(sp => OpenAIClientFactory.CreateAzureOpenAIClient(endpoint, apiKey));
        return services;
    }

    public static IServiceCollection AddAzureOpenAIClientWithDefaultAzureCredential(
        this IServiceCollection services,
        string endpoint)
    {
        services.AddSingleton(sp => OpenAIClientFactory.CreateAzureOpenAIClientWithDefaultAzureCredential(endpoint));
        return services;
    }

    public static IServiceCollection AddOpenAIClient(
        this IServiceCollection services,
        string apiKey)
    {
        services.AddSingleton(sp => OpenAIClientFactory.CreateOpenAIClient(apiKey));
        return services;
    }
}