# OpenAI Bindings - Python Samples

This project contains sample code for using the OpenAI bindings with Azure Functions for Python using Programming Model V2.

## Prerequisites

* Please refer Azure Functions Python [supported versions](https://learn.microsoft.com/azure/azure-functions/functions-reference-python?tabs=asgi%2Capplication-level&pivots=python-mode-decorators#python-version).
* Install the corresponding version of Python.
* Please refer to the root level [README](../../../README.md#requirements) for prerequisites.

## Configure the Function App

If using Azure OpenAI, update the deployment name to model property in textCompletion input binding or use it to override the default model value for OpenAI.

```python
@app.generic_input_binding(arg_name="response", type="textCompletion", data_type=func.DataType.STRING, prompt="{Prompt}", model = "gpt-3.5-turbo")
```

## Running the Function App

1. Start Azurite for local development storage. See [these instructions](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) for more information on how to work with Azurite.
1. Navigate to the folder samples\other\python

    ```bash
        cd samples/other/python
    ```

1. Build the extension dependency

    ```bash
        dotnet build --output bin
    ```

1. Run the function host

    ```bash
        func start
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
