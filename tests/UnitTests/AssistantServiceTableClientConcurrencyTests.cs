// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs.Extensions.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using OpenAI;
using Xunit;
using Match = System.Text.RegularExpressions.Match;

namespace WebJobsOpenAIUnitTests;

/// <summary>
/// Tests <see cref="DefaultAssistantService"/> using an in-memory HTTP handler.
/// </summary>
public class AssistantServiceTableClientConcurrencyTests
{
    const int ConnectionCount = 6;
    static readonly string[] TableNames = { "ChatStateA", "ChatStateB" };

    sealed record Expected(string? ConnectionSetting, string Host, string? AccountName, string? BearerToken, string TableName);

    sealed record Observation(Expected? Expected, string Method, string Host, string Path, string? Authorization, IReadOnlyList<string> Tables, string Body);

    sealed class RecordingHandler : HttpMessageHandler
    {
        public static readonly AsyncLocal<Expected?> Current = new();

        static readonly Regex SubRequestLine = new(@"^(POST|PUT|PATCH|DELETE|MERGE|GET) (\S+) HTTP/1\.1\r?$", RegexOptions.Multiline);

        public ConcurrentQueue<Observation> Observations { get; } = new();

        public Dictionary<string, List<JObject>>? StoredEntities { get; init; }

        public ConcurrentQueue<string> ChatRequests { get; } = new();

        public ConcurrentQueue<string> ChatResponses { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri uri = request.RequestUri!;
            string path = Uri.UnescapeDataString(uri.AbsolutePath);
            string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            if (path.EndsWith("/chat/completions", StringComparison.Ordinal))
            {
                this.ChatRequests.Enqueue(body);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent(
                        this.ChatResponses.TryDequeue(out string? chatResponse) ? chatResponse :
                            """{"id":"test","object":"chat.completion","created":0,"model":"test","choices":[{"index":0,"message":{"role":"assistant","content":"reply-for-b"},"finish_reason":"stop"}],"usage":{"prompt_tokens":1,"completion_tokens":1,"total_tokens":2}}""",
                        Encoding.UTF8,
                        "application/json"),
                };
            }

            List<string> tables = new();
            HttpResponseMessage response;

            if (request.Method == HttpMethod.Post && path.EndsWith("/$batch", StringComparison.Ordinal))
            {
                // Table transactions: each sub-request names its own host and table.
                MatchCollection subRequests = SubRequestLine.Matches(body);
                foreach (Match match in subRequests)
                {
                    Uri subUri = new(match.Groups[2].Value);
                    tables.Add(subUri.Host + "|" + Uri.UnescapeDataString(subUri.AbsolutePath).Trim('/').Split('(')[0]);
                }

                if (this.StoredEntities is not null)
                {
                    List<JObject> entities = Regex.Matches(body, @"^\{.+\}\r?$", RegexOptions.Multiline)
                        .Select(m => JObject.Parse(m.Value)).ToList();
                    Assert.Equal(subRequests.Count, entities.Count);
                    for (int i = 0; i < entities.Count; i++)
                    {
                        this.StoredEntities.TryAdd(tables[i], new());
                        List<JObject> rows = this.StoredEntities[tables[i]];
                        JObject entity = entities[i];
                        JObject? existing = rows.SingleOrDefault(row =>
                            JToken.DeepEquals(row["PartitionKey"], entity["PartitionKey"])
                            && JToken.DeepEquals(row["RowKey"], entity["RowKey"]));
                        if (subRequests[i].Groups[1].Value == "POST")
                        {
                            Assert.Null(existing);
                            rows.Add(entity);
                        }
                        else
                        {
                            Assert.Equal("PATCH", subRequests[i].Groups[1].Value);
                            Assert.NotNull(existing);
                            existing.Merge(entity);
                        }
                    }
                }

                response = CreateBatchResponse(subRequests.Count);
            }
            else if (request.Method == HttpMethod.Post && path.TrimEnd('/').EndsWith("/Tables", StringComparison.Ordinal))
            {
                Match match = Regex.Match(body, "\"TableName\"\\s*:\\s*\"([^\"]+)\"");
                tables.Add(uri.Host + "|" + match.Groups[1].Value);
                response = new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            else
            {
                // Entity queries use the "/{table}()" path.
                tables.Add(uri.Host + "|" + path.Trim('/').Replace("()", string.Empty, StringComparison.Ordinal));
                string json = this.StoredEntities is not null && this.StoredEntities.TryGetValue(tables[0], out List<JObject>? rows)
                    ? new JObject { ["value"] = new JArray(rows) }.ToString()
                    : "{\"value\":[]}";
                response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
            }

            this.Observations.Enqueue(new Observation(
                Current.Value,
                request.Method.Method,
                uri.Host,
                path,
                request.Headers.Authorization?.ToString(),
                tables,
                body));

            response.RequestMessage = request;
            return response;
        }

