// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace WebJobsOpenAIUnitTests;

public class OpenAIExtensionTests
{
    const string RetirementWarning =
        "The Azure Functions OpenAI extension retires on April 20, 2027. " +
        "Preview extension bundle version 4.60.0 and later don't include the extension. " +
        "Migrate to the OpenAI, Microsoft Foundry, or Azure AI Search SDKs directly, depending on your scenario. " +
        "Existing apps can use preview extension bundle versions earlier than 4.60.0 by setting the version range to [4.*, 4.60.0) in host.json. " +
        ".NET apps can continue using the corresponding Azure OpenAI extension NuGet packages directly. " +
        "Neither pinning an older bundle nor continuing to use the NuGet packages extends support beyond April 20, 2027. " +
        "More information: https://learn.microsoft.com/azure/azure-functions/functions-bindings-openai";

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Startup_LogsRetirementWarningOncePerHost(bool includeBindings, bool registerTwice)
    {
        for (int hostIndex = 0; hostIndex < 2; hostIndex++)
        {
            var logger = new Mock<ILogger>();
            logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            var loggerProvider = new Mock<ILoggerProvider>();
            loggerProvider.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);
            loggerProvider.Setup(p => p.CreateLogger(typeof(OpenAIExtension).FullName!)).Returns(logger.Object);

            Type[] functionTypes = includeBindings
                ? [typeof(Functions), typeof(OpenAIFunctions)]
                : [typeof(Functions)];
            var typeLocator = new Mock<ITypeLocator>();
            typeLocator.Setup(l => l.GetTypes()).Returns(functionTypes);

            using IHost host = new HostBuilder()
                .ConfigureLogging(logging => logging.AddProvider(loggerProvider.Object))
                .ConfigureWebJobs(builder =>
                {
                    new OpenAIWebJobsStartup().Configure(builder);
                    if (registerTwice)
                    {
                        builder.AddOpenAIBindings();
                    }
                })
                .ConfigureServices(services => services.AddSingleton(typeLocator.Object))
                .Build();

            Assert.DoesNotContain(logger.Invocations, invocation => invocation.Method.Name == nameof(ILogger.Log));

            await host.StartAsync(TestContext.Current.CancellationToken);

            AssertWarning(logger);

            IJobHost jobHost = host.Services.GetRequiredService<IJobHost>();
            await jobHost.CallAsync(nameof(Functions.Plain), cancellationToken: TestContext.Current.CancellationToken);
            await jobHost.CallAsync(nameof(Functions.Plain), cancellationToken: TestContext.Current.CancellationToken);
            await host.StopAsync(TestContext.Current.CancellationToken);

            AssertWarning(logger);
        }
    }

    static void AssertWarning(Mock<ILogger> logger)
    {
        var log = Assert.Single(logger.Invocations, invocation => invocation.Method.Name == nameof(ILogger.Log));
        Assert.Equal(LogLevel.Warning, log.Arguments[0]);
        Assert.Equal(RetirementWarning, log.Arguments[2].ToString());
        Assert.Null(log.Arguments[3]);
    }

    public static class Functions
    {
        [NoAutomaticTrigger]
        public static void Plain()
        {
        }
    }

    public static class OpenAIFunctions
    {
        [NoAutomaticTrigger]
        public static void Completion([TextCompletion("Test prompt")] string completion)
        {
        }

        [NoAutomaticTrigger]
        public static void Embeddings([Embeddings("Test input", InputType.RawText)] string embeddings)
        {
        }
    }
}
