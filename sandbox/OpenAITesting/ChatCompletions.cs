// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

namespace OpenAITesting;

static class ChatCompletions
{
    public static async Task Run()
    {
        OpenAIClient openAIClient = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        try
        {
            Azure.Response<Azure.AI.OpenAI.ChatCompletions> completionResult = await openAIClient.GetChatCompletionsAsync(new ChatCompletionsOptions
            {
                Messages =
                {
                    new ChatRequestSystemMessage("You are a helpful assistant."),
                    new ChatRequestUserMessage("Who won the world series in 2020?"),
                    new ChatRequestAssistantMessage("The Los Angeles Dodgers won the World Series in 2020."),
                    new ChatRequestUserMessage("Where was it played?")
                },
                DeploymentName = OpenAIModels.gpt_35_turbo,
            });

            string? content = completionResult?.Value?.Choices[0]?.Message?.Content;

            Console.WriteLine(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
