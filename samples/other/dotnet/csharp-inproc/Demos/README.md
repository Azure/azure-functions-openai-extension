# Semantic Search Demo

## Prerequisites

You must have the following installed on your local machine in order to run these samples.

* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or newer
* [Azure Functions Core Tools v4.x](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Ccsharp%2Cportal%2Cbash)
* An [OpenAI API key](https://platform.openai.com/account/api-keys) saved into a `OPENAI_API_KEY` environment variable
* Azure Storage emulator such as [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) running in the background

## Running the sample

This sample requires creating a Kusto cluster and database. You can do this by following the [Kusto quickstart](https://docs.microsoft.com/azure/data-explorer/create-cluster-database-portal).

Once you have a Kusto cluster and database, you can run the sample by following these steps:

1. Update the `KustoConnectionString` value in `local.settings.json` to match your Kusto cluster and database names.
1. Run the following command in [Azure Data Explorer](https://dataexplorer.azure.com/), in the context of your new Kusto database, to create a "Documents" table in your Kusto database. This is where the ingest function will save embeddings.

    ```sh
    .create table Documents (Id:string, Title:string, Text:string, Embeddings:dynamic, Timestamp:datetime)
    ```

1. Use a terminal window to navigate to the sample directory

    ```sh
    cd samples/other/dotnet/csharp-inproc
    ```

1. Build and start the app

    ```sh
    dotnet build && cd bin/debug/net6.0 && func start
    ```

1. Send an HTTP POST request to ingest a text file. You can do this multiple times with different files if you like.

    ```http
    POST http://localhost:7071/api/IngestEmail
    Content-Type: application/json

    {"FilePath":"/path/to/file.txt"}
    ```

    The results of the request will be the embeddings of the text file, which will be saved to your Kusto database.

1. Send an HTTP GET request to query your ingested data.

    ```http
    POST http://localhost:7071/api/PromptEmail
    Content-Type: application/json

    {"Prompt":"INSERT PROMPT HERE"}
    ```

    The results of the request will be the model's response to the prompt, using information from the previously ingested embeddings.
    The response should also contain citations, indicating which embeddings it used to help generate the response.