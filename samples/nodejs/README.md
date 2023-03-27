# OpenAI Bindings - TypeScript Samples

This project contains sample code for using the OpenAI bindings with Azure Functions for node.js. The examples use TypeScript and the V4 programming model.

## Prerequisites

* [Node.js v18+](https://nodejs.org/en/download)
* [Azure Functions Core Tools v4.x](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Cnode%2Cportal%2Cbash)
* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (this requirement will be removed in the future)

## Installing the OpenAI extension

The OpenAI extension isn't yet available in [extension bundles](https://learn.microsoft.com/azure/azure-functions/functions-bindings-register#extension-bundles), so it needs to be installed manually. The instructions for manually installing extensions can be found [here](https://learn.microsoft.com/azure/azure-functions/functions-run-local#install-extensions).

In this case, the extension must also be _built_ manually, since it's not yet available in the [public NuGet feed](https://www.nuget.org/profiles/AzureFunctionsExtensions). To build the extension, run the following command from the root of the repository:

```bash
dotnet build --output bin
```
