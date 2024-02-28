using Functions.Worker.Extensions.OpenAI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AssistantSample;

/// <summary>
/// Defines assistant skills that can be triggered by the assistant chat bot.
/// </summary>
public class AssistantSkills
{
    readonly ITodoManager todoManager;
    readonly ILogger<AssistantSkills> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantSkills"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is called by the Azure Functions runtime's dependency injection container.
    /// </remarks>
    public AssistantSkills(ITodoManager todoManager, ILogger<AssistantSkills> logger)
    {
        this.todoManager = todoManager ?? throw new ArgumentNullException(nameof(todoManager));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Called by the assistant to create new todo tasks.
    /// </summary>
    [Function(nameof(AddTodo))]
    public Task AddTodo([AssistantSkillTrigger("Create a new todo task")] string taskDescription)
    {
        if (string.IsNullOrEmpty(taskDescription))
        {
            throw new ArgumentException("Task description cannot be empty");
        }

        this.logger.LogInformation("Adding todo: {task}", taskDescription);

        string todoId = Guid.NewGuid().ToString()[..6];
        return this.todoManager.AddTodoAsync(new TodoItem(todoId, taskDescription));
    }

    /// <summary>
    /// Called by the assistant to fetch the list of previously created todo tasks.
    /// </summary>
    [Function(nameof(GetTodos))]
    public Task<IReadOnlyList<TodoItem>> GetTodos(
        [AssistantSkillTrigger("Fetch the list of previously created todo tasks")] object inputIgnored)
    {
        this.logger.LogInformation("Fetching list of todos");

        return this.todoManager.GetTodosAsync();
    }
}
