// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBSearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;
using ConnectionInfo = Microsoft.Azure.WebJobs.Extensions.OpenAI.Search.ConnectionInfo;

namespace WebJobsOpenAIUnitTests;

/// <summary>
/// Tests <see cref="CosmosDBSearchProvider"/> using mocked Mongo clients.
/// </summary>
public class CosmosDBSearchProviderConcurrencyTests
{
    const int ConnectionCount = 4;
    static readonly string[] CollectionNames = { "collection-a", "collection-b", "collection-c" };

    sealed record Expected(string ConnectionName, string Host, string CollectionName);

    sealed record Observation(Expected? Expected, string Host, string Operation, string CollectionName);

    sealed class Recorder
    {
        public static readonly AsyncLocal<Expected?> Current = new();

        public ConcurrentQueue<Observation> Observations { get; } = new();

        public ConcurrentQueue<string> CreatedClients { get; } = new();

        public IMongoClient CreateClient(MongoClientSettings settings)
        {
            string host = settings.Server.Host;
            this.CreatedClients.Enqueue(host);

            Mock<IMongoClient> client = new();
            client
                .Setup(c => c.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                .Returns((string _, MongoDatabaseSettings _) => this.CreateDatabase(host));
            return client.Object;
        }

        void Record(string host, string operation, string collectionName) =>
            this.Observations.Enqueue(new Observation(Current.Value, host, operation, collectionName));

        IMongoDatabase CreateDatabase(string host)
        {
            Mock<IMongoDatabase> database = new();
            database
                .Setup(d => d.GetCollection<BsonDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
                .Returns((string name, MongoCollectionSettings _) => this.CreateCollection(host, name));
            database
                .Setup(d => d.RunCommand(It.IsAny<Command<BsonDocument>>(), It.IsAny<ReadPreference>(), It.IsAny<CancellationToken>()))
                .Returns((Command<BsonDocument> command, ReadPreference _, CancellationToken _) =>
                {
                    BsonDocument document = ((BsonDocumentCommand<BsonDocument>)command).Document;
                    this.Record(host, "CreateIndexes", document["createIndexes"].AsString);
                    return new BsonDocument("ok", 1);
                });
            return database.Object;
        }

        IMongoCollection<BsonDocument> CreateCollection(string host, string collectionName)
        {
            Mock<IMongoIndexManager<BsonDocument>> indexes = new();
            indexes
                .Setup(i => i.List(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    this.Record(host, "ListIndexes", collectionName);

                    // Simulates the latency of a round trip to the server.
                    Thread.Sleep(1);
                    return EmptyCursor();
                });

            Mock<IMongoCollection<BsonDocument>> collection = new();
            collection.SetupGet(c => c.Indexes).Returns(indexes.Object);
            collection
                .Setup(c => c.InsertManyAsync(It.IsAny<IEnumerable<BsonDocument>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    this.Record(host, "InsertMany", collectionName);
                    return Task.CompletedTask;
                });
            collection
                .Setup(c => c.AggregateAsync(It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(), It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    this.Record(host, "Aggregate", collectionName);
                    return Task.FromResult(EmptyCursor());
                });
            return collection.Object;
        }

        static IAsyncCursor<BsonDocument> EmptyCursor()
        {
            Mock<IAsyncCursor<BsonDocument>> cursor = new();
            cursor.Setup(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(false);
            cursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            return cursor.Object;
        }
    }

    static (IConfiguration Configuration, List<Expected> Connections) BuildConnections()
    {
        Dictionary<string, string?> values = new();
        List<Expected> connections = new();

        for (int i = 0; i < ConnectionCount; i++)
        {
            string name = $"CosmosMongo{i}";
            string host = $"mongo{i}.example.test";
            values[name] = $"mongodb://user{i}:password{i}@{host}:10255/?ssl=true";
            connections.Add(new Expected(name, host, string.Empty));
        }

        return (new ConfigurationBuilder().AddInMemoryCollection(values).Build(), connections);
    }

    static CosmosDBSearchProvider CreateProvider(IConfiguration configuration, Recorder recorder) =>
        new(
            LoggerFactory.Create(_ => { }),
            Microsoft.Extensions.Options.Options.Create(new CosmosDBSearchConfigOptions { VectorSearchDimensions = 2 }),
            configuration,
            recorder.CreateClient);

    static Task InvokeAsync(CosmosDBSearchProvider provider, Expected expected, bool addDocument)
    {
        Recorder.Current.Value = expected;
        ConnectionInfo connectionInfo = new(expected.ConnectionName, expected.CollectionName);

        if (addDocument)
        {
            SearchableDocument document = new("title")
            {
                ConnectionInfo = connectionInfo,
                Embeddings = new EmbeddingsContext(new List<string>(), null),
            };
            return provider.AddDocumentAsync(document, CancellationToken.None);
        }

        return provider.SearchAsync(new SearchRequest("hello", new float[] { 0.1f, 0.2f }, 1, connectionInfo));
    }

    static List<string> FindMismatches(IEnumerable<Observation> observations)
    {
        List<string> mismatches = new();
        foreach (Observation o in observations)
        {
            if (o.Expected is null)
            {
                mismatches.Add($"Operation without expectation: {o.Operation} {o.Host}/{o.CollectionName}");
                continue;
            }

            Expected e = o.Expected;
            bool connectionMismatch = !string.Equals(o.Host, e.Host, StringComparison.Ordinal);
            bool collectionMismatch = !string.Equals(o.CollectionName, e.CollectionName, StringComparison.Ordinal);
            if (connectionMismatch || collectionMismatch)
            {
                mismatches.Add(
                    $"[{(connectionMismatch ? "connection" : "collection")}] {e.ConnectionName}/{e.CollectionName}: " +
                    $"{o.Operation} used {o.Host}/{o.CollectionName}");
            }
        }

        return mismatches;
    }

    [Fact]
    public async Task ConcurrentFirstUse_EachCallUsesItsOwnConnectionAndCollection()
    {
        const int rounds = 25;
        const int repeatsPerCombination = 6;
        (IConfiguration configuration, List<Expected> connections) = BuildConnections();

        List<string> mismatches = new();
        ConcurrentQueue<Exception> failures = new();
        int observationCount = 0;

        for (int round = 0; round < rounds; round++)
        {
            Recorder recorder = new();

            CosmosDBSearchProvider provider = CreateProvider(configuration, recorder);

            List<(Expected Expected, bool AddDocument)> work = (
                from connection in connections
                from collectionName in CollectionNames
                from addDocument in new[] { true, false }
                from _ in Enumerable.Range(0, repeatsPerCombination)
                select (connection with { CollectionName = collectionName }, addDocument)).ToList();
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
            mismatches.AddRange(FindMismatches(recorder.Observations));
        }

        int connectionMismatches = mismatches.Count(m => m.StartsWith("[connection]", StringComparison.Ordinal));
        Assert.True(observationCount > 0, "No operations were observed.");
        Assert.True(
            failures.IsEmpty && mismatches.Count == 0,
            $"{failures.Count} calls failed; {mismatches.Count} of {observationCount} operations used unexpected settings " +
            $"({connectionMismatches} connection, {mismatches.Count - connectionMismatches} collection).{Environment.NewLine}" +
            string.Join(Environment.NewLine, mismatches.Take(5)) +
            Environment.NewLine +
            string.Join(Environment.NewLine, failures.Select(f => f.GetType().Name + ": " + f.Message).Distinct().Take(5)));
    }

    [Fact]
    public async Task SequentialCalls_DifferentConnectionsAndCollections_UseMatchingSettings()
    {
        (IConfiguration configuration, List<Expected> connections) = BuildConnections();
        Recorder recorder = new();
        CosmosDBSearchProvider provider = CreateProvider(configuration, recorder);

        foreach (Expected connection in connections)
        {
            foreach (string collectionName in CollectionNames)
            {
                await InvokeAsync(provider, connection with { CollectionName = collectionName }, addDocument: true);
                await InvokeAsync(provider, connection with { CollectionName = collectionName }, addDocument: false);
            }
        }

        // Each AddDocumentAsync lists indexes, creates the index and inserts; each SearchAsync aggregates.
        Assert.Equal(connections.Count * CollectionNames.Length * 4, recorder.Observations.Count);
        Assert.Empty(FindMismatches(recorder.Observations));
        Assert.Equal(connections.Count, recorder.CreatedClients.Count);
    }
}
