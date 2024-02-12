// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebJobs.Extensions.OpenAI;
using WebJobs.Extensions.OpenAI.Models;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;

public interface IChatBotService
{
    Task CreateChatBotAsync(ChatBotCreateRequest request, CancellationToken cancellationToken);
    Task<ChatBotState> GetStateAsync(string id, DateTime since, CancellationToken cancellationToken);
    Task PostMessageAsync(ChatBotPostRequest request, CancellationToken cancellationToken);
}

public class DefaultChatBotService : IChatBotService
{
    readonly TableClient tableClient;
    readonly TableServiceClient tableServiceClient;
    readonly OpenAIClient openAIClient;
    readonly ILogger logger;

    ChatBotRuntimeState? InitialState;
    static readonly Dictionary<string, Func<ChatMessageEntity, ChatRequestMessage>> MessageFactories = new()
    {
        { ChatRole.User.ToString(), msg => new ChatRequestUserMessage(msg.Content) },
        { ChatRole.Assistant.ToString(), msg => new ChatRequestAssistantMessage(msg.Content) },
        { ChatRole.System.ToString(), msg => new ChatRequestSystemMessage(msg.Content) }
    };

    public DefaultChatBotService(
        OpenAIClient openAiClient,
        IOptions<OpenAIConfigOptions> openAiConfigOptions,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        if (openAiClient is null)
        {
            throw new ArgumentNullException(nameof(openAiClient));
        }

        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        if (openAiConfigOptions is null)
        {
            throw new ArgumentNullException(nameof(openAiConfigOptions));
        }

        this.logger = loggerFactory.CreateLogger<DefaultChatBotService>();

        string connectionStringName = openAiConfigOptions.Value.StorageConnectionName;

        // Set connection string name to be AzureWebJobsStorage if it's null or empty
        if (string.IsNullOrEmpty(connectionStringName))
        {
            connectionStringName = "AzureWebJobsStorage";
        }

        this.logger.LogInformation("Using {ConnectionStringName} for table storage connection string name", connectionStringName);

        string connectionString = configuration.GetValue<string>(connectionStringName);

        this.tableServiceClient = new TableServiceClient(connectionString);
        this.tableClient = this.tableServiceClient.GetTableClient(openAiConfigOptions.Value.CollectionName);
        this.openAIClient = openAiClient;
    }

    void Initialize(ChatBotCreateRequest request)
    {
        this.logger.LogInformation(
            "[{Id}] Creating new chat session with instructions = \"{Text}\"",
            request.Id,
            request.Instructions ?? "(none)");

        this.InitialState = new ChatBotRuntimeState
        {
            ChatMessages = string.IsNullOrEmpty(request.Instructions) ?
                new List<ChatMessageEntity>() :
                new List<ChatMessageEntity>() { new ChatMessageEntity(request.Instructions, ChatRole.System.ToString()) },
        };
    }

    public async Task CreateChatBotAsync(ChatBotCreateRequest request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Creating chat bot with id '{Id}'", request.Id);

        // Create the table if it doesn't exist
        await this.tableClient.CreateIfNotExistsAsync();

        // Check to see if the chat bot has already been initialized
        Pageable<TableEntity> queryResultsFilter = this.tableClient.Query<TableEntity>(filter: $"PartitionKey eq '{request.Id}'");

        // Create a batch of table transaction actions
        List<TableTransactionAction> batch = new List<TableTransactionAction>();

        // Get all entities with the same partition key and add to batch for deletion
        foreach (TableEntity entity in queryResultsFilter)
        {
            this.logger.LogInformation("Deleting already existing entity with partition id {partitionKey} and row key {rowKey}", entity.PartitionKey, entity.RowKey);
            batch.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
        }

        // Get chatbot runtime state
        this.Initialize(request);

        // Add first chat message entity to table
        if (this.InitialState?.ChatMessages?.Count > 0)
        {
            ChatMessageEntity firstInstruction = this.InitialState.ChatMessages![0];
            ChatMessageTableEntity chatMessageEntity = new ChatMessageTableEntity
            {
                RowKey = this.GetRowKey(1), // ChatMessage00001
                PartitionKey = request.Id,
                ChatMessage = firstInstruction.Content,
                Role = firstInstruction.Role,
            };

            batch.Add(new TableTransactionAction(TableTransactionActionType.Add, chatMessageEntity));
        }

        // Add chat bot state entity to table
        ChatBotStateEntity chatBotStateEntity = new ChatBotStateEntity()
        {
            RowKey = "ChatBotState",
            PartitionKey = request.Id,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            Exists = true,
            TotalMessages = 1,
        };

        batch.Add(new TableTransactionAction(TableTransactionActionType.Add, chatBotStateEntity));

        // Add the batch of table transaction actions to the table
        await this.tableClient.SubmitTransactionAsync(batch);
        
    }

