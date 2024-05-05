# Azure Functions bindings for OpenAI's GPT engine

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build](https://dev.azure.com/azfunc/Azure%20Functions/_apis/build/status%2FExtension-OpenAI%2FAzure%20Functions%20OpenAI%20Extension%20PR%20CI?branchName=main)](https://dev.azure.com/azfunc/Azure%20Functions/_build/latest?definitionId=303&branchName=main)

This project adds support for [OpenAI](https://platform.openai.com/) LLM (GPT-3.5-turbo, GPT-4) bindings in [Azure Functions](https://azure.microsoft.com/products/functions/).

This extension depends on the [Azure AI OpenAI SDK](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/openai/Azure.AI.OpenAI).

## NuGet Packages

The following NuGet packages are available as part of this project.

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.svg?label=microsoft.azure.functions.worker.extensions.openai)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.OpenAI)
[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Kusto.svg?label=microsoft.azure.functions.worker.extensions.openai.kusto)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Kusto)
[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.AzureAISearch.svg?label=microsoft.azure.functions.worker.extensions.openai.azureaisearch)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.AzureAISearch)
[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.CosmosDBSearch.svg?label=microsoft.azure.functions.worker.extensions.openai.cosmosdbsearch)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.OpenAI.CosmosDBSearch)

## Preview Bundle

Add following section to `host.json` of the function app for non dotnet languages to utilise the preview bundle and consume extension packages:

```json
"extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle.Preview",
    "version": "[4.*, 5.0.0)"
  }
```

## Requirements

* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or greater (Visual Studio 2022 recommended)
* [Azure Functions Core Tools v4.x](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Cnode%2Cportal%2Cbash)
* Update settings in Azure Function or the `local.settings.json` file for local development with the following keys:
    1. For Azure, `AZURE_OPENAI_ENDPOINT` - [Azure OpenAI resource](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource?pivots=web-portal) (e.g. `https://***.openai.azure.com/`) set.
    1. For Azure, assign the user or function app managed identity `Cognitive Services OpenAI User` role on the Azure OpenAI resource. It is strongly recommended  to use managed identity to avoid overhead of secrets maintenance, however if there is a need for key based authentication add the setting `AZURE_OPENAI_KEY` and its value in the settings.
    1. For non- Azure, `OPENAI_API_KEY` - An OpenAI account and an [API key](https://platform.openai.com/account/api-keys) saved into a setting.  
    If using environment variables, Learn more in [.env readme](./env/README.md).
    1. Update `CHAT_MODEL_DEPLOYMENT_NAME` and `EMBEDDING_MODEL_DEPLOYMENT_NAME` keys to Azure Deployment names or override default OpenAI model names.
    1. If using user assigned managed identity, add `AZURE_CLIENT_ID` to environment variable settings with value as client id of the managed identity.
    1. Visit binding specific samples README for additional settings that might be required for each binding.
* Azure Storage emulator such as [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) running in the background
* The target language runtime (e.g. dotnet, nodejs, powershell, python, java) installed on your machine. Refer the official supported versions.

## Features

The following features are currently available. More features will be slowly added over time.

* [Text completions](#text-completion-input-binding)
* [Chat completion](#chat-completion)
* [Assistants](#assistants)
* [Embeddings generators](#embeddings-generator)
* [Semantic search](#semantic-search)

### Text completion input binding

The `textCompletion` input binding can be used to invoke the [OpenAI Chat Completions API](https://platform.openai.com/docs/guides/text-generation/chat-completions-vs-completions) and return the results to the function.

The examples below define "who is" HTTP-triggered functions with a hardcoded `"who is {name}?"` prompt, where `{name}` is the substituted with the value in the HTTP request path. The OpenAI input binding invokes the OpenAI GPT endpoint to surface the answer to the prompt to the function, which then returns the result text as the response content.

#### [C# example](./samples/textcompletion/csharp-ooproc/)

Setting a model is optional for non-Azure OpenAI, [see here](#default-openai-models) for default model values for OpenAI.

```csharp
[Function(nameof(WhoIs))]
public static HttpResponseData WhoIs(
    [HttpTrigger(AuthorizationLevel.Function, Route = "whois/{name}")] HttpRequestData req,
    [TextCompletionInput("Who is {name}?")] TextCompletionResponse response)
{
    HttpResponseData responseData = req.CreateResponse(HttpStatusCode.OK);
    responseData.WriteString(response.Content);
    return responseData;
}
```

#### [TypeScript example](./samples/textcompletion/nodejs/)

```typescript
import { app, input } from "@azure/functions";

// This OpenAI completion input requires a {name} binding value.
const openAICompletionInput = input.generic({
    prompt: 'Who is {name}?',
    maxTokens: '100',
    type: 'textCompletion',
    model: 'gpt-35-turbo'
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

#### [PowerShell example](./samples/textcompletion/powershell/)

```PowerShell
using namespace System.Net

param($Request, $TriggerMetadata, $TextCompletionResponse)

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::OK
        Body       = $TextCompletionResponse.Content
    })
```

In the same directory as the PowerShell function, define the bindings in a function.json file.  

If using Azure OpenAI, update `CHAT_MODEL_DEPLOYMENT_NAME` key in `local.settings.json` with the deployment name or update model property directly in function.json for textCompletion input binding or use it to override the default model value for OpenAI.

```json
{
    "type": "textCompletion",
    "direction": "in",
    "name": "TextCompletionResponse",
    "prompt": "Who is {name}?",
    "maxTokens": "100",
    "model": "gpt-3.5-turbo"
}
```

#### [Python example](./samples/textcompletion/python/)

Setting a model is optional for non-Azure OpenAI, [see here](#default-openai-models) for default model values for OpenAI.

```python
@app.route(route="whois/{name}", methods=["GET"])
@app.generic_input_binding(arg_name="response", type="textCompletion", data_type=func.DataType.STRING, prompt="Who is {name}?", maxTokens="100", model = "gpt-3.5-turbo")
def whois(req: func.HttpRequest, response: str) -> func.HttpResponse:
    response_json = json.loads(response)
    return func.HttpResponse(response_json["content"], status_code=200)
```

#### Running locally

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

Pikachu is a fictional creature from the Pokemon franchise. It is a yellow
mouse-like creature with powerful electrical abilities and a mischievous
personality. Pikachu is one of the most iconic and recognizable characters
from the franchise, and is featured in numerous video games, anime series,
movies, and other media.
```

You can find more instructions for running the samples in the corresponding project directories. The goal is to have samples for all languages supported by Azure Functions.

### Chat completion

[Chat completions](https://platform.openai.com/docs/guides/chat) are useful for building AI-powered assistants.

There are three bindings you can use to interact with the assistant:

1. The `assistantCreate` output binding creates a new assistant with a specified system prompt.
1. The `assistantPost` output binding sends a message to the assistant and saves the response in its internal state.
1. The `assistantQuery` input binding fetches the assistant history and passes it to the function.

You can find samples in multiple languages with instructions [in the chat samples directory](./samples/chat/).

### Assistants

Assistants build on top of the chat functionality to provide assistants with custom skills defined as functions.
This internally uses the [function calling](https://platform.openai.com/docs/guides/function-calling) feature of OpenAIs GPT models to select which functions to invoke and when.

You can define functions that can be triggered by assistants by using the `assistantSkillTrigger` trigger binding.
These functions are invoked by the extension when a assistant signals that it would like to invoke a function in response to a user prompt.

The name of the function, the description provided by the trigger, and the parameter name are all hints that the underlying language model use to determine when and how to invoke an assistant function.

#### [C# example](./samples/assistant/csharp-ooproc)

```csharp
public class AssistantSkills
{
    readonly ITodoManager todoManager;
    readonly ILogger<AssistantSkills> logger;

    // This constructor is called by the Azure Functions runtime's dependency injection container.
    public AssistantSkills(ITodoManager todoManager, ILogger<AssistantSkills> logger)
    {
        this.todoManager = todoManager ?? throw new ArgumentNullException(nameof(todoManager));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Called by the assistant to create new todo tasks.
    [Function(nameof(AddTodo))]
    public Task AddTodo([AssistantSkillTrigger("Create a new todo task")] string taskDescription)
    {
        if (string.IsNullOrEmpty(taskDescription))
        {
            throw new ArgumentException("Task description cannot be empty");
        }

        this.logger.LogInformation("Adding todo: {task}", taskDescription);

        string todoId = Guid.NewGuid().ToString()[..6];
        return this.todoManager.AddTodoAsync(new TodoItem(todoId, taskDescription));
    }

    // Called by the assistant to fetch the list of previously created todo tasks.
    [Function(nameof(GetTodos))]
    public Task<IReadOnlyList<TodoItem>> GetTodos(
        [AssistantSkillTrigger("Fetch the list of previously created todo tasks")] object inputIgnored)
    {
        this.logger.LogInformation("Fetching list of todos");

        return this.todoManager.GetTodosAsync();
    }
}
```

You can find samples with instructions [in the assistant samples directory](./samples/assistant/).

### Embeddings Generator

OpenAI's [text embeddings](https://platform.openai.com/docs/guides/embeddings) measure the relatedness of text strings. Embeddings are commonly used for:

* **Search** (where results are ranked by relevance to a query string)
* **Clustering** (where text strings are grouped by similarity)
* **Recommendations** (where items with related text strings are recommended)
* **Anomaly detection** (where outliers with little relatedness are identified)
* **Diversity measurement** (where similarity distributions are analyzed)
* **Classification** (where text strings are classified by their most similar label)

Processing of the source text files typically involves chunking the text into smaller pieces, such as sentences or paragraphs, and then making an OpenAI call to produce embeddings for each chunk independently. Finally, the embeddings need to be stored in a database or other data store for later use.

#### [C# embeddings generator example](./samples/embeddings/csharp-ooproc/EmbeddingsGenerator.cs)

```csharp
[Function(nameof(GenerateEmbeddings_Http_RequestAsync))]
public async Task GenerateEmbeddings_Http_RequestAsync(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings")] HttpRequestData req,
    [EmbeddingsInput("{RawText}", InputType.RawText)] EmbeddingsContext embeddings)
{
    using StreamReader reader = new(req.Body);
    string request = await reader.ReadToEndAsync();

    EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

    this.logger.LogInformation(
        "Received {count} embedding(s) for input text containing {length} characters.",
        embeddings.Count,
        requestBody.RawText.Length);

    // TODO: Store the embeddings into a database or other storage.
}
```

### Semantic Search

The semantic search feature allows you to import documents into a vector database using an output binding and query the documents in that database using an input binding. For example, you can have a function that imports documents into a vector database and another function that issues queries to OpenAI using content stored in the vector database as context (also known as the Retrieval Augmented Generation, or RAG technique).

Only raw text files are supported at present for data ingestion.

The supported list of vector databases is extensible, and more can be added by authoring a specially crafted NuGet package. Visit the currently supported vector specific folder for specific usage information:

* [Azure AI Search](https://learn.microsoft.com/azure/search/search-create-service-portal) - See [source code](./src/WebJobs.Extensions.OpenAI.AISearch/)
* [Azure Data Explorer](https://azure.microsoft.com/services/data-explorer/) - See [source code](./src/WebJobs.Extensions.OpenAI.Kusto/)
* [Azure Cosmos DB using MongoDB (vCore)](https://learn.microsoft.com/azure/cosmos-db/mongodb/vcore/introduction) - See [source code](./src/WebJobs.Extensions.OpenAI.CosmosDBSearch/)

 More may be added over time.

#### [C# document storage example](./samples/rag-aisearch/csharp-ooproc/FilePrompt.cs)

This HTTP trigger function takes a URL of a file as input, generates embeddings for the file, and stores the result into an Azure AI Search Index.

```csharp
public class EmbeddingsRequest
{
    [JsonPropertyName("Url")]
    public string? Url { get; set; }
}

[Function("IngestFile")]
public static async Task<EmbeddingsStoreOutputResponse> IngestFile(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
{
    using StreamReader reader = new(req.Body);
    string request = await reader.ReadToEndAsync();

    EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

    if (requestBody == null || requestBody.Url == null)
    {
        throw new ArgumentException("Invalid request body. Make sure that you pass in {\"Url\": value } as the request body.");
    }

    Uri uri = new(requestBody.Url);
    string filename = Path.GetFileName(uri.AbsolutePath);

    HttpResponseData response = req.CreateResponse(HttpStatusCode.Created);

    return new EmbeddingsStoreOutputResponse
    {
        HttpResponse = response,
        SearchableDocument = new SearchableDocument(filename)
    };
}
```

**Tip** - To improve context preservation between chunks in case of large documents, specify the max overlap between chunks and also the chunk size. The default values for `MaxChunkSize` and `MaxOverlap` are 8 * 1024 and 128 characters respectively.

#### [C# document query example](./samples/rag-aisearch/csharp-ooproc/FilePrompt.cs)

This HTTP trigger function takes a query prompt as input, pulls in semantically similar document chunks into a prompt, and then sends the combined prompt to OpenAI. The results are then made available to the function, which simply returns that chat response to the caller.
**Tip** - To improve the knowledge for OpenAI model, the number of result sets being sent to the model with system prompt can be increased with binding property - `MaxKnowledgeCount` which has default value as 1. Also, the `SystemPrompt` in SemanticSearchRequest can be tweaked as per user instructions on how to process the knowledge sets being appended to it.

```csharp
public class SemanticSearchRequest
{
    [JsonPropertyName("Prompt")]
    public string? Prompt { get; set; }
}

[Function("PromptFile")]
public static IActionResult PromptFile(
    [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
    [SemanticSearchInput("AISearchEndpoint", "openai-index", Query = "{Prompt}", ChatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", EmbeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] SemanticSearchContext result)
{
    return new ContentResult { Content = result.Response, ContentType = "text/plain" };
}
```

The responses from the above function will be based on relevant document snippets which were previously uploaded to the vector database. For example, assuming you uploaded internal emails discussing a new feature of Azure Functions that supports OpenAI, you could issue a query similar to the following:

```http
POST http://localhost:7127/api/PromptFile
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
[Function(nameof(WhoIs))]
public static string WhoIs(
    [HttpTrigger(AuthorizationLevel.Function, Route = "whois/{name}")] HttpRequest req,
    [TextCompletion("Who is {name}?", Model = "gpt-35-turbo")] TextCompletionResponse response)
{
    return response.Content;
}
```

```csharp
public record SemanticSearchRequest(string Prompt);

// "my-gpt-4" and "my-ada-2" are the names of Azure OpenAI deployments corresponding to gpt-4 and text-embedding-3-small models, respectively
[Function("PromptEmail")]
public static IActionResult PromptEmail(
    [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
    [SemanticSearch("KustoConnectionString", "Documents", Query = "{Prompt}", ChatModel = "my-gpt-4", EmbeddingsModel = "my-ada-2")] SemanticSearchContext result)
{
    return new ContentResult { Content = result.Response, ContentType = "text/plain" };
}
```

## Default OpenAI models

1. Chat Completion - gpt-3.5-turbo
1. Embeddings - text-embedding-3-small
1. Text Completion - gpt-3.5-turbo

While using non-Azure OpenAI, you can omit the Model specification in attributes to use the default models.

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
