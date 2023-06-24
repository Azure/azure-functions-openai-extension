// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace WebJobs.Extensions.OpenAI.Search;

/// <summary>
/// Input binding target for the <see cref="SemanticSearchAttribute"/>.
/// </summary>
/// <param name="Embeddings">The embeddings context associated with the semantic search.</param>
/// <param name="Chat">The chat response from the large language model.</param>
public record SemanticSearchContext(EmbeddingsContext Embeddings, ChatCompletionCreateResponse Chat)
{
    /// <summary>
    /// Gets the latest response message from the OpenAI Chat API.
    /// </summary>
    public string Response => this.Chat.Choices.Last().Message.Content;
}

class SemanticSearchConverter :
    IAsyncConverter<SemanticSearchAttribute, SemanticSearchContext>,
    IAsyncConverter<SemanticSearchAttribute, IAsyncCollector<SearchableDocument>>
{
    readonly IOpenAIService service;
    readonly ILogger logger;
    readonly ISearchProvider? searchProvider;

    public SemanticSearchConverter(IOpenAIService service, ILoggerFactory loggerFactory, ISearchProvider searchProvider)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.logger = loggerFactory?.CreateLogger<SemanticSearchConverter>() ?? throw new ArgumentNullException(nameof(loggerFactory));

        // This will be null if no search provider extension is configured
        // TODO: Eventually we need to resolve this by name at execution time by name so that we can support
        //       multiple search providers.
        this.searchProvider = searchProvider;
    }

    public Task<IAsyncCollector<SearchableDocument>> ConvertAsync(
        SemanticSearchAttribute input,
        CancellationToken cancellationToken)
    {
        if (this.searchProvider == null)
        {
            throw new InvalidOperationException(
                "No search provider is configured. Search providers can be added via nuget package references.");
        }

        IAsyncCollector<SearchableDocument> collector = new SemanticDocumentCollector(input, this.searchProvider);
        return Task.FromResult(collector);
    }

    async Task<SemanticSearchContext> IAsyncConverter<SemanticSearchAttribute, SemanticSearchContext>.ConvertAsync(
        SemanticSearchAttribute attribute,
        CancellationToken cancellationToken)
    {
        if (this.searchProvider == null)
        {
            throw new InvalidOperationException(
                "No search provider is configured. Search providers can be added via nuget package references.");
        }

        if (string.IsNullOrEmpty(attribute.Query))
        {
            throw new InvalidOperationException("The query must be specified.");
        }

        // Get the embeddings for the query, which will be used for doing a semantic search
        EmbeddingCreateRequest embeddingsRequest = new()
        {
            Input = attribute.Query,
            Model = attribute.EmbeddingsModel,
        };

        this.logger.LogInformation("Sending OpenAI embeddings request: {request}", embeddingsRequest);
        EmbeddingCreateResponse embeddingsResponse = await this.service.Embeddings.CreateEmbedding(
            embeddingsRequest,
            cancellationToken);
        this.logger.LogInformation("Received OpenAI embeddings response: {response}", embeddingsResponse);

        if (attribute.ThrowOnError && embeddingsResponse.Error is not null)
        {
            throw new InvalidOperationException(
                $"OpenAI returned an error of type '{embeddingsResponse.Error.Type}': {embeddingsResponse.Error.Message}");
        }

        ConnectionInfo connectionInfo = new(attribute.ConnectionName, attribute.Collection);
        if (string.IsNullOrEmpty(connectionInfo.ConnectionName))
        {
            throw new InvalidOperationException("No connection string information was provided.");
        }
        else if (string.IsNullOrEmpty(connectionInfo.CollectionName))
        {
            throw new InvalidOperationException("No collection name information was provided.");
        }

        // Search for relevant document snippets using the original query and the embeddings
        SearchRequest searchRequest = new(
            attribute.Query,
            embeddingsResponse.Data[0].Embedding,
            attribute.MaxKnowledgeCount,
            connectionInfo);
        SearchResponse searchResponse = await this.searchProvider.SearchAsync(searchRequest);

        // Append the fetched knowledge from the system prompt
        StringBuilder promptBuilder = new(capacity: 8 * 1024);
        promptBuilder.AppendLine(attribute.SystemPrompt);
        foreach (SearchResult result in searchResponse.OrderedResults)
        {
            promptBuilder.AppendLine(result.ToString());
        }

        // Call the chat API with the new combined prompt to get a response back
        ChatCompletionCreateRequest chatRequest = new()
        {
            Messages = new[]
            {
                ChatMessage.FromSystem(promptBuilder.ToString()),
                ChatMessage.FromUser(attribute.Query),
            },
            Model = attribute.ChatModel,
        };

        ChatCompletionCreateResponse chatResponse = await this.service.ChatCompletion.CreateCompletion(chatRequest);
        if (attribute.ThrowOnError && chatResponse.Error is not null)
        {
            throw new InvalidOperationException(
                $"OpenAI returned an error of type '{chatResponse.Error.Type}': {chatResponse.Error.Message}");
        }

        // Give the user the full context, including the embeddings information as well as the chat info
        return new SemanticSearchContext(new EmbeddingsContext(embeddingsRequest, embeddingsResponse), chatResponse);
    }

    sealed class SemanticDocumentCollector : IAsyncCollector<SearchableDocument>
    {
        readonly SemanticSearchAttribute attribute;
        readonly ISearchProvider searchProvider;

        public SemanticDocumentCollector(SemanticSearchAttribute attribute, ISearchProvider searchProvider)
        {
            this.attribute = attribute;
            this.searchProvider = searchProvider;
        }

        public Task AddAsync(SearchableDocument item, CancellationToken cancellationToken = default)
        {
            if (item.ConnectionInfo == null)
            {
                item.ConnectionInfo = new ConnectionInfo(this.attribute.ConnectionName, this.attribute.Collection);
            }

            if (string.IsNullOrEmpty(item.ConnectionInfo.ConnectionName))
            {
                throw new InvalidOperationException("No connection string information was provided.");
            }
            else if (string.IsNullOrEmpty(item.ConnectionInfo.CollectionName))
            {
                throw new InvalidOperationException("No collection name information was provided.");
            }

            return this.searchProvider.AddDocumentAsync(item, cancellationToken);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
