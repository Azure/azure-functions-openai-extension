// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace AssistantSample;

/// <summary>
/// Todo item record which gets stored in the database.
/// </summary>
/// <param name="Id">A unique ID for the todo item.</param>
/// <param name="Task">A description of the task.</param>
public record TodoItem(string Id, string Task);

/// <summary>
/// Interface for managing todo items.
/// </summary>
public interface ITodoManager
{
    /// <summary>
    /// Adds a new todo item to the database.
    /// </summary>
    Task AddTodoAsync(TodoItem todo);

    /// <summary>
    /// Gets all todo items from the database.
    /// </summary>
    Task<IReadOnlyList<TodoItem>> GetTodosAsync();
}

/// <summary>
/// Implementation of <see cref="ITodoManager"/> which stores todo items in memory.
/// </summary>
/// <remarks>
/// This implementation is designed to be used when running the function app locally.
/// </remarks>
class InMemoryTodoManager : ITodoManager
{
    readonly List<TodoItem> todos = new();

    public Task AddTodoAsync(TodoItem todo)
    {
        this.todos.Add(todo);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TodoItem>> GetTodosAsync()
    {
        return Task.FromResult<IReadOnlyList<TodoItem>>(this.todos.ToImmutableList());
    }
}

/// <summary>
/// Implementation of <see cref="ITodoManager"/> which stores todo items in Cosmos DB.
/// </summary>
/// <remarks>
/// This implementation can be used in production or when running the function app locally.
/// This implementation is also suitable for when the app needs to be scaled out or when
/// the todos need to be persisted even after the function app is recycled.
/// </remarks>
class CosmosDbTodoManager : ITodoManager
{
    readonly ILogger logger;
    readonly Container container;

    public CosmosDbTodoManager(ILoggerFactory loggerFactory, CosmosClient cosmosClient)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        if (cosmosClient is null)
        {
            throw new ArgumentNullException(nameof(cosmosClient));
        }

        this.logger = loggerFactory.CreateLogger<CosmosDbTodoManager>();
        this.container = cosmosClient.GetContainer("testdb", "my-todos");
    }

    public async Task AddTodoAsync(TodoItem todo)
    {
        this.logger.LogInformation("Adding todo ID = {Id} to container '{Container}'.", todo.Id, this.container.Id);
        await this.container.CreateItemAsync(todo, new PartitionKey(todo.Id));
    }

    public async Task<IReadOnlyList<TodoItem>> GetTodosAsync()
    {
        this.logger.LogInformation("Getting all todos from container '{Container}'.", this.container.Id);
        FeedIterator<TodoItem> query = this.container.GetItemQueryIterator<TodoItem>(
            new QueryDefinition("SELECT * FROM c"));

        List<TodoItem> results = new();
        while (query.HasMoreResults)
        {
            FeedResponse<TodoItem> response = await query.ReadNextAsync();
            results.AddRange(response.Resource);
        }

        this.logger.LogInformation("Found {Count} todos in container '{Container}'.", results.Count, this.container.Id);
        return results;
    }
}
