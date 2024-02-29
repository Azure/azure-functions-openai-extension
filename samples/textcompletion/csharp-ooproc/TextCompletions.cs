// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.TextCompletion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CSharpIsolatedSamples;

/// <summary>
/// These samples show how to use the OpenAI Chat Completions API for Text Completion. For more details on the Completions APIs, see
/// https://platform.openai.com/docs/guides/text-generation/chat-completions-vs-completions.
/// </summary>
public static class TextCompletions
{
    /// <summary>
    /// This sample demonstrates the "templating" pattern, where the function takes a parameter
    /// and embeds it into a text prompt, which is then sent to the OpenAI completions API.
    /// </summary>
    [Function(nameof(WhoIs))]
    public static HttpResponseData WhoIs(
        [HttpTrigger(AuthorizationLevel.Function, Route = "whois/{name}")] HttpRequestData req,
        [TextCompletionInput("Who is {name}?")] TextCompletionResponse response)
    {
        HttpResponseData responseData = req.CreateResponse(HttpStatusCode.OK);
        responseData.WriteString(response.Content);
        return responseData;
    }

    /// <summary>
    /// This sample takes a prompt as input, sends it directly to the OpenAI completions API, and results the 
    /// response as the output.
    /// </summary>
    [Function(nameof(GenericCompletion))]
    public static HttpResponseData GenericCompletion(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        [TextCompletionInput("{Prompt}")] TextCompletionResponse response,
        ILogger log)
    {
        HttpResponseData responseData = req.CreateResponse(HttpStatusCode.OK);
        responseData.WriteString(response.Content);
        return responseData;
    }
}