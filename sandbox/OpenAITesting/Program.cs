// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using OpenAI.GPT3.Managers;
using OpenAI.GPT3;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

OpenAIService openAiService = new(new OpenAiOptions()
{
    ApiKey = (Environment.GetEnvironmentVariable("OPENAI_API_KEY")!) ?? (Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")!),
    BaseDomain = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
    ProviderType = ProviderType.Azure,
    ApiVersion = Environment.GetEnvironmentVariable("OPENAI_API_VERSION") ?? "2023-05-15",
    DeploymentId = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATGPT_DEPLOYMENT")!,
});

CompletionCreateResponse completionResult = await openAiService.Completions.CreateCompletion(
    new CompletionCreateRequest()
    {
        Prompt = "Once upon a time",
        Model = Models.TextDavinciV3,
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
