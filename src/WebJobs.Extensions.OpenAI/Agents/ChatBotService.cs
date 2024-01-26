// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using WebJobs.Extensions.OpenAI.Agents;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;

public interface IChatBotService
{
    Task CreateChatBotAsync(ChatBotCreateRequest request, CancellationToken cancellationToken);
    Task<ChatBotState> GetStateAsync(string id, DateTime since, CancellationToken cancellationToken);
    Task PostMessageAsync(ChatBotPostRequest request, CancellationToken cancellationToken);
}

    public class DefaultChatBotService : IChatBotService
{
    public TableServiceClient tableServiceClient { get; set; }

    public BlobServiceClient blobClient { get; set; }

    public IOpenAIServiceProvider openAiServiceProvider { get; set; }
    readonly ILogger logger;

    ChatBotRuntimeState? InitialState { get; set; }

    public DefaultChatBotService(
        IOpenAIServiceProvider openAIServiceProvider,
        ILoggerFactory loggerFactory)
    {
        if (openAIServiceProvider is null)
        {
            throw new ArgumentNullException(nameof(openAIServiceProvider));
        }

        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        this.logger = loggerFactory.CreateLogger<DefaultChatBotService>();

        // TODO make a table service client provider to handle auth
        this.tableServiceClient = new TableServiceClient(
            new Uri("https://aibhandaritabletest.table.core.windows.net/?sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2024-07-20T02:15:16Z&st=2024-01-25T19:15:16Z&spr=https&sig=Gomjmn0SRnc6artAX6d0E9B1%2FHr7rTU%2BiDzhc2tALHw%3D"),
            new TableClientOptions());

        this.openAiServiceProvider = openAIServiceProvider;
    }

