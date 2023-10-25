// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;

namespace WebJobs.Extensions.OpenAI.Agents;

public interface IChatBotEntity
{
    void Initialize(ChatBotCreateRequest request);
    Task PostAsync(ChatBotPostRequest request);
}

// IMPORTANT: Do not change the names or order of these enum values!
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatBotStatus
{
    Uninitialized,
    Active,
    Expired,
}

record struct MessageRecord(DateTime Timestamp, ChatMessage Message);

[JsonObject(MemberSerialization.OptIn)]
class ChatBotRuntimeState
{
    [JsonProperty("messages")]
    public List<MessageRecord>? ChatMessages { get; set; }

    [JsonProperty("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonProperty("status")]
    public ChatBotStatus Status { get; set; } = ChatBotStatus.Uninitialized;
}

[JsonObject(MemberSerialization.OptIn)]
class ChatBotEntity : IChatBotEntity
{
    readonly ILogger logger;
    readonly IOpenAIServiceProvider openAIServiceProvider;

    public ChatBotEntity(ILoggerFactory loggerFactory, IOpenAIServiceProvider openAIServiceProvider)
    {
        // When initialized via dependency injection
        this.logger = loggerFactory.CreateLogger<ChatBotEntity>();
        this.openAIServiceProvider = openAIServiceProvider ?? throw new ArgumentNullException(nameof(openAIServiceProvider));
    }

    [JsonConstructor]
    ChatBotEntity()
    {
        // For deserialization
        this.logger = null!;
        this.openAIServiceProvider = null!;
    }

    [JsonProperty("state")]
    public ChatBotRuntimeState? State { get; set; }

    public void Initialize(ChatBotCreateRequest request)
    {
        this.logger.LogInformation(
            "[{Id}] Creating new chat session with expiration = {Timestamp} and instructions = \"{Text}\"",
            Entity.Current.EntityId,
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

    public async Task PostAsync(ChatBotPostRequest request)
    {
        if (this.State is null || this.State.Status != ChatBotStatus.Active)
        {
            this.logger.LogWarning("[{Id}] Ignoring message sent to an uninitialized or expired chat bot.", Entity.Current.EntityId);
            return;
        }

        if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
        {
            this.logger.LogWarning("[{Id}] Ignoring empty message.", Entity.Current.EntityId);
            return;
        }

        this.logger.LogInformation("[{Id}] Received message: {Text}", Entity.Current.EntityId, request.UserMessage);

        this.State.ChatMessages ??= new List<MessageRecord>();
        this.State.ChatMessages.Add(new(DateTime.UtcNow, ChatMessage.FromUser(request.UserMessage)));
        
        // Get the next response from the LLM
        ChatCompletionCreateRequest chatRequest = new()
        {
            Messages = this.State.ChatMessages.Select(item => item.Message).ToList(),
            Model = Models.Gpt_3_5_Turbo,
        };

        IOpenAIService service = this.openAIServiceProvider.GetService(chatRequest.Model);
        ChatCompletionCreateResponse response = await service.ChatCompletion.CreateCompletion(chatRequest);
        if (!response.Successful)
        {
            // Throwing an exception will cause the entity to abort the current operation.
            // Any changes to the entity state will be discarded.
            Error error = response.Error ?? new Error() { MessageObject = "Unspecified error" };
            throw new ApplicationException($"The {chatRequest.Model} engine returned an error: {error}");
        }

        // We don't normally expect more than one message, but just in case we get multiple messages,
        // return all of them separated by two newlines.
        string replyMessage = string.Join(
            Environment.NewLine + Environment.NewLine,
            response.Choices.Select(choice => choice.Message.Content));

        this.logger.LogInformation(
            "[{Id}] Got LLM response consisting of {Count} tokens: {Text}",
            Entity.Current.EntityId,
            response.Usage.CompletionTokens,
            replyMessage);
        
        this.State.ChatMessages.Add(new(DateTime.UtcNow, ChatMessage.FromAssistant(replyMessage)));

        this.logger.LogInformation(
            "[{Id}] Chat length is now {Count} messages",
            Entity.Current.EntityId,
            this.State.ChatMessages.Count);
    }
}