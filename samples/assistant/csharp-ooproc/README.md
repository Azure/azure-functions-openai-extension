# Assistants

This sample demonstrates how to build an AI assistant chatbot with custom skills using Azure Functions and a local build of the OpenAI extension.
It builds upon the concepts [chatbot](../chatbot) sample, which demonstrates how to create a simple chatbot.

## Introduction

AI assistants are chat bots that can be configured with custom skills. Custom skills are implemented using the `assistantSkillTrigger` binding.
Custom skills are useful ways to extend the functionality of an AI assistant. For example, you can create a custom skill that saves a todo item to a database, or a custom skill that queries a database for a list of todo items.
In both cases, the skills are defined in your app and any the language model doesn't need to know anything about the skill implementation, making it useful for interacting with private data.

This OpenAI extension internally uses the [function calling](https://platform.openai.com/docs/guides/function-calling) functionality available in
`gpt-3.5-turbo` and `gpt-4` models to implement assistant skills.

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
@skills.generic_trigger(arg_name="taskDescription", type="assistantSkillTrigger", data_type=func.DataType.STRING, functionDescription="Create a new todo task")
def add_todo(taskDescription: str) -> None:
    if not taskDescription:
        raise ValueError("Task description cannot be empty")

    logging.info(f"Adding todo: {taskDescription}")

    todo_id = str(uuid.uuid4())[0:6]
    todo_manager.add_todo(TodoItem(id=todo_id, task=taskDescription))
    return
```

The `AssistantSkillTrigger` attribute requires a `FunctionDescription` string value, which is text describing what the function does.
This is critical for the AI assistant to be able to invoke the skill at the right time.
The name of the function parameter (e.g., `taskDescription`) is also an important hint to the AI assistant about what kind of information to provide to the skill.

> **NOTE:** The `AssistantSkillTrigger` attribute only supports primitive types as function parameters, such as `string`, `int`, `bool`, etc. Function return values can be of any JSON-serializable type.

Any function that uses the `AssistantSkillTrigger` binding will be automatically registered as a skill that can be invoked by any AI assistant.
The assistant will invoke a skill function whenever it decides to do so to satisfy a user prompt, and will provide function parameters based on the context of the conversation. The skill function can then return a response to the assistant, which will be used to satisfy the user prompt.

## Prerequisites

The sample is available in the following language stacks:

* [C# on the out-of-process worker](csharp-ooproc)
* [nodejs](nodejs)
* [python](python) - supported on host runtime version >= 4.34.0.0

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
    POST http://localhost:7168/api/assistants/assistant123
    Content-Type: application/json

    {
      "message": "Remind me to call my dad"
    }
    ```

    The response should be an HTTP 202, indicating that the request was accepted. You should also see some relevant log output in the terminal window where the app is running.

    > **NOTE:** The AI assistant runs asynchronously in the background, so it doesn't respond immediately to prompts.  You should wait for about 10 seconds before sending subsequent messages to the assistant to give it time to finish processing the previous prompt.

    In the function log output, you should observe that the `AddTodo` function was triggered. This function is a custom skill that was automatically registered with the assistant when the app was started.

1. Ask the assistant to create another todo item using another `POST` request to the `PostUserQuery` function. The following is an example request:

    ```http
    POST http://localhost:7168/api/assistants/assistant123
    Content-Type: application/json

    {
      "message": "Oh, and to take out the trash"
    }
    ```

    The AI assistant remembers the context from the chat, so it knows that you're still talking about todo items, even if your prompt doesn't mention this explicitly.

1. If you're running the sample with Cosmos DB persistence configured, then you can use the Cosmos DB Data Explorer to view the documents in the `my-todos` collection in the `testdb` database. You should see two documents, one for each todo item that the assistant created.

1. Ask the assistant to list your todo items by sending a `POST` request to the `PostUserQuery` function. The following is an example request:

    ```http
    POST http://localhost:7168/api/assistants/assistant123
    Content-Type: application/json

    {
      "message": "What do I need to do today?"
    }
    ```

    The response should be an HTTP 202, indicating that the prompt was accepted. In the function log output, you should observe that the `GetTodos` function was triggered. This function is a custom skill that the assistant users to query any previously saved todos.

1. Query the chat history by sending a `GET` request to the `GetChatState` function. The following is an example request:

    ```http
    GET http://localhost:7168/api/assistants/assistant123?timestampUTC=2023-01-01T00:00:00Z
    ```

    The response body should look something like the following:

    ```json
    {
      "id": "assistant123",
      "exists": true,
      "createdAt": "2023-11-26T00:40:56.7864809Z",
      "lastUpdatedAt": "2023-11-26T00:41:21.0153489Z",
      "totalMessages": 10,
      "totalTokens": 153,
      "recentMessages": [
        {
          "role": "system",
          "content": "Don't make assumptions about what values to plug into functions.\r\nAsk for clarification if a user request is ambiguous.",
        },
        {
          "role": "user",
          "content": "Remind me to call my dad"
        },
        {
          "role": "function",
          "content": "The function call succeeded. Let the user know that you completed the action.",
        },
        {
          "role": "assistant",
          "content": "I have added a reminder for you to call your dad."
        },
        {
          "role": "user",
          "content": "Oh, and to take out the trash"
        },
        {
          "role": "function",
          "content": "The function call succeeded. Let the user know that you completed the action.",
        },
        {
          "role": "assistant",
          "content": "I have added a reminder for you to take out the trash."
        },
        {
          "role": "user",
          "content": "What do I need to do today?"
        },
        {
          "role": "function",
          "content": "[{\"Id\":\"4d3170\",\"Task\":\"Call my dad\"},{\"Id\":\"f1413f\",\"Task\":\"Take out the trash\"}]",
        },
        {
          "role": "assistant",
          "content": "Today, you need to:\n- Call your dad\n- Take out the trash"
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