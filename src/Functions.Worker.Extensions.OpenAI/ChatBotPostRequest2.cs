// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Functions.Worker.Extensions.OpenAI;

    public class ChatBotPostRequest2
    {

        public string UserMessage { get; set; }
        public string Id { get; set; } = string.Empty;

        public string? Model { get; set; }
    }

