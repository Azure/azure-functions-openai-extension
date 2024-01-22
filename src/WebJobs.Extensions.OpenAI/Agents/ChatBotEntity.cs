// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;

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

record struct MessageRecord(DateTime Timestamp, ChatMessageEntity ChatMessageEntity);

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
    readonly OpenAIClient openAIClient;

    static readonly Dictionary<string, Func<ChatMessageEntity, ChatRequestMessage>> messageFactories = new()
    {
        { ChatRole.User.ToString(), msg => new ChatRequestUserMessage(msg.Content) },
        { ChatRole.Assistant.ToString(), msg => new ChatRequestAssistantMessage(msg.Content) },
        { ChatRole.System.ToString(), msg => new ChatRequestSystemMessage(msg.Content) }
    };

    public ChatBotEntity(ILoggerFactory loggerFactory, OpenAIClient openAIClient)
    {
        // When initialized via dependency injection
        this.logger = loggerFactory.CreateLogger<ChatBotEntity>();
        this.openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
    }

    [JsonConstructor]
    ChatBotEntity()
    {
        // For deserialization
        this.logger = null!;
        this.openAIClient = null!;
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
                new List<MessageRecord>() { new(DateTime.UtcNow, new ChatMessageEntity(request.Instructions, ChatRole.System.ToString())) },
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
        this.State.ChatMessages.Add(new(DateTime.UtcNow, new ChatMessageEntity(request.UserMessage, ChatRole.User.ToString())));

        string deploymentName = request.Model ?? OpenAIModels.gpt_35_turbo;

        // Get the next response from the LLM
        ChatCompletionsOptions chatRequest = new (deploymentName, this.PopulateChatRequestMessages(this.State.ChatMessages.Select(x => x.ChatMessageEntity)));

        Response<ChatCompletions> response = await this.openAIClient.GetChatCompletionsAsync(chatRequest);

        // We don't normally expect more than one message, but just in case we get multiple messages,
        // return all of them separated by two newlines.
        string replyMessage = string.Join(
            Environment.NewLine + Environment.NewLine,
            response.Value.Choices.Select(choice => choice.Message.Content));

        this.logger.LogInformation(
            "[{Id}] Got LLM response consisting of {Count} tokens: {Text}",
            Entity.Current.EntityId,
            response.Value.Usage.CompletionTokens,
            replyMessage);

        this.State.ChatMessages.Add(new(DateTime.UtcNow, new ChatMessageEntity(replyMessage, ChatRole.Assistant.ToString())));

        this.logger.LogInformation(
            "[{Id}] Chat length is now {Count} messages",
            Entity.Current.EntityId,
            this.State.ChatMessages.Count);
    }

    internal IEnumerable<ChatRequestMessage> PopulateChatRequestMessages(IEnumerable<ChatMessageEntity> messages)
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