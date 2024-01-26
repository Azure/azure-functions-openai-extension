# Azure Functions bindings for OpenAI's GPT engine

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build](https://github.com/Azure/azure-functions-openai-extension/actions/workflows/build.yml/badge.svg)](https://github.com/Azure/azure-functions-openai-extension/actions/workflows/build.yml)

This is an **experimental** project that adds support for [OpenAI](https://platform.openai.com/) LLM (GPT-3.5-turbo, GPT-4) bindings in [Azure Functions](https://azure.microsoft.com/products/functions/).

This extension depends on the [Azure Open AI SDK](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/openai/Azure.AI.OpenAI).

## NuGet Packages

The following NuGet packages are available as part of this project.

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.WebJobs.Extensions.OpenAI.svg?label=microsoft.azure.webjobs.extensions.openai)](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.OpenAI)<br/>
[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.WebJobs.Extensions.OpenAI.Kusto.svg?label=microsoft.azure.webjobs.extensions.openai.kusto)](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.OpenAI.Kusto)

## Requirements

* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or greater (Visual Studio 2022 recommended)
* [Azure Functions Core Tools v4.x](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Cnode%2Cportal%2Cbash)
* [Azure OpenAI resource](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource?pivots=web-portal) with `AZURE_OPENAI_ENDPOINT` (e.g. `https://***.openai.azure.com/`) set. For authentication, use one of the below option:
    - System Managed Identity - assign the user/function app `Cognitive Services OpenAI User` role on the Azure Open AI resource.
    - set `AZURE_OPENAI_KEY` environment variable.
