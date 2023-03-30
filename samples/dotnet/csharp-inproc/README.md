# OpenAI Bindings - .NET in-proc samples

This project contains sample code for using the OpenAI bindings with the Azure Functions .NET in-proc experience. The examples are written in C#.

## Prerequisites

You must have the following installed on your local machine in order to run these samples.

* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
* [Azure Functions Core Tools v4.x](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Ccsharp%2Cportal%2Cbash)
* An [OpenAI API key](https://platform.openai.com/account/api-keys) saved into a `OPENAI_API_KEY` environment variable

## Installing the OpenAI extension

The OpenAI extension isn't yet available on nuget.org and therefore must be built locally. This is handled automatically by the build process.

## Running the Function app

Use the following command to build the project:

```bash
dotnet build
```

Once built, you can run the sample using the following command:

```bash
func start
```

You should see output similar to the following:

```text
Azure Functions Core Tools
Core Tools Version:       4.0.4915 Commit hash: N/A  (64-bit)
Function Runtime Version: 4.14.0.19631

Functions:

        (list of functions)
```

## Running the "who is" Text Completion sample

Use a tool like [Postman](https://www.postman.com/) to send a request to the `WhoIs` function. The following is an example request:

```http
POST http://localhost:7071/api/whois/pikachu
```

The HTTP response should look something like the following example (with newlines added for readability):

```text
Pikachu is a fictional creature from the Pok�mon franchise. It is a yellow
mouse-like creature with powerful electrical abilities and a mischievous
personality. Pikachu is one of the most iconic and recognizable characters
from the franchise, and is featured in numerous video games, anime series,
movies, and other media.
```

## Running the ChatBot sample

Use a tool like [Postman](https://www.postman.com/) to send a request to the `CreateChatBot` function. The following is an example request:

```http
POST http://localhost:7071/api/chats

You are a helpful chatbot that is helping someone named Chris. Please use Chris's name in each response. Respond to this first message with "Hi Chris. How can I help you?"
```

The HTTP response should contain a JSON payload that looks something like the following:

```json
{
  "message": "Request accepted",
  "chatId": "chat-43e7184b",
  "location": "http://localhost:7071/api/chats/chat-43e7184b"
}
```

To get the chatbot response, send a GET request to the URL in the `location` field. The following is an example request that assumes the chat ID is `chat-43e7184b`. Your actual URL may be different:

```http
GET http://localhost:7071/api/chats/chat-43e7184b
```

The result will be a JSON payload that includes the response from the chat bot in the `assistantMessage` field.

```json
{
  "assistantMessage": "Hi Chris. How can I help you?",
  "lastUpdated": "2023-03-30T01:59:58"
}
```

To send a message to the chatbot, send a POST request to the URL in the `location` field. The following is an example request that assumes the chat ID is `chat-43e7184b`. Your actual URL may be different:

```http
POST http://localhost:7071/api/chats/chat-43e7184b

Who won the SuperBowl in 2014?
```

As before, you can get the chatbot response by sending a GET request to the previous URL in the `location` field. The following is the updated response.

```json
{
  "assistantMessage": "The SuperBowl in 2014 was won by the Seattle Seahawks. Is there anything else I can help you with, Chris?",
  "lastUpdated": "2023-03-30T02:05:33"
}
```

The chat bot orchestration tracks the conversation history and uses it to generate the next response. You can continue to send messages to the chat bot and get responses until the chat bot is stopped. Use the following HTTP request to send a followup question to the chat bot.

```http
POST http://localhost:7071/api/chats/chat-43e7184b

Amazing! Do you know who performed the halftime show?
```

As before, you can get the chatbot response by sending a GET request to the previous URL in the `location` field. The following is an example response to the followup question.

```json
{
  "assistantMessage": "Yes, I do know, Chris. The halftime show of that SuperBowl was headlined by Bruno Mars and the Red Hot Chili Peppers.",
  "lastUpdated": "2023-03-30T02:09:36"
}
```
