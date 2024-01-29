﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

/// <summary>
/// Input binding attribute for getting an instance of the <see cref="OpenAIService"/> class.
/// </summary>
/// <remarks>
/// WARNING: This may be removed in a future version.
/// </remarks>
[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class OpenAIServiceAttribute : Attribute
{
}
