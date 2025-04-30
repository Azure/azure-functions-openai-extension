# Semantic Search Demo

## Prerequisites

The sample is available in the following language stacks:

* [C# on the in process worker](csharp-inproc/)
* [TypeScript](typescript/)
* [JavaScript](javascript/)
* [PowerShell](powershell/)
* [Python](python/)
* [Java](java/)

Please refer to the [root README](../../README.md#requirements) for common prerequisites that apply to all samples.

This sample requires creating an Azure AI Search with (optional) Semantic Ranker. You can do this by following the [Azure AI Search quickstart](https://learn.microsoft.com/en-us/azure/search/search-create-service-portal)
and optionally [enable semantic ranking](https://learn.microsoft.com/en-us/azure/search/semantic-how-to-enable-disable?tabs=enable-portal) for semantic ranking and captions features.

## Managed Identity and Endpoint Configuration

1. Configure AI Search API Access Control (strongly recommended to use RBAC) -
   Update the AI Search API access control to `Role-based access control`:
   * Navigate to **Settings** → **Keys** → **API Access Control**.
   * If API keys must be used, refer to the [Use of API Keys](#use-of-api-key) section.

2. Assign Required Roles (strongly recommended to use managed identity) -
   Ensure the user or function app's managed identity has the following roles assigned:
   * **Search Service Contributor**: Provides access to manage search service resources like indexes and indexers.
   * **Search Index Data Contributor**: Provides read/write access to search indexes.  
     For more details on available roles, visit [Search Service RBAC Roles](https://learn.microsoft.com/azure/search/search-security-rbac#built-in-roles-used-in-search).

3. Managed Identity-Based Authentication on Azure

    If no Resource Id or Client Id is specified, the system-assigned managed identity will be used by default.

    1. User-Assigned Managed Identity:

        ```json
        "<ConnectionNamePrefix>__endpoint": "https://<resource-name>.search.windows.net",
        "<ConnectionNamePrefix>__credential": "managedidentity",
        "<ConnectionNamePrefix>__managedIdentityResourceId": "Resource Id of managed identity", 
        "<ConnectionNamePrefix>__clientId": "Client Id of managed identity"
        ```

        Only one of managedIdentityResourceId or clientId should be specified, not both.

    2. System-Assigned Managed Identity or local development:

        ```json
        "<ConnectionNamePrefix>__endpoint": "https://<resource-name>.search.windows.net",
        "<ConnectionNamePrefix>__credential": "managedidentity",
        ```

       Specifying credential is optional for system assigned managed identity

4. Binding Configuration -
    Pass the configured `ConnectionNamePrefix` value, example `AISearch` to the `searchConnectionName` property in the `SemanticSearchInput` or `EmbeddingsStoreOutput` bindings. Default is `AISearchEndpoint` if just the endpoint is being configured in local.settings.json or environment variables to use DefaultAzureCredential.

## Running the sample

Once you have an Azure AI Search resource, you can run the sample by following these steps:

1. Always configure the search provider type in the `host.json` as shown in below snippet.
1. Use of Semantic Search, Semantic Captions and Vector Search Dimensions are configurable. You may configure the `host.json` file within the project and following example shows the default values:

    ```json
    "extensions": {
        "openai": {
            "searchProvider": {
                "type": "azureAiSearch",
                "isSemanticSearchEnabled": true,
                "useSemanticCaptions": true,
                "vectorSearchDimensions": 1536
            }
        }
    }
    ```

    `VectorSearchDimensions` is length of the embedding vector. [The dimensions attribute has a minimum of 2 and a maximum of 3072 floating point values each](https://learn.microsoft.com/azure/search/search-get-started-vector#:~:text=dimensions%20attribute%20has%20a%20minimum%20of%202%20and%20a%20maximum%20of%203072%20floating%20point%20values%20each). By default, the length of the embedding vector will be 1536 for text-embedding-ada-002.

1. Use a terminal window to navigate to the sample directory

    ```sh
    cd samples/rag-aisearch/<language-stack>
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

## Azure AI Search Index Schema

Semantic Search binding for Azure AI Search expects/creates a very specific index schema. If your existing index is not the same, you can give a new index name and it will be auto created, vector dimension is configurable as per embedding model.

| Field name | Type             | Retrievable | Facetable | Searchable | Analyzer    | Dimensions  |
|:----------:|:----------------:|:-----------:|:---------:|:----------:|:-----------:|:-----------:|
| id         | String           | x           |           |            |             |             |
| text       | String           | x           |           | x          |             |             |
| title      | String           | x           | x         |            |             |             |
| embeddings | SingleCollection | x           |           | x          | EnMicrosoft | 1536        |
| timestamp  | DateTimeOffset   | x           | x         |            |             |             |

### Semantic Configuration

* Content Field - text
* Title Field - title

### Vector Search Configuration

Algorithm - HnswAlgorithmConfiguration

### Use of API Key

Azure AI Search offers key-based authentication that you can use on connections to your search service. [More information](https://learn.microsoft.com/azure/search/search-security-api-keys). Use of API Key is optional and managed identities are recommended way for authentication.

1. Update the AI search API access control to `API keys` or `Both` (Settings -> Keys -> API Access Control).
1. Update your configuration (local.settings.json or environment variables) to include the following settings or configuration section:

   ```json
   "<ConnectionNamePrefix>__endpoint": "https://<resource-name>.search.windows.net",
   "<ConnectionNamePrefix>__apiKey": "<AISearch-API-Key>"
   ```

1. Pass the configured `ConnectionNamePrefix` value, example `AISearch` to the `connectionName` property in the `SemanticSearchInput` or `EmbeddingsStoreOutput` bindings.
