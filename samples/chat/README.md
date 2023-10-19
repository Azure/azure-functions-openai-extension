# Chat

This sample demonstrates how to build a chatbot using Azure Functions and a local build of the experimental OpenAI extension.

The sample is available in the following language stacks:

* [C# on the in-process worker](csharp-inproc)
* [TypeScript on the Node.js worker](nodejs)

## Prerequisites

You must have the following installed on your local machine in order to run these samples.

* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or newer
* [Azure Functions Core Tools v4.x](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Ccsharp%2Cportal%2Cbash)
* An [OpenAI API key](https://platform.openai.com/account/api-keys) saved into a `OPENAI_API_KEY` environment variable
* Azure Storage emulator such as [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) running in the background
* The target language runtime (e.g. .NET, Node.js, etc.) installed on your machine

## Running the sample

1. Use a terminal window to navigate to the sample directory (e.g. `cd samples/chat/csharp-inproc`)
2. Reference the table below for instructions on building and starting the app:

    | Language Worker | Command |
    | --------------- | ------- |
    | .NET in-proc | `dotnet build && cd bin/debug/net6.0 && func start` |
    | Node.js | `npm install && dotnet build --output bin && npm run build && npm run start` |

    If successful, you should see the following output from the `func` command:

    ```plaintext
    Functions:

        CreateChatBot: [PUT] http://localhost:7071/api/chats/{chatId}

        GetChatState: [GET] http://localhost:7071/api/chats/{chatId}

        PostUserResponse: [POST] http://localhost:7071/api/chats/{chatId}

        OpenAI::ChatBotEntity: entityTrigger
    ```

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
    GET http://localhost:7071/api/chats/test123?timestampUTC=2023-08-10T07:00:00
    ```

    The response should look something like the following example, formatted for readability.
    Note that the responses from the bot will vary based on the `Instructions` provided when the chatbot was created, and based on how the language model decides to respond (which is non-deterministic).

    ```json
    {
      "id": "test123",
      "exists": true,
      "status": "Active",
      "createdAt": "2023-08-10T07:24:53.201607Z",
      "lastUpdatedAt": "2023-08-10T07:27:03.332001Z",
      "recentMessages": [
        {
          "role": "system",
          "content": "You are a helpful chatbot. In all your English responses, speak as if you are Shakespeare."
        },
        {
          "role": "user",
          "content": "Who won SuperBowl XLVIII in 2014?"
        },
        {
          "role": "assistant",
          "content": "Hark, good sir! The victor of SuperBowl XLVIII was none other than the fierce and indomitable Seattle Seahawks. They didst vanquish their adversaries, the Denver Broncos, with great mirth and skill upon the field of battle."
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
    GET http://localhost:7071/api/chats/test123?timestampUTC=2023-08-10T07:51:10Z
    ```

    Assuming that the `timestampUTC` property correctly filters out all but the last message, you can expect to see a response similar to the following:

    ```json
    {
      "id": "test123",
      "exists": true,
      "status": "Active",
      "createdAt": "2023-08-10T07:24:53.201607Z",
      "lastUpdatedAt": "2023-08-10T07:51:11.760825Z",
      "recentMessages": [
        {
          "role": "assistant",
          "content": "Aye, fair question dost thou pose to me,\nThe minstrels who graced the halftime show at that decree,\n'Twas none other than the illustrious Bruno Mars,\nWhose voice and melodies did reach the stars."
        }
      ]
    }
    ```
