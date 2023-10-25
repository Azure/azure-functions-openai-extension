// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace OpenAITesting;

static class ChatCompletions
{
    public static async Task Run()
    {
        OpenAIService openAiService = new(new OpenAiOptions()
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
        });

        var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem("You are a helpful assistant."),
                ChatMessage.FromUser("Who won the world series in 2020?"),
                ChatMessage.FromAssistant("The Los Angeles Dodgers won the World Series in 2020."),
                ChatMessage.FromUser("Where was it played?")
            },
            Model = Models.Gpt_3_5_Turbo,
        });

        if (completionResult.Successful)
        {
            Console.WriteLine(completionResult.Choices.First().Message.Content);
        }
        else
        {
            if (completionResult.Error == null)
            {
                throw new Exception("Unknown Error");
            }

            Console.WriteLine($"{completionResult.Error.Code}: {completionResult.Error.Message}");
        }
    }
}
