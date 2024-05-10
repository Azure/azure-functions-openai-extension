/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.azfs;

import java.util.ArrayList;
import java.util.List;

import com.azure.cosmos.CosmosClient;
import com.azure.cosmos.CosmosContainer;
import com.azure.cosmos.CosmosDatabase;
import com.azure.cosmos.models.CosmosContainerResponse;
import com.azure.cosmos.models.CosmosDatabaseResponse;
import com.azure.cosmos.models.CosmosQueryRequestOptions;
import com.azure.cosmos.util.CosmosPagedIterable;

/**
 * Interface for managing todo items.
 */
public interface TodoManager {

    /**
     * Adds a new todo item.
     * @param todoItem The todo item to add.
     */
    public void addTodo(TodoItem todoItem);

    /**
     * Gets all todo items.
     * @return A list of all todo items.
     */
    public List<TodoItem> getTodos();

}

/**
 * Todo item pojo which gets stored in the database.
 */
class TodoItem {

    // A unique ID for the todo item.
    private String id;

    // A description of the task.
    private String task;

    public TodoItem(String id, String task) {
        this.id = id;
        this.task = task;
    }
}

/**
 * Implementation of {@link TodoManager} which stores todo items in memory.
 * This implementation is designed to be used when running the function app locally.
 * This implementation is also suitable for testing purposes.
 */
class InMemoryTodoManager implements TodoManager {

    private final List<TodoItem> todos = new ArrayList<>();

    @Override
    public void addTodo(TodoItem todoItem) {
        this.todos.add(todoItem);        
    }

    @Override
    public List<TodoItem> getTodos() {
        return this.todos;
    }

}

/**
 * Implementation of {@link TodoManager} which stores todo items in Cosmos DB.
 * This implementation can be used in production or when running the function app locally.
 * This implementation is also suitable for when the app needs to be scaled out or when
 * the todos need to be persisted even after the function app is recycled.
/ */
class CosmosDbTodoManager implements TodoManager {

    CosmosClient cosmosClient;
    CosmosDatabase database;
    CosmosContainer container;

    public CosmosDbTodoManager(CosmosClient cosmosClient) {

        if(cosmosClient == null)
            throw new IllegalArgumentException("cosmosClient is required");

        this.cosmosClient = cosmosClient;

        String databaseName = System.getenv("CosmosDatabaseName");
        String containerName = System.getenv("CosmosContainerName");

        if( databaseName == null || databaseName.isEmpty() || containerName == null || containerName.isEmpty())
            throw new IllegalArgumentException("CosmosDatabaseName and CosmosContainerName environment variables are required");

        CosmosDatabaseResponse cosmosDatabaseResponse =
                this.cosmosClient.createDatabaseIfNotExists(databaseName);
        this.database = this.cosmosClient.getDatabase(cosmosDatabaseResponse.getProperties().getId());

        CosmosContainerResponse cosmosContainerResponse =
                this.database.createContainerIfNotExists(containerName, "/id");
        this.container = database.getContainer(cosmosContainerResponse.getProperties().getId());
    }

    @Override
    public void addTodo(TodoItem todoItem) {
        this.container.createItem(todoItem);
    }

    @Override
    public List<TodoItem> getTodos() {
        CosmosQueryRequestOptions queryOptions = new CosmosQueryRequestOptions();
        CosmosPagedIterable<TodoItem> todoItemCosmosPagedIterable = this.container.queryItems("SELECT * FROM c", queryOptions, TodoItem.class);

        List<TodoItem> results = new ArrayList<>();

        todoItemCosmosPagedIterable.iterableByPage(10).forEach(cosmosItemPropertiesFeedResponse -> {
            results.addAll(cosmosItemPropertiesFeedResponse.getResults());
        });
        return results;
    }
        
}