        static HttpResponseMessage CreateBatchResponse(int operationCount)
        {
            StringBuilder builder = new();
            builder.Append("--batchresponse_1\r\n");
            builder.Append("Content-Type: multipart/mixed; boundary=changesetresponse_1\r\n\r\n");
            for (int i = 0; i < operationCount; i++)
            {
                builder.Append("--changesetresponse_1\r\n");
                builder.Append("Content-Type: application/http\r\n");
                builder.Append("Content-Transfer-Encoding: binary\r\n\r\n");
                builder.Append("HTTP/1.1 204 No Content\r\n");
                builder.Append("DataServiceVersion: 3.0;\r\n");
                builder.Append("ETag: W/\"datetime'2026-01-01T00%3A00%3A00Z'\"\r\n\r\n");
            }

            builder.Append("--changesetresponse_1--\r\n");
            builder.Append("--batchresponse_1--\r\n");

            StringContent content = new(builder.ToString(), Encoding.UTF8);
            content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("multipart/mixed; boundary=batchresponse_1");
            return new HttpResponseMessage(HttpStatusCode.Accepted) { Content = content };
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

    static string ConnectionString(string accountName) =>
        $"DefaultEndpointsProtocol=https;AccountName={accountName};" +
        $"AccountKey={Convert.ToBase64String(Encoding.UTF8.GetBytes("test-key-" + accountName))};" +
        $"TableEndpoint=https://{accountName}.table.example.test/";

    static (IConfiguration Configuration, List<Expected> Connections) BuildConnections()
    {
        Dictionary<string, string?> values = new()
        {
            // Used when no chat storage connection setting is specified.
            ["AzureWebJobsStorage"] = ConnectionString("acctdefault"),
        };
        List<Expected> connections = new()
        {
            new Expected(null, "acctdefault.table.example.test", "acctdefault", null, string.Empty),
        };

        for (int i = 0; i < ConnectionCount; i++)
        {
            string name = $"ChatStorage{i}";
            string account = $"acct{i}";
            string host = $"{account}.table.example.test";

            if (i % 2 == 0)
            {
                values[name] = ConnectionString(account);
                connections.Add(new Expected(name, host, account, null, string.Empty));
            }
            else
            {
                values[$"{name}:tableServiceUri"] = $"https://{host}/";
                connections.Add(new Expected(name, host, null, $"token-{name}", string.Empty));
            }
        }

        return (new ConfigurationBuilder().AddInMemoryCollection(values).Build(), connections);
    }

    static DefaultAssistantService CreateService(
        IConfiguration configuration,
        RecordingHandler handler,
        ILoggerFactory? loggerFactory = null,
        IAssistantSkillInvoker? skillInvoker = null)
    {
        Mock<AzureComponentFactory> azureComponentFactory = new();
        azureComponentFactory
            .Setup(f => f.CreateTokenCredential(It.IsAny<IConfiguration>()))
            .Returns((IConfiguration section) => new NamedTokenCredential($"token-{((IConfigurationSection)section).Path}"));

        TableClientOptions tableClientOptions = new()
        {
            Transport = new HttpClientTransport(new HttpClient(handler)),
        };
        tableClientOptions.Retry.MaxRetries = 0;
        tableClientOptions.Diagnostics.IsLoggingEnabled = false;
        tableClientOptions.Diagnostics.IsDistributedTracingEnabled = false;

        loggerFactory ??= LoggerFactory.Create(_ => { });
        AzureOpenAIClientOptions openAIOptions = new()
        {
            Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
        };
        return new DefaultAssistantService(
            new OpenAIClientFactory(
                configuration,
                azureComponentFactory.Object,
                loggerFactory,
                (endpoint, credential) => new AzureOpenAIClient(endpoint, credential, openAIOptions),
                (endpoint, credential) => new AzureOpenAIClient(endpoint, credential, openAIOptions),
                key => new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions
                {
                    Transport = openAIOptions.Transport,
                    RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
                })),
            azureComponentFactory.Object,
            configuration,
            skillInvoker ?? Mock.Of<IAssistantSkillInvoker>(),
            loggerFactory,
            tableClientOptions);
    }

