// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;

OpenAIService openAiService = new(new OpenAiOptions()
{
    ApiKey = (Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY"))!,
    BaseDomain = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
    ProviderType = ProviderType.Azure,
    DeploymentId = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")!,
});

CompletionCreateResponse completionResult = await openAiService.Completions.CreateCompletion(
    new CompletionCreateRequest()
    {
        Prompt = "Once upon a time",
        Model = Models.TextDavinciV3, // NOTE: The Model value is ignored when using Azure OpenAI
        MaxTokens = 500,
    });

if (completionResult.Successful)
{
    Console.WriteLine(completionResult.Choices.FirstOrDefault());
}
else //handle errors
{
    if (completionResult.Error == null)
    {
        throw new Exception("Unknown Error");
    }

    Console.WriteLine($"{completionResult.Error.Code}: {completionResult.Error.Message}");
}
