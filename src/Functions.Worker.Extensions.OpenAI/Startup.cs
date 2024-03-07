// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embedding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI;
using System.Text.Json;

[assembly: WorkerExtensionStartup(typeof(Startup))]

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI;

/// <summary>
/// Startup class needed to add converters for Embeddings and EmbeddingsOptions.
/// </summary>
public sealed class Startup : WorkerExtensionStartup
{
    public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.Configure<JsonSerializerOptions>(jsonSerializerOptions =>
        {
            jsonSerializerOptions.Converters.Add(new EmbeddingsJsonConverter());
            jsonSerializerOptions.Converters.Add(new EmbeddingsOptionsJsonConverter());
        });
    }
}

