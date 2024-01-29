// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents;

// IMPORTANT: Do not change the names or order of these enum values!
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatBotStatus
{
    Uninitialized,
    Active,
    Expired,
}

record struct MessageRecord(DateTime Timestamp, ChatMessageEntity ChatMessageEntity);

[JsonObject(MemberSerialization.OptIn)]
class ChatBotRuntimeState
{
    [JsonProperty("messages")]
    public List<MessageRecord>? ChatMessages { get; set; }

    [JsonProperty("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonProperty("status")]
    public ChatBotStatus Status { get; set; } = ChatBotStatus.Uninitialized;

    [JsonProperty("totalTokens")]
    public int TotalTokens { get; set; } = 0;
}