// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Extensions.Logging;

namespace CSharpInProcSamples;

/// <summary>
/// These samples show how to use the OpenAI Chat Completions API for Text Completion. For more details on the Completions APIs, see
/// https://platform.openai.com/docs/guides/text-generation/chat-completions-vs-completions.
/// </summary>
public static class Completions
{
    /// <summary>
    /// This sample demonstrates the "templating" pattern, where the function takes a parameter
    /// and embeds it into a text prompt, which is then sent to the OpenAI completions API.
    /// </summary>
    [FunctionName(nameof(WhoIs))]
    public static IActionResult WhoIs(
        [HttpTrigger(AuthorizationLevel.Function, Route = "whois/{name}")] HttpRequest req,
        [TextCompletion("Who is {name}?")] TextCompletionResponse response)
    {
        return new OkObjectResult(response.Content);
    }

    /// <summary>
    /// This sample takes a prompt as input, sends it directly to the OpenAI completions API, and results the 
    /// response as the output.
    /// </summary>
    [FunctionName(nameof(GenericCompletion))]
    public static IActionResult GenericCompletion(
        [HttpTrigger(AuthorizationLevel.Function, "post")] PromptPayload payload,
        [TextCompletion("{Prompt}", Model = "gpt-3.5-turbo")] TextCompletionResponse response,
        ILogger log)
    {
        log.LogInformation("Prompt = {prompt}, Response = {response}", payload.Prompt, response);
        string text = response.Content;
        return new OkObjectResult(text);
    }

    public record PromptPayload(string Prompt);
}
