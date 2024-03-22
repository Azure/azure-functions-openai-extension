// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.AzureAISearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                config.GetSection("azurefunctionsjobhost:extensions:openai:searchprovider").Bind(options);
            });
        builder.Services.AddSingleton<ISearchProvider, AzureAISearchProvider>();
    }
}