* **OR** Non-Azure Option - An OpenAI account and an [API key](https://platform.openai.com/account/api-keys) saved into a `OPENAI_API_KEY` environment variable. Learn more in [.env readme](./env/README.md).
* Azure Storage emulator such as [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) running in the background
* The target language runtime (e.g. .NET, Node.js, etc.) installed on your machine

## Features

The following features are currently available. More features will be slowly added over time.

* [Text completions](#text-completion-input-binding)
* [Chat bots](#chat-bots)
* [Embeddings generators](#embeddings-generator)
* [Semantic search](#semantic-search)

### Text completion input binding

The `textCompletion` input binding can be used to invoke the [OpenAI Chat Completions API](https://platform.openai.com/docs/guides/text-generation/chat-completions-vs-completions) and return the results to the function.

The examples below define "who is" HTTP-triggered functions with a hardcoded `"who is {name}?"` prompt, where `{name}` is the substituted with the value in the HTTP request path. The OpenAI input binding invokes the OpenAI GPT endpoint to surface the answer to the prompt to the function, which then returns the result text as the response content.

#### [C# example](./samples/other/dotnet/csharp-inproc/)

Setting a model is optional for non-Azure Open AI, [see here](#default-open-ai-models) for default model values for Open AI.

```csharp
[FunctionName(nameof(WhoIs))]
public static string WhoIs(
    [HttpTrigger(AuthorizationLevel.Function, Route = "whois/{name}")] HttpRequest req,
    [TextCompletion("Who is {name}?", Model = "gpt-35-turbo")] TextCompletionResponse response)
{
    return response.Content;
}
```

#### [TypeScript example](./samples/other/nodejs/)

```typescript
import { app, input } from "@azure/functions";

// This OpenAI completion input requires a {name} binding value.
const openAICompletionInput = input.generic({
    prompt: 'Who is {name}?',
    maxTokens: '100',
    type: 'textCompletion',
    model: 'gpt-35-turbo' // skip this for Open AI or provide exact model name to override.
})

app.http('whois', {
    methods: ['GET'],
    route: 'whois/{name}',
    authLevel: 'function',
    extraInputs: [openAICompletionInput],
    handler: async (_request, context) => {
        var response: any = context.extraInputs.get(openAICompletionInput)
        return { body: response.content.trim() }
    }
});
```

You can run the above function locally using the Azure Functions Core Tools and sending an HTTP request, similar to the following:

```http
GET http://localhost:7127/api/whois/pikachu
```

The result that comes back will include the response from the GPT language model:

```text
HTTP/1.1 200 OK
Content-Type: text/plain; charset=utf-8
Date: Tue, 28 Mar 2023 18:25:40 GMT
Server: Kestrel
Transfer-Encoding: chunked

Pikachu is a fictional creature from the Pok�mon franchise. It is a yellow
mouse-like creature with powerful electrical abilities and a mischievous
personality. Pikachu is one of the most iconic and recognizable characters
from the franchise, and is featured in numerous video games, anime series,
movies, and other media.
```

You can find more instructions for running the samples in the corresponding project directories. The goal is to have samples for all languages supported by Azure Functions.

### Chat bots

[Chat completions](https://platform.openai.com/docs/guides/chat) are useful for building AI-powered chat bots. This extension adds a built-in `OpenAI::ChatBotEntity` function that's powered by the [Durable Functions](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-overview) extension to implement a long-running chat bot entity.

There are three bindings you can use to interact with the chat bot:

1. The `chatBotCreate` output binding creates a new chat bot with a specified system prompt.
1. The `chatBotPost` output binding sends a message to the chat bot and saves the response in its internal state.
1. The `chatBotQuery` input binding fetches the chat bot history and passes it to the function.

You can find samples in multiple languages with instructions [in the chat samples directory](./samples/chat/).

### Embeddings Generator

OpenAI's [text embeddings](https://platform.openai.com/docs/guides/embeddings) measure the relatedness of text strings. Embeddings are commonly used for:

* **Search** (where results are ranked by relevance to a query string)
* **Clustering** (where text strings are grouped by similarity)
* **Recommendations** (where items with related text strings are recommended)
* **Anomaly detection** (where outliers with little relatedness are identified)
* **Diversity measurement** (where similarity distributions are analyzed)
* **Classification** (where text strings are classified by their most similar label)

Processing of the source text files typically involves chunking the text into smaller pieces, such as sentences or paragraphs, and then making an OpenAI call to produce embeddings for each chunk independently. Finally, the embeddings need to be stored in a database or other data store for later use.

#### [C# embeddings generator example](./samples/other/dotnet/csharp-inproc/EmbeddingsGenerator.cs)

```csharp
[FunctionName(nameof(GenerateEmbeddings_Http_Request))]
public static void GenerateEmbeddings_Http_Request(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings")] EmbeddingsRequest req,
    [Embeddings("{RawText}", InputType.RawText)] EmbeddingCreateResponse embeddingsResponse,
    ILogger logger)
{
    logger.LogInformation(
        "Received {count} embedding(s) for input text containing {length} characters.",
        embeddingsResponse.Data.Count,
        req.RawText.Length);

    // TODO: Store the embeddings into a database or other storage.
}
```

### Semantic Search

The semantic search feature allows you to import documents into a vector database using an output binding and query the documents in that database using an input binding. For example, you can have a function that imports documents into a vector database and another function that issues queries to OpenAI using content stored in the vector database as context (also known as the Retrieval Augmented Generation, or RAG technique).

 The supported list of vector databases is extensible, and more can be added by authoring a specially crafted NuGet package. Currently supported vector databases include:

* [Azure Data Explorer](https://azure.microsoft.com/services/data-explorer/) - See [this project](./src/WebJobs.Extensions.OpenAI.Kusto/)

 More may be added over time.

#### [C# document storage example](./samples/other/dotnet/csharp-inproc/Demos/EmailPromptDemo.cs)

This HTTP trigger function takes a path to a local file as input, generates embeddings for the file, and stores the result into [Azure Data Explorer](https://azure.microsoft.com/services/data-explorer/) (a.k.a. Kusto).

```csharp
public record EmbeddingsRequest(string FilePath);

[FunctionName("IngestEmail")]
public static async Task<IActionResult> IngestEmail(
    [HttpTrigger(AuthorizationLevel.Function, "post")] EmbeddingsRequest req,
    [Embeddings("{FilePath}", InputType.FilePath, Model = "text-embedding-ada-002")] EmbeddingsContext embeddings,
    [SemanticSearch("KustoConnectionString", "Documents", ChatModel = "gpt-3.5-turbo", EmbeddingsModel = "text-embedding-ada-002")] IAsyncCollector<SearchableDocument> output)
{
    string title = Path.GetFileNameWithoutExtension(req.FilePath);
    await output.AddAsync(new SearchableDocument(title, embeddings));
    return new OkObjectResult(new { status = "success", title, chunks = embeddings.Count });
}
```

#### [C# document query example](./samples/other/dotnet/csharp-inproc/Demos/EmailPromptDemo.cs)

This HTTP trigger function takes a query prompt as input, pulls in semantically similar document chunks into a prompt, and then sends the combined prompt to OpenAI. The results are then made available to the function, which simply returns that chat response to the caller.

```csharp
public record SemanticSearchRequest(string Prompt);

[FunctionName("PromptEmail")]
public static IActionResult PromptEmail(
    [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
    [SemanticSearch("KustoConnectionString", "Documents", Query = "{Prompt}", ChatModel = "gpt-3.5-turbo", EmbeddingsModel = "text-embedding-ada-002")] SemanticSearchContext result)
{
    return new ContentResult { Content = result.Response, ContentType = "text/plain" };
}
```

The responses from the above function will be based on relevant document snippets which were previously uploaded to the vector database. For example, assuming you uploaded internal emails discussing a new feature of Azure Functions that supports OpenAI, you could issue a query similar to the following:

```http
POST http://localhost:7127/api/PromptEmail
Content-Type: application/json

{
    "Prompt": "Was a decision made to officially release an OpenAI binding for Azure Functions?"
}
```

And you might get a response that looks like the following (actual results may vary):

```text
HTTP/1.1 200 OK
Content-Length: 454
Content-Type: text/plain

There is no clear decision made on whether to officially release an OpenAI binding for Azure Functions as per the email "Thoughts on Functions+AI conversation" sent by Bilal. However, he suggests that the team should figure out if they are able to free developers from needing to know the details of AI/LLM APIs by sufficiently/elegantly designing bindings to let them do the "main work" they need to do. Reference: Thoughts on Functions+AI conversation.
```

**IMPORTANT:** Azure OpenAI requires you to specify a *deployment* when making API calls instead of a *model*. The *deployment* is a specific instance of a model that you have deployed to your Azure OpenAI resource. In order to make code more portable across OpenAI and Azure OpenAI, the bindings in this extension use the `Model`, `ChatModel` and `EmbeddingsModel` to refer to either the OpenAI model or the Azure OpenAI deployment ID, depending on whether you're using OpenAI or Azure OpenAI.

All samples in this project rely on default model selection, which assumes the models are named after the OpenAI models. If you want to use an Azure OpenAI deployment, you'll want to configure the `Model`, `ChatModel` and `EmbeddingsModel` properties explicitly in your binding configuration. Here are a couple examples:

```csharp
// "gpt-35-turbo" is the name of an Azure OpenAI deployment
[FunctionName(nameof(WhoIs))]
public static string WhoIs(
    [HttpTrigger(AuthorizationLevel.Function, Route = "whois/{name}")] HttpRequest req,
    [TextCompletion("Who is {name}?", Model = "gpt-35-turbo")] TextCompletionResponse response)
{
    return response.Content;
}
```

```csharp
public record SemanticSearchRequest(string Prompt);

// "my-gpt-4" and "my-ada-2" are the names of Azure OpenAI deployments corresponding to gpt-4 and text-embedding-ada-002 models, respectively
[FunctionName("PromptEmail")]
public static IActionResult PromptEmail(
    [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
    [SemanticSearch("KustoConnectionString", "Documents", Query = "{Prompt}", ChatModel = "my-gpt-4", EmbeddingsModel = "my-ada-2")] SemanticSearchContext result)
{
    return new ContentResult { Content = result.Response, ContentType = "text/plain" };
}
```

## Default Open AI models

1. Chat Completion - gpt-3.5-turbo
1. Embeddings - text-embedding-ada-002
1. Text Completion - gpt-3.5-turbo

While using non-Azure Open AI, you can omit the Model specification in attributes to use the default models.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
