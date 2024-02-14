// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;

record struct MessageRecord(DateTime Timestamp, ChatMessageEntity ChatMessageEntity);

[JsonObject(MemberSerialization.OptIn)]
class ChatBotRuntimeState
{
    [JsonProperty("messages")]
    public List<ChatMessageEntity>? ChatMessages { get; set; }

    [JsonProperty("totalTokens")]
    public int TotalTokens { get; set; } = 0;
}