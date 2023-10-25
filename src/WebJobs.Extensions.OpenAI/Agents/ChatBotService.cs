// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.ObjectModels.RequestModels;

namespace WebJobs.Extensions.OpenAI.Agents;

public interface IChatBotService
{
    Task CreateChatBotAsync(ChatBotCreateRequest request, CancellationToken cancellationToken);
    Task<ChatBotState> GetStateAsync(string id, DateTime since, CancellationToken cancellationToken);
    Task PostMessageAsync(ChatBotPostRequest request, CancellationToken cancellationToken);
}

public class DefaultChatBotService : IChatBotService
{
    readonly IDurableClient durableClient;
    readonly ILogger logger;

    public DefaultChatBotService(
        IDurableClientFactory durableClient,
        IOptions<DurableTaskOptions> durableOptions,
        ILoggerFactory loggerFactory)
    {
        if (durableClient is null)
        {
            throw new ArgumentNullException(nameof(durableClient));
        }

        if (durableOptions is null)
        {
            throw new ArgumentNullException(nameof(durableOptions));
        }

        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        this.logger = loggerFactory.CreateLogger<DefaultChatBotService>();
        this.logger.LogInformation("Creating durable client for hub '{TaskHub}'", durableOptions.Value.HubName);

        DurableClientOptions clientOptions = new()
        {
            TaskHub = durableOptions.Value.HubName,
            ConnectionName = "AzureWebJobsStorage",
            IsExternalClient = true,
        };

        this.durableClient = durableClient?.CreateClient(clientOptions) ?? throw new ArgumentNullException(nameof(durableClient));
    }

    public async Task CreateChatBotAsync(ChatBotCreateRequest request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Creating chat bot durable entity with id '{Id}'", request.Id);
        EntityId entityId = GetEntityId(request.Id);
        await this.durableClient.SignalEntityAsync<IChatBotEntity>(entityId, entity => entity.Initialize(request));
    }

    public async Task<ChatBotState> GetStateAsync(string id, DateTime after, CancellationToken cancellationToken)
    {
        EntityId entityId = GetEntityId(id);

        this.logger.LogInformation(
            "Reading state for chat bot entity '{Id}' and getting chat messages after {Timestamp}",
            entityId,
            after.ToString("o"));

        EntityStateResponse<ChatBotEntity> entityState =
            await this.durableClient.ReadEntityStateAsync<ChatBotEntity>(entityId);
        if (!entityState.EntityExists)
        {
            this.logger.LogInformation("Entity does not exist with ID '{Id}'", entityId);
            return new ChatBotState(id, false, ChatBotStatus.Uninitialized, default, default, 0, Array.Empty<ChatMessage>());
        }

        ChatBotRuntimeState? runtimeState = entityState.EntityState?.State;
        if (runtimeState == null)
        {
            this.logger.LogWarning("Chat bot state is null for entity '{Id}'", entityId);
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
            allChatMessages.Count, entityId);

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

    public Task PostMessageAsync(ChatBotPostRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Id))
        {
            throw new ArgumentException("The chat bot ID must be specified.", nameof(request));
        }

        EntityId entityId = GetEntityId(request.Id);
        this.logger.LogInformation("Posting message to chat bot entity '{Id}'", entityId);
        return this.durableClient.SignalEntityAsync<IChatBotEntity>(entityId, entity => entity.PostAsync(request));
    }

    static EntityId GetEntityId(string id) => new("OpenAI::ChatBotEntity", id);
}