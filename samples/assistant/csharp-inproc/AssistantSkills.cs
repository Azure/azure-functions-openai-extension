using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;

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
    public Task<IReadOnlyList<TodoItem>> GetTodos(
        [AssistantSkillTrigger("Fetch the list of previously created todo tasks")] object inputIgnored,
        ILogger log)
    {
        log.LogInformation("Fetching list of todos");

        return this.todoManager.GetTodosAsync();
    }
}
