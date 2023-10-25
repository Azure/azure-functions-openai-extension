// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Interfaces;
using OpenAI.Managers;

namespace WebJobs.Extensions.OpenAI;

interface IOpenAIServiceProvider
{
    IOpenAIService GetService(string deploymentId);
}

class DefaultOpenAIServiceProvider : IOpenAIServiceProvider
{
    readonly ConcurrentDictionary<string, IOpenAIService> openAIServiceCache = new(StringComparer.OrdinalIgnoreCase);
    readonly OpenAiOptions defaultOptions;
    readonly IOpenAIService defaultService;

    public DefaultOpenAIServiceProvider(IOptions<OpenAiOptions> defaultOptions, IOpenAIService defaultService)
    {
        this.defaultOptions = defaultOptions?.Value ?? throw new ArgumentNullException(nameof(defaultOptions));
        this.defaultService = defaultService ?? throw new ArgumentNullException(nameof(defaultService));
    }

    public IOpenAIService GetService(string deploymentId)
    {
        // OpenAI: use the default service object
        if (this.defaultOptions.ProviderType == ProviderType.OpenAi)
        {
            return this.defaultService;
        }

        // Azure: We need to create a separate service object for each deployment ID
        return this.openAIServiceCache.GetOrAdd(
            deploymentId ?? string.Empty,
            id => new OpenAIService(this.GetClonedOptions(id)));
    }

    OpenAiOptions GetClonedOptions(string? deploymentId)
    {
        return new OpenAiOptions
        {
            ApiKey = this.defaultOptions.ApiKey,
            ApiVersion = this.defaultOptions.ApiVersion,
            BaseDomain = this.defaultOptions.BaseDomain,
            DeploymentId = deploymentId,
            Organization = this.defaultOptions.Organization,
            ProviderType = this.defaultOptions.ProviderType,
        };
    }
}
