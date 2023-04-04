// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace WebJobs.Extensions.OpenAI;

[Extension("OpenAI")]
partial class OpenAIExtension : IExtensionConfigProvider
{
    readonly IOpenAIService service;
    readonly ILogger logger;

    public OpenAIExtension(IOpenAIService service, ILoggerFactory loggerFactory)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.logger = loggerFactory.CreateLogger<OpenAIExtension>();
    }

    void IExtensionConfigProvider.Initialize(ExtensionConfigContext context)
    {
        // Completions input binding support
        CompletionCreateResponseConverter textCompletionConverter = new(this.service, this.logger);
        var rule = context.AddBindingRule<TextCompletionAttribute>();
        rule.BindToInput<CompletionCreateResponse>(textCompletionConverter);
        rule.BindToInput<string>(textCompletionConverter);

        // Embeddings input binding support
        EmbeddingsConverter embeddingsConverter = new(this.service, this.logger);
        var embeddingsRule = context.AddBindingRule<EmbeddingsAttribute>();
        embeddingsRule.BindToInput<EmbeddingCreateResponse>(embeddingsConverter);
        embeddingsRule.BindToInput<string>(embeddingsConverter);

        // OpenAI service input binding support (NOTE: This may be removed in a future version.)
        context.AddBindingRule<OpenAIServiceAttribute>().BindToInput(_ => this.service);
    }
}
