// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebJobs.Extensions.OpenAI.Agents;

class ChatBotBindingConverter :
    IConverter<ChatBotCreateAttribute, IAsyncCollector<ChatBotCreateRequest>>,
    IConverter<ChatBotPostAttribute, IAsyncCollector<ChatBotPostRequest>>,
    IAsyncConverter<ChatBotQueryAttribute, ChatBotState>,
    IAsyncConverter<ChatBotQueryAttribute, string>
{
    readonly IChatBotService chatBotService;
    readonly ILogger logger;

    public ChatBotBindingConverter(IChatBotService chatBotService, ILoggerFactory loggerFactory)
    {
        this.chatBotService = chatBotService ?? throw new ArgumentNullException(nameof(chatBotService));
        this.logger = loggerFactory.CreateLogger<ChatBotBindingConverter>();
    }

    public IAsyncCollector<ChatBotCreateRequest> Convert(ChatBotCreateAttribute attribute)
    {
        return new ChatBotCreateCollector(this.chatBotService, this.logger);
    }

    public IAsyncCollector<ChatBotPostRequest> Convert(ChatBotPostAttribute input)
    {
        return new ChatBotPostCollector(this.chatBotService, this.logger, input);
    }

    public Task<ChatBotState> ConvertAsync(
        ChatBotQueryAttribute input,
        CancellationToken cancellationToken)
    {
        string timestampString = Uri.UnescapeDataString(input.TimestampUtc);
        if (!DateTime.TryParse(timestampString, out DateTime timestamp))
        {
            throw new ArgumentException($"Invalid timestamp '{timestampString}'");
        }

        if (timestamp.Kind != DateTimeKind.Utc)
        {
            timestamp = timestamp.ToUniversalTime();
        }

        return this.chatBotService.GetStateAsync(input.Id, timestamp, cancellationToken);
    }

    async Task<string> IAsyncConverter<ChatBotQueryAttribute, string>.ConvertAsync(
        ChatBotQueryAttribute input,
        CancellationToken cancellationToken)
    {
        ChatBotState state = await this.ConvertAsync(input, cancellationToken);
        return JsonConvert.SerializeObject(state);
    }

    internal ChatBotCreateRequest ToChatBotCreateRequest(JObject json)
    {
        this.logger.LogDebug("Creating chat bot request from JObject: {Text}", json);
        return json.ToObject<ChatBotCreateRequest>() ?? throw new ArgumentException("Invalid chat bot create request");
    }

    // Called by the host when processing binding requests from out-of-process workers.
    internal ChatBotCreateRequest ToChatBotCreateRequest(string json)
    {
        this.logger.LogDebug("Creating chat bot request from JSON string: {Text}", json);
        return JsonConvert.DeserializeObject<ChatBotCreateRequest>(json) ?? throw new ArgumentException("Invalid chat bot create request");
    }

    internal ChatBotPostRequest ToChatBotPostRequest(JObject json)
    {
        this.logger.LogDebug("Creating chat bot post request from JObject: {Text}", json);
        return json.ToObject<ChatBotPostRequest>() ?? throw new ArgumentException("Invalid chat bot post request");
    }

    // Called by the host when processing binding requests from out-of-process workers.
    internal ChatBotPostRequest ToChatBotPostRequest(string json)
    {
        this.logger.LogDebug("Creating chat bot post request from JSON string: {Text}", json);
        return JsonConvert.DeserializeObject<ChatBotPostRequest>(json) ?? throw new ArgumentException("Invalid chat bot post request");
    }

    class ChatBotCreateCollector : IAsyncCollector<ChatBotCreateRequest>
    {
        readonly IChatBotService chatService;
        readonly ILogger logger;

        public ChatBotCreateCollector(IChatBotService chatService, ILogger logger)
        {
            this.chatService = chatService;
            this.logger = logger;
        }

        public async Task AddAsync(ChatBotCreateRequest item, CancellationToken cancellationToken = default)
        {
            ChatBotCreateRequest request = new(item.Id, item.Instructions);
            await this.chatService.CreateChatBotAsync(request, cancellationToken);
            this.logger.LogInformation("Created chat bot '{Id}'", request.Id);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    class ChatBotPostCollector : IAsyncCollector<ChatBotPostRequest>
    {
        readonly IChatBotService chatService;
        readonly ILogger logger;
        readonly ChatBotPostAttribute attribute;

        public ChatBotPostCollector(IChatBotService chatService, ILogger logger, ChatBotPostAttribute attribute)
        {
            this.chatService = chatService;
            this.logger = logger;
            this.attribute = attribute;
        }

        public Task AddAsync(ChatBotPostRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(request.Id))
            {
                request.Id = this.attribute.Id;
            }

            request.Model = this.attribute.Model;

            this.logger.LogInformation("Posting message to chat bot '{Id}': {Text}", request.Id, request.UserMessage);
            return this.chatService.PostMessageAsync(request, cancellationToken);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}