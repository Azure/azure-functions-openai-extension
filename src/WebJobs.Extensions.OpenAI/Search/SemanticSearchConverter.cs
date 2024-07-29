// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using Azure;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAISDK = OpenAI;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;

class SemanticSearchConverter :
    IAsyncConverter<SemanticSearchAttribute, SemanticSearchContext>,
    IAsyncConverter<SemanticSearchAttribute, string>
{
    readonly OpenAISDK.OpenAIClient openAIClient;
    readonly ILogger logger;
    readonly ISearchProvider? searchProvider;

    static readonly JsonSerializerOptions options = new()
    {
        Converters = { 
            new SearchableDocumentJsonConverter(),
            new EmbeddingsContextConverter(),
            new EmbeddingsOptionsJsonConverter(),
            new ChatCompletionsJsonConverter()}
        };

    public SemanticSearchConverter(
        ILoggerFactory loggerFactory,
        IEnumerable<ISearchProvider> searchProviders,
        IOptions<OpenAIConfigOptions> openAiConfigOptions)
    {
        this.logger = loggerFactory?.CreateLogger<SemanticSearchConverter>() ?? throw new ArgumentNullException(nameof(loggerFactory));

        openAiConfigOptions.Value.SearchProvider.TryGetValue("type", out object value);
        this.logger.LogInformation("Type of the searchProvider configured in host file: {type}", value);

        this.searchProvider = searchProviders?
            .FirstOrDefault(x => string.Equals(x.Name, value?.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    async Task<SemanticSearchContext> ConvertHelperAsync(
        SemanticSearchAttribute attribute,
        CancellationToken cancellationToken)
    {
        if (this.searchProvider == null)
        {
            throw new InvalidOperationException(
                "No search provider is configured. Search providers are configured in the host.json file. For .NET apps, the appropriate nuget package must also be added to the app's project file.");
        }

        if (string.IsNullOrEmpty(attribute.Query))
        {
            throw new InvalidOperationException("The query must be specified.");
        }

        // Get the embeddings for the query, which will be used for doing a semantic search
        OpenAISDK.EmbeddingsOptions embeddingsRequest = new(attribute.EmbeddingsModel, new List<string> { attribute.Query });

        this.logger.LogInformation("Sending OpenAI embeddings request: {request}", embeddingsRequest);
        Response<OpenAISDK.Embeddings> embeddingsResponse = await this.openAIClient.GetEmbeddingsAsync(embeddingsRequest, cancellationToken);
        this.logger.LogInformation("Received OpenAI embeddings response: {response}", embeddingsResponse);


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

        // Call the chat API with the new combined prompt to get a response back
        OpenAISDK.Chat.ChatCompletionOptions chatCompletionsOptions = new()
        {
            DeploymentName = attribute.ChatModel,
            Messages =
                {
                    new OpenAISDK.Chat.SystemChatMessage(promptBuilder.ToString()),
                    new OpenAISDK.Chat.UserChatMessage(attribute.Query),
                }
        };

        Response<OpenAISDK.ChatCompletions> chatResponse = await this.openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);

        // Give the user the full context, including the embeddings information as well as the chat info
        return new SemanticSearchContext(new EmbeddingsContext(embeddingsRequest, embeddingsResponse), chatResponse);
    }

    async Task<SemanticSearchContext> IAsyncConverter<SemanticSearchAttribute, SemanticSearchContext>.ConvertAsync(
        SemanticSearchAttribute attribute,
        CancellationToken cancellationToken)
    {
        return await this.ConvertHelperAsync(attribute, cancellationToken);
    }

    async Task<string> IAsyncConverter<SemanticSearchAttribute, string>.ConvertAsync(SemanticSearchAttribute input, CancellationToken cancellationToken)
    {
        SemanticSearchContext semanticSearchContext = await this.ConvertHelperAsync(input, cancellationToken);
        return JsonSerializer.Serialize(semanticSearchContext, options);
    }
}
