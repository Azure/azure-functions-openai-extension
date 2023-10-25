// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Newtonsoft.Json.Linq;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.ResponseModels;
using WebJobs.Extensions.OpenAI.Agents;
using WebJobs.Extensions.OpenAI.Search;

namespace WebJobs.Extensions.OpenAI;

[Extension("OpenAI")]
partial class OpenAIExtension : IExtensionConfigProvider
{
    readonly IOpenAIService service;
    readonly TextCompletionConverter textCompletionConverter;
    readonly EmbeddingsConverter embeddingsConverter;
    readonly SemanticSearchConverter semanticSearchConverter;
    readonly ChatBotBindingConverter chatBotConverter;

    public OpenAIExtension(
        IOpenAIService service,
        TextCompletionConverter textCompletionConverter,
        EmbeddingsConverter embeddingsConverter,
        SemanticSearchConverter semanticSearchConverter,
        ChatBotBindingConverter chatBotConverter)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.textCompletionConverter = textCompletionConverter ?? throw new ArgumentNullException(nameof(textCompletionConverter));
        this.embeddingsConverter = embeddingsConverter ?? throw new ArgumentNullException(nameof(embeddingsConverter));
        this.semanticSearchConverter = semanticSearchConverter ?? throw new ArgumentNullException(nameof(semanticSearchConverter));
        this.chatBotConverter = chatBotConverter ?? throw new ArgumentNullException(nameof(chatBotConverter));
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

        // ChatBot support
        var chatBotCreateRule = context.AddBindingRule<ChatBotCreateAttribute>();
        chatBotCreateRule.BindToCollector<ChatBotCreateRequest>(this.chatBotConverter);
        context.AddConverter<JObject, ChatBotCreateRequest>(this.chatBotConverter.ToChatBotCreateRequest);
        context.AddConverter<string, ChatBotCreateRequest>(this.chatBotConverter.ToChatBotCreateRequest);

        var chatBotPostRule = context.AddBindingRule<ChatBotPostAttribute>();
        chatBotPostRule.BindToCollector<ChatBotPostRequest>(this.chatBotConverter);
        context.AddConverter<JObject, ChatBotPostRequest>(this.chatBotConverter.ToChatBotPostRequest);
        context.AddConverter<string, ChatBotPostRequest>(this.chatBotConverter.ToChatBotPostRequest);

        var chatBotQueryRule = context.AddBindingRule<ChatBotQueryAttribute>();
        chatBotQueryRule.BindToInput<ChatBotState>(this.chatBotConverter);
        chatBotQueryRule.BindToInput<string>(this.chatBotConverter);

        // OpenAI service input binding support (NOTE: This may be removed in a future version.)
        context.AddBindingRule<OpenAIServiceAttribute>().BindToInput(_ => this.service);
    }
}
