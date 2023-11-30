// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI;
using Microsoft.Azure.WebJobs.Hosting;

// This auto-registers the OpenAI extension when a webjobs/functions host starts up.
[assembly: WebJobsStartup(typeof(OpenAIWebJobsStartup))]

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

class OpenAIWebJobsStartup : IWebJobsStartup2
{
    public void Configure(WebJobsBuilderContext context, IWebJobsBuilder builder)
    {
        this.Configure(builder);
    }

    public void Configure(IWebJobsBuilder builder)
    {
        builder.AddOpenAIBindings();
    }
}
