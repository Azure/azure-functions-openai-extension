// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using WebJobs.Extensions.OpenAI.Search;

namespace WebJobs.Extensions.OpenAI;

[Extension("OpenAI")]
partial class OpenAIExtension : IExtensionConfigProvider
{
    readonly IOpenAIService service;
    readonly TextCompletionConverter textCompletionConverter;
    readonly EmbeddingsConverter embeddingsConverter;
    readonly SemanticSearchConverter semanticSearchConverter;

    public OpenAIExtension(
        IOpenAIService service,
        TextCompletionConverter textCompletionConverter,
        EmbeddingsConverter embeddingsConverter,
        SemanticSearchConverter semanticSearchConverter)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.textCompletionConverter = textCompletionConverter ?? throw new ArgumentNullException(nameof(textCompletionConverter));
        this.embeddingsConverter = embeddingsConverter ?? throw new ArgumentNullException(nameof(embeddingsConverter));
        this.semanticSearchConverter = semanticSearchConverter ?? throw new ArgumentNullException(nameof(semanticSearchConverter));
    }

    void IExtensionConfigProvider.Initialize(ExtensionConfigContext context)
    {
        // Completions input binding support
        var rule = context.AddBindingRule<TextCompletionAttribute>();
        rule.BindToInput<CompletionCreateResponse>(this.textCompletionConverter);
        rule.BindToInput<string>(this.textCompletionConverter);

        // Embeddings input binding support
        var embeddingsRule = context.AddBindingRule<EmbeddingsAttribute>();
        embeddingsRule.BindToInput<EmbeddingsContext>(this.embeddingsConverter);
        embeddingsRule.BindToInput<string>(this.embeddingsConverter);

        // Semantic search input binding support
        var semanticSearchRule = context.AddBindingRule<SemanticSearchAttribute>();
        semanticSearchRule.BindToInput<SemanticSearchContext>(this.semanticSearchConverter);
        // TODO: Add string binding support to enable binding in non-.NET languages.
        semanticSearchRule.BindToCollector<SearchableDocument>(this.semanticSearchConverter);

        // OpenAI service input binding support (NOTE: This may be removed in a future version.)
        context.AddBindingRule<OpenAIServiceAttribute>().BindToInput(_ => this.service);
    }
}
