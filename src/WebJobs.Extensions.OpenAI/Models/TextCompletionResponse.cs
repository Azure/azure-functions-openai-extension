// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

/// <summary>
/// Text Completion Response Class
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class TextCompletionResponse
{
    /// <summary>
    /// Initiliazes a new instance of the <see cref="TextCompletionResponse"/> class.
    /// </summary>
    /// <param name="content">The text completion message content.</param>
    /// <param name="totalTokens">The total token usage.</param>
    public TextCompletionResponse(string content, int totalTokens)
    {
        this.Content = content;
        this.TotalTokens = totalTokens;
    }

    /// <summary>
    /// The text completion message content.
    /// </summary>
    [JsonProperty("content")]
    public string Content { get; set; }

    /// <summary>
    /// The total token usage.
    /// </summary>
    [JsonProperty("totalTokens")]
    public int TotalTokens{ get; set; }
}