    public async Task<ChatBotState> GetStateAsync(string id, DateTime after, CancellationToken cancellationToken)
    { 
        this.logger.LogInformation(
            "Reading state for chat bot entity '{Id}' and getting chat messages after {Timestamp}",
            id,
        after.ToString("o"));

        string afterString = after.ToString("o");

        // Check to see if any entity exists with partition id
        AsyncPageable<TableEntity> itemsWithPartitionKey = this.tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{id}'");

        ChatBotStateEntity chatBotStateEntity = new();

        List<ChatMessageEntity> filteredChatMessages = new List<ChatMessageEntity>();
        await foreach (TableEntity chatMessage in itemsWithPartitionKey)
        {
            if (chatMessage.RowKey.StartsWith("ChatMessage"))
            {
                if (DateTimeOffset.Parse(chatMessage.Timestamp.ToString()).DateTime > after)
                {
                    filteredChatMessages.Add(new ChatMessageEntity(chatMessage["ChatMessage"].ToString(), chatMessage["Role"].ToString()));
                }
            }

            if (chatMessage.RowKey == "ChatBotState")
            {
                chatBotStateEntity = new ChatBotStateEntity
                {
                    RowKey = chatMessage.RowKey,
                    PartitionKey = chatMessage.PartitionKey,
                    ETag = chatMessage.ETag,
                    Timestamp = chatMessage.Timestamp,
                    CreatedAt = ((DateTimeOffset)chatMessage["CreatedAt"]).DateTime,
                    LastUpdatedAt = ((DateTimeOffset)chatMessage["LastUpdatedAt"]).DateTime,
                    TotalMessages = (int)chatMessage["TotalMessages"],
                    TotalTokens = (int)chatMessage["TotalTokens"],
                    Exists = (bool)chatMessage["Exists"],
                };
            }
        }

        if (chatBotStateEntity == null)
        {
            this.logger.LogWarning("No chat bot exists with ID = '{Id}'", id);
            return new ChatBotState(id, false, default, default, 0, 0, Array.Empty<ChatMessageEntity>());
        }

        this.logger.LogInformation(
            "Returning {Count}/{Total} chat messages from entity '{Id}'",
            filteredChatMessages.Count,
            chatBotStateEntity.TotalMessages, id);

        ChatBotState state = new(
            id,
            true,
            chatBotStateEntity.CreatedAt,
            chatBotStateEntity.LastUpdatedAt,
            chatBotStateEntity.TotalMessages,
            chatBotStateEntity.TotalTokens,
            filteredChatMessages);
        return state;
    }