    static Task InvokeAsync(DefaultAssistantService service, Expected expected, bool create)
    {
        RecordingHandler.Current.Value = expected;

        if (create)
        {
            AssistantCreateRequest request = new($"assistant-{Guid.NewGuid():N}", "instructions")
            {
                ChatStorageConnectionSetting = expected.ConnectionSetting,
                CollectionName = expected.TableName,
            };
            return service.CreateAssistantAsync(request, CancellationToken.None);
        }

        AssistantQueryAttribute query = new($"assistant-{Guid.NewGuid():N}")
        {
            TimestampUtc = "2026-01-01T00:00:00Z",
            ChatStorageConnectionSetting = expected.ConnectionSetting,
            CollectionName = expected.TableName,
        };
        return service.GetStateAsync(query, CancellationToken.None);
    }

    static List<string> FindMismatches(IEnumerable<Observation> observations)
    {
        List<string> mismatches = new();
        foreach (Observation o in observations)
        {
            if (o.Expected is null)
            {
                mismatches.Add($"Request without expectation: {o.Method} {o.Host}{o.Path}");
                continue;
            }

            Expected e = o.Expected;
            string authorization = o.Authorization ?? string.Empty;
            bool credentialMatches = e.BearerToken is not null
                ? authorization == "Bearer" + " " + e.BearerToken
                : authorization.StartsWith("SharedKey", StringComparison.Ordinal) && authorization.Contains($" {e.AccountName}:", StringComparison.Ordinal);

            bool connectionMismatch = !string.Equals(o.Host, e.Host, StringComparison.OrdinalIgnoreCase) || !credentialMatches;
            bool tableMismatch = o.Tables.Count == 0 || o.Tables.Any(t => !string.Equals(t, $"{e.Host}|{e.TableName}", StringComparison.OrdinalIgnoreCase));

            if (connectionMismatch || tableMismatch)
            {
                mismatches.Add(
                    $"[{(connectionMismatch ? "connection" : "table")}] {e.ConnectionSetting ?? "(default)"}/{e.TableName}: expected host={e.Host} account={e.AccountName} token={e.BearerToken}; " +
                    $"actual {o.Method} host={o.Host} path={o.Path} tables={string.Join(",", o.Tables)} auth={o.Authorization?.Split(' ')[0]}");
            }
        }

        return mismatches;
    }

    [Fact]
    public async Task ConcurrentFirstUse_EachCallUsesItsOwnStorageConnectionAndTable()
    {
        const int rounds = 10;
        const int repeatsPerCombination = 6;
        (IConfiguration configuration, List<Expected> connections) = BuildConnections();

        List<string> mismatches = new();
        ConcurrentQueue<Exception> failures = new();
        int observationCount = 0;

        for (int round = 0; round < rounds; round++)
        {
            RecordingHandler handler = new();

            DefaultAssistantService service = CreateService(configuration, handler);

            List<(Expected Expected, bool Create)> work = (
                from connection in connections
                from tableName in TableNames
                from create in new[] { true, false }
                from _ in Enumerable.Range(0, repeatsPerCombination)
                select (connection with { TableName = tableName }, create)).ToList();
            Random random = new(round);
            work = work.OrderBy(_ => random.Next()).ToList();

            await Parallel.ForEachAsync(
                work,
                new ParallelOptions { MaxDegreeOfParallelism = 64 },
                async (item, _) =>
                {
                    try
                    {
                        await InvokeAsync(service, item.Expected, item.Create);
                    }
                    catch (Exception ex)
                    {
                        failures.Enqueue(ex);
                    }
                });

            observationCount += handler.Observations.Count;
            mismatches.AddRange(FindMismatches(handler.Observations));
        }

        int connectionMismatches = mismatches.Count(m => m.StartsWith("[connection]", StringComparison.Ordinal));
        Assert.True(observationCount > 0, "No requests were observed.");
        Assert.True(
            failures.IsEmpty && mismatches.Count == 0,
            $"{failures.Count} calls failed; {mismatches.Count} of {observationCount} requests used unexpected settings " +
            $"({connectionMismatches} endpoint/credential, {mismatches.Count - connectionMismatches} table).{Environment.NewLine}" +
            string.Join(Environment.NewLine, mismatches.Take(5)) +
            Environment.NewLine +
            string.Join(Environment.NewLine, failures.Select(f => f.GetType().Name + ": " + f.Message).Distinct().Take(5)));
    }