    public void Initialize(ChatBotCreateRequest request)
    {
        this.logger.LogInformation(
            "[{Id}] Creating new chat session with expiration = {Timestamp} and instructions = \"{Text}\"",
            request.Id,
            request.ExpiresAt?.ToString("o") ?? "never",
            request.Instructions ?? "(none)");

        this.InitialState = new ChatBotRuntimeState
        {
            ChatMessages = string.IsNullOrEmpty(request.Instructions) ?
                new List<MessageRecord>() :
                new List<MessageRecord>() { new(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc), ChatMessage.FromSystem(request.Instructions)) },
            ExpiresAt = request.ExpiresAt ?? DateTime.SpecifyKind(DateTime.UtcNow.AddHours(24), DateTimeKind.Utc),
            Status = ChatBotStatus.Active,
        };
    }

    public async Task CreateChatBotAsync(ChatBotCreateRequest request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Creating chat bot durable entity with id '{Id}'", request.Id);

        // Create the table if it doesn't exist
        this.tableServiceClient.CreateTableIfNotExists("ChatBotRequests");
        var tableClient = this.tableServiceClient.GetTableClient("ChatBotRequests");

        // Check to see if the chat bot has already been initialized
        Pageable<TableEntity> queryResultsFilter = tableClient.Query<TableEntity>(filter: $"PartitionKey eq '{request.Id}'");

        if (queryResultsFilter.Any())
        {
            return;
        }

        // Get chatbot runtime state
        this.Initialize(request);

        // Add first chat message entity to table
        if (this.InitialState.ChatMessages.Count > 0)
        {
            var chatMessageEntity = new ChatMessageEntity
            {
                RowKey = "ChatMessage0",
                PartitionKey = request.Id,
                ChatMessage = JsonConvert.SerializeObject(this.InitialState.ChatMessages[0]),
            };

            await tableClient.AddEntityAsync(chatMessageEntity);
        }

        // Add chat bot state entity to table
        var chatBotStateEntity = new ChatBotStateEntity()
        {
            RowKey = "ChatBotState",
            PartitionKey = request.Id,
            Status = this.InitialState.Status,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            LastUpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
        };
        await tableClient.AddEntityAsync(chatBotStateEntity);
        
    }

    public async Task<ChatBotState> GetStateAsync(string id, DateTime after, CancellationToken cancellationToken)
    { 
        this.logger.LogInformation(
            "Reading state for chat bot entity '{Id}' and getting chat messages after {Timestamp}",
            id,
            after.ToString("o"));

        var tableClient = this.tableServiceClient.GetTableClient("ChatBotRequests");

        //Check to see if ChatBotState entity exists with partition id
        var chatBotStateEntity = tableClient.Query<ChatBotStateEntity>(filter: $"RowKey eq 'ChatBotState' and PartitionKey eq '{id}'");

        if (chatBotStateEntity.Count() == 0)
        {
            this.logger.LogWarning("Chat bot state is null for entity '{Id}'", id);
            return new ChatBotState(id, false, ChatBotStatus.Uninitialized, default, default, 0, Array.Empty<ChatMessage>());
        }

        // Get all chat messages for this chat bot
        var allChatMessages = tableClient
            .Query<ChatMessageEntity>()
            .Where(entity => entity.RowKey.StartsWith("ChatMessage") && entity.PartitionKey == id)
            .ToList();

        // Filter the chat messages by the after timestamp
        DateTimeOffset afterOffset = new DateTimeOffset(after);
        var chatMessageEntity = allChatMessages.Where(entity => entity.Timestamp > afterOffset);

        IList<MessageRecord>? filteredChatMessages = new List<MessageRecord>();
        foreach (var chatMessage in chatMessageEntity)
        {
            filteredChatMessages.Add(JsonConvert.DeserializeObject<MessageRecord>(chatMessage.ChatMessage));
        }

        this.logger.LogInformation(
            "Returning {Count}/{Total} chat messages from entity '{Id}'",
            filteredChatMessages.Count,
            allChatMessages.Count(), id);

        var recentMessages = filteredChatMessages.Select(filteredChatMessages => filteredChatMessages.Message).ToList();

        ChatBotState state = new(
            id,
            true,
            chatBotStateEntity.First().Status,
            allChatMessages.First().Timestamp.Value.UtcDateTime,
            allChatMessages.Last().Timestamp.Value.UtcDateTime,
            allChatMessages.Count(),
            recentMessages);
        return state;
    }

    public async Task PostAsync(ChatBotPostRequest request)
    {
        var tableClient = this.tableServiceClient.GetTableClient("ChatBotRequests");

        //Check to see if ChatBotState entity exists with partition id
        var chatBotStateEntity = tableClient.Query<ChatBotStateEntity>(filter: $"RowKey eq 'ChatBotState' and PartitionKey eq '{request.Id}'");

        if (chatBotStateEntity.Count() == 0 || chatBotStateEntity.First().Status != ChatBotStatus.Active)
        {
            this.logger.LogWarning("[{Id}] Ignoring message sent to an uninitialized or expired chat bot.", request.Id);
            return;
        }

        if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
        {
            this.logger.LogWarning("[{Id}] Ignoring empty message.", request.Id);
            return;
        }

        this.logger.LogInformation("[{Id}] Received message: {Text}", request.Id, request.UserMessage);

        // Get all chat messages for this chat bot
        var allChatMessages = tableClient
            .Query<ChatMessageEntity>()
            .Where(entity => entity.RowKey.StartsWith("ChatMessage") && entity.PartitionKey == request.Id)
            .ToList();

        // Deserialize the chat messages
        var chatMessageList = new List<MessageRecord>();
        foreach (var chatMessage in allChatMessages)
        {
            chatMessageList.Add(JsonConvert.DeserializeObject<MessageRecord>(chatMessage.ChatMessage));
        }
        var chatMessageToSend = new MessageRecord(DateTime.UtcNow, ChatMessage.FromUser(request.UserMessage));
        chatMessageList.Add(chatMessageToSend);

        // Add the user message as a new Chat message entity
        var chatMessageEntity = new ChatMessageEntity
        {
            RowKey = "ChatMessage" + allChatMessages.Count(),
            PartitionKey = request.Id,
            ChatMessage = JsonConvert.SerializeObject(chatMessageToSend),
        };
        await tableClient.AddEntityAsync(chatMessageEntity);

        // Get the next response from the LLM
        ChatCompletionCreateRequest chatRequest = new()
        {
            Messages = chatMessageList.Select(item => item.Message).ToList(),
            Model = request.Model ?? Models.Gpt_3_5_Turbo,
        };

        IOpenAIService service = this.openAiServiceProvider.GetService(chatRequest.Model);
        ChatCompletionCreateResponse response = await service.ChatCompletion.CreateCompletion(chatRequest);
        if (!response.Successful)
        {
            // Throwing an exception will cause the entity to abort the current operation.
            // Any changes to the entity state will be discarded.
            Error error = response.Error ?? new Error() { Code = "Unspecified", MessageObject = "Unspecified error" };
            throw new ApplicationException($"The OpenAI {chatRequest.Model} engine returned a '{error.Code}' error: {error.Message}");
        }

        // We don't normally expect more than one message, but just in case we get multiple messages,
        // return all of them separated by two newlines.
        string replyMessage = string.Join(
            Environment.NewLine + Environment.NewLine,
            response.Choices.Select(choice => choice.Message.Content));

        this.logger.LogInformation(
            "[{Id}] Got LLM response consisting of {Count} tokens: {Text}",
            request.Id,
            response.Usage.CompletionTokens,
            replyMessage);

        var replyFromAssistant = new MessageRecord(DateTime.UtcNow, ChatMessage.FromAssistant(replyMessage));

        // Add the user message as a new Chat message entity
        var replyFromAssistantEntity = new ChatMessageEntity
        {
            RowKey = "ChatMessage" + (allChatMessages.Count() + 1),
            PartitionKey = request.Id,
            ChatMessage = JsonConvert.SerializeObject(replyFromAssistant),
        };
        await tableClient.AddEntityAsync(replyFromAssistantEntity);

        this.logger.LogInformation(
            "[{Id}] Chat length is now {Count} messages",
            request.Id,
            allChatMessages.Count() + 2);
    }
    public  Task PostMessageAsync(ChatBotPostRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Id))
        {
            throw new ArgumentException("The chat bot ID must be specified.", nameof(request));
        }

        this.logger.LogInformation("Posting message to chat bot entity '{Id}'", request.Id);
        return this.PostAsync(request);
    }
}