    async Task PostAsync(ChatBotPostRequest request)
    {
        // Throw errors if the user input is invalid
        if (string.IsNullOrEmpty(request.Id))
        {
            throw new ArgumentException("The chat bot ID must be specified.", nameof(request));
        }

        if (string.IsNullOrEmpty(request.UserMessage))
        {
            throw new ArgumentException("The chat bot must have a user message", nameof(request));
        }

        // Check to see if any entity exists with partition id
        Pageable<TableEntity> itemsWithPartitionKey = this.tableClient.Query<TableEntity>(filter: $"PartitionKey eq '{request.Id}'");

        // No entities exist at with the partition key
        if (!itemsWithPartitionKey.Any())
        {
            this.logger.LogWarning("[{Id}] Ignoring message sent to chat bot that does not exist.", request.Id);
            return;
        }

        this.logger.LogInformation("[{Id}] Received message: {Text}", request.Id, request.UserMessage);

        // Deserialize the chat messages
        List<ChatMessageEntity> chatMessageList = new List<ChatMessageEntity>();
        ChatBotStateEntity chatBotStateEntity = new();

        foreach (TableEntity chatMessage in itemsWithPartitionKey)
        {
            // Add chat message to list
            if (chatMessage.RowKey.StartsWith("ChatMessage"))
            {
                chatMessageList.Add(new ChatMessageEntity(chatMessage["ChatMessage"].ToString(), chatMessage["Role"].ToString()));
            }

            // Get chat bot state
            if (chatMessage.RowKey == "ChatBotState")
            {
                chatBotStateEntity = new ChatBotStateEntity
                {
                    RowKey = chatMessage.RowKey,
                    PartitionKey = chatMessage.PartitionKey,
                    ETag = chatMessage.ETag,
                    CreatedAt = DateTime.SpecifyKind(((DateTimeOffset)chatMessage["CreatedAt"]).DateTime, DateTimeKind.Utc),
                    LastUpdatedAt = DateTime.SpecifyKind(((DateTimeOffset)chatMessage["LastUpdatedAt"]).DateTime, DateTimeKind.Utc),
                    TotalMessages = (int)chatMessage["TotalMessages"],
                    TotalTokens = (int)chatMessage["TotalTokens"],
                    Exists = (bool)chatMessage["Exists"],
                };
            }
        }

        // Check if chat bot has been deactivated
        if (chatBotStateEntity == null || chatBotStateEntity.Exists == false)
        {
            throw new NullReferenceException($"[{ request.Id }] Ignoring message sent to chat bot that has been deactivated.");
        }

        ChatMessageEntity chatMessageToSend = new ChatMessageEntity(request.UserMessage, ChatRole.User.ToString());
        chatMessageList.Add(chatMessageToSend);

        // Create a batch of table transaction actions
        List<TableTransactionAction> batch = new List<TableTransactionAction>();

        // Add the user message as a new Chat message entity
        ChatMessageTableEntity chatMessageEntity = new ChatMessageTableEntity
        {
            RowKey = this.GetRowKey(chatBotStateEntity.TotalMessages + 1), // Example: ChatMessage00012
            PartitionKey = request.Id,
            ChatMessage = request.UserMessage,
            Role = ChatRole.User.ToString()
        };

        // Add the chat message to the batch
        batch.Add(new TableTransactionAction(TableTransactionActionType.Add, chatMessageEntity));

        string deploymentName = request.Model ?? OpenAIModels.Gpt_35_Turbo;
        // Get the next response from the LLM
        ChatCompletionsOptions chatRequest = new(deploymentName, PopulateChatRequestMessages(chatMessageList));

        Response<ChatCompletions> response = await this.openAIClient.GetChatCompletionsAsync(chatRequest);

        // We don't normally expect more than one message, but just in case we get multiple messages,
        // return all of them separated by two newlines.
        string replyMessage = string.Join(
            Environment.NewLine + Environment.NewLine,
            response.Value.Choices.Select(choice => choice.Message.Content));

        this.logger.LogInformation(
            "[{Id}] Got LLM response consisting of {Count} tokens: {Text}",
            request.Id,
            response.Value.Usage.CompletionTokens,
            replyMessage);

        // Add the user message as a new Chat message entity
        ChatMessageTableEntity replyFromAssistantEntity = new ChatMessageTableEntity
        {
            RowKey = this.GetRowKey(chatBotStateEntity.TotalMessages + 2), // Example: ChatMessage00013
            PartitionKey = request.Id,
            ChatMessage = replyMessage,
            Role = ChatRole.Assistant.ToString()
        };

        // Add the reply from assistant chat message to the batch
        batch.Add(new TableTransactionAction(TableTransactionActionType.Add, replyFromAssistantEntity));

        this.logger.LogInformation(
            "[{Id}] Chat length is now {Count} messages",
            request.Id,
            chatBotStateEntity.TotalMessages + 2);

        chatBotStateEntity.LastUpdatedAt = DateTime.UtcNow;
        chatBotStateEntity.TotalMessages += 2;


        // Update the chat bot state entity
        batch.Add(new TableTransactionAction(TableTransactionActionType.UpdateMerge, chatBotStateEntity));

        // Add the batch of table transaction actions to the table
        await this.tableClient.SubmitTransactionAsync(batch);
    }

    public Task PostMessageAsync(ChatBotPostRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Id))
        {
            throw new ArgumentException("The chat bot ID must be specified.", nameof(request));
        }

        this.logger.LogInformation("Posting message to chat bot entity '{Id}'", request.Id);
        return this.PostAsync(request);
    }

    internal string GetRowKey(int messageNumber) => $"ChatMessage{messageNumber:D5}";

    internal static IEnumerable<ChatRequestMessage> PopulateChatRequestMessages(IEnumerable<ChatMessageEntity> messages)
    {
        foreach (ChatMessageEntity message in messages)
        {
            if (MessageFactories.TryGetValue(message.Role, out Func<ChatMessageEntity, ChatRequestMessage>? factory))
            {
                yield return factory.Invoke(message);
            }
            else
            {
                throw new InvalidOperationException($"Unknown chat role '{message.Role}'");
            }
        }
    }
}