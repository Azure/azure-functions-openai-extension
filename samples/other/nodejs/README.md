# OpenAI Bindings - TypeScript Samples

This project contains sample code for using the OpenAI bindings with Azure Functions for node.js. The examples use TypeScript and the V4 programming model.

## Prerequisites

* [Node.js v18+](https://nodejs.org/en/download)
* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (this requirement will be removed in the future)
* Refer root level [README](../../README.md) for all pre - requisites.

## Installing the OpenAI extension

The OpenAI extension isn't yet available in [extension bundles](https://learn.microsoft.com/azure/azure-functions/functions-bindings-register#extension-bundles), so it needs to be installed manually. The instructions for manually installing extensions can be found [here](https://learn.microsoft.com/azure/azure-functions/functions-run-local#install-extensions).

In this case, the extension must also be _built_ manually, since it's not yet available in the [public NuGet feed](https://www.nuget.org/profiles/AzureFunctionsExtensions). To build the extension, run the following commands:

1. (In Visual Studio Code), press `F1` to open the command palette. In the command palette, search for and select `Azurite: Start`
2. Instructions on building and starting the app:
`cd samples/other/nodejs && npm install && dotnet build --output bin && npm run build && npm run start`
