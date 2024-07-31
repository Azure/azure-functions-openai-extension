// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBNoSqlSearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Reference: https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
[assembly: FunctionsStartup(typeof(OpenAICosmosDBNoSqlSearch))]

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBNoSqlSearch;

class OpenAICosmosDBNoSqlSearch : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder
            .Services.AddOptions<CosmosDBNoSqlSearchConfigOptions>()
            .Configure<IConfiguration>(
                (options, config) =>
                {
                    config
                        .GetSection("azurefunctionsjobhost:extensions:openai:searchprovider")
                        .Bind(options);
                }
            );
        builder.Services.AddSingleton<ISearchProvider, CosmosDBNoSqlSearchProvider>();
    }
}
