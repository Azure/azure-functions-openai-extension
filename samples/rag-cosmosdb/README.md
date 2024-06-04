# Semantic Search Demo

## Prerequisites

The sample is available in the following language stacks:

* [C# on the isolated worker](csharp-ooproc/)
* [NodeJS](nodejs/)
* [PowerShell](powershell/)
* [Java](java/)
* [Python](python/)

Please refer to the [root README](../../README.md#requirements) for common prerequisites that apply to all samples.

## Running the sample

This sample requires creating an Azure Cosmos DB for MongoDB vCore. You can do this by following the [Cosmos DB for MongoDB vCore Quickstart](https://learn.microsoft.com/azure/cosmos-db/mongodb/vcore/quickstart-portal).

Once you have an Cosmos DB resource, you can run the sample by following these steps:

1. Update the `CosmosDBMongoConnectionString` value in `local.settings.json` to match your connection string from the Cosmos DB resource. You may obtain the connection string by following [these instructions](https://learn.microsoft.com/azure/cosmos-db/mongodb/vcore/quickstart-portal#get-cluster-credentials).
1. Always configure the search provider type in the `host.json` as shown in below snippet.
1. Use of Vector Search Dimensions and the number of clusers that the inverted file (IVF) index uses to group the vector data are configurable. You may configure the `host.json` file within the project and following example shows the default values:

    ```json
    "extensions": {
        "openai": {
            "searchProvider": {
                "type": "cosmosDBSearch",
                "vectorSearchDimensions": 1536,
                "numLists":  1
            }
        }
    }
    ```

    `VectorSearchDimensions` is length of the embedding vector. [The dimensions attribute has a minimum of 2 and a maximum of 2000 floating point values each](https://learn.microsoft.com/azure/cosmos-db/mongodb/vcore/vector-search#create-an-vector-index-using-ivf). By default, the length of the embedding vector will be 1536 for text-embedding-ada-002.

    `NumLists` is the number of clusters that the inverted file (IVF) uses to group the vector data, as mentioned [here](https://learn.microsoft.com/azure/cosmos-db/mongodb/vcore/vector-search#create-an-vector-index-using-ivf). By default, the number of clusters will be 1.

1. Use a terminal window to navigate to the sample directory

    ```sh
    cd samples/rag-cosmosdb/<language-stack>
    ```

1. If using the extensions.csproj with non-dotnet languages and refer the extension project

    ```sh
    dotnet build --output bin
    ```
2. If using python, run `pip install -r requirements.txt` to install the correct library version.
1. Build and start the app

    ```sh
    func start
    ```

1. Refer the [demo.http](demo.http) file for the format of requests.

## Cosmos DB Search Index Schema

An index is created similar to the one specified in the [documentation](https://learn.microsoft.com/azure/cosmos-db/mongodb/vcore/vector-search#create-an-vector-index-using-ivf). You can specify the collection name, number of clusters used in the IVF algorithm, and the vector search dimensions. By default, the index uses `vector-ivf` for the type of vector to create and `COS` for the similarity index.

### Vector Search Configuration

Algorithm - IVF (inverted file)
