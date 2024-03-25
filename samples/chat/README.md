# Chat

This sample demonstrates how to build a chatbot using Azure Functions and a local build of the experimental Azure OpenAI extension.

The sample is available in the following language stacks:

* [C# on the in-process worker](csharp-inproc)
* [C# on the out of process worker](csharp-ooproc)
* [TypeScript on the Node.js worker](nodejs)
* [Powershell](powershell)
* [Python](python)
* [Java](java)

## Prerequisites

Please refer to the root level [README](../../README.md#requirements) for prerequisites.

## Running the sample

1. Start Azurite for local development storage. See [these instructions](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) for more information on how to work with Azurite.
2. Reference the table below for instructions on building and starting the app:

    | Language Worker | Command |
    | --------------- | ------- |
    | .NET oo-proc | `cd samples/chat/csharp-ooproc && dotnet build && cd bin/debug/net6.0 && func start` |
    | Node.js | `cd samples/chat/nodejs && npm install && dotnet build --output bin && npm run build && npm run start` |
    | PowerShell | `cd samples/chat/powershell && dotnet build --output bin && func start` |
    | Python | `cd samples/chat/python && dotnet build --output bin && func start` |
    | Java | `cd samples/chat/java && mvn clean package && dotnet build && mvn azure-functions:run` |

    If successful, you should see the following output from the `func` command:

    ```plaintext
    Functions:

        CreateChatBot: [PUT] http://localhost:7071/api/chats/{chatId}

        GetChatState: [GET] http://localhost:7071/api/chats/{chatId}

        PostUserResponse: [POST] http://localhost:7071/api/chats/{chatId}
    ```

    Note for running the post user response function provided in the sample, please specify a model name as a value for key `CHAT_MODEL_DEPLOYMENT_NAME` in `local.settings.json`. This value can be an Azure deployment name or a GPT model name.

    For example, if you were running the chat bot scenario using the Azure OpenAI, you would have created a deployment name here as specified in step #6 [here](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal#deploy-a-model). Here is an example of what this would look like:

    ```csharp
    public class PostResponseOutput
    {
        [AssistantPostOutput("{chatId}", Model = "DeploymentName")]
        public AssistantPostRequest? ChatBotPostRequest { get; set; }

        public HttpResponseData? HttpResponse { get; set; }
    }

    ```

    If you were running the chat bot scneario using open AI, you can override the default model value used for OpenAI, which is `gpt-3.5-turbo` and update the model field within `AssistantPostOutput`.

3. Use an HTTP client tool to send a request to the `CreateChatBot` function. The following is an example request:

    ```http
    PUT http://localhost:7071/api/chats/test123
    Content-Type: application/json

    {
        "instructions": "You are a helpful chatbot. In all your English responses, speak as if you are Shakespeare."
    }
    ```

    Feel free to change the `instructions` property to whatever you want. The `test123` URL segment value is used to identify the chatbot and must be unique.

    The HTTP response should look something like the following:

    ```json
    {"chatId":"test123"}
    ```

    You should also see some relevant log output in the terminal window where the app is running.

    The chat bot is now created and ready to receive prompts.

4. Use an HTTP client to send a message to the `test123` chat bot.

    ```http
    POST http://localhost:7071/api/chats/test123
    Content-Type: text/plain

    Who won the SuperBowl in 2014?
    ```

    The response should be an HTTP 202 Accepted response with an empty body. This means the chatbot is processing the request.
    You should also see additional log output in the terminal window where the app is running.

5. Use an HTTP client to get the latest chat history for the `test123` chat bot.

    ```http
    GET http://localhost:7071/api/chats/test123?timestampUTC=2024-01-15T22:00:00
    ```

    The response should look something like the following example, formatted for readability.
    Note that the responses from the bot will vary based on the `Instructions` provided when the chatbot was created, and based on how the language model decides to respond (which is non-deterministic).

    ```json
    {
      "id": "test123",
      "exists": true,
      "createdAt": "2024-01-15T22:33:15.0664078Z",
      "lastUpdatedAt": "2024-01-15T22:33:45.5591906Z",
      "totalMessages": 3,
      "totalTokens": 139,
      "recentMessages": [
          {
              "content": "You are a helpful chatbot. In all your English responses, speak as if you are Shakespeare.",
              "role": "system"
          },
          {
              "content": "Who won the SuperBowl in 2014?",
              "role": "user"
          },
          {
              "content": "Alas, in the year of our Lord 2014, the SuperBowl victor was the illustrious Seattle Seahawks. They demonstrated great prowess and prevailed over their worthy adversaries, the Denver Broncos.",
              "role": "assistant"
          }
      ]
    }
    ```

    If the `recentMessages` array doesn't have at least *three* elements, then the chatbot is still processing the request. Try again in a few seconds.

    > **NOTE**<br/>
    > You can use the `timestampUTC` query string parameter to get the chat history at a specific point in time. For example, setting `timestampUTC` to be the last observed value of `lastUpdatedAt` ensures that the response will only contain messages generated *after* the specified timestamp. This is useful for polling HTTP clients that need to know when the chatbot has finished processing a request.

6. Repeat steps 4 and 5 as many times as you want. For example, a followup question can be asked by sending another `POST` request to the chatbot, as in the following example.

    ```http
    POST http://localhost:7071/api/chats/test123
    Content-Type: text/plain

    Amazing! Do you know who performed the halftime show?
    ```

    You can then see the response by sending another `GET` request to the chatbot, as in the following example.

    ```http
    GET http://localhost:7071/api/chats/test123?timestampUTC=2024-01-15T22:36:00
    ```

    Assuming that the `timestampUTC` property correctly filters out all but the last message, you can expect to see a response similar to the following:

    ```json
    {
      "id": "test123",
      "exists": true,
      "createdAt": "2024-01-15T22:33:15.0664078Z",
      "lastUpdatedAt": "2024-01-15T22:36:32.3760309Z",
      "totalMessages": 5,
      "totalTokens": 178,
      "recentMessages": [
          {
              "content": "Ah, verily! The halftime show at the SuperBowl of 2014 was graced by the presence of the fair enchantress known as Bruno Mars. With his dulcet voice and captivating melodies, he entertained the masses gathered with his musical prowess.",
              "role": "assistant"
          }
      ]
    }
    ```
