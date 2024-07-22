// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Microsoft.Azure.WebJobs.Extensions.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

public interface IAssistantService
{
    Task CreateAssistantAsync(AssistantCreateRequest request, CancellationToken cancellationToken);
    Task<AssistantState> GetStateAsync(string id, DateTime since, CancellationToken cancellationToken);
    Task<AssistantState> PostMessageAsync(AssistantPostAttribute attribute, CancellationToken cancellationToken);
}

class DefaultAssistantService : IAssistantService
{
    record InternalChatState(string Id, AssistantStateEntity Metadata, List<ChatMessageTableEntity> Messages);

    /// <summary>
    /// The maximum number of messages we allow in a function call loop.
    /// This number must be small enough to ensure we never exceed a batch size of 100.
    /// </summary>
    const int FunctionCallBatchLimit = 50;
    const string DefaultChatStorage = "AzureWebJobsStorage";
    readonly OpenAIClient openAIClient;
    readonly IAssistantSkillInvoker skillInvoker;
    readonly ILogger logger;
    readonly AzureComponentFactory azureComponentFactory;
    readonly IConfiguration configuration;
    TableServiceClient? tableServiceClient;
    TableClient? tableClient;

    public DefaultAssistantService(
        OpenAIClient openAIClient,
        AzureComponentFactory azureComponentFactory,
        IConfiguration configuration,
        IAssistantSkillInvoker skillInvoker,
        ILoggerFactory loggerFactory)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        this.skillInvoker = skillInvoker ?? throw new ArgumentNullException(nameof(skillInvoker));
        this.openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));

        this.logger = loggerFactory.CreateLogger<DefaultAssistantService>();
        this.azureComponentFactory = azureComponentFactory ?? throw new ArgumentNullException(nameof(azureComponentFactory));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    

    public async Task CreateAssistantAsync(AssistantCreateRequest request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation(
            "[{Id}] Creating new chat session with instructions = \"{Text}\"",
            request.Id,
            request.Instructions ?? "(none)");

        this.CreateTableClient(request);

        if (this.tableClient is null)
        {
            throw new ArgumentNullException(nameof(this.tableClient));
        }

        // Create the table if it doesn't exist
        await this.tableClient.CreateIfNotExistsAsync();

        // Check to see if the assistant has already been initialized
        AsyncPageable<TableEntity> queryResultsFilter = this.tableClient.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{request.Id}'",
            cancellationToken: cancellationToken);

        // Create a batch of table transaction actions for deleting entities
        List<TableTransactionAction> deleteBatch = new(capacity: 100);

        // Local function for deleting batches of assistant state
        async Task DeleteBatch()
        {
            if (deleteBatch.Count > 0)
            {
                this.logger.LogInformation(
                    "Deleting {Count} record(s) for assistant '{Id}'.",
                    deleteBatch.Count,
                    request.Id);
                await this.tableClient.SubmitTransactionAsync(deleteBatch);
                deleteBatch.Clear();
            }
        }

        await foreach (TableEntity entity in queryResultsFilter)
        {
            // If the count is greater than or equal to 100, submit the transaction and clear the batch
            if (deleteBatch.Count >= 100)
            {
                await DeleteBatch();
            }

            deleteBatch.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
        }

        if (deleteBatch.Any())
        {
            // delete any remaining
            await DeleteBatch();
        }

        // Create a batch of table transaction actions
        List<TableTransactionAction> batch = new();

        // Add first chat message entity to table
        if (!string.IsNullOrWhiteSpace(request.Instructions))
        {
            ChatMessageTableEntity chatMessageEntity = new(
                partitionKey: request.Id,
                messageIndex: 1, // 1-based index
                content: request.Instructions,
                role: ChatRole.System);

            batch.Add(new TableTransactionAction(TableTransactionActionType.Add, chatMessageEntity));
        }

        // Add assistant state entity to table
        AssistantStateEntity assistantStateEntity = new(request.Id) { TotalMessages = batch.Count };

        batch.Add(new TableTransactionAction(TableTransactionActionType.Add, assistantStateEntity));

        // Add the batch of table transaction actions to the table
        await this.tableClient.SubmitTransactionAsync(batch);
    }

    public async Task<AssistantState> GetStateAsync(string id, DateTime after, CancellationToken cancellationToken)
    {
        DateTime afterUtc = after.ToUniversalTime();
        this.logger.LogInformation(
            "Reading state for assistant entity '{Id}' and getting chat messages after {Timestamp}",
            id,
            afterUtc.ToString("o"));

        InternalChatState? chatState = await this.LoadChatStateAsync(id, cancellationToken);
        if (chatState is null)
        {
            this.logger.LogWarning("No assistant exists with ID = '{Id}'", id);
            return new AssistantState(id, false, default, default, 0, 0, Array.Empty<ChatMessage>());
        }

        List<ChatMessageTableEntity> filteredChatMessages = chatState.Messages
            .Where(msg => msg.CreatedAt > afterUtc)
            .ToList();

        this.logger.LogInformation(
            "Returning {Count}/{Total} chat messages from entity '{Id}'",
            filteredChatMessages.Count,
            chatState.Metadata.TotalMessages, id);

        AssistantState state = new(
            id,
            true,
            chatState.Metadata.CreatedAt,
            chatState.Metadata.LastUpdatedAt,
            chatState.Metadata.TotalMessages,
            chatState.Metadata.TotalTokens,
            filteredChatMessages.Select(msg => new ChatMessage(msg.Content, msg.Role, msg.Name)).ToList());
        return state;
    }

    public async Task<AssistantState> PostMessageAsync(AssistantPostAttribute attribute, CancellationToken cancellationToken)
    {
        DateTime timeFilter = DateTime.UtcNow;
        if (string.IsNullOrEmpty(attribute.Id))
        {
            throw new ArgumentException("The assistant ID must be specified.", nameof(attribute));
        }

        if (string.IsNullOrEmpty(attribute.UserMessage))
        {
            throw new ArgumentException("The assistant must have a user message", nameof(attribute));
        }

        if (this.tableClient is null)
        {
            throw new ArgumentException("The assistant must be initialized first using CreateAssistantAsync", nameof(this.tableClient));
        }

        this.logger.LogInformation("Posting message to assistant entity '{Id}'", attribute.Id);

        InternalChatState? chatState = await this.LoadChatStateAsync(attribute.Id, cancellationToken);

        // Check if assistant has been deactivated
        if (chatState is null || !chatState.Metadata.Exists)
        {
            this.logger.LogWarning("[{Id}] Ignoring request sent to nonexistent assistant.", attribute.Id);
            return new AssistantState(attribute.Id, false, default, default, 0, 0, Array.Empty<ChatMessage>());
        }

        this.logger.LogInformation("[{Id}] Received message: {Text}", attribute.Id, attribute.UserMessage);

        // Create a batch of table transaction actions
        List<TableTransactionAction> batch = new();

        // Add the user message as a new Chat message entity
        ChatMessageTableEntity chatMessageEntity = new(
            partitionKey: attribute.Id,
            messageIndex: ++chatState.Metadata.TotalMessages,
            content: attribute.UserMessage,
            role: ChatRole.User);
        chatState.Messages.Add(chatMessageEntity);

        // Add the chat message to the batch
        batch.Add(new TableTransactionAction(TableTransactionActionType.Add, chatMessageEntity));

        string deploymentName = attribute.Model ?? OpenAIModels.DefaultChatModel;
        IList<ChatCompletionsFunctionToolDefinition>? functions = this.skillInvoker.GetFunctionsDefinitions();

        // We loop if the model returns function calls. Otherwise, we break after receiving a response.
        while (true)
        {
            // Get the next response from the LLM
            ChatCompletionsOptions chatRequest = new(deploymentName, ToOpenAIChatRequestMessages(chatState.Messages));
            if (functions is not null)
            {
                foreach (ChatCompletionsFunctionToolDefinition fn in functions)
                {
                    chatRequest.Tools.Add(fn);
                }
            }

            Response<ChatCompletions> response = await this.openAIClient.GetChatCompletionsAsync(
                chatRequest,
                cancellationToken);

            // We don't normally expect more than one message, but just in case we get multiple messages,
            // return all of them separated by two newlines.
            string replyMessage = string.Join(
                Environment.NewLine + Environment.NewLine,
                response.Value.Choices.Select(choice => choice.Message.Content));
            if (!string.IsNullOrWhiteSpace(replyMessage))
            {
                this.logger.LogInformation(
                    "[{Id}] Got LLM response consisting of {Count} tokens: {Text}",
                    attribute.Id,
                    response.Value.Usage.CompletionTokens,
                    replyMessage);

                // Add the user message as a new Chat message entity
                ChatMessageTableEntity replyFromAssistantEntity = new(
                    partitionKey: attribute.Id,
                    messageIndex: ++chatState.Metadata.TotalMessages,
                    content: replyMessage,
                    role: ChatRole.Assistant);
                chatState.Messages.Add(replyFromAssistantEntity);

                // Add the reply from assistant chat message to the batch
                batch.Add(new TableTransactionAction(TableTransactionActionType.Add, replyFromAssistantEntity));

                this.logger.LogInformation(
                    "[{Id}] Chat length is now {Count} messages",
                    attribute.Id,
                    chatState.Metadata.TotalMessages);
            }

            // Set the total tokens that have been consumed.
            chatState.Metadata.TotalTokens = response.Value.Usage.TotalTokens;

            // Check for function calls (which are described in the API as tools)
            List<ChatCompletionsFunctionToolCall> functionCalls = response.Value.Choices
                .SelectMany(c => c.Message.ToolCalls)
                .OfType<ChatCompletionsFunctionToolCall>()
                .ToList();
            if (functionCalls.Count == 0)
            {
                // No function calls, so we're done
                break;
            }

            if (batch.Count > FunctionCallBatchLimit)
            {
                // Too many function calls, something might be wrong. Break out of the loop
                // to avoid infinite loops and to avoid exceeding the batch size limit of 100.
                this.logger.LogWarning(
                    "[{Id}] Ignoring {Count} function call(s) in response due to exceeding the limit of {Limit}.",
                    attribute.Id,
                    functionCalls.Count,
                    FunctionCallBatchLimit);
                break;
            }

            // Loop case: found some functions to execute
            this.logger.LogInformation(
                "[{Id}] Found {Count} function call(s) in response",
                attribute.Id,
                functionCalls.Count);

            // Invoke the function calls and add the responses to the chat history.
            List<Task<object>> tasks = new(capacity: functionCalls.Count);
            foreach (ChatCompletionsFunctionToolCall call in functionCalls)
            {
                // CONSIDER: Call these in parallel
                this.logger.LogInformation(
                    "[{Id}] Calling function '{Name}' with arguments: {Args}",
                    attribute.Id,
                    call.Name,
                    call.Arguments);

                string? functionResult;
                try
                {
                    // NOTE: In Consumption plans, calling a function from another function results in double-billing.
                    // CONSIDER: Use a background thread to invoke the action to avoid double-billing.
                    functionResult = await this.skillInvoker.InvokeAsync(call, cancellationToken);

                    this.logger.LogInformation(
                        "[{id}] Function '{Name}' returned the following content: {Content}",
                        attribute.Id,
                        call.Name,
                        functionResult);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(
                        ex,
                        "[{id}] Function '{Name}' failed with an unhandled exception",
                        attribute.Id,
                        call.Name);

                    // CONSIDER: Automatic retries?
                    functionResult = "The function call failed. Let the user know and ask if they'd like you to try again";
                }

                if (string.IsNullOrWhiteSpace(functionResult))
                {
                    // When experimenting with gpt-4-0613, an empty result would cause the model to go into a
                    // function calling loop. By instead providing a result with some instructions, we were able
                    // to get the model to response to the user in a natural way.
                    functionResult = "The function call succeeded. Let the user know that you completed the action.";
                }

                ChatMessageTableEntity functionResultEntity = new(
                    partitionKey: attribute.Id,
                    messageIndex: ++chatState.Metadata.TotalMessages,
                    content: functionResult,
                    role: ChatRole.Function,
                    name: call.Name);
                chatState.Messages.Add(functionResultEntity);

                batch.Add(new TableTransactionAction(TableTransactionActionType.Add, functionResultEntity));
            }
        }

        // Update the assistant state entity
        chatState.Metadata.TotalMessages = chatState.Messages.Count;
        chatState.Metadata.LastUpdatedAt = DateTime.UtcNow;
        batch.Add(new TableTransactionAction(TableTransactionActionType.UpdateMerge, chatState.Metadata));

        // Add the batch of table transaction actions to the table
        await this.tableClient.SubmitTransactionAsync(batch, cancellationToken);

        // return the latest assistant message in the chat state
        List<ChatMessageTableEntity> filteredChatMessages = chatState.Messages
            .Where(msg => msg.CreatedAt > timeFilter && msg.Role == ChatRole.Assistant)
            .ToList();

        this.logger.LogInformation(
            "Returning {Count}/{Total} chat messages from entity '{Id}'",
            filteredChatMessages.Count,
            chatState.Metadata.TotalMessages,
            attribute.Id);

        AssistantState state = new(
            attribute.Id,
            true,
            chatState.Metadata.CreatedAt,
            chatState.Metadata.LastUpdatedAt,
            chatState.Metadata.TotalMessages,
            chatState.Metadata.TotalTokens,
            filteredChatMessages.Select(msg => new ChatMessage(msg.Content, msg.Role, msg.Name)).ToList());

        return state;
    }

    async Task<InternalChatState?> LoadChatStateAsync(string id, CancellationToken cancellationToken)
    {
        if (this.tableClient is null)
        {
            throw new ArgumentException("The assistant must be initialized first using CreateAssistantAsync", nameof(this.tableClient));
        }

        // Check to see if any entity exists with partition id
        AsyncPageable<TableEntity> itemsWithPartitionKey = this.tableClient.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{id}'",
            cancellationToken: cancellationToken);

        // Deserialize the chat messages
        List<ChatMessageTableEntity> chatMessageList = new();
        AssistantStateEntity? assistantStateEntity = null;

        await foreach (TableEntity entity in itemsWithPartitionKey)
        {
            // Add chat message to list
            if (entity.RowKey.StartsWith(ChatMessageTableEntity.RowKeyPrefix))
            {
                chatMessageList.Add(new ChatMessageTableEntity(entity));
            }

            // Get assistant state
            if (entity.RowKey == AssistantStateEntity.FixedRowKeyValue)
            {
                assistantStateEntity = new AssistantStateEntity(entity);
            }
        }

        if (assistantStateEntity is null)
        {
            return null;
        }

        return new InternalChatState(id, assistantStateEntity, chatMessageList);
    }

    static IEnumerable<ChatRequestMessage> ToOpenAIChatRequestMessages(IEnumerable<ChatMessageTableEntity> entities)
    {
        foreach (ChatMessageTableEntity entity in entities)
        {
            switch (entity.Role.ToLowerInvariant())
            {
                case "user":
                    yield return new ChatRequestUserMessage(entity.Content);
                    break;
                case "assistant":
                    yield return new ChatRequestAssistantMessage(entity.Content);
                    break;
                case "system":
                    yield return new ChatRequestSystemMessage(entity.Content);
                    break;
                case "function":
                    yield return new ChatRequestFunctionMessage(entity.Name, entity.Content);
                    break;
                case "tool":
                    yield return new ChatRequestToolMessage(entity.Content, toolCallId: entity.Name);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown chat role '{entity.Role}'");
            }
        }
    }

    void CreateTableClient(AssistantCreateRequest request)
    {
        string connectionStringName = request.ChatStorageConnectionSection ?? string.Empty;
        IConfigurationSection tableConfigSection = this.configuration.GetSection(connectionStringName);
        string storageAccountUri = string.Empty;
        if (tableConfigSection.Exists())
        {
            storageAccountUri = tableConfigSection["tableServiceUri"];
        }

        // Check if URI for table storage is present
        if (!string.IsNullOrEmpty(storageAccountUri))
        {
            this.logger.LogInformation("Using Managed Identity");

            // Create an instance of TablesBindingOptions and set its properties
            TableBindingOptions tableOptions = new()
            {
                ServiceUri = new Uri(storageAccountUri),
                Credential = this.azureComponentFactory.CreateTokenCredential(tableConfigSection)
            };

            // Now call CreateClient without any arguments
            this.tableServiceClient = tableOptions.CreateClient();

        }
        else
        {
            // Else, will use the connection string
            connectionStringName = request.ChatStorageConnectionSection ?? DefaultChatStorage;

            this.logger.LogInformation("Using {ConnectionStringName} for table storage connection string name", connectionStringName);

            string connectionString = this.configuration.GetValue<string>(connectionStringName);

            this.tableServiceClient = new TableServiceClient(connectionString);
        }
        this.logger.LogInformation("Using {CollectionName} for table storage collection name", request.CollectionName);
        this.tableClient = this.tableServiceClient.GetTableClient(request.CollectionName);
    }
}