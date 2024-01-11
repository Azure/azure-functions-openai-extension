// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using OpenAI.ObjectModels.RequestModels;


namespace Functions.Worker.Extensions.OpenAI;


public class ChatBotState2
{

    public string Id { get; set; }
    public bool Exists { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public int TotalMessages { get; set; }
    public List<ChatMessage> RecentMessages { get; set; }

}
