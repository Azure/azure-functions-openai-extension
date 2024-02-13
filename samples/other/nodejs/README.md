# OpenAI Bindings - TypeScript Samples

This project contains sample code for using the OpenAI bindings with Azure Functions for node.js. The examples use TypeScript and the V4 programming model.

## Prerequisites

* [Node.js v18+](https://nodejs.org/en/download)
* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (this requirement will be removed in the future)
* Please refer to the root level [README](../../../README.md#requirements) for prerequisites.

## Running the Function App

1. Start Azurite for local development storage. See [these instructions](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) for more information on how to work with Azurite.
1. Run the following command to start the function app.

    ```bash
    cd samples/other/nodejs && npm install && dotnet build --output bin && npm run build && npm run start
    ```

1. Use a tool like [Postman](https://www.postman.com/) to send a request to the `WhoIs` function. The following is an example request:

```http
POST http://localhost:7071/api/whois/pikachu
```

The HTTP response should look something like the following example (with newlines added for readability):

```text
Pikachu is a fictional creature from the Pokemon franchise. It is a yellow
mouse-like creature with powerful electrical abilities and a mischievous
personality. Pikachu is one of the most iconic and recognizable characters
from the franchise, and is featured in numerous video games, anime series,
movies, and other media.
```
