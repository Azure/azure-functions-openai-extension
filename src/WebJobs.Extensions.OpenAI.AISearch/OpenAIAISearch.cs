// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.AISearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.DependencyInjection;
using WebJobs.Extensions.OpenAI.AISearch;

// Reference: https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
[assembly: FunctionsStartup(typeof(AISearch))]

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.AISearch;

class AISearch : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddSingleton<ISearchProvider, AISearchProvider>();
    }
}
