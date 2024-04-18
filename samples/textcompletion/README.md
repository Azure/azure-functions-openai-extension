# Chat

This sample demonstrates how to build a text completion sample using Azure Functions and a local build of the Azure OpenAI extension.

The sample is available in the following language stacks:

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
    | .NET oo-proc | `cd samples/textcompletion/csharp-ooproc && dotnet build && cd bin/debug/net6.0 && func start` |
    | Node.js | `cd samples/textcompletion/nodejs && npm install && dotnet build --output bin && npm run build && npm run start` |
    | PowerShell | `cd samples/textcompletion/powershell && dotnet build --output bin && func start` |
    | Python | `cd samples/textcompletion/python && dotnet build --output bin && func start` |
    | Java | `cd samples/textcompletion/java && mvn clean package && dotnet build && mvn azure-functions:run` |

    If successful, you should see the following output from the `func` command:

    ```text
    Azure Functions Core Tools
    Core Tools Version:       4.0.5530 Commit hash: N/A  (64-bit)
    Function Runtime Version: 4.28.5.21962

    Functions:

        GenericCompletion: [POST] http://localhost:7071/api/GenericCompletion

        WhoIs: [GET] http://localhost:7071/api/whois/{name}
    ```

## Running the "who is" Text Completion sample

1. Refer the [demo.http](demo.http) file for the format of requests.
1. Send a request to the `WhoIs` function. The following is an example request:

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
