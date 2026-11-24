// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Azure.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBNoSqlSearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ConnectionInfo = Microsoft.Azure.WebJobs.Extensions.OpenAI.Search.ConnectionInfo;

namespace WebJobsOpenAIUnitTests;

/// <summary>
/// Tests <see cref="CosmosDBNoSqlSearchProvider"/> using mocked clients.
/// </summary>
public class CosmosDBNoSqlSearchProviderConcurrencyTests
{
    const int ConnectionCount = 6;
    static readonly string[] ContainerNames = { "container-a", "container-b" };

    sealed record Expected(string ConnectionName, string ClientSettings, string ContainerName);

    sealed record Observation(Expected? Expected, string ClientSettings, string Operation, string ContainerName);

    sealed class SearchReachedException : Exception
    {
    }

    sealed class NamedTokenCredential : TokenCredential
    {
        public NamedTokenCredential(string token) => this.Token = token;

        public string Token { get; }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new(this.Token, DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new(this.GetToken(requestContext, cancellationToken));
    }

    sealed class Recorder
    {
        public static readonly AsyncLocal<Expected?> Current = new();

        public ConcurrentQueue<Observation> Observations { get; } = new();

        public ConcurrentQueue<string> CreatedClients { get; } = new();

        public CosmosClient CreateClient(string endpointOrConnectionString, TokenCredential? credential, CosmosClientOptions options)
        {
            string credentialName = credential switch
            {
                null => "none",
                NamedTokenCredential named => named.Token,
                _ => credential.GetType().Name,
            };
            string clientSettings = $"{endpointOrConnectionString}|{credentialName}";
            this.CreatedClients.Enqueue(clientSettings);

            Mock<CosmosClient> client = new();
            client
                .Setup(c => c.CreateDatabaseIfNotExistsAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .Returns((string id, int? _, RequestOptions _, CancellationToken _) =>
                {
                    Mock<DatabaseResponse> response = new();
                    response.SetupGet(r => r.Database).Returns(this.CreateDatabase(clientSettings));
                    return Task.FromResult(response.Object);
                });
            client
                .Setup(c => c.GetDatabase(It.IsAny<string>()))
                .Returns((string _) => this.CreateDatabase(clientSettings));
            return client.Object;
        }

        Database CreateDatabase(string clientSettings)
        {
            Mock<Database> database = new();
            database
                .Setup(d => d.CreateContainerIfNotExistsAsync(It.IsAny<ContainerProperties>(), It.IsAny<int?>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .Returns((ContainerProperties properties, int? _, RequestOptions _, CancellationToken _) =>
                {
                    this.Observations.Enqueue(new Observation(Current.Value, clientSettings, "CreateContainer", properties.Id));
                    return Task.FromResult<ContainerResponse>(null!);
                });
            database
                .Setup(d => d.GetContainer(It.IsAny<string>()))
                .Returns((string id) =>
                {
                    this.Observations.Enqueue(new Observation(Current.Value, clientSettings, "GetContainer", id));
                    Mock<Container> container = new();
                    container
                        .Setup(c => c.GetItemQueryIterator<It.IsAnyType>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                        .Throws(new SearchReachedException());
                    return container.Object;
                });
            return database.Object;
        }
    }

    static (IConfiguration Configuration, List<Expected> Connections) BuildConnections()
    {
        Dictionary<string, string?> values = new();
        List<Expected> connections = new();

        for (int i = 0; i < ConnectionCount; i++)
        {
            string name = $"CosmosNoSql{i}";
            string endpoint = $"https://cosmos{i}.example.test:443/";

            switch (i % 3)
            {
                case 0:
                    values[$"{name}:Endpoint"] = endpoint;
                    connections.Add(new Expected(name, $"{endpoint}|token-{name}", string.Empty));
                    break;
                case 1:
                    // Flat endpoint setting.
                    values[name] = endpoint;
                    connections.Add(new Expected(name, $"{endpoint}|DefaultAzureCredential", string.Empty));
                    break;
                default:
                    string connectionString = $"AccountEndpoint={endpoint};AccountKey={Convert.ToBase64String(Encoding.UTF8.GetBytes("test-key-" + name))};";
                    values[name] = connectionString;
                    connections.Add(new Expected(name, $"{connectionString}|none", string.Empty));
                    break;
            }
        }

        return (new ConfigurationBuilder().AddInMemoryCollection(values).Build(), connections);
    }

    static CosmosDBNoSqlSearchProvider CreateProvider(IConfiguration configuration, Recorder recorder)
    {
        Mock<AzureComponentFactory> azureComponentFactory = new();
        azureComponentFactory
            .Setup(f => f.CreateTokenCredential(It.IsAny<IConfiguration>()))
            .Returns((IConfiguration section) => new NamedTokenCredential($"token-{((IConfigurationSection)section).Path}"));

        return new CosmosDBNoSqlSearchProvider(
            configuration,
            LoggerFactory.Create(_ => { }),
            Microsoft.Extensions.Options.Options.Create(new CosmosDBNoSqlSearchConfigOptions { EmbeddingKey = "/embedding" }),
            azureComponentFactory.Object,
            recorder.CreateClient);
    }

    static async Task InvokeAsync(CosmosDBNoSqlSearchProvider provider, Expected expected, bool addDocument)
    {
        Recorder.Current.Value = expected;
        ConnectionInfo connectionInfo = new(expected.ConnectionName, expected.ContainerName);

        if (addDocument)
        {
            SearchableDocument document = new("title")
            {
                ConnectionInfo = connectionInfo,
                Embeddings = new EmbeddingsContext(new List<string>(), null),
            };
            await provider.AddDocumentAsync(document, CancellationToken.None);
            return;
        }

        try
        {
            await provider.SearchAsync(new SearchRequest("hello", new float[] { 0.1f, 0.2f }, 1, connectionInfo));
        }
        catch (SearchReachedException)
        {
            // The query reached the mocked container; the container that was used has been recorded.
        }
    }

    static List<string> FindMismatches(IEnumerable<Observation> observations)
    {
        List<string> mismatches = new();
        foreach (Observation o in observations)
        {
            if (o.Expected is null)
            {
                mismatches.Add($"Operation without expectation: {o.Operation} {o.ContainerName}");
                continue;
            }

            Expected e = o.Expected;
            bool connectionMismatch = !string.Equals(o.ClientSettings, e.ClientSettings, StringComparison.Ordinal);
            bool containerMismatch = !string.Equals(o.ContainerName, e.ContainerName, StringComparison.Ordinal);
            if (connectionMismatch || containerMismatch)
            {
                mismatches.Add(
                    $"[{(connectionMismatch ? "connection" : "container")}] {e.ConnectionName}/{e.ContainerName}: " +
                    $"{o.Operation} used client for {o.ClientSettings.Split('|')[0].Split(';')[0]} and container {o.ContainerName}");
            }
        }

        return mismatches;
    }

    [Fact]
    public async Task ConcurrentFirstUse_EachConnectionUsesItsOwnClientAndContainer()
    {
        const int rounds = 25;
        const int repeatsPerCombination = 8;
        (IConfiguration configuration, List<Expected> connections) = BuildConnections();

        List<string> mismatches = new();
        ConcurrentQueue<Exception> failures = new();
        int observationCount = 0;
        int unexpectedClients = 0;
        HashSet<string> expectedClientSettings = connections.Select(c => c.ClientSettings).ToHashSet();

        for (int round = 0; round < rounds; round++)
        {
            Recorder recorder = new();

            CosmosDBNoSqlSearchProvider provider = CreateProvider(configuration, recorder);

            List<(Expected Expected, bool AddDocument)> work = (
                from connection in connections
                from containerName in ContainerNames
                from addDocument in new[] { true, false }
                from _ in Enumerable.Range(0, repeatsPerCombination)
                select (connection with { ContainerName = containerName }, addDocument)).ToList();
            Random random = new(round);
            work = work.OrderBy(_ => random.Next()).ToList();

            await Parallel.ForEachAsync(
                work,
                new ParallelOptions { MaxDegreeOfParallelism = 64 },
                async (item, _) =>
                {
                    try
                    {
                        await InvokeAsync(provider, item.Expected, item.AddDocument);
                    }
                    catch (Exception ex)
                    {
                        failures.Enqueue(ex);
                    }
                });

            observationCount += recorder.Observations.Count;
            unexpectedClients += recorder.CreatedClients.Count(c => !expectedClientSettings.Contains(c));
            mismatches.AddRange(FindMismatches(recorder.Observations));
        }

        int connectionMismatches = mismatches.Count(m => m.StartsWith("[connection]", StringComparison.Ordinal));
        Assert.True(observationCount > 0, "No operations were observed.");
        Assert.True(
            failures.IsEmpty && mismatches.Count == 0 && unexpectedClients == 0,
            $"{failures.Count} calls failed; {mismatches.Count} of {observationCount} operations used unexpected settings " +
            $"({connectionMismatches} client, {mismatches.Count - connectionMismatches} container); {unexpectedClients} unexpected clients.{Environment.NewLine}" +
            string.Join(Environment.NewLine, mismatches.Take(5)) +
            Environment.NewLine +
            string.Join(Environment.NewLine, failures.Select(f => f.GetType().Name + ": " + f.Message).Distinct().Take(5)));
    }

    [Fact]
    public async Task SequentialCalls_DifferentConnectionsAndContainers_UseMatchingSettings()
    {
        (IConfiguration configuration, List<Expected> connections) = BuildConnections();
        Recorder recorder = new();
        CosmosDBNoSqlSearchProvider provider = CreateProvider(configuration, recorder);

        foreach (Expected connection in connections)
        {
            foreach (string containerName in ContainerNames)
            {
                await InvokeAsync(provider, connection with { ContainerName = containerName }, addDocument: true);
                await InvokeAsync(provider, connection with { ContainerName = containerName }, addDocument: false);
            }
        }

        Assert.Equal(connections.Count * ContainerNames.Length * 2, recorder.Observations.Count);
        Assert.Empty(FindMismatches(recorder.Observations));
        Assert.Equal(connections.Count, recorder.CreatedClients.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NullConnectionName_AfterOtherConnection_UsesConfiguredDefault(bool addDocument)
    {
        const string defaultEndpoint = "https://default.example.test:443/";
        const string otherEndpoint = "https://other.example.test:443/";
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CosmosDBNoSqlConnectionString"] = defaultEndpoint,
                ["ConnB:Endpoint"] = otherEndpoint,
            })
            .Build();
        Recorder recorder = new();
        CosmosDBNoSqlSearchProvider provider = CreateProvider(configuration, recorder);

        await InvokeAsync(provider, new Expected("ConnB", $"{otherEndpoint}|token-ConnB", "container-b"), addDocument);
        await InvokeAsync(provider, new Expected(null!, $"{defaultEndpoint}|DefaultAzureCredential", "container-a"), addDocument);

        Assert.Equal(2, recorder.Observations.Count);
        Assert.Empty(FindMismatches(recorder.Observations));
        Assert.Equal(
            new[] { $"{otherEndpoint}|token-ConnB", $"{defaultEndpoint}|DefaultAzureCredential" },
            recorder.CreatedClients.ToArray());
        Assert.Equal(
            new[] { "ConnB", "CosmosDBNoSqlConnectionString" },
            GetCachedClients(provider).Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void DefaultClientFactory_CreatesClientForEachConfigurationShape()
    {
        string connectionString = $"AccountEndpoint=https://key.example.test:443/;AccountKey={Convert.ToBase64String(Encoding.UTF8.GetBytes("test-key"))};";
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SectionConn:Endpoint"] = "https://section.example.test:443/",
                ["EndpointConn"] = "https://endpoint.example.test:443/",
                ["KeyConn"] = connectionString,
            })
            .Build();
        Mock<AzureComponentFactory> azureComponentFactory = CreateAzureComponentFactory();
        CosmosDBNoSqlSearchProvider provider = CreateProviderWithDefaultClientFactory(configuration, azureComponentFactory.Object);

        using CosmosClient sectionClient = GetCosmosClient(provider, "SectionConn");
        azureComponentFactory.Verify(
            f => f.CreateTokenCredential(It.Is<IConfiguration>(c => ((IConfigurationSection)c).Path == "SectionConn")),
            Times.Once);

        using CosmosClient endpointClient = GetCosmosClient(provider, "EndpointConn");
        using CosmosClient keyClient = GetCosmosClient(provider, "KeyConn");
        azureComponentFactory.Verify(f => f.CreateTokenCredential(It.IsAny<IConfiguration>()), Times.Once);

        Assert.Equal(new Uri("https://section.example.test:443/"), sectionClient.Endpoint);
        Assert.Equal(new Uri("https://endpoint.example.test:443/"), endpointClient.Endpoint);
        Assert.Equal(new Uri("https://key.example.test:443/"), keyClient.Endpoint);
        Assert.Contains("TokenCredential", GetAuthorizationProviderTypeName(sectionClient), StringComparison.Ordinal);
        Assert.Contains("TokenCredential", GetAuthorizationProviderTypeName(endpointClient), StringComparison.Ordinal);
        Assert.Contains("MasterKey", GetAuthorizationProviderTypeName(keyClient), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingConfiguration_ErrorNamesRequestedConnection()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CosmosDBNoSqlConnectionString"] = "https://default.example.test:443/",
            })
            .Build();
        CosmosDBNoSqlSearchProvider provider = CreateProviderWithDefaultClientFactory(configuration, CreateAzureComponentFactory().Object);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InvokeAsync(provider, new Expected("MissingConn", string.Empty, "container-a"), addDocument: true));

        Assert.Contains("'MissingConn'", ex.Message, StringComparison.Ordinal);
        Assert.Empty(GetCachedClients(provider));
    }

    [Fact]
    public async Task DefaultClientFactory_ConcurrentFirstUse_CachesOneClientPerConnectionWithItsEndpoint()
    {
        const int rounds = 10;
        const int callsPerConnection = 16;
        (IConfiguration configuration, List<Expected> connections) = BuildConnections();
        Dictionary<string, Uri> expectedEndpoints = connections.ToDictionary(
            c => c.ConnectionName,
            c => new Uri($"https://cosmos{c.ConnectionName.Substring("CosmosNoSql".Length)}.example.test:443/"));

        for (int round = 0; round < rounds; round++)
        {
            CosmosDBNoSqlSearchProvider provider = CreateProviderWithDefaultClientFactory(configuration, CreateAzureComponentFactory().Object);
            List<string> work = connections
                .SelectMany(c => Enumerable.Repeat(c.ConnectionName, callsPerConnection))
                .OrderBy(_ => Random.Shared.Next())
                .ToList();
            ConcurrentBag<(string ConnectionName, CosmosClient Client)> results = new();

            await Parallel.ForEachAsync(
                work,
                new ParallelOptions { MaxDegreeOfParallelism = 64 },
                (connectionName, _) =>
                {
                    results.Add((connectionName, GetCosmosClient(provider, connectionName)));
                    return ValueTask.CompletedTask;
                });

            ConcurrentDictionary<string, CosmosClient> cache = GetCachedClients(provider);
            try
            {
                Assert.Equal(expectedEndpoints.Keys.OrderBy(k => k), cache.Keys.OrderBy(k => k));
                foreach ((string connectionName, CosmosClient client) in results)
                {
                    Assert.Same(cache[connectionName], client);
                }

                foreach (KeyValuePair<string, CosmosClient> entry in cache)
                {
                    Assert.Equal(expectedEndpoints[entry.Key], entry.Value.Endpoint);
                }

                Assert.Equal(cache.Count, cache.Values.Distinct().Count());
            }
            finally
            {
                foreach (CosmosClient client in cache.Values)
                {
                    client.Dispose();
                }
            }
        }
    }

    static Mock<AzureComponentFactory> CreateAzureComponentFactory()
    {
        Mock<AzureComponentFactory> azureComponentFactory = new();
        azureComponentFactory
            .Setup(f => f.CreateTokenCredential(It.IsAny<IConfiguration>()))
            .Returns((IConfiguration section) => new NamedTokenCredential($"token-{((IConfigurationSection)section).Path}"));
        return azureComponentFactory;
    }

    static CosmosDBNoSqlSearchProvider CreateProviderWithDefaultClientFactory(IConfiguration configuration, AzureComponentFactory azureComponentFactory) =>
        new(
            configuration,
            LoggerFactory.Create(_ => { }),
            Microsoft.Extensions.Options.Options.Create(new CosmosDBNoSqlSearchConfigOptions { EmbeddingKey = "/embedding" }),
            azureComponentFactory,
            clientFactory: null);

    static CosmosClient GetCosmosClient(CosmosDBNoSqlSearchProvider provider, string connectionName)
    {
        MethodInfo method = typeof(CosmosDBNoSqlSearchProvider).GetMethod("GetCosmosClient", BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            return (CosmosClient)method.Invoke(provider, new object[] { connectionName })!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    static ConcurrentDictionary<string, CosmosClient> GetCachedClients(CosmosDBNoSqlSearchProvider provider) =>
        (ConcurrentDictionary<string, CosmosClient>)typeof(CosmosDBNoSqlSearchProvider)
            .GetField("cosmosDBClients", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(provider)!;

    static string GetAuthorizationProviderTypeName(CosmosClient client) =>
        typeof(CosmosClient)
            .GetProperty("AuthorizationTokenProvider", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(client)!
            .GetType()
            .Name;
}
