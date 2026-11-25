# Azure Functions OpenAI Extension

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build](https://dev.azure.com/azfunc/public/_apis/build/status%2Fazure%2Fazure-functions-openai-extension%2Fopenai-extension.public?repoName=Azure%2Fazure-functions-openai-extension&branchName=main)](https://dev.azure.com/azfunc/public/_build/latest?definitionId=558&repoName=Azure%2Fazure-functions-openai-extension&branchName=main)

> [!WARNING]
> The Azure Functions OpenAI extension retires on **April 20, 2027**. Preview extension bundle version **4.60.0 and later** don't include the extension.
>
> See [retirement and migration guidance](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai).

This project adds [OpenAI](https://platform.openai.com/) bindings to [Azure Functions](https://azure.microsoft.com/products/functions/) using the [Azure AI OpenAI SDK](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/openai/Azure.AI.OpenAI).

For installation, configuration, binding references, and examples, see the [Microsoft Learn documentation](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai).

## NuGet Packages

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.svg?label=microsoft.azure.functions.worker.extensions.openai)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.OpenAI)
[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Kusto.svg?label=microsoft.azure.functions.worker.extensions.openai.kusto)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Kusto)
[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.AzureAISearch.svg?label=microsoft.azure.functions.worker.extensions.openai.azureaisearch)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.AzureAISearch)
[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.CosmosDBSearch.svg?label=microsoft.azure.functions.worker.extensions.openai.cosmosdbsearch)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.CosmosDBSearch)
[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.CosmosDBNoSqlSearch.svg?label=microsoft.azure.functions.worker.extensions.openai.cosmosdbnosqlsearch)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.CosmosDBNoSqlSearch)

## Preview Bundle

See the [installation guidance](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai) for using the preview bundle in existing apps.

## Requirements

To run the samples:

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.
* [Azure Functions Core Tools v4.x](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Cnode%2Cportal%2Cbash).
* An Azure Storage emulator such as [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite).
* The runtime for your chosen sample.
* Set `CHAT_MODEL_DEPLOYMENT_NAME` and `EMBEDDING_MODEL_DEPLOYMENT_NAME` to the Azure OpenAI deployment names or OpenAI model names. Check each sample's README for additional settings.

## Configuring AI Service Connections

Use the `AIConnectionName` binding property to specify a connection. See [connection guidance](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai#connections) for configuration and authentication.

Missing `Endpoint` and `Key` values in a named section fall back individually to `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_KEY`. A key takes precedence over identity-based authentication, so leave both key settings unset to use an identity.

A nonempty `OPENAI_API_KEY` selects OpenAI rather than Azure OpenAI.

## Using Reasoning Models

See the `IsReasoningModel` option in the [assistant post](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai-assistantpost-input), [semantic search](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai-semanticsearch-input), and [text completion](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai-textcompletion-input) binding references.

## Features

### Text completion input binding

See the [binding documentation](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai-textcompletion-input) and [samples](./samples/textcompletion/).

### Chat completion

See the [binding overview](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai) and [chat samples](./samples/chat/).

### Assistants

See the [assistant trigger documentation](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai-assistant-trigger) and [assistant samples](./samples/assistant/).

### Embeddings Generator

See the [binding documentation](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai-embeddings-input) and [samples](./samples/embeddings/).

### Restricting FilePath and Url inputs

> [!IMPORTANT]
> Configure [file and URL input restrictions](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai-embeddings-input#restrict-file-and-url-inputs) in the deployed function app when inputs are caller-supplied. Without restrictions, the binding can read any file the app can access or fetch any HTTPS URL. `local.settings.json` applies only to local development.

The settings are read once per host process. When the URL allowlist is configured, URLs with user information are rejected and downloads are limited to 10 MB and 30 seconds.

### Semantic Search

See the [embeddings store output](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai-embeddingsstore-output) and [semantic search input](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai-semanticsearch-input) binding references.

Use the same `EmbeddingsModel` for indexing and querying an index; if you change the model, re-index its documents. See [embedding model guidance](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai-embeddings-input#usage).

The defaults are 8,192 characters for `MaxChunkLength`, 128 for `MaxOverlap`, and 1 for `MaxKnowledgeCount`.

Samples are available for [Azure AI Search](./samples/rag-aisearch/), [Azure Data Explorer](./samples/rag-kusto/), [Azure Cosmos DB for MongoDB](./samples/rag-cosmosdb/), and [Azure Cosmos DB for NoSQL](./samples/rag-cosmosdb-nosql/).

## Default OpenAI models

See the [binding documentation](https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai) for model options and defaults. In this repository, `SemanticSearch` defaults to the `text-embedding-ada-002` embedding model.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.
