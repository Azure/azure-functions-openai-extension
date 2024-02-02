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
    ChatBotRuntimeState? InitialState { get; set; }
    readonly ILogger logger;

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
            Status = ChatBotStatus.Active,
        };
    }

    public async Task CreateChatBotAsync(ChatBotCreateRequest request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Creating chat bot durable entity with id '{Id}'", request.Id);

        // Create the table if it doesn't exist
        await this.tableClient.CreateIfNotExistsAsync();

        // Check to see if the chat bot has already been initialized
        Pageable<TableEntity> queryResultsFilter = this.tableClient.Query<TableEntity>(filter: $"PartitionKey eq '{request.Id}'");

        // Delete all entities with the same partition key
        foreach (var entity in queryResultsFilter)
        {
            this.logger.LogInformation("Deleting already existing entity with partition id {partitionKey} and row key {rowKey}", entity.PartitionKey, entity.RowKey);
            await this.tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
        }

        // Get chatbot runtime state
        this.Initialize(request);

        // Create a batch of table transaction actions
        List<TableTransactionAction> batch = new List<TableTransactionAction>();

        // Add first chat message entity to table
        if (this.InitialState?.ChatMessages?.Count > 0)
        {
            var firstInstruction = this.InitialState.ChatMessages![0];
            var chatMessageEntity = new ChatMessageTableEntity
            {
                RowKey = "ChatMessage00001",
                PartitionKey = request.Id,
                ChatMessage = firstInstruction.Content,
                Role = firstInstruction.Role,
            };

            batch.Add(new TableTransactionAction(TableTransactionActionType.Add, chatMessageEntity));
        }

        // Add chat bot state entity to table
        var chatBotStateEntity = new ChatBotStateEntity()
        {
            RowKey = "ChatBotState",
            PartitionKey = request.Id,
            Status = ChatBotStatus.Active,
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
        var itemsWithPartitionKey = this.tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{id}' and (RowKey eq 'ChatBotState' or Timestamp gt datetime'{afterString}')");

        ChatBotStateEntity chatBotStateEntity = new();

        IList<ChatMessageEntity>? filteredChatMessages = new List<ChatMessageEntity>();
        await foreach (var chatMessage in itemsWithPartitionKey)
        {
            if (chatMessage.RowKey.StartsWith("ChatMessage"))
            {
               filteredChatMessages.Add(new ChatMessageEntity(chatMessage["ChatMessage"].ToString(), chatMessage["Role"].ToString()));
            }

            if (chatMessage.RowKey == "ChatBotState")
            {
                Enum.TryParse(chatMessage["Status"].ToString(), out ChatBotStatus status);
                chatBotStateEntity = new ChatBotStateEntity
                {
                    RowKey = chatMessage.RowKey,
                    PartitionKey = chatMessage.PartitionKey,
                    ETag = chatMessage.ETag,
                    Timestamp = chatMessage.Timestamp,
                    Status = status,
                    CreatedAt = DateTime.Parse(chatMessage["CreatedAt"].ToString()),
                    LastUpdatedAt = DateTime.Parse(chatMessage["LastUpdatedAt"].ToString()),
                    TotalMessages = int.Parse(chatMessage["TotalMessages"].ToString()),
                    TotalTokens = int.Parse(chatMessage["TotalTokens"].ToString()),
                    Exists = bool.Parse(chatMessage["Exists"].ToString()),
                };
            }
        }

        if (chatBotStateEntity == null)
        {
            this.logger.LogWarning("No chat bot exists with ID = '{Id}'", id);
            return new ChatBotState(id, false, ChatBotStatus.Uninitialized, default, default, 0, 0, Array.Empty<ChatMessageEntity>());
        }

        this.logger.LogInformation(
            "Returning {Count}/{Total} chat messages from entity '{Id}'",
            filteredChatMessages.Count,
            chatBotStateEntity?.TotalMessages, id);

        var recentMessages = filteredChatMessages.ToList();

        ChatBotState state = new(
            id,
            true,
            chatBotStateEntity!.Status,
            chatBotStateEntity.CreatedAt,
            chatBotStateEntity.LastUpdatedAt,
            chatBotStateEntity.TotalMessages,
            chatBotStateEntity.TotalTokens,
            recentMessages);
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
        var itemsWithPartitionKey = this.tableClient.Query<TableEntity>(filter: $"PartitionKey eq '{request.Id}'");

        // No entities exist at with the partition key
        if (!itemsWithPartitionKey.Any())
        {
            this.logger.LogWarning("[{Id}] Ignoring message sent to chat bot that does not exist.", request.Id);
            return;
        }

        this.logger.LogInformation("[{Id}] Received message: {Text}", request.Id, request.UserMessage);

        // Deserialize the chat messages
        var chatMessageList = new List<ChatMessageEntity>();
        ChatBotStateEntity chatBotStateEntity = new();

        foreach (var chatMessage in itemsWithPartitionKey)
        {
            // Add chat message to list
            if (chatMessage.RowKey.StartsWith("ChatMessage"))
            {
                chatMessageList.Add(new ChatMessageEntity(chatMessage["ChatMessage"].ToString(), chatMessage["Role"].ToString()));
            }

            // Get chat bot state
            if (chatMessage.RowKey == "ChatBotState")
            {
                Enum.TryParse(chatMessage["Status"].ToString(), out ChatBotStatus status);
                chatBotStateEntity = new ChatBotStateEntity
                {
                    RowKey = chatMessage.RowKey,
                    PartitionKey = chatMessage.PartitionKey,
                    ETag = chatMessage.ETag,
                    Status = status,
                    CreatedAt = DateTime.SpecifyKind(DateTime.Parse(chatMessage["CreatedAt"].ToString()), DateTimeKind.Utc),
                    LastUpdatedAt = DateTime.SpecifyKind(DateTime.Parse(chatMessage["LastUpdatedAt"].ToString()), DateTimeKind.Utc),
                    TotalMessages = int.Parse(chatMessage["TotalMessages"].ToString()),
                    TotalTokens = int.Parse(chatMessage["TotalTokens"].ToString()),
                    Exists = bool.Parse(chatMessage["Exists"].ToString()),
                };
            }
        }

        // Check if chat bot has been deactivated
        if (chatBotStateEntity == null || chatBotStateEntity!.Status != ChatBotStatus.Active)
        {
            this.logger.LogWarning("[{Id}] Ignoring message sent to chat bot that has been deactivated. Current state of chat bot is {}.", request.Id, chatBotStateEntity.Status);
            return;
        }

        var chatMessageToSend = new ChatMessageEntity(request.UserMessage, ChatRole.User.ToString());
        chatMessageList.Add(chatMessageToSend);

        // Create a batch of table transaction actions
        List<TableTransactionAction> batch = new List<TableTransactionAction>();

        // Add the user message as a new Chat message entity
        var chatMessageEntity = new ChatMessageTableEntity
        {
            RowKey = "ChatMessage" + (chatBotStateEntity!.TotalMessages + 1).ToString("D5"),
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
        var replyFromAssistantEntity = new ChatMessageTableEntity
        {
            RowKey = "ChatMessage" + (chatBotStateEntity!.TotalMessages + 2).ToString("D5"),
            PartitionKey = request.Id,
            ChatMessage = replyMessage,
            Role = ChatRole.Assistant.ToString()
        };

        // Add the reply from assistant chat message to the batch
        batch.Add(new TableTransactionAction(TableTransactionActionType.Add, replyFromAssistantEntity));

        this.logger.LogInformation(
            "[{Id}] Chat length is now {Count} messages",
            request.Id,
            chatBotStateEntity!.TotalMessages + 2);

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

    static readonly Dictionary<string, Func<ChatMessageEntity, ChatRequestMessage>> messageFactories = new()
    {
        { ChatRole.User.ToString(), msg => new ChatRequestUserMessage(msg.Content) },
        { ChatRole.Assistant.ToString(), msg => new ChatRequestAssistantMessage(msg.Content) },
        { ChatRole.System.ToString(), msg => new ChatRequestSystemMessage(msg.Content) }
    };

    internal static IEnumerable<ChatRequestMessage> PopulateChatRequestMessages(IEnumerable<ChatMessageEntity> messages)
    {
        foreach (ChatMessageEntity message in messages)
        {
            if (messageFactories.TryGetValue(message.Role, out var factory))
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