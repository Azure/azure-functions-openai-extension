// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

record struct MessageRecord(DateTime Timestamp, AssistantMessage ChatMessageEntity);

[JsonObject(MemberSerialization.OptIn)]
class AssistantRuntimeState
{
    [JsonProperty("messages")]
    public List<AssistantMessage>? ChatMessages { get; set; }

    [JsonProperty("totalTokens")]
    public int TotalTokens { get; set; } = 0;
}