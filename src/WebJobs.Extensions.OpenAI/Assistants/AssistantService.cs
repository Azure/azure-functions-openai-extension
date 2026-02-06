// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
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
    TableServiceClient? tableServiceClient;
    TableClient? tableClient;

    public DefaultAssistantService(
        OpenAIClientFactory openAIClientFactory,
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
        this.logger = loggerFactory.CreateLogger<DefaultAssistantService>();
        this.openAIClientFactory = openAIClientFactory ?? throw new ArgumentNullException(nameof(openAIClientFactory));
        this.azureComponentFactory = azureComponentFactory ?? throw new ArgumentNullException(nameof(azureComponentFactory));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task CreateAssistantAsync(AssistantCreateRequest request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation(
            "[{Id}] Creating new chat session with instructions = \"{Text}\"",
            request.Id,
            request.Instructions ?? "(none)");

        TableClient tableClient = this.GetOrCreateTableClient(request.ChatStorageConnectionSetting, request.CollectionName);

        // Create the table if it doesn't exist
        await tableClient.CreateIfNotExistsAsync();

        if (request.PreserveChatHistory)
        {
            InternalChatState? existingState = await this.LoadChatStateAsync(request.Id, tableClient, cancellationToken);
            if (existingState is not null && existingState.Metadata.Exists)
            {
                await this.UpdateAssistantInstructionsAsync(request, existingState, tableClient, cancellationToken);
                return;
            }
        }
        else
        {
            await this.DeleteAssistantStateAsync(request.Id, tableClient, cancellationToken);
        }

        await this.CreateAssistantEntitiesAsync(request, tableClient, cancellationToken);
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

        this.logger.LogInformation("[{Id}] Received message: {Text}", attribute.Id, attribute.UserMessage);

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
    async Task DeleteAssistantStateAsync(string assistantId, TableClient tableClient, CancellationToken cancellationToken)
    {
        AsyncPageable<TableEntity> queryResultsFilter = tableClient.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{assistantId}'",
            cancellationToken: cancellationToken);

        List<TableTransactionAction> deleteBatch = new(capacity: 100);

        async Task DeleteBatch()
        {
            if (deleteBatch.Count > 0)
            {
                this.logger.LogInformation(
                    "Deleting {Count} record(s) for assistant '{Id}'.",
                    deleteBatch.Count,
                    assistantId);
                await tableClient.SubmitTransactionAsync(deleteBatch, cancellationToken);
                deleteBatch.Clear();
            }
        }

        await foreach (TableEntity entity in queryResultsFilter)
        {
            if (deleteBatch.Count >= 100)
            {
                await DeleteBatch();
            }

            deleteBatch.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
        }

        if (deleteBatch.Any())
        {
            await DeleteBatch();
        }
    }

    async Task CreateAssistantEntitiesAsync(AssistantCreateRequest request, TableClient tableClient, CancellationToken cancellationToken)
    {
        List<TableTransactionAction> batch = new();

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

        AssistantStateEntity assistantStateEntity = new(request.Id) { TotalMessages = batch.Count };

        batch.Add(new TableTransactionAction(TableTransactionActionType.Add, assistantStateEntity));

        await tableClient.SubmitTransactionAsync(batch, cancellationToken);
    }

    async Task UpdateAssistantInstructionsAsync(
        AssistantCreateRequest request,
        InternalChatState chatState,
        TableClient tableClient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Instructions))
        {
            this.logger.LogInformation(
                "[{Id}] PreserveChatHistory requested without new instructions. No changes applied.",
                request.Id);
            return;
        }

        ChatMessageTableEntity? systemMessage = chatState.Messages
            .Where(msg => string.Equals(msg.Role, ChatMessageRole.System.ToString(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(msg => msg.RowKey, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        List<TableTransactionAction> batch = new();

        if (systemMessage is not null)
        {
            systemMessage.Content = request.Instructions;
            systemMessage.CreatedAt = DateTime.UtcNow;
            batch.Add(new TableTransactionAction(TableTransactionActionType.UpdateMerge, systemMessage));
        }
        else
        {
            this.logger.LogWarning(
                "[{Id}] No existing system message found. Appending new system instructions.",
                request.Id);

            ChatMessageTableEntity newSystemMessage = new(
                partitionKey: request.Id,
                messageIndex: chatState.Metadata.TotalMessages + 1,
                content: request.Instructions,
                role: ChatMessageRole.System,
                toolCalls: null);

            chatState.Messages.Add(newSystemMessage);
            chatState.Metadata.TotalMessages++;
            batch.Add(new TableTransactionAction(TableTransactionActionType.Add, newSystemMessage));
        }

        chatState.Metadata.LastUpdatedAt = DateTime.UtcNow;
        batch.Add(new TableTransactionAction(TableTransactionActionType.UpdateMerge, chatState.Metadata));

        await tableClient.SubmitTransactionAsync(batch, cancellationToken);
    }
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
            "[{Id}] Got LLM response consisting of {Count} tokens: [{Text}] && {Count} ToolCalls",
            assistantId,
            response.Value.Usage.OutputTokenCount,
            replyMessage,
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
            "[{Id}] Calling function '{Name}' with arguments: {Args}",
            assistantId,
            call.FunctionName,
            call.FunctionArguments);

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
                "[{id}] Function '{Name}' returned the following content: {Content}",
                assistantId,
                call.FunctionName,
                result);

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
        if (this.tableClient is not null)
        {
            return this.tableClient;
        }

        string connectionStringName = chatStorageConnectionSetting ?? string.Empty;
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
            connectionStringName = chatStorageConnectionSetting ?? DefaultChatStorage;
            string connectionString = this.configuration.GetValue<string>(connectionStringName);
            
            this.logger.LogInformation("using connection string for table service client");

            this.tableServiceClient = new TableServiceClient(connectionString);
        }

        this.logger.LogInformation("Using {CollectionName} for table storage collection name", collectionName);
        this.tableClient = this.tableServiceClient.GetTableClient(collectionName);

        return this.tableClient;
    }
}