# Azure Functions bindings for OpenAI's GPT engine

This is an **experimental** project that adds support for [OpenAI](https://platform.openai.com/) GPT-3 bindings in [Azure Functions](https://azure.microsoft.com/products/functions/). It is not currently endorsed or supported by Microsoft.

This extension depends on the [Betalgo.OpenAI.GPT3](https://github.com/betalgo/openai) by [Betalgo](https://github.com/betalgo).

## Requirements

* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or greater (Visual Studio 2022 recommended)
* [Azure Functions Core Tools v4.x](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Cnode%2Cportal%2Cbash)
* An OpenAI account and an [API key](https://platform.openai.com/account/api-keys) saved into a `OPENAI_API_KEY` environment variable

## Features

The following features are currently available. More features will be slowly added over time.

### Text completions input binding

The `openAICompletion` input binding can be used to invoke the [OpenAI Text Completions API](https://platform.openai.com/docs/guides/completion) and return the results to the function.

The examples below define "who is" HTTP-triggered functions with a hardcoded `"who is {name}?"` prompt, where `{name}` is the substituted with the value in the HTTP request path. The OpenAI input binding invokes the OpenAI GPT endpoint to surface the answer to the prompt to the function, which then returns the result text as the response content.

#### [C# example](./samples/dotnet/csharp-inproc/)

```csharp
[FunctionName(nameof(WhoIs))]
public static string WhoIs(
    [HttpTrigger(AuthorizationLevel.Function, Route = "whois/{name}")] HttpRequest req,
    [OpenAICompletion("Who is {name}?")] CompletionCreateResponse response)
{
    return response.Choices[0].Text;
}
```

#### [TypeScript example](./samples/nodejs/)

```typescript
import { app, input } from "@azure/functions";

// This OpenAI completion input requires a {name} binding value.
const openAICompletionInput = input.generic({
    prompt: 'Who is {name}?',
    maxTokens: '100',
    type: 'openAICompletion'
})

app.http('whois', {
    methods: ['GET'],
    route: 'whois/{name}',
    authLevel: 'function',
    extraInputs: [openAICompletionInput],
    handler: async (_request, context) => {
        var response: any = context.extraInputs.get(openAICompletionInput)
        return { body: response.choices[0].text.trim() }
    }
});
```

You can find instructions for running the samples in the corresponding project directories. The goal is to have samples for all languages supported by Azure Functions.