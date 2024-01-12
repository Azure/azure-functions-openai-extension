// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

OpenAIClient openAIClient;
Uri? azureOpenAIEndpoint = Uri.TryCreate(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"), UriKind.Absolute, out var uri) ? uri : null;
string? azureOpenAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
string? openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (azureOpenAIEndpoint != null)
{
    openAIClient = azureOpenAIKey != null ? new (azureOpenAIEndpoint, new AzureKeyCredential(azureOpenAIKey)) : new (azureOpenAIEndpoint, new DefaultAzureCredential());
}
else
{
    openAIClient = new (openAIKey);
}

try
{
    CompletionsOptions completionsOptions = new (OpenAIModels.gpt_35_turbo_instruct, new List<string> { "Once upon a time", })
    {
        MaxTokens = 500
    };
    Response<Completions> completionResult = await openAIClient.GetCompletionsAsync(completionsOptions);

    string? content = completionResult?.Value?.Choices[0]?.Text;

    Console.WriteLine(content);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
