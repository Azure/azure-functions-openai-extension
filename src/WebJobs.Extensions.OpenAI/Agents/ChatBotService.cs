// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;

public interface IChatBotService
{
    Task CreateChatBotAsync(ChatBotCreateRequest request, CancellationToken cancellationToken);
    Task<ChatBotState> GetStateAsync(string id, DateTime since, CancellationToken cancellationToken);
    Task PostMessageAsync(ChatBotPostRequest request, CancellationToken cancellationToken);
}

    public class DefaultChatBotService : IChatBotService
{
    public BlobServiceClient blobClient { get; set; }

    public IOpenAIServiceProvider openAiServiceProvider { get; set; }
    readonly ILogger logger;

    ChatBotRuntimeState? State { get; set; }

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

        this.blobClient = new BlobServiceClient(
            new Uri("https://aibhandaritabletest.blob.core.windows.net"),
            new DefaultAzureCredential());

        this.openAiServiceProvider = openAIServiceProvider;
    }

    public void Initialize(ChatBotCreateRequest request)
    {
        this.logger.LogInformation(
            "[{Id}] Creating new chat session with expiration = {Timestamp} and instructions = \"{Text}\"",
            request.Id,
            request.ExpiresAt?.ToString("o") ?? "never",
            request.Instructions ?? "(none)");

        this.State = new ChatBotRuntimeState
        {
            ChatMessages = string.IsNullOrEmpty(request.Instructions) ?
                new List<MessageRecord>() :
                new List<MessageRecord>() { new(DateTime.UtcNow, ChatMessage.FromSystem(request.Instructions)) },
            ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddHours(24),
            Status = ChatBotStatus.Active,
        };
    }

    public async Task CreateChatBotAsync(ChatBotCreateRequest request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Creating chat bot durable entity with id '{Id}'", request.Id);

        // Create the container and return a container client object
        BlobContainerClient containerClient = this.blobClient.GetBlobContainerClient(request.Id);

        this.Initialize(request);

        // Create the container if it does not exist
        if (!containerClient.Exists())
        {
            await containerClient.CreateAsync();

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this.State))))
            {
                var blob = this.blobClient.GetBlobContainerClient(request.Id).GetBlobClient("chatbotstate.json");
                await blob.UploadAsync(ms);
            }
        }
    }

    public async Task<ChatBotState> GetStateAsync(string id, DateTime after, CancellationToken cancellationToken)
    { 
        this.logger.LogInformation(
            "Reading state for chat bot entity '{Id}' and getting chat messages after {Timestamp}",
            id,
            after.ToString("o"));

        BlobContainerClient containerClient = this.blobClient.GetBlobContainerClient(id);

        // Create the container if it does not exist
        if (!containerClient.Exists())
        {
            this.logger.LogInformation("Entity does not exist with ID '{Id}'", id);
            return new ChatBotState(id, false, ChatBotStatus.Uninitialized, default, default, 0, Array.Empty<ChatMessage>());
        }

        ChatBotRuntimeState runtimeState = null;

        var blob = this.blobClient.GetBlobContainerClient(id).GetBlobClient("chatbotstate.json");

        var response = await blob.DownloadAsync();

        using (var streamReader = new StreamReader(response.Value.Content))
        {
            var content = await streamReader.ReadToEndAsync();
            runtimeState = JsonConvert.DeserializeObject<ChatBotRuntimeState>(content);
        }

        if (runtimeState == null)
        {
            this.logger.LogWarning("Chat bot state is null for entity '{Id}'", id);
            return new ChatBotState(id, false, ChatBotStatus.Uninitialized, default, default, 0, Array.Empty<ChatMessage>());
        }

        IList<MessageRecord>? allChatMessages = runtimeState.ChatMessages;
        allChatMessages ??= Array.Empty<MessageRecord>();

        List<ChatMessage> filteredMessages = allChatMessages
            .Where(item => item.Timestamp > after)
            .Select(item => item.Message)
            .ToList();
        
        this.logger.LogInformation(
            "Returning {Count}/{Total} chat messages from entity '{Id}'",
            filteredMessages.Count,
            allChatMessages.Count, id);
        

        ChatBotState state = new(
            id,
            true,
            runtimeState.Status,
            allChatMessages.First().Timestamp,
            allChatMessages.Last().Timestamp,
            allChatMessages.Count,
            filteredMessages);
        return state;
    }

    public async Task PostAsync(ChatBotPostRequest request)
    {

        ChatBotRuntimeState runtimeState = null;

        var blob = this.blobClient.GetBlobContainerClient(request.Id).GetBlobClient("chatbotstate.json");

        var blobResponse = await blob.DownloadAsync();

        using (var streamReader = new StreamReader(blobResponse.Value.Content))
        {
            var content = await streamReader.ReadToEndAsync();
            runtimeState = JsonConvert.DeserializeObject<ChatBotRuntimeState>(content);
        }

        if (runtimeState is null || runtimeState.Status != ChatBotStatus.Active)
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

        runtimeState.ChatMessages ??= new List<MessageRecord>();
        runtimeState.ChatMessages.Add(new(DateTime.UtcNow, ChatMessage.FromUser(request.UserMessage)));

        // Get the next response from the LLM
        ChatCompletionCreateRequest chatRequest = new()
        {
            Messages = runtimeState.ChatMessages.Select(item => item.Message).ToList(),
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

        runtimeState.ChatMessages.Add(new(DateTime.UtcNow, ChatMessage.FromAssistant(replyMessage)));

        using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(runtimeState))))
        {
            var newBlob = this.blobClient.GetBlobContainerClient(request.Id).GetBlobClient("chatbotstate.json");
            await newBlob.UploadAsync(ms, overwrite: true);
        }

        this.logger.LogInformation(
            "[{Id}] Chat length is now {Count} messages",
            request.Id,
            runtimeState.ChatMessages.Count);
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