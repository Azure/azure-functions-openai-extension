// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.AzureAISearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebJobs.Extensions.OpenAI;
using WebJobs.Extensions.OpenAI.AzureAISearch;

// Reference: https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
[assembly: FunctionsStartup(typeof(OpenAIAzureAISearch))]

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.AzureAISearch;

class OpenAIAzureAISearch : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddOptions<AzureAISearchConfigOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                // For in-proc
                config.GetSection("azurefunctionsjobhost:extensions:azureaisearch").Bind(options);
            });
        builder.Services.AddSingleton<ISearchProvider, AzureAISearchProvider>();
    }
}
