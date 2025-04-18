# Semantic Search Demo

## Prerequisites

This sample is available in the following language stacks:

- [C# on the isolated worker](csharp-ooproc/)
- [Python](python/)

Please refer to the [root README](../../README.md#requirements) for common prerequisites that apply to all samples.

## Running the sample

This sample requires creating an Azure CosmosDB NoSql. You can do this by follwing the [CosmosDB for NoSql Quickstart](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/quickstart-portal).

Once you have an Cosmos DB resource, you can run the sample by following these steps:

1. The application can either be run with Key based authentication or AAD.

Key based auth:
Update the `CosmosDBNoSql` value in `local.settings.json` to match your connection string from the Cosmos DB resource. You may obtain the connection string by following [these instructions](https://learn.microsoft.com/en-us/azure/cosmos-db/data-explorer).

AAD auth:
Update the `CosmosDBNoSql__Endpoint` value in `local.settings.json` to match your connection string from the Cosmos DB resource. You may follow the below links to add aad authentication for [Data Plane](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/security/how-to-grant-data-plane-role-based-access?tabs=built-in-definition%2Cjava&pivots=azure-interface-cli) and [Control Plane](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/security/how-to-grant-control-plane-role-based-access?tabs=built-in-definition%2Cjava&pivots=azure-interface-portal) operations for CosmosDB.

1. Always configure the search provider type in the `host.json` as shown in below snippet. Read here about the [configs](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/vector-search) provided below.

   ```json
    "extensions": {
        "openai": {
            "searchProvider": {
                "type": "cosmosDBNoSqlSearch",
                "applicationName": "openai-functions-nosql",
                "vectorDataType": "float32",
                "vectorDimensions": 1536,
                "vectorDistanceFunction": "cosine",
                "vectorIndexType": "quantizedFlat",
                "databaseName": "function-db",
                "databaseThroughput": 5000,
                "containerThroughput": 5000,
                "embeddingKey": "/embedding",
                "textKey": "/text",
                "whereFilterClause": "",
                "limitOffsetFilterClause": ""
            }
        }
    }
   ```

1. Use a terminal window to navigate to the sample directory

   ```sh
   cd samples/rag-cosmosdb-nosql/<language-stack>
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
