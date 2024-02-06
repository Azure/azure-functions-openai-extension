# OpenAI Bindings - PowerShell Samples

This project contains sample code for using the OpenAI bindings with Azure Functions for PowerShell.

## Prerequisites

* [PowerShell 7](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.4)
* Please refer to the root level [README](../../README.md/#requirements) for prerequisites.

## Installing the OpenAI extension

The OpenAI extension isn't yet available in [extension bundles](https://learn.microsoft.com/azure/azure-functions/functions-bindings-register#extension-bundles), so it needs to be installed manually. The instructions for manually installing extensions can be found [here](https://learn.microsoft.com/azure/azure-functions/functions-run-local#install-extensions).

In this case, the extension must also be _built_ manually, since it's not yet available in the [public NuGet feed](https://www.nuget.org/profiles/AzureFunctionsExtensions). To build the extension, run the following command from the root of the repository:

```bash
dotnet build --output bin
```

## Running the Function App

1. If using Azure Open AI, pass the deployment name to model property in function.json for textCompletion input binding or use it override the default model value for Open AI.

    ```json
        {
        "type": "textCompletion",
        "direction": "in",
        "name": "TextCompletionResponse",
        "prompt": "Who is {name}?",
        "maxTokens": "100",
        "model": "gpt-3.5-turbo"
        }
    ```

1. Start Azurite for local development storage. See [these instructions](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) for more information on how to work with Azurite.
1. Navigate to the folder samples\other\powershell

    ```bash
        cd samples/other/powershell
    ```

1. Build the extension dependency

    ```bash
        dotnet build --output bin
    ```

1. Run the function host

    ```bash
        func start
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
