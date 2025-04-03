// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Azure.WebJobs.Host.Config;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

[Extension("OpenAI")]
partial class OpenAIExtension : IExtensionConfigProvider
{
    readonly AzureOpenAIClient openAIClient;
    readonly TextCompletionConverter textCompletionConverter;
    readonly EmbeddingsConverter embeddingsConverter;
    readonly EmbeddingsStoreConverter embeddingsStoreConverter;
    readonly SemanticSearchConverter semanticSearchConverter;
    readonly AssistantBindingConverter assistantConverter;
    readonly AssistantSkillTriggerBindingProvider assistantskillTriggerBindingProvider;

    public OpenAIExtension(
        AzureOpenAIClient openAIClient,
        TextCompletionConverter textCompletionConverter,
        EmbeddingsConverter embeddingsConverter,
        EmbeddingsStoreConverter embeddingsStoreConverter,
        SemanticSearchConverter semanticSearchConverter,
        AssistantBindingConverter assistantConverter,
        AssistantSkillTriggerBindingProvider assistantTriggerBindingProvider)
    {
        this.openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
        this.textCompletionConverter = textCompletionConverter ?? throw new ArgumentNullException(nameof(textCompletionConverter));
        this.embeddingsConverter = embeddingsConverter ?? throw new ArgumentNullException(nameof(embeddingsConverter));
        this.embeddingsStoreConverter = embeddingsStoreConverter ?? throw new ArgumentNullException(nameof(embeddingsStoreConverter));
        this.semanticSearchConverter = semanticSearchConverter ?? throw new ArgumentNullException(nameof(semanticSearchConverter));
        this.assistantConverter = assistantConverter ?? throw new ArgumentNullException(nameof(assistantConverter));
        this.assistantskillTriggerBindingProvider = assistantTriggerBindingProvider ?? throw new ArgumentNullException(nameof(assistantTriggerBindingProvider));
    }

    void IExtensionConfigProvider.Initialize(ExtensionConfigContext context)
    {
        // Completions input binding support
        var rule = context.AddBindingRule<TextCompletionAttribute>();
        rule.BindToInput<TextCompletionResponse>(this.textCompletionConverter);
        rule.BindToInput<string>(this.textCompletionConverter);

        // Embeddings input binding support
        var embeddingsRule = context.AddBindingRule<EmbeddingsAttribute>();
        embeddingsRule.BindToInput<EmbeddingsContext>(this.embeddingsConverter);
        embeddingsRule.BindToInput<string>(this.embeddingsConverter);

        // Embeddings store binding support
        var embeddingsStoreRule = context.AddBindingRule<EmbeddingsStoreAttribute>();
        embeddingsStoreRule.BindToCollector<SearchableDocument>(this.embeddingsStoreConverter);
        context.AddConverter<string, SearchableDocument>(this.embeddingsStoreConverter.ToSearchableDocument);

        // Semantic search input binding support
        var semanticSearchRule = context.AddBindingRule<SemanticSearchAttribute>();
        semanticSearchRule.BindToInput<SemanticSearchContext>(this.semanticSearchConverter);
        semanticSearchRule.BindToInput<string>(this.semanticSearchConverter);

        // Assistant support
        var assistantCreateRule = context.AddBindingRule<AssistantCreateAttribute>();
        assistantCreateRule.BindToCollector<AssistantCreateRequest>(this.assistantConverter);
        context.AddConverter<JObject, AssistantCreateRequest>(this.assistantConverter.ToAssistantCreateRequest);
        context.AddConverter<string, AssistantCreateRequest>(this.assistantConverter.ToAssistantCreateRequest);

        var assistantPostRule = context.AddBindingRule<AssistantPostAttribute>();
        assistantPostRule.BindToInput<AssistantState>(this.assistantConverter);
        assistantPostRule.BindToInput<string>(this.assistantConverter);

        var assistantQueryRule = context.AddBindingRule<AssistantQueryAttribute>();
        assistantQueryRule.BindToInput<AssistantState>(this.assistantConverter);
        assistantQueryRule.BindToInput<string>(this.assistantConverter);

        // Assistant skill trigger support
        context.AddBindingRule<AssistantSkillTriggerAttribute>()
            .BindToTrigger(this.assistantskillTriggerBindingProvider);

        // OpenAI service input binding support (NOTE: This may be removed in a future version.)
        context.AddBindingRule<OpenAIServiceAttribute>().BindToInput(_ => this.openAIClient);
    }
}