    [Fact]
    public async Task SequentialCalls_DifferentConnectionsAndTables_UseMatchingSettings()
    {
        (IConfiguration configuration, List<Expected> connections) = BuildConnections();
        RecordingHandler handler = new();
        DefaultAssistantService service = CreateService(configuration, handler);

        foreach (Expected connection in connections)
        {
            foreach (string tableName in TableNames)
            {
                await InvokeAsync(service, connection with { TableName = tableName }, create: true);
                await InvokeAsync(service, connection with { TableName = tableName }, create: false);
            }
        }

        Assert.NotEmpty(handler.Observations);
        Assert.Empty(FindMismatches(handler.Observations));
    }

    [Fact]
    public async Task CreateAssistant_LogsIdWithoutInstructions_StillStoresInstructions()
    {
        const string chatId = "logging-test";
        const string instructions = "private-instructions-sentinel";
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ChatStorage"] = ConnectionString("accta") })
            .Build();
        Mock<ILogger> logger = new();
        Mock<ILoggerFactory> loggerFactory = new();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);
        RecordingHandler handler = new();
        DefaultAssistantService service = CreateService(configuration, handler, loggerFactory.Object);

        await service.CreateAssistantAsync(
            new AssistantCreateRequest(chatId, instructions)
            {
                ChatStorageConnectionSetting = "ChatStorage",
                CollectionName = "ChatState",
            },
            CancellationToken.None);

