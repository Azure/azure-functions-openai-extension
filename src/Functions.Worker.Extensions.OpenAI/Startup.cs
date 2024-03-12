// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embedding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI;
using System.Text.Json;
using Functions.Worker.Extensions.OpenAI.Search;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embedding;
using WebJobs.Extensions.OpenAI.Search;

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
            jsonSerializerOptions.Converters.Add(new Embedding.EmbeddingsOptionsJsonConverter());
            jsonSerializerOptions.Converters.Add(new SearchableDocumentJsonConverter());
            jsonSerializerOptions.Converters.Add(new ChatCompletionsJsonConverter());
        });
    }
}

