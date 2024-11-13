// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;
using Microsoft.Extensions.Logging;

namespace AssistantSample;

/// <summary>
/// Defines assistant skills that can be triggered by the assistant chat bot.
/// </summary>
public class AssistantSkills
{
    readonly ITodoManager todoManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantSkills"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is called by the Azure Functions runtime's dependency injection container.
    /// </remarks>
    public AssistantSkills(ITodoManager todoManager)
    {
        this.todoManager = todoManager ?? throw new ArgumentNullException(nameof(todoManager));
    }

    /// <summary>
    /// Called by the assistant to create new todo tasks.
    /// </summary>
    [FunctionName(nameof(AddTodo))]
    public Task AddTodo([AssistantSkillTrigger("Create a new todo task")] string taskDescription, ILogger log)
    {
        if (string.IsNullOrEmpty(taskDescription))
        {
            throw new ArgumentException("Task description cannot be empty");
        }

        log.LogInformation("Adding todo: {task}", taskDescription);

        string todoId = Guid.NewGuid().ToString()[..6];
        return this.todoManager.AddTodoAsync(new TodoItem(todoId, taskDescription));
    }

    /// <summary>
    /// Called by the assistant to fetch the list of previously created todo tasks.
    /// </summary>
    [FunctionName(nameof(GetTodos))]
    public async Task<IReadOnlyList<TodoItem>> GetTodos(
        [AssistantSkillTrigger("Fetch the list of previously created todo tasks")] object inputIgnored,
        ILogger log)
    {
        log.LogInformation("Fetching list of todos");

        IReadOnlyList<TodoItem> results = await this.todoManager.GetTodosAsync();
        return results;
    }
}