        object[] logStates = logger.Invocations.Where(i => i.Method.Name == nameof(ILogger.Log))
            .Select(i => i.Arguments[2]).ToArray();
        Assert.Contains(logStates, state => state.ToString() == $"[{chatId}] Creating new chat session");
        Assert.All(logStates, state =>
        {
            Assert.DoesNotContain(instructions, state.ToString()!);
            IEnumerable<KeyValuePair<string, object?>> properties =
                Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state);
            Assert.All(properties, property => Assert.DoesNotContain(instructions, property.Value?.ToString() ?? string.Empty));
        });
        Assert.Contains(handler.Observations, o => o.Path == "/$batch" && o.Body.Contains(instructions, StringComparison.Ordinal));
    }

    [Fact]
    public async Task PostMessage_LogsMetadata_StillStoresConversation()
    {
        const string chatId = "logging-test";
        const string instructions = "instructions-sentinel";
        const string userMessage = "user-message-sentinel";
        const string reply = "model-reply-sentinel";
        const string arguments = "function-arguments-sentinel";
        const string result = "function-result-sentinel";
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ChatStorage"] = ConnectionString("accta"),
                ["ChatAI:Endpoint"] = "https://chat.example.test/",
                ["ChatAI:Key"] = "test-chat-key",
            })
            .Build();
        Mock<ILogger> logger = new();
        Mock<ILoggerFactory> loggerFactory = new();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);
        Mock<ITriggeredFunctionExecutor> executor = new();
        executor.Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Callback((TriggeredFunctionData data, CancellationToken _) =>
            {
                SkillInvocationContext context = Assert.IsType<SkillInvocationContext>(data.TriggerValue);
                Assert.Contains(arguments, context.Arguments.ToString());
                context.Result = result;
            })
            .ReturnsAsync(new FunctionResult(true));
        AssistantSkillManager skillManager = new(loggerFactory.Object);
        skillManager.RegisterSkill(
            "TestSkill",
            new AssistantSkillTriggerAttribute("Test function"),
            typeof(Action<string>).GetMethod(nameof(Action<string>.Invoke))!.GetParameters()[0],
            executor.Object);
        RecordingHandler handler = new() { StoredEntities = new() };
        handler.ChatResponses.Enqueue(
            $$$"""
            {"id":"test","object":"chat.completion","created":0,"model":"test",
             "choices":[{"index":0,"message":{"role":"assistant","content":"{{{reply}}}",
              "tool_calls":[{"id":"call-1","type":"function","function":{"name":"TestSkill","arguments":"{\"value\":\"{{{arguments}}}\"}"}}]},
              "finish_reason":"tool_calls"}],"usage":{"prompt_tokens":1,"completion_tokens":1,"total_tokens":2}}
            """);
        DefaultAssistantService service = CreateService(configuration, handler, loggerFactory.Object, skillManager);
        AssistantBindingConverter converter = new(service, loggerFactory.Object);

        await service.CreateAssistantAsync(
            new AssistantCreateRequest(chatId, instructions)
            {
                ChatStorageConnectionSetting = "ChatStorage",
                CollectionName = "ChatState",
            },
            CancellationToken.None);
        AssistantState posted = await converter.ConvertAsync(
            new AssistantPostAttribute(chatId, userMessage)
            {
                ChatStorageConnectionSetting = "ChatStorage",
                CollectionName = "ChatState",
                AIConnectionName = "ChatAI",
            },
            CancellationToken.None);
        AssistantState stored = await service.GetStateAsync(
            new AssistantQueryAttribute(chatId)
            {
                TimestampUtc = "2026-01-01T00:00:00Z",
                ChatStorageConnectionSetting = "ChatStorage",
                CollectionName = "ChatState",
            },
            CancellationToken.None);

        Assert.True(posted.Exists);
        Assert.Equal(5, stored.TotalMessages);
        Assert.Contains(stored.RecentMessages, m => m.Content == instructions);
        Assert.Contains(stored.RecentMessages, m => m.Content == userMessage);
        Assert.Contains(stored.RecentMessages, m => m.Content == reply);
        Assert.Contains(stored.RecentMessages, m => m.Content.Contains(result, StringComparison.Ordinal));
        Assert.Contains(stored.RecentMessages, m => m.Content == "reply-for-b");
        Assert.Equal(2, handler.ChatRequests.Count);
        Assert.Contains(result, handler.ChatRequests.Last());
        Assert.Contains(handler.Observations, o => o.Path == "/$batch" && o.Body.Contains(arguments, StringComparison.Ordinal));
        executor.Verify(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()), Times.Once);

        object[] logStates = logger.Invocations
            .Where(i => i.Method.Name == nameof(ILogger.Log) && (LogLevel)i.Arguments[0] == LogLevel.Information)
            .Select(i => i.Arguments[2]).ToArray();
        Assert.NotEmpty(logStates);
        Assert.All(logStates, state =>
        {
            IEnumerable<KeyValuePair<string, object?>> properties =
                Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state);
            foreach (string content in new[] { instructions, userMessage, reply, arguments, result, "reply-for-b" })
            {
                Assert.DoesNotContain(content, state.ToString()!);
                Assert.All(properties, property => Assert.DoesNotContain(content, property.Value?.ToString() ?? string.Empty));
            }
        });
    }

    [Fact]
    public async Task StorageConfigurationUpdated_UsesCurrentClient()
    {
        string first = ConnectionString("accta");
        string second = ConnectionString("acctb");
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ChatStorage"] = first })
            .Build();
        RecordingHandler handler = new();
        DefaultAssistantService service = CreateService(configuration, handler);
        Expected a = new("ChatStorage", "accta.table.example.test", "accta", null, "ChatState");
        Expected b = new("ChatStorage", "acctb.table.example.test", "acctb", null, "ChatState");

        await InvokeAsync(service, a, create: false);
        configuration["ChatStorage"] = second;
        await InvokeAsync(service, b, create: false);
        await InvokeAsync(service, b, create: false);

        Assert.Equal(3, handler.Observations.Count);
        Assert.Empty(FindMismatches(handler.Observations));
        FieldInfo cacheField = Assert.Single(
            typeof(DefaultAssistantService).GetFields(BindingFlags.Instance | BindingFlags.NonPublic),
            field => typeof(IDictionary).IsAssignableFrom(field.FieldType));
        IDictionary cache = Assert.IsAssignableFrom<IDictionary>(cacheField.GetValue(service));
        object[] keys = cache.Keys.Cast<object>().ToArray();
        Assert.Equal(2, keys.Length);
        foreach (object key in keys)
        {
            foreach (FieldInfo field in key.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                string value = field.GetValue(key)?.ToString() ?? string.Empty;
                Assert.DoesNotContain(first, value);
                Assert.DoesNotContain(second, value);
                Assert.DoesNotContain(Convert.ToBase64String(Encoding.UTF8.GetBytes("test-key-accta")), value);
                Assert.DoesNotContain(Convert.ToBase64String(Encoding.UTF8.GetBytes("test-key-acctb")), value);
            }
        }
    }

    [Fact]
    public async Task AssistantOperations_UseConfiguredStorage()
    {
        const string chatId = "shared-chat-id";
        const string hostA = "accta.table.example.test";
        const string hostB = "acctb.table.example.test";
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ChatStorageA"] = ConnectionString("accta"),
                ["ChatStorageB:tableServiceUri"] = $"https://{hostB}/",
                ["ChatAI:Endpoint"] = "https://chat.example.test/",
                ["ChatAI:Key"] = "test-chat-key",
            })
            .Build();
        RecordingHandler handler = new() { StoredEntities = new() };
        DefaultAssistantService service = CreateService(configuration, handler);
        Expected a = new("ChatStorageA", hostA, "accta", null, "ChatStateA");
        Expected b = new("ChatStorageB", hostB, null, "token-ChatStorageB", "ChatStateB");

        RecordingHandler.Current.Value = a;
        await service.CreateAssistantAsync(
            new AssistantCreateRequest(chatId, "instructions-for-a")
            {
                ChatStorageConnectionSetting = a.ConnectionSetting,
                CollectionName = a.TableName,
            },
            CancellationToken.None);

        RecordingHandler.Current.Value = b;
        await service.CreateAssistantAsync(
            new AssistantCreateRequest(chatId, "instructions-for-b")
            {
                ChatStorageConnectionSetting = b.ConnectionSetting,
                CollectionName = b.TableName,
            },
            CancellationToken.None);
        AssistantState queried = await service.GetStateAsync(
            new AssistantQueryAttribute(chatId)
            {
                TimestampUtc = "2026-01-01T00:00:00Z",
                ChatStorageConnectionSetting = b.ConnectionSetting,
                CollectionName = b.TableName,
            },
            CancellationToken.None);
        AssistantState posted = await service.PostMessageAsync(
            new AssistantPostAttribute(chatId, "hello")
            {
                ChatStorageConnectionSetting = b.ConnectionSetting,
                CollectionName = b.TableName,
                AIConnectionName = "ChatAI",
            },
            CancellationToken.None);

        Assert.True(queried.Exists);
        Assert.Equal("instructions-for-b", Assert.Single(queried.RecentMessages).Content);
        Assert.True(posted.Exists);
        Assert.Equal(3, posted.TotalMessages);
        string chatRequest = Assert.Single(handler.ChatRequests);
        Assert.Contains("instructions-for-b", chatRequest);
        Assert.DoesNotContain("instructions-for-a", chatRequest);

        AssistantState updatedB = await service.GetStateAsync(
            new AssistantQueryAttribute(chatId)
            {
                TimestampUtc = "2026-01-01T00:00:00Z",
                ChatStorageConnectionSetting = b.ConnectionSetting,
                CollectionName = b.TableName,
            },
            CancellationToken.None);
        Assert.Equal(new[] { "instructions-for-b", "hello", "reply-for-b" }, updatedB.RecentMessages.Select(m => m.Content));

        RecordingHandler.Current.Value = a;
        AssistantState unchangedA = await service.GetStateAsync(
            new AssistantQueryAttribute(chatId)
            {
                TimestampUtc = "2026-01-01T00:00:00Z",
                ChatStorageConnectionSetting = a.ConnectionSetting,
                CollectionName = a.TableName,
            },
            CancellationToken.None);
        Assert.Equal(1, unchangedA.TotalMessages);
        Assert.Equal("instructions-for-a", Assert.Single(unchangedA.RecentMessages).Content);
        Assert.Empty(FindMismatches(handler.Observations));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AzureWebJobsStorage")]
    public async Task DefaultConnection_IdentityBasedStorage_UsesServiceUriAndTokenCredential(string? connectionSetting)
    {
        const string host = "acctidentity.table.example.test";
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureWebJobsStorage:tableServiceUri"] = $"https://{host}/",
            })
            .Build();
        RecordingHandler handler = new();
        DefaultAssistantService service = CreateService(configuration, handler);
        Expected expected = new(connectionSetting, host, null, "token-AzureWebJobsStorage", "ChatState");

        // Create with an explicit setting, then query with the requested (possibly omitted) setting.
        await InvokeAsync(service, expected with { ConnectionSetting = "AzureWebJobsStorage" }, create: true);
        await InvokeAsync(service, expected, create: false);
        await InvokeAsync(service, expected, create: true);

        Assert.NotEmpty(handler.Observations);
        Assert.Empty(FindMismatches(handler.Observations));
    }
}
