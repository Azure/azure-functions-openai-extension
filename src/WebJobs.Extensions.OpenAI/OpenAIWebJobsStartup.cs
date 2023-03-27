// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Azure.WebJobs;
using WebJobs.Extensions.OpenAI;

// This auto-registers the OpenAI extension when a webjobs/functions host starts up.
[assembly: WebJobsStartup(typeof(OpenAIWebJobsStartup))]

namespace WebJobs.Extensions.OpenAI;

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
