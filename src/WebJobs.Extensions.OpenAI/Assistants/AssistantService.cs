// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

public interface IAssistantService
{
    Task CreateAssistantAsync(AssistantCreateRequest request, CancellationToken cancellationToken);
    Task<AssistantState> GetStateAsync(AssistantQueryAttribute assistantQuery, CancellationToken cancellationToken);
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
    readonly OpenAIClientFactory openAIClientFactory;
    readonly IAssistantSkillInvoker skillInvoker;
    readonly ILogger logger;
    readonly AzureComponentFactory azureComponentFactory;
    readonly IConfiguration configuration;
    readonly TableClientOptions? tableClientOptions;
    readonly Func<string?, string?, TableClient>? tableClientFactory;
    readonly ConcurrentDictionary<TableServiceClientCacheKey, TableServiceClient> tableServiceClients = new();

    public DefaultAssistantService(
        OpenAIClientFactory openAIClientFactory,
        AzureComponentFactory azureComponentFactory,
        IConfiguration configuration,
        IAssistantSkillInvoker skillInvoker,
        ILoggerFactory loggerFactory)
        : this(openAIClientFactory, azureComponentFactory, configuration, skillInvoker, loggerFactory, tableClientOptions: null)
    {
    }

    internal DefaultAssistantService(
        OpenAIClientFactory openAIClientFactory,
        AzureComponentFactory azureComponentFactory,
        IConfiguration configuration,
        IAssistantSkillInvoker skillInvoker,
        ILoggerFactory loggerFactory,
        TableClientOptions? tableClientOptions,
        Func<string?, string?, TableClient>? tableClientFactory = null)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        this.skillInvoker = skillInvoker ?? throw new ArgumentNullException(nameof(skillInvoker));
        this.logger = loggerFactory.CreateLogger<DefaultAssistantService>();
        this.openAIClientFactory = openAIClientFactory ?? throw new ArgumentNullException(nameof(openAIClientFactory));
        this.azureComponentFactory = azureComponentFactory ?? throw new ArgumentNullException(nameof(azureComponentFactory));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.tableClientOptions = tableClientOptions;
        this.tableClientFactory = tableClientFactory;
    }

    public async Task CreateAssistantAsync(AssistantCreateRequest request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("[{Id}] Creating new chat session", request.Id);

        TableClient tableClient = this.GetOrCreateTableClient(request.ChatStorageConnectionSetting, request.CollectionName);

        // Create the table if it doesn't exist
        await tableClient.CreateIfNotExistsAsync();

        // Check to see if the assistant has already been initialized
        AsyncPageable<TableEntity> queryResultsFilter = tableClient.QueryAsync<TableEntity>(
            filter: TableClient.CreateQueryFilter($"PartitionKey eq {request.Id}"),
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
                await tableClient.SubmitTransactionAsync(deleteBatch);
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
                role: ChatMessageRole.System,
                toolCalls: null);

            batch.Add(new TableTransactionAction(TableTransactionActionType.Add, chatMessageEntity));
        }

        // Add assistant state entity to table
        AssistantStateEntity assistantStateEntity = new(request.Id) { TotalMessages = batch.Count };

        batch.Add(new TableTransactionAction(TableTransactionActionType.Add, assistantStateEntity));

        // Add the batch of table transaction actions to the table
        await tableClient.SubmitTransactionAsync(batch);
    }

    public async Task<AssistantState> GetStateAsync(AssistantQueryAttribute assistantQuery, CancellationToken cancellationToken)
    {
        string id = assistantQuery.Id;
        string timestampString = Uri.UnescapeDataString(assistantQuery.TimestampUtc);
        if (!DateTime.TryParse(timestampString, out DateTime timestamp))
        {
            throw new ArgumentException($"Invalid timestamp '{assistantQuery.TimestampUtc}'");
        }

        DateTime afterUtc = timestamp.ToUniversalTime();
        this.logger.LogInformation(
            "Reading state for assistant entity '{Id}' and getting chat messages after {Timestamp}",
            id,
            afterUtc.ToString("o"));

        TableClient tableClient = this.GetOrCreateTableClient(assistantQuery.ChatStorageConnectionSetting, assistantQuery.CollectionName);

        InternalChatState? chatState = await this.LoadChatStateAsync(id, tableClient, cancellationToken);
        if (chatState is null)
        {
            this.logger.LogWarning("No assistant exists with ID = '{Id}'", id);
            return new AssistantState(id, false, default, default, 0, 0, Array.Empty<AssistantMessage>());
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
            filteredChatMessages.Select(msg => new AssistantMessage(msg.Content, msg.Role, msg.ToolCallsString)).ToList());
        return state;
    }

    public async Task<AssistantState> PostMessageAsync(AssistantPostAttribute attribute, CancellationToken cancellationToken)
    {
        // Validate inputs and prepare for processing
        DateTime timeFilter = DateTime.UtcNow;
        this.ValidateAttributes(attribute);

        this.logger.LogInformation("Posting message to assistant entity '{Id}'", attribute.Id);
        TableClient tableClient = this.GetOrCreateTableClient(attribute.ChatStorageConnectionSetting, attribute.CollectionName);

        // Load and validate chat state
        InternalChatState? chatState = await this.LoadChatStateAsync(attribute.Id, tableClient, cancellationToken);
        if (chatState is null || !chatState.Metadata.Exists)
        {
            return this.CreateNonExistentAssistantState(attribute.Id);
        }

        this.logger.LogInformation("[{Id}] Received message", attribute.Id);

        // Process the conversation
        List<TableTransactionAction> batch = new();
        this.AddUserMessageToChat(attribute, chatState, batch);

        await this.ProcessConversationWithLLM(attribute, chatState, batch, cancellationToken);

        // Update state and persist changes
        this.UpdateAssistantState(chatState, batch);
        await tableClient.SubmitTransactionAsync(batch, cancellationToken);

        // Return results
        return this.CreateAssistantStateResponse(attribute.Id, chatState, timeFilter);
    }

    // Helper methods
    void ValidateAttributes(AssistantPostAttribute attribute)
    {
        if (string.IsNullOrEmpty(attribute.Id))
        {
            throw new ArgumentException("The assistant ID must be specified.", nameof(attribute));
        }

        if (string.IsNullOrEmpty(attribute.UserMessage))
        {
            throw new ArgumentException("The assistant must have a user message", nameof(attribute));
        }
    }

    AssistantState CreateNonExistentAssistantState(string id)
    {
        this.logger.LogWarning("[{Id}] Ignoring request sent to nonexistent assistant.", id);
        return new AssistantState(id, false, default, default, 0, 0, Array.Empty<AssistantMessage>());
    }

    void AddUserMessageToChat(AssistantPostAttribute attribute, InternalChatState chatState, List<TableTransactionAction> batch)
    {
        ChatMessageTableEntity chatMessageEntity = new(
            partitionKey: attribute.Id,
            messageIndex: ++chatState.Metadata.TotalMessages,
            content: attribute.UserMessage,
            role: ChatMessageRole.User,
            toolCalls: null);
        chatState.Messages.Add(chatMessageEntity);
        batch.Add(new TableTransactionAction(TableTransactionActionType.Add, chatMessageEntity));
    }

    async Task ProcessConversationWithLLM(
        AssistantPostAttribute attribute,
        InternalChatState chatState,
        List<TableTransactionAction> batch,
        CancellationToken cancellationToken)
    {
        IList<ChatTool>? functions = this.skillInvoker.GetFunctionsDefinitions();

        // We loop if the model returns function calls. Otherwise, we break after receiving a response.
        while (true)
        {
            // Get the LLM response
            ClientResult<ChatCompletion> response = await this.GetLLMResponse(attribute, chatState, functions, cancellationToken);

            // Process text response if available
            string replyMessage = this.FormatReplyMessage(response);
            if (!string.IsNullOrWhiteSpace(replyMessage) || response.Value.ToolCalls.Any())
            {
                this.LogAndAddAssistantReply(attribute.Id, replyMessage, response, chatState, batch);
            }

            // Update token count
            chatState.Metadata.TotalTokens = response.Value.Usage.TotalTokenCount;

            // Handle function calls
            List<ChatToolCall> functionCalls = response.Value.ToolCalls.OfType<ChatToolCall>().ToList();
            if (functionCalls.Count == 0)
            {
                // No function calls, so we're done
                break;
            }

            if (batch.Count > FunctionCallBatchLimit)
            {
                // Too many function calls, something might be wrong
                this.LogBatchLimitExceeded(attribute.Id, functionCalls.Count);
                break;
            }

            // Process function calls
            await this.ProcessFunctionCalls(attribute.Id, functionCalls, chatState, batch, cancellationToken);
        }
    }

    async Task<ClientResult<ChatCompletion>> GetLLMResponse(
        AssistantPostAttribute attribute,
        InternalChatState chatState,
        IList<ChatTool>? functions,
        CancellationToken cancellationToken)
    {
        ChatCompletionOptions chatRequest = attribute.BuildRequest();
        if (functions is not null)
        {
            foreach (ChatTool fn in functions)
            {
                chatRequest.Tools.Add(fn);
            }
        }

        IEnumerable<ChatMessage> chatMessages = ToOpenAIChatRequestMessages(chatState.Messages);

        return await this.openAIClientFactory.GetChatClient(
            attribute.AIConnectionName,
            attribute.ChatModel).CompleteChatAsync(chatMessages, chatRequest, cancellationToken: cancellationToken);
    }

    string FormatReplyMessage(ClientResult<ChatCompletion> response)
    {
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            response.Value.Content.Select(message => message.Text));
    }

    void LogAndAddAssistantReply(
        string assistantId,
        string replyMessage,
        ClientResult<ChatCompletion> response,
        InternalChatState chatState,
        List<TableTransactionAction> batch)
    {
        this.logger.LogInformation(
            "[{Id}] Got LLM response consisting of {Count} tokens and {ToolCallCount} tool calls",
            assistantId,
            response.Value.Usage.OutputTokenCount,
            response.Value.ToolCalls.Count);

        ChatMessageTableEntity replyFromAssistantEntity = new(
            partitionKey: assistantId,
            messageIndex: ++chatState.Metadata.TotalMessages,
            content: replyMessage,
            role: ChatMessageRole.Assistant,
            toolCalls: response.Value.ToolCalls);

        chatState.Messages.Add(replyFromAssistantEntity);
        batch.Add(new TableTransactionAction(TableTransactionActionType.Add, replyFromAssistantEntity));

        this.logger.LogInformation(
            "[{Id}] Chat length is now {Count} messages",
            assistantId,
            chatState.Metadata.TotalMessages);
    }

    void LogBatchLimitExceeded(string assistantId, int functionCallCount)
    {
        this.logger.LogWarning(
            "[{Id}] Ignoring {Count} function call(s) in response due to exceeding the limit of {Limit}.",
            assistantId,
            functionCallCount,
            FunctionCallBatchLimit);
    }

    async Task ProcessFunctionCalls(
        string assistantId,
        List<ChatToolCall> functionCalls,
        InternalChatState chatState,
        List<TableTransactionAction> batch,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation(
            "[{Id}] Found {Count} function call(s) in response",
            assistantId,
            functionCalls.Count);

        foreach (ChatToolCall call in functionCalls)
        {
            await this.ProcessSingleFunctionCall(assistantId, call, chatState, batch, cancellationToken);
        }
    }

    async Task ProcessSingleFunctionCall(
        string assistantId,
        ChatToolCall call,
        InternalChatState chatState,
        List<TableTransactionAction> batch,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation(
            "[{Id}] Calling function '{Name}'",
            assistantId,
            call.FunctionName);

        string? functionResult = await this.InvokeFunctionWithErrorHandling(assistantId, call, cancellationToken);

        if (string.IsNullOrWhiteSpace(functionResult))
        {
            functionResult = "The function call succeeded. Let the user know that you completed the action.";
        }

        ChatMessageTableEntity functionResultEntity = new(
            partitionKey: assistantId,
            messageIndex: ++chatState.Metadata.TotalMessages,
            content: $"Function Name: '{call.FunctionName}' and Function Result: '{functionResult}'",
            role: ChatMessageRole.Tool,
            name: call.Id,
            toolCalls: null);

        chatState.Messages.Add(functionResultEntity);
        batch.Add(new TableTransactionAction(TableTransactionActionType.Add, functionResultEntity));
    }

    async Task<string?> InvokeFunctionWithErrorHandling(
        string assistantId,
        ChatToolCall call,
        CancellationToken cancellationToken)
    {
        try
        {
            // NOTE: In Consumption plans, calling a function from another function results in double-billing.
            // CONSIDER: Use a background thread to invoke the action to avoid double-billing.
            string? result = await this.skillInvoker.InvokeAsync(call, cancellationToken);

            this.logger.LogInformation(
                "[{id}] Function '{Name}' completed",
                assistantId,
                call.FunctionName);

            return result;
        }
        catch (Exception ex)
        {
            this.logger.LogError(
                ex,
                "[{id}] Function '{Name}' failed with an unhandled exception",
                assistantId,
                call.FunctionName);

            // CONSIDER: Automatic retries?
            return "The function call failed. Let the user know and ask if they'd like you to try again";
        }
    }

    void UpdateAssistantState(InternalChatState chatState, List<TableTransactionAction> batch)
    {
        chatState.Metadata.TotalMessages = chatState.Messages.Count;
        chatState.Metadata.LastUpdatedAt = DateTime.UtcNow;
        batch.Add(new TableTransactionAction(TableTransactionActionType.UpdateMerge, chatState.Metadata));
    }

    AssistantState CreateAssistantStateResponse(string assistantId, InternalChatState chatState, DateTime timeFilter)
    {
        List<ChatMessageTableEntity> filteredChatMessages = chatState.Messages
            .Where(msg => msg.CreatedAt > timeFilter && msg.Role == ChatMessageRole.Assistant.ToString())
            .ToList();

        this.logger.LogInformation(
            "Returning {Count}/{Total} chat messages from entity '{Id}'",
            filteredChatMessages.Count,
            chatState.Metadata.TotalMessages,
            assistantId);

        return new AssistantState(
            assistantId,
            true,
            chatState.Metadata.CreatedAt,
            chatState.Metadata.LastUpdatedAt,
            chatState.Metadata.TotalMessages,
            chatState.Metadata.TotalTokens,
            filteredChatMessages.Select(msg => new AssistantMessage(msg.Content, msg.Role, msg.ToolCallsString)).ToList());
    }

    async Task<InternalChatState?> LoadChatStateAsync(string id, TableClient tableClient, CancellationToken cancellationToken)
    {
        // Check to see if any entity exists with partition id
        AsyncPageable<TableEntity> itemsWithPartitionKey = tableClient.QueryAsync<TableEntity>(
            filter: TableClient.CreateQueryFilter($"PartitionKey eq {id}"),
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

    static IEnumerable<ChatMessage> ToOpenAIChatRequestMessages(IEnumerable<ChatMessageTableEntity> entities)
    {
        foreach (ChatMessageTableEntity entity in entities)
        {
            switch (entity.Role.ToLowerInvariant())
            {
                case "user":
                    yield return new UserChatMessage(entity.Content);
                    break;
                case "assistant":
                    if (entity.ToolCalls != null && entity.ToolCalls.Any())
                    {
                        yield return new AssistantChatMessage(entity.ToolCalls);
                    }
                    else
                    {
                        yield return new AssistantChatMessage(entity.Content);
                    }
                    break;
                case "system":
                    yield return new SystemChatMessage(entity.Content);
                    break;
                case "tool":
                    yield return new ToolChatMessage(toolCallId: entity.Name, entity.Content);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown chat role '{entity.Role}'");
            }
        }
    }

    TableClient GetOrCreateTableClient(string? chatStorageConnectionSetting, string? collectionName)
    {
        if (this.tableClientFactory is not null)
        {
            return this.tableClientFactory(chatStorageConnectionSetting, collectionName);
        }

        TableServiceClientCacheKey key = TableServiceClientCacheKey.Create(
            this.ResolveTableConnectionSettings(chatStorageConnectionSetting));

        TableServiceClient tableServiceClient = this.tableServiceClients.GetOrAdd(
            key,
            static (key, service) => service.CreateTableServiceClient(key),
            this);

        this.logger.LogDebug("Using {CollectionName} for table storage collection name", collectionName);
        return tableServiceClient.GetTableClient(collectionName);
    }

    TableServiceClient CreateTableServiceClient(TableServiceClientCacheKey key)
    {
        TableConnectionSettings settings = this.ResolveTableConnectionSettings(key.ConnectionSetting);
        if (TableServiceClientCacheKey.Create(settings) != key)
        {
            string errorMessage = $"The configuration for chat storage connection '{key.ConnectionSetting}' changed while a client was being created. Retry the operation.";
            this.logger.LogWarning(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // Check if URI for table storage is present
        if (!string.IsNullOrEmpty(settings.ServiceUri))
        {
            this.logger.LogInformation("Using Managed Identity");

            // Create an instance of TablesBindingOptions and set its properties
            TableBindingOptions tableOptions = new()
            {
                ServiceUri = new Uri(settings.ServiceUri),
                Credential = this.azureComponentFactory.CreateTokenCredential(settings.ConfigSection),
                TableClientOptions = this.tableClientOptions,
            };

            return tableOptions.CreateClient();
        }

        // Else, will use the connection string
        this.logger.LogInformation("using connection string for table service client");

        return this.tableClientOptions is null
            ? new TableServiceClient(settings.ConnectionString)
            : new TableServiceClient(settings.ConnectionString, this.tableClientOptions);
    }

    TableConnectionSettings ResolveTableConnectionSettings(string? chatStorageConnectionSetting)
    {
        string connectionSetting = string.IsNullOrEmpty(chatStorageConnectionSetting)
            ? DefaultChatStorage
            : chatStorageConnectionSetting!;
        IConfigurationSection tableConfigSection = this.configuration.GetSection(connectionSetting);
        string storageAccountUri = string.Empty;
        if (tableConfigSection.Exists())
        {
            storageAccountUri = tableConfigSection["tableServiceUri"] ?? string.Empty;
        }

        if (!string.IsNullOrEmpty(storageAccountUri))
        {
            return new TableConnectionSettings(connectionSetting, storageAccountUri, connectionString: null, tableConfigSection);
        }

        string? connectionString = this.configuration.GetValue<string>(connectionSetting);
        return new TableConnectionSettings(connectionSetting, serviceUri: null, connectionString, tableConfigSection);
    }

    /// <summary>
    /// Resolved chat storage settings.
    /// </summary>
    sealed class TableConnectionSettings
    {
        public TableConnectionSettings(string? connectionSetting, string? serviceUri, string? connectionString, IConfigurationSection configSection)
        {
            this.ConnectionSetting = connectionSetting;
            this.ServiceUri = serviceUri;
            this.ConnectionString = connectionString;
            this.ConfigSection = configSection;
        }

        public string? ConnectionSetting { get; }

        public string? ServiceUri { get; }

        public string? ConnectionString { get; }

        public IConfigurationSection ConfigSection { get; }
    }

    readonly struct TableServiceClientCacheKey : IEquatable<TableServiceClientCacheKey>
    {
        TableServiceClientCacheKey(string? connectionSetting, string? serviceUri, string? connectionStringFingerprint)
        {
            this.ConnectionSetting = connectionSetting;
            this.ServiceUri = serviceUri;
            this.ConnectionStringFingerprint = connectionStringFingerprint;
        }

        public string? ConnectionSetting { get; }

        public string? ServiceUri { get; }

        public string? ConnectionStringFingerprint { get; }

        public static TableServiceClientCacheKey Create(TableConnectionSettings settings) =>
            new(settings.ConnectionSetting, settings.ServiceUri, Fingerprint(settings.ConnectionString));

        static string? Fingerprint(string? value)
        {
            if (value is null)
            {
                return null;
            }

            using SHA256 sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }

        public static bool operator ==(TableServiceClientCacheKey left, TableServiceClientCacheKey right) => left.Equals(right);

        public static bool operator !=(TableServiceClientCacheKey left, TableServiceClientCacheKey right) => !left.Equals(right);

        public bool Equals(TableServiceClientCacheKey other) =>
            string.Equals(this.ConnectionSetting, other.ConnectionSetting, StringComparison.Ordinal)
            && string.Equals(this.ServiceUri, other.ServiceUri, StringComparison.Ordinal)
            && string.Equals(this.ConnectionStringFingerprint, other.ConnectionStringFingerprint, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is TableServiceClientCacheKey other && this.Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(this.ConnectionSetting, this.ServiceUri, this.ConnectionStringFingerprint);

        public override string ToString() => $"{this.ConnectionSetting}|{this.ServiceUri}";
    }
}