/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.azfs;

import java.util.List;
import java.util.UUID;

import com.azure.cosmos.CosmosClient;
import com.azure.cosmos.CosmosClientBuilder;
import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.openai.annotation.assistant.*;

/**
 * Defines assistant skills that can be triggered by the assistant chat bot.
 */
public class AssistantSkills {

    TodoManager todoManager = createTodoManager();

    /**
     * Called by the assistant to create new todo tasks.
     */
    @FunctionName("AddTodo")
    public void addTodo(
        @AssistantSkillTrigger(
                name = "assistantSkillCreateTodo",
                functionDescription = "Create a new todo task"
        ) String taskDescription,
        final ExecutionContext context) {

        if (taskDescription == null || taskDescription.isEmpty()) {
            throw new IllegalArgumentException("Task description cannot be empty");
        }
        context.getLogger().info("Adding todo: " + taskDescription);

        String todoId = UUID.randomUUID().toString().substring(0, 6);
        TodoItem todoItem = new TodoItem(todoId, taskDescription);
        todoManager.addTodo(todoItem);
    }

    /**
       Called by the assistant to fetch the list of previously created todo tasks.
     */
    @FunctionName("GetTodos")
    public List<TodoItem> getTodos(
        @AssistantSkillTrigger(
                name = "assistantSkillGetTodos",
                functionDescription = "Fetch the list of previously created todo tasks")
        Object inputIgnored,
        final ExecutionContext context) {

        context.getLogger().info("Fetching list of todos");
        return todoManager.getTodos();
    }

    private TodoManager createTodoManager() {
        String cosmosDbConnectionEndpoint = System.getenv("CosmosDbConnectionEndpoint");
        String cosmosDbKey = System.getenv("CosmosDbKey");

        if (cosmosDbConnectionEndpoint == null || cosmosDbConnectionEndpoint.isEmpty()
        || cosmosDbKey == null || cosmosDbKey.isEmpty()) {
            return new InMemoryTodoManager();
        } else {
            CosmosClient cosmosClient = new CosmosClientBuilder()
                    .endpoint(cosmosDbConnectionEndpoint)
                    .key(cosmosDbKey)
                    .buildClient();
            return new CosmosDbTodoManager(cosmosClient);
        }
    }
}

