# Semantic Search Demo

## Prerequisites

The sample is available in the following language stacks:

* [C# on the out of process worker](csharp-ooproc/)
* [NodeJS](nodejs/)
* [PowerShell](powershell/)
* [Java](java/)
* [Python](python/)

Please refer to the [root README](../../README.md#requirements) for common prerequisites that apply to all samples.

## Running the sample

This sample requires creating a Kusto cluster and database. You can do this by following the [Kusto quickstart](https://docs.microsoft.com/azure/data-explorer/create-cluster-database-portal).

Once you have a Kusto cluster and database, you can run the sample by following these steps:

1. Update the `KustoConnectionString` value in `local.settings.json` to match your Kusto cluster and database names.
1. Add the user or function app managed identity as `database user` for database in Kusto cluster.
1. Always configure the search provider type in the `host.json` as shown in below snippet.

    ```json
    "extensions": {
        "openai": {
            "searchProvider": {
                "type": "kusto"
            }
        }
    }
    ```

1. Run the following command in [Azure Data Explorer](https://dataexplorer.azure.com/), in the context of your new Kusto database, to create a "Documents" table in your Kusto database. This is where the ingest function will save embeddings.

    ```sh
    .create table Documents (Id:string, Title:string, Text:string, Embeddings:dynamic, Timestamp:datetime)
    ```

1. Use a terminal window to navigate to the sample directory

    ```sh
    cd samples/rag-kusto/<language-stack>
    ```

1. If using the extensions.csproj with non-dotnet languages and refer the extension project

    ```sh
    dotnet build --output bin
    ```

1. If using python, run `pip install -r requirements.txt` to install the correct library version.
1. Build and start the app

    ```sh
    func start
    ```

1. Refer the [demo.http](demo.http) file for the format of requests.
1. Send an HTTP POST request to ingest a text file. You can do this multiple times with different files if you like.

    ```http
    POST http://localhost:7071/api/IngestEmail
    Content-Type: application/json

    {"Url":"https://url/test/test_file.txt"}
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
