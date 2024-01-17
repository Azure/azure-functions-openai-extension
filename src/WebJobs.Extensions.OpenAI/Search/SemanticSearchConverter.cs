// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;

/// <summary>
/// Input binding target for the <see cref="SemanticSearchAttribute"/>.
/// </summary>
/// <param name="Embeddings">The embeddings context associated with the semantic search.</param>
/// <param name="Chat">The chat response from the large language model.</param>
public record SemanticSearchContext(EmbeddingsContext Embeddings, Response<ChatCompletions> Chat)
{
    /// <summary>
    /// Gets the latest response message from the OpenAI Chat API.
    /// </summary>
    public string Response => this.Chat.Value.Choices.Last().Message.Content;
}

class SemanticSearchConverter :
    IAsyncConverter<SemanticSearchAttribute, SemanticSearchContext>,
    IAsyncConverter<SemanticSearchAttribute, IAsyncCollector<SearchableDocument>>
{
    readonly OpenAIClient openAIClient;
    readonly ILogger logger;
    readonly ISearchProvider? searchProvider;

    public SemanticSearchConverter(
        OpenAIClient openAIClient,
        ILoggerFactory loggerFactory,
        ISearchProvider searchProvider)
    {
        this.openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
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
        EmbeddingsOptions embeddingsRequest = new(attribute.EmbeddingsModel, new List<string> { attribute.Query });
        Response<Embeddings> embeddingsResponse;
        try
        {
            this.logger.LogInformation("Sending OpenAI embeddings request: {request}", embeddingsRequest);
            embeddingsResponse = await this.openAIClient.GetEmbeddingsAsync(embeddingsRequest, cancellationToken);
            this.logger.LogInformation("Received OpenAI embeddings response: {response}", embeddingsResponse);
        }
        catch (Exception ex) when (attribute.ThrowOnError)
        {
            this.logger.LogError(ex, $"Error getting embeddings from OpenAI, message: {ex.Message}");
            throw new InvalidOperationException($"OpenAI returned an error: {ex.Message}");
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
            embeddingsResponse.Value.Data[0].Embedding,
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

        Response<ChatCompletions> chatResponse;
        try
        {
            // Call the chat API with the new combined prompt to get a response back
            ChatCompletionsOptions chatCompletionsOptions = new()
            {
                DeploymentName = attribute.ChatModel,
                Messages =
                {
                    new ChatRequestSystemMessage(promptBuilder.ToString()),
                    new ChatRequestUserMessage(attribute.Query),
                }
            };

            chatResponse = await this.openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
        }
        catch (Exception ex) when (attribute.ThrowOnError)
        {
            this.logger.LogError(ex, $"Error getting response from Chat Completion model, message: {ex.Message}");
            throw new InvalidOperationException($"OpenAI returned an error: {ex.Message}");
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
