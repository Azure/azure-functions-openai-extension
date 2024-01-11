// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Functions.Worker.Extensions.OpenAI;

[AttributeUsage(AttributeTargets.Parameter| AttributeTargets.Property)]
public class ChatBotCreateAttribute : Attribute
{
    // No configuration needed
}
