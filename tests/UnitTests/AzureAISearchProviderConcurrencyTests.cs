// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.AzureAISearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace WebJobsOpenAIUnitTests;

/// <summary>
/// Tests <see cref="AzureAISearchProvider"/> using an in-memory HTTP handler.
/// </summary>
public class AzureAISearchProviderConcurrencyTests
{
    const int ConnectionCount = 7;
    static readonly string[] IndexNames = { "index-a", "index-b" };

    sealed record Expected(string ConnectionName, string Host, string? ApiKey, string? BearerToken, string IndexName);

    sealed record Observation(Expected? Expected, string Method, string Host, string PathAndQuery, string? ApiKey, string? Authorization);

    sealed class RecordingHandler : HttpMessageHandler
    {
        public static readonly AsyncLocal<Expected?> Current = new();

        public ConcurrentQueue<Observation> Observations { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? apiKey = request.Headers.TryGetValues("api-key", out IEnumerable<string>? values) ? values.Single() : null;
            Uri uri = request.RequestUri!;
            this.Observations.Enqueue(new Observation(
                Current.Value,
                request.Method.Method,
                uri.Host,
                Uri.UnescapeDataString(uri.PathAndQuery),
                apiKey,
                request.Headers.Authorization?.ToString()));

            string body = request.Method == HttpMethod.Get && uri.AbsolutePath.TrimEnd('/').EndsWith("/indexes", StringComparison.Ordinal)
                ? "{\"value\":[" + string.Join(",", IndexNames.Select(n => $"{{\"name\":\"{n}\"}}")) + "]}"
                : "{\"value\":[]}";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    sealed class NamedTokenCredential : TokenCredential
    {
        readonly string token;

        public NamedTokenCredential(string token) => this.token = token;

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new(this.token, DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new(this.GetToken(requestContext, cancellationToken));
    }

    static (IConfiguration Configuration, List<Expected> Connections) BuildConnections()
    {
        Dictionary<string, string?> values = new();
        List<Expected> connections = new();

        for (int i = 0; i < ConnectionCount; i++)
        {
            string name = $"SearchConn{i}";
            string host = $"search{i}.example.test";
            string endpoint = $"https://{host}/";

            switch (i % 3)
            {
                case 0:
                    values[$"{name}:endPoint"] = endpoint;
                    values[$"{name}:apiKey"] = $"key-{i}";
                    connections.Add(new Expected(name, host, $"key-{i}", null, string.Empty));
                    break;
                case 1:
                    values[$"{name}:endPoint"] = endpoint;
                    connections.Add(new Expected(name, host, null, $"token-{name}", string.Empty));
                    break;
                default:
                    values[name] = endpoint;
                    connections.Add(new Expected(name, host, null, $"token-{name}", string.Empty));
                    break;
            }
        }

        return (new ConfigurationBuilder().AddInMemoryCollection(values).Build(), connections);
    }

    static AzureAISearchProvider CreateProvider(IConfiguration configuration, RecordingHandler handler)
    {
        Mock<AzureComponentFactory> azureComponentFactory = new();
        azureComponentFactory
            .Setup(f => f.CreateTokenCredential(It.IsAny<IConfiguration>()))
            .Returns((IConfiguration section) => new NamedTokenCredential($"token-{((IConfigurationSection)section).Path}"));

        SearchClientOptions clientOptions = new()
        {
            Transport = new HttpClientTransport(new HttpClient(handler)),
        };
        clientOptions.Retry.MaxRetries = 0;
        clientOptions.Diagnostics.IsLoggingEnabled = false;
        clientOptions.Diagnostics.IsDistributedTracingEnabled = false;

        return new AzureAISearchProvider(
            configuration,
            LoggerFactory.Create(_ => { }),
            Microsoft.Extensions.Options.Options.Create(new AzureAISearchConfigOptions()),
            azureComponentFactory.Object,
            clientOptions);
    }

    static Task InvokeAsync(AzureAISearchProvider provider, Expected expected, bool addDocument)
    {
        RecordingHandler.Current.Value = expected;
        ConnectionInfo connectionInfo = new(expected.ConnectionName, expected.IndexName);

        if (addDocument)
        {
            SearchableDocument document = new("title")
            {
                ConnectionInfo = connectionInfo,
                Embeddings = new EmbeddingsContext(new List<string>(), null),
            };
            return provider.AddDocumentAsync(document, CancellationToken.None);
        }

        return provider.SearchAsync(new SearchRequest("hello", ReadOnlyMemory<float>.Empty, 1, connectionInfo));
    }

    static List<string> FindMismatches(IEnumerable<Observation> observations)
    {
        List<string> mismatches = new();
        foreach (Observation o in observations)
        {
            if (o.Expected is null)
            {
                mismatches.Add($"Request without expectation: {o.Method} {o.Host}{o.PathAndQuery}");
                continue;
            }

            Expected e = o.Expected;
            string expectedAuthorization = e.BearerToken is null ? string.Empty : $"Bearer {e.BearerToken}";
            bool indexRequest = o.PathAndQuery.Contains("/indexes('", StringComparison.Ordinal);

            bool connectionMismatch = !string.Equals(o.Host, e.Host, StringComparison.OrdinalIgnoreCase)
                || o.ApiKey != e.ApiKey
                || (o.Authorization ?? string.Empty) != expectedAuthorization;
            bool indexMismatch = indexRequest && !o.PathAndQuery.Contains($"/indexes('{e.IndexName}')", StringComparison.Ordinal);

            if (connectionMismatch || indexMismatch)
            {
                mismatches.Add(
                    $"[{(connectionMismatch ? "connection" : "index")}] {e.ConnectionName}/{e.IndexName}: expected host={e.Host} key={e.ApiKey} auth={expectedAuthorization}; " +
                    $"actual {o.Method} host={o.Host} path={o.PathAndQuery} key={o.ApiKey} auth={o.Authorization}");
            }
        }

        return mismatches;
    }

    static List<string> FindCacheMismatches(AzureAISearchProvider provider, List<Expected> connections)
    {
        IDictionary cache = (IDictionary)typeof(AzureAISearchProvider)
            .GetField("searchIndexClients", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(provider)!;
        List<string> mismatches = new();
        if (cache.Count != connections.Count)
        {
            mismatches.Add($"[cache] expected {connections.Count} cached clients but found {cache.Count}");
        }

        Dictionary<string, Expected> expectedByName = connections.ToDictionary(c => c.ConnectionName);
        foreach (DictionaryEntry entry in cache)
        {
            Type keyType = entry.Key.GetType();
            string connectionName = (string)keyType.GetProperty("ConnectionName")!.GetValue(entry.Key)!;
            string keyEndpoint = (string)keyType.GetProperty("Endpoint")!.GetValue(entry.Key)!;
            Uri clientEndpoint = ((SearchIndexClient)entry.Value!).Endpoint;

            if (!expectedByName.TryGetValue(connectionName, out Expected? expected))
            {
                mismatches.Add($"[cache] unexpected cached connection {connectionName}");
                continue;
            }

            if (!string.Equals(new Uri(keyEndpoint).Host, expected.Host, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(clientEndpoint.Host, expected.Host, StringComparison.OrdinalIgnoreCase))
            {
                mismatches.Add($"[cache] {connectionName}: expected host={expected.Host}; key endpoint={keyEndpoint} client endpoint={clientEndpoint}");
            }
        }

        return mismatches;
    }

    [Fact]
    public async Task ConcurrentFirstUse_EachConnectionUsesItsOwnEndpointAndCredential()
    {
        const int rounds = 25;
        const int repeatsPerCombination = 8;
        (IConfiguration configuration, List<Expected> connections) = BuildConnections();

        List<string> mismatches = new();
        ConcurrentQueue<Exception> failures = new();
        int observationCount = 0;

        for (int round = 0; round < rounds; round++)
        {
            RecordingHandler handler = new();

            AzureAISearchProvider provider = CreateProvider(configuration, handler);

            List<(Expected Expected, bool AddDocument)> work = (
                from connection in connections
                from indexName in IndexNames
                from addDocument in new[] { true, false }
                from _ in Enumerable.Range(0, repeatsPerCombination)
                select (connection with { IndexName = indexName }, addDocument)).ToList();
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

            observationCount += handler.Observations.Count;
            mismatches.AddRange(FindMismatches(handler.Observations));
            mismatches.AddRange(FindCacheMismatches(provider, connections));
        }

        int connectionMismatches = mismatches.Count(m => m.StartsWith("[connection]", StringComparison.Ordinal));
        Assert.True(observationCount > 0, "No requests were observed.");
        Assert.True(
            failures.IsEmpty && mismatches.Count == 0,
            $"{failures.Count} calls failed; {mismatches.Count} of {observationCount} requests used unexpected settings " +
            $"({connectionMismatches} endpoint/credential, {mismatches.Count - connectionMismatches} index/cache).{Environment.NewLine}" +
            string.Join(Environment.NewLine, mismatches.OrderBy(m => m.StartsWith("[connection]", StringComparison.Ordinal) ? 0 : 1).Take(5)) +
            Environment.NewLine +
            string.Join(Environment.NewLine, failures.Select(f => f.GetType().Name + ": " + f.Message).Distinct().Take(5)));
    }

    [Fact]
    public async Task SameConnection_DifferentCollections_UseMatchingIndex()
    {
        (IConfiguration configuration, List<Expected> connections) = BuildConnections();
        RecordingHandler handler = new();
        AzureAISearchProvider provider = CreateProvider(configuration, handler);

        foreach (string indexName in IndexNames)
        {
            await InvokeAsync(provider, connections[0] with { IndexName = indexName }, addDocument: false);
            await InvokeAsync(provider, connections[0] with { IndexName = indexName }, addDocument: true);
        }

        Assert.NotEmpty(handler.Observations);
        Assert.Empty(FindMismatches(handler.Observations));
    }

    [Fact]
    public async Task ConfigurationChange_NewEndpointIsUsed()
    {
        Dictionary<string, string?> values = new()
        {
            ["SearchConn:endPoint"] = "https://before.example.test/",
            ["SearchConn:apiKey"] = "key-before",
        };
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        RecordingHandler handler = new();
        AzureAISearchProvider provider = CreateProvider(configuration, handler);

        await InvokeAsync(provider, new Expected("SearchConn", "before.example.test", "key-before", null, "index-a"), addDocument: false);

        configuration["SearchConn:endPoint"] = "https://after.example.test/";
        configuration["SearchConn:apiKey"] = "key-after";

        await InvokeAsync(provider, new Expected("SearchConn", "after.example.test", "key-after", null, "index-a"), addDocument: false);

        Assert.Equal(2, handler.Observations.Count);
        Assert.Empty(FindMismatches(handler.Observations));
    }
}
