# Assistants

This sample demonstrates how to build an AI assistant chatbot with custom skills using Azure Functions and a local build of the OpenAI extension.
It builds upon the concepts [chatbot](../chatbot) sample, which demonstrates how to create a simple chatbot.
The sample is available in the following language stacks:

* [C# on the out of process worker](csharp-ooproc)
* [TypeScript on the Node.js worker](nodejs)
* [Powershell](powershell)
* [Python](python)
* [Java](java)

## Introduction

AI assistants are chat bots that can be configured with custom skills. Custom skills are implemented using the `assistantSkillTrigger` binding.
Custom skills are useful ways to extend the functionality of an AI assistant. For example, you can create a custom skill that saves a todo item to a database, or a custom skill that queries a database for a list of todo items.
In both cases, the skills are defined in your app and any the language model doesn't need to know anything about the skill implementation, making it useful for interacting with private data.

This OpenAI extension internally uses the [function calling](https://platform.openai.com/docs/guides/function-calling) functionality available in
`gpt-3.5-turbo` and `gpt-4` models to implement assistant skills.

## Supported model version and known issues

* [Supported model versions](https://platform.openai.com/docs/guides/function-calling/supported-models)
* 0301 is the default and oldest model version for gpt-3.5 but it doesn't support this feature.
* Model version 1106 has known issue with duplicate function calls in the OpenAI extension, check the repo issues for progress as the extension team works on it.

## Defining skills

You can define a skill by creating a function that uses the `AssistantSkillTrigger` binding. The following example shows a skill that adds a todo item to a database:

C# example:

```csharp
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
```

Nodejs example:

```ts
app.generic('AddTodo', {
    trigger: trigger.generic({
        type: 'assistantSkillTrigger',
        functionDescription: 'Create a new todo task'
    }),
    handler: async (taskDescription: string, context: InvocationContext) => {
        if (!taskDescription) {
            throw new Error('Task description cannot be empty')
        }

        context.log(`Adding todo: ${taskDescription}`)

        const todoId = crypto.randomUUID().substring(0, 6)
        return todoManager.AddTodo(new TodoItem(todoId, taskDescription))
    }
})
```

Python example:

```py

skills = func.Blueprint()
todo_manager = CreateTodoManager()

@skills.function_name("AddTodo")
@skills.assistant_skill_trigger(arg_name="taskDescription", function_description="Create a new todo task")
def add_todo(taskDescription: str) -> None:
    if not taskDescription:
        raise ValueError("Task description cannot be empty")

    logging.info(f"Adding todo: {taskDescription}")

    todo_id = str(uuid.uuid4())[0:6]
    todo_manager.add_todo(TodoItem(id=todo_id, task=taskDescription))
    return
```

PowerShell example:

```json
{
  "bindings": [
    {
      "name": "TaskDescription",
      "type": "assistantSkillTrigger",
      "dataType": "string",
      "direction": "in",
      "functionDescription": "Create a new todo task"
    }
  ]
}
```

```pwsh
using namespace System.Net

param($TaskDescription, $TriggerMetadata)
$ErrorActionPreference = "Stop"

if (-not $TaskDescription) {
    throw "Task description cannot be empty"
}

Write-Information "Adding todo: $TaskDescription"
$todoID = [Guid]::NewGuid().ToString().Substring(0, 5)
Add-Todo $todoId $TaskDescription
```

Java example:

```java

TodoManager todoManager = createTodoManager();

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
```

The `AssistantSkillTrigger` attribute requires a `FunctionDescription` string value, which is text describing what the function does.
This is critical for the AI assistant to be able to invoke the skill at the right time.
The name of the function parameter (e.g., `taskDescription`) is also an important hint to the AI assistant about what kind of information to provide to the skill.

> **NOTE:** The `AssistantSkillTrigger` attribute only supports primitive types as function parameters, such as `string`, `int`, `bool`, etc. Function return values can be of any JSON-serializable type.

Any function that uses the `AssistantSkillTrigger` binding will be automatically registered as a skill that can be invoked by any AI assistant.
The assistant will invoke a skill function whenever it decides to do so to satisfy a user prompt, and will provide function parameters based on the context of the conversation. The skill function can then return a response to the assistant, which will be used to satisfy the user prompt.

## Prerequisites

Please refer to the [root README](../../README.md#requirements) for common prerequisites that apply to all samples.

Additionally, if you want to run the sample with Cosmos DB, then you must also do the following:

* Install the [Azure Cosmos DB Emulator](https://docs.microsoft.com/azure/cosmos-db/local-emulator), or get a connection string to a real Azure Cosmos DB resource.
* Update the `CosmosDbConnectionString` setting in the `local.settings.json` file and configure it with the connection string to your Cosmos DB resource (local or Azure).

Also note that the storage of chat history is done via table storage. You may configure the `host.json` file within the project to be as follows:

```json
"extensions": {
    "openai": {
      "storageConnectionName": "AzureWebJobsStorage",
      "collectionName": "SampleChatState"
    }
}
```

`StorageConnectionName` is the name of connection string of a storage account and `CollectionName` is the name of the table that would hold the chat state and messages.

## Running the sample

1. Clone this repo and navigate to the sample folder.
1. Use a terminal window to navigate to the sample directory (e.g. `cd samples/assistant/csharp-ooproc`)
1. If using python, run `pip install -r requirements.txt` to install the correct library version.
1. Run `func start` to build and run the sample function app

    If successful, you should see the following output from the `func` command:

    ```plaintext
    Functions:

        CreateAssistant: [PUT] http://localhost:7168/api/assistants/{assistantId}

        GetChatState: [GET] http://localhost:7168/api/assistants/{assistantId}

        PostUserQuery: [POST] http://localhost:7168/api/assistants/{assistantId}

        AddTodo: assistantSkillTrigger

        GetTodos: assistantSkillTrigger
    ```

1. Use an HTTP client tool to send a `PUT` request to the `CreateAssistant` function. The following is an example request:

    ```http
    PUT http://localhost:7168/api/assistants/assistant123
    ```

    > **NOTE:** The `assistant123` value is the unique ID of the assistant. You can use any value you like, but it must be unique and must be used consistently in all subsequent requests.

    > **NOTE:** All the HTTP requests in this sample can also be found in the [demo.http](demo.http) file, which can be opened and run in most IDEs.

    The HTTP response should look something like the following:

    ```json
    {"assistantId":"assistant123"}
    ```

    You should also see some relevant log output in the terminal window where the app is running.

    The AI assistant is now created and ready to receive prompts.

1. Ask the assistant to create a todo item by sending a `POST` request to the `PostUserQuery` function to send a prompt to the assistant. The following is an example request:

    ```http
    ### Reminder #1 - Remind me to call my dad
    POST http://localhost:7071/api/assistants/assistant123?message=Remind%20me%20to%20call%20my%20dad

    ```

    The response should be an HTTP 200 and something like below:

    ```text
    I've added \"Call my dad\" to your todo list.
    ```

    In the function log output, you should observe that the `AddTodo` function was triggered. This function is a custom skill that was automatically registered with the assistant when the app was started.

1. Ask the assistant to create another todo item using another `POST` request to the `PostUserQuery` function. The following is an example request:

    ```http
    ### Reminder #2 - Oh, and to take out the trash
    POST http://localhost:7071/api/assistants/assistant123?message=Oh,%20and%20to%20take%20out%20the%20trash

    ```

    The AI assistant remembers the context from the chat, so it knows that you're still talking about todo items, even if your prompt doesn't mention this explicitly.

1. If you're running the sample with Cosmos DB persistence configured, then you can use the Cosmos DB Data Explorer to view the documents in the `my-todos` collection in the `testdb` database. You should see two documents, one for each todo item that the assistant created.

1. Ask the assistant to list your todo items by sending a `POST` request to the `PostUserQuery` function. The following is an example request:

    ```http
    ### Get the list of tasks - What do I need to do today?
    POST http://localhost:7071/api/assistants/assistant123?message=What%20do%20I%20need%20to%20do%20today%3F

    ```

    The response should be an HTTP 200 and something like below -

    ```text
    Here are your tasks for today:\n\n1. Call my dad\n2. Take out the trash
    ```

    In the function log output, you should observe that the `GetTodos` function was triggered. This function is a custom skill that the assistant users to query any previously saved todos.

1. Query the chat history by sending a `GET` request to the `GetChatState` function. The following is an example request:

    ```http
    GET http://localhost:7168/api/assistants/assistant123?timestampUTC=2023-01-01T00:00:00Z
    ```

    The response body should look something like the following:

    ```json
    {
      "id": "assistant123",
      "exists": true,
      "createdAt": "2024-05-06T20:40:48.481582Z",
      "lastUpdatedAt": "2024-05-06T20:43:49.9760621Z",
      "totalMessages": 10,
      "totalTokens": 242,
      "recentMessages": [
        {
          "content": "Don't make assumptions about what values to plug into functions.\r\nAsk for clarification if a user request is ambiguous.",
          "role": "system"
        },
        {
          "content": "Remind me to call my dad",
          "role": "user"
        },
        {
          "content": "The function call succeeded. Let the user know that you completed the action.",
          "role": "function",
          "name": "AddTodo"
        },
        {
          "content": "I've added \"Call my dad\" to your todo list.",
          "role": "assistant"
        },
        {
          "content": "Oh, and to take out the trash",
          "role": "user"
        },
        {
          "content": "The function call succeeded. Let the user know that you completed the action.",
          "role": "function",
          "name": "AddTodo"
        },
        {
          "content": "I've also added \"Take out the trash\" to your todo list.",
          "role": "assistant"
        },
        {
          "content": "What do I need to do today?",
          "role": "user"
        },
        {
          "content": "The function call succeeded. Let the user know that you completed the action.",
          "role": "function",
          "name": "GetTodos"
        },
        {
          "content": "Here are your tasks for today:\n\n1. Call my dad\n2. Take out the trash",
          "role": "assistant"
        }
      ]
    }
    ```

    > **NOTE**: Notice that the list of messages comes from four different roles, `system`, `user`, `assistant`, and `function`. Of these, only messages created by `user` and `assistant` should be displayed to end users. Messages created by `system` and `function` are auto-generated and should not be displayed.

As you can hopefully see, the assistant acknowledged the two todo items that you created, and then listed them back to you when you asked for your todo list. The interaction effectively looked something like the following:

> **USER**: Remind me to call my dad
>
> **ASSISTANT**: I have added a reminder for you to call your dad.
>
> **USER**: Oh, and to take out the trash
>
> **ASSISTANT**: I have added a reminder for you to take out the trash.
>
> **USER**: What do I need to do today?
>
> **ASSISTANT**: Today, you need to:
>
> * Call your dad
> * Take out the trash
