// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.WebJobs.Extensions.OpenAI;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Xunit;

namespace WebJobsOpenAIUnitTests;

public class OpenAIClientFactoryTests
{
    readonly Mock<IConfiguration> mockConfiguration;
    readonly Mock<AzureComponentFactory> mockAzureComponentFactory;
    readonly Mock<ILoggerFactory> mockLoggerFactory;
    readonly Mock<ILogger<OpenAIClientFactory>> mockLogger;

    public OpenAIClientFactoryTests()
    {
        this.mockConfiguration = new Mock<IConfiguration>();
        this.mockAzureComponentFactory = new Mock<AzureComponentFactory>();
        this.mockLoggerFactory = new Mock<ILoggerFactory>();
        this.mockLogger = new Mock<ILogger<OpenAIClientFactory>>();
        this.mockLoggerFactory
        .Setup(f => f.CreateLogger(It.Is<string>(s => s == typeof(OpenAIClientFactory).FullName)))
        .Returns(this.mockLogger.Object);
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        IConfiguration? configuration = null;

#nullable disable
        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new OpenAIClientFactory(configuration, this.mockAzureComponentFactory.Object, this.mockLoggerFactory.Object));
#nullable restore
        Assert.Equal("configuration", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullAzureComponentFactory_ThrowsArgumentNullException()
    {
        // Arrange
        AzureComponentFactory? azureComponentFactory = null;

#nullable disable
        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new OpenAIClientFactory(this.mockConfiguration.Object, azureComponentFactory, this.mockLoggerFactory.Object));
#nullable restore
        Assert.Equal("azureComponentFactory", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange
        ILoggerFactory? loggerFactory = null;

        // Act & Assert
#nullable disable
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new OpenAIClientFactory(this.mockConfiguration.Object, this.mockAzureComponentFactory.Object, loggerFactory));
#nullable restore
        Assert.Equal("loggerFactory", exception.ParamName);
    }

    [Fact]
    public void GetChatClient_WithAzureOpenAI_ReturnsClient()
    {
        // Arrange
        Mock<IConfigurationSection> mockSection = CreateMockSection(
            exists: true,
            endpoint: "https://test-endpoint.openai.azure.com/",
            key: "test-key");

        this.mockConfiguration.Setup(c => c.GetSection("TestConnection")).Returns(mockSection.Object);

        // Clear environment variable if set
        string? originalValue = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

            OpenAIClientFactory factory = new(
                this.mockConfiguration.Object,
                this.mockAzureComponentFactory.Object,
                this.mockLoggerFactory.Object);

            // Act
            ChatClient chatClient = factory.GetChatClient("TestConnection", "gpt-35-turbo");

            // Assert
            Assert.NotNull(chatClient);
        }
        finally
        {
            // Restore original environment value
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalValue);
        }
    }

    [Fact]
    public void GetChatClient_WithOpenAIKey_ReturnsClient()
    {
        // Arrange
        string? originalValue = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-openai-key");

            OpenAIClientFactory factory = new(
                this.mockConfiguration.Object,
                this.mockAzureComponentFactory.Object,
                this.mockLoggerFactory.Object);

            // Act
            ChatClient chatClient = factory.GetChatClient("TestConnection", "gpt-4");

            // Assert
            Assert.NotNull(chatClient);
        }
        finally
        {
            // Restore original environment value
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalValue);
        }
    }

    [Fact]
    public void GetEmbeddingClient_WithAzureOpenAI_ReturnsClient()
    {
        // Arrange
        Mock<IConfigurationSection> mockSection = CreateMockSection(
            exists: true,
            endpoint: "https://test-endpoint.openai.azure.com/",
            key: "test-key");

        this.mockConfiguration.Setup(c => c.GetSection("TestConnection")).Returns(mockSection.Object);

        // Clear environment variable if set
        string? originalValue = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

            OpenAIClientFactory factory = new(
                this.mockConfiguration.Object,
                this.mockAzureComponentFactory.Object,
                this.mockLoggerFactory.Object);

            // Act
            EmbeddingClient embeddingClient = factory.GetEmbeddingClient("TestConnection", "text-embedding-ada-002");

            // Assert
            Assert.NotNull(embeddingClient);
        }
        finally
        {
            // Restore original environment value
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalValue);
        }
    }

    [Fact]
    public void GetEmbeddingClient_WithOpenAIKey_ReturnsClient()
    {
        // Arrange
        string? originalValue = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-openai-key");

            OpenAIClientFactory factory = new(
                this.mockConfiguration.Object,
                this.mockAzureComponentFactory.Object,
                this.mockLoggerFactory.Object);

            // Act
            EmbeddingClient embeddingClient = factory.GetEmbeddingClient("TestConnection", "text-embedding-ada-002");

            // Assert
            Assert.NotNull(embeddingClient);
        }
        finally
        {
            // Restore original environment value
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalValue);
        }
    }

    [Fact]
    public void HasOpenAIKey_KeyExists_SetsHasKeyToTrue()
    {
        // Arrange
        string? originalValue = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");

            // Use reflection to access private static method
            MethodInfo? methodInfo = typeof(OpenAIClientFactory).GetMethod("HasOpenAIKey",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act
#nullable disable
            object[] parameters = [false, null];
#nullable restore
            methodInfo?.Invoke(null, parameters);

            // Assert
            Assert.True((bool)parameters[0]);
            Assert.Equal("test-key", parameters[1]);
        }
        finally
        {
            // Restore original environment value
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalValue);
        }
    }

    [Fact]
    public void HasOpenAIKey_NoKeyExists_SetsHasKeyToFalse()
    {
        // Arrange
        string? originalValue = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

            // Use reflection to access private static method
            MethodInfo? methodInfo = typeof(OpenAIClientFactory).GetMethod("HasOpenAIKey",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            object[] parameters = [true, "some-value"];
            methodInfo?.Invoke(null, parameters);

            // Assert
            Assert.False((bool)parameters[0]);
            Assert.Null((string)parameters[1]);
        }
        finally
        {
            // Restore original environment value
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalValue);
        }
    }

    [Fact]
    public void CreateClientFromConfigSection_MissingEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange
        Mock<IConfigurationSection> mockSection = CreateMockSection(
            exists: true,
            endpoint: null,
            key: "test-key");

        this.mockConfiguration.Setup(c => c.GetSection("TestConnection")).Returns(mockSection.Object);

        // Clear environment variable if set
        string? originalEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        try
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", null);

            OpenAIClientFactory factory = new(
                this.mockConfiguration.Object,
                this.mockAzureComponentFactory.Object,
                this.mockLoggerFactory.Object);

            // Act & Assert
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                factory.GetChatClient("TestConnection", "gpt-35-turbo"));
            Assert.Contains("missing required 'Endpoint'", exception.Message);
        }
        finally
        {
            // Restore original environment value
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", originalEndpoint);
        }
    }

    [Fact]
    public void CreateClientFromConfigSection_WithTokenCredential_ReturnsClient()
    {
        // Arrange
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        Mock<IConfigurationSection> mockSection = CreateMockSection(
            exists: true,
            endpoint: "https://test-endpoint.openai.azure.com/",
            key: null);

        this.mockConfiguration.Setup(c => c.GetSection("TestConnection")).Returns(mockSection.Object);

        TokenCredential tokenCredential = new Mock<TokenCredential>().Object;
        this.mockAzureComponentFactory.Setup(f => f.CreateTokenCredential(It.IsAny<IConfigurationSection>()))
            .Returns(tokenCredential);

        TokenCredential? selectedCredential = null;
        OpenAIClientFactory factory = this.CreateCredentialInspectingFactory(
            this.mockConfiguration.Object,
            this.mockAzureComponentFactory.Object,
            (_, credential) => selectedCredential = credential);

        // Act
        ChatClient chatClient = factory.GetChatClient("TestConnection", "gpt-35-turbo");

        // Assert
        Assert.NotNull(chatClient);
        Assert.Same(tokenCredential, selectedCredential);
        this.mockAzureComponentFactory.Verify(f => f.CreateTokenCredential(It.IsAny<IConfigurationSection>()), Times.Once);
    }

    [Fact]
    public void CreateClientFromConfigSection_WithEnvironmentVariables_UsesEnvironmentValues()
    {
        // Arrange
        Mock<IConfigurationSection> mockSection = CreateMockSection(
            exists: false,
            endpoint: null,
            key: null);
        this.mockConfiguration.Setup(c => c.GetSection("TestConnection")).Returns(mockSection.Object);

        string? originalEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? originalKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        try
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", "https://env-endpoint.openai.azure.com/");
            Environment.SetEnvironmentVariable("AZURE_OPENAI_KEY", "env-key");

            OpenAIClientFactory factory = new(
                this.mockConfiguration.Object,
                this.mockAzureComponentFactory.Object,
                this.mockLoggerFactory.Object);

            // Act
            ChatClient chatClient = factory.GetChatClient("TestConnection", "gpt-35-turbo");

            // Assert
            Assert.NotNull(chatClient);
        }
        finally
        {
            // Restore original environment values
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", originalEndpoint);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_KEY", originalKey);
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void GetChatClient_WithoutExplicitCredential_UsesExistingFallback(bool useConfigurationSection, bool hasClientId)
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        Dictionary<string, string?> settings = new();
        if (useConfigurationSection)
        {
            settings["Connection:Endpoint"] = "https://example.openai.azure.com/";
            if (hasClientId)
            {
                settings["Connection:clientId"] = "00000000-0000-0000-0000-000000000001";
            }
        }
        else
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", "https://example.openai.azure.com/");
        }

        ServiceCollection services = new();
        services.AddAzureClientsCore();
        using ServiceProvider provider = services.BuildServiceProvider();
        TokenCredential? selectedCredential = null;
        OpenAIClientFactory factory = this.CreateCredentialInspectingFactory(
            BuildConfiguration(settings),
            provider.GetRequiredService<AzureComponentFactory>(),
            (_, credential) => selectedCredential = credential);

        factory.GetChatClient("Connection", "model");

        Assert.IsType<DefaultAzureCredential>(selectedCredential);
    }

    [Fact]
    public void GetChatClient_WithConfiguredManagedIdentity_UsesManagedIdentityCredential()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        Dictionary<string, string?> settings = new()
        {
            ["Connection:Endpoint"] = "https://example.openai.azure.com/",
            ["Connection:credential"] = "managedidentity",
            ["Connection:clientId"] = "00000000-0000-0000-0000-000000000001",
        };

        ServiceCollection services = new();
        services.AddAzureClientsCore();
        using ServiceProvider provider = services.BuildServiceProvider();
        TokenCredential? selectedCredential = null;
        OpenAIClientFactory factory = this.CreateCredentialInspectingFactory(
            BuildConfiguration(settings),
            provider.GetRequiredService<AzureComponentFactory>(),
            (_, credential) => selectedCredential = credential);

        factory.GetChatClient("Connection", "model");

        Assert.IsType<ManagedIdentityCredential>(selectedCredential);
    }

    [Fact]
    public void GetChatClient_AfterCredentialCreationFailure_RetriesOnNextCall()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        IConfiguration configuration = BuildConfiguration(new()
        {
            ["Connection:Endpoint"] = "https://example.openai.azure.com/",
            ["Connection:credential"] = "managedidentity",
        });
        int attempts = 0;
        this.mockAzureComponentFactory
            .Setup(f => f.CreateTokenCredential(It.IsAny<IConfiguration>()))
            .Returns(() =>
            {
                if (++attempts == 1)
                {
                    throw new InvalidOperationException("Credential initialization failed");
                }

                return CreateTokenCredential("token");
            });
        OpenAIClientFactory factory = this.CreateCredentialInspectingFactory(
            configuration,
            this.mockAzureComponentFactory.Object,
            (_, _) => { });

        Assert.Throws<InvalidOperationException>(() => factory.GetChatClient("Connection", "model"));
        Assert.NotNull(factory.GetChatClient("Connection", "model"));
        Assert.Equal(2, attempts);
    }

    [Fact]
    public void GetChatClient_ConnectionNamedOpenAI_DoesNotShareClientWithOpenAIProvider()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        IConfiguration configuration = BuildConfiguration(new()
        {
            ["OpenAI:Endpoint"] = "https://example.openai.azure.com/",
            ["OpenAI:Key"] = "azure-key",
        });
        OpenAIClientFactory factory = this.CreateRecordingFactory(configuration, new RecordingHandler());

        ChatClient azureClient = factory.GetChatClient("OpenAI", "model");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "openai-key");
        ChatClient openAIClient = factory.GetChatClient("OpenAI", "model");

        Assert.NotSame(azureClient.Pipeline, openAIClient.Pipeline);
    }

    [Fact]
    public void GetChatClient_OpenAIKeyChanges_DoesNotReusePreviousClient()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        OpenAIClientFactory factory = this.CreateRecordingFactory(BuildConfiguration(new()), new RecordingHandler());

        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "first-openai-key");
        ChatClient firstClient = factory.GetChatClient("Connection", "model");
        Assert.Same(firstClient.Pipeline, factory.GetChatClient("Connection", "model").Pipeline);
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "second-openai-key");
        ChatClient secondClient = factory.GetChatClient("Connection", "model");

        Assert.NotSame(firstClient.Pipeline, secondClient.Pipeline);
    }

    [Fact]
    public void GetClients_ApiKeyCaches_DoNotContainRawSecrets()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        const string azureKey = "fake-azure-secret-key";
        const string openAIKey = "fake-openai-secret-key";
        IConfiguration configuration = BuildConfiguration(new()
        {
            ["Connection:Endpoint"] = "https://example.openai.azure.com/",
            ["Connection:Key"] = azureKey,
        });
        OpenAIClientFactory factory = this.CreateRecordingFactory(configuration, new RecordingHandler());

        factory.GetChatClient("Connection", "model");
        factory.GetEmbeddingClient("Connection", "model");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", openAIKey);
        factory.GetChatClient("Connection", "model");
        factory.GetEmbeddingClient("Connection", "model");

        foreach (string fieldName in new[] { "connectionClients", "openAIClients" })
        {
            object cache = typeof(OpenAIClientFactory)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(factory)!;
            var keys = (System.Collections.IEnumerable)cache.GetType().GetProperty("Keys")!.GetValue(cache)!;
            foreach (object key in keys)
            {
                Assert.DoesNotContain(azureKey, key.ToString()!);
                Assert.DoesNotContain(openAIKey, key.ToString()!);
            }
        }
    }

    [Fact]
    public void GetChatClient_AnotherConnectionInitializedMidway_UsesOwnEndpointAndKey()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        OpenAIClientFactory? factory = null;
        bool interleaved = false;

        Mock<IConfigurationSection> sectionA = CreateMockSection(true, "https://connection-a.openai.azure.com/", "key-a");
        Mock<IConfigurationSection> sectionB = CreateMockSection(true, "https://connection-b.openai.azure.com/", "key-b");

        Mock<IConfigurationSection> keySectionB = new();
        keySectionB.Setup(s => s.Value).Returns(() =>
        {
            if (!interleaved)
            {
                interleaved = true;
                factory!.GetChatClient("A", "gpt-4o");
            }

            return "key-b";
        });
        sectionB.Setup(s => s.GetSection("Key")).Returns(keySectionB.Object);

        this.mockConfiguration.Setup(c => c.GetSection("A")).Returns(sectionA.Object);
        this.mockConfiguration.Setup(c => c.GetSection("B")).Returns(sectionB.Object);

        RecordingHandler handler = new();
        factory = this.CreateRecordingFactory(this.mockConfiguration.Object, handler);

        // Act
        ChatClient clientB = factory.GetChatClient("B", "gpt-4o");
        ChatClient clientA = factory.GetChatClient("A", "gpt-4o");
        SendChat(clientB);
        SendChat(clientA);

        // Assert
        Assert.True(interleaved);
        RecordedRequest[] requests = handler.Requests.ToArray();
        Assert.Equal(2, requests.Length);
        Assert.Contains(requests, r => r.Host == "connection-b.openai.azure.com" && r.ApiKey == "key-b");
        Assert.Contains(requests, r => r.Host == "connection-a.openai.azure.com" && r.ApiKey == "key-a");
        Assert.DoesNotContain(requests, r => r.Host == "connection-a.openai.azure.com" && r.ApiKey == "key-b");
    }

    [Fact]
    public void GetClients_ConcurrentKeyConnections_EachUsesOwnEndpointAndKey()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        const int connectionCount = 64;
        Dictionary<string, string?> settings = new();
        for (int i = 0; i < connectionCount; i++)
        {
            settings[$"Conn{i}:Endpoint"] = $"https://conn{i}.openai.azure.com/";
            settings[$"Conn{i}:Key"] = $"key-{i}";
        }

        RecordingHandler handler = new();
        OpenAIClientFactory factory = this.CreateRecordingFactory(BuildConfiguration(settings), handler);

        // Act
        Parallel.For(0, connectionCount * 4, new ParallelOptions { MaxDegreeOfParallelism = 32 }, i =>
        {
            int connectionIndex = i % connectionCount;
            string connection = $"Conn{connectionIndex}";
            SendChat(factory.GetChatClient(connection, $"chat-model-{connectionIndex}"));
            SendEmbedding(factory.GetEmbeddingClient(connection, $"embedding-model-{connectionIndex}"));
        });

        // Assert
        RecordedRequest[] requests = handler.Requests.ToArray();
        Assert.Equal(connectionCount * 8, requests.Length);
        foreach (RecordedRequest request in requests)
        {
            string connectionIndex = request.Host.Split('.')[0].Substring("conn".Length);
            Assert.Equal($"key-{connectionIndex}", request.ApiKey);
            Assert.Null(request.Authorization);
            Assert.True(
                request.Path.Contains($"/deployments/chat-model-{connectionIndex}/") ||
                request.Path.Contains($"/deployments/embedding-model-{connectionIndex}/"));
        }

        Assert.Equal(connectionCount, requests.Select(r => r.Host).Distinct().Count());
    }

    [Fact]
    public void GetClients_ConcurrentTokenCredentialConnections_EachUsesOwnEndpointAndCredential()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        const int connectionCount = 64;
        Dictionary<string, string?> settings = new();
        Dictionary<string, TokenCredential> credentials = new();
        for (int i = 0; i < connectionCount; i++)
        {
            settings[$"Conn{i}:Endpoint"] = $"https://conn{i}.openai.azure.com/";
            settings[$"Conn{i}:credential"] = "managedidentity";
            credentials[$"Conn{i}"] = CreateTokenCredential($"token-{i}");
        }

        this.mockAzureComponentFactory
            .Setup(f => f.CreateTokenCredential(It.IsAny<IConfiguration>()))
            .Returns((IConfiguration section) => credentials[((IConfigurationSection)section).Path]);

        RecordingHandler handler = new();
        OpenAIClientFactory factory = this.CreateRecordingFactory(BuildConfiguration(settings), handler);

        // Act
        Parallel.For(0, connectionCount * 4, new ParallelOptions { MaxDegreeOfParallelism = 32 }, i =>
        {
            int connectionIndex = i % connectionCount;
            string connection = $"Conn{connectionIndex}";
            SendChat(factory.GetChatClient(connection, $"chat-model-{connectionIndex}"));
            SendEmbedding(factory.GetEmbeddingClient(connection, $"embedding-model-{connectionIndex}"));
        });

        // Assert
        RecordedRequest[] requests = handler.Requests.ToArray();
        Assert.Equal(connectionCount * 8, requests.Length);
        foreach (RecordedRequest request in requests)
        {
            string connectionIndex = request.Host.Split('.')[0].Substring("conn".Length);
            Assert.Equal($"Bearer token-{connectionIndex}", request.Authorization);
            Assert.Null(request.ApiKey);
            Assert.True(
                request.Path.Contains($"/deployments/chat-model-{connectionIndex}/") ||
                request.Path.Contains($"/deployments/embedding-model-{connectionIndex}/"));
        }

        Assert.Equal(connectionCount, requests.Select(r => r.Host).Distinct().Count());
    }

    [Fact]
    public void GetChatClient_TokenConnectionsSharingEndpoint_EachUsesOwnCredential()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        Dictionary<string, string?> settings = new()
        {
            ["IdentityA:Endpoint"] = "https://shared.openai.azure.com/",
            ["IdentityA:credential"] = "managedidentity",
            ["IdentityB:Endpoint"] = "https://shared.openai.azure.com/",
            ["IdentityB:credential"] = "managedidentity",
        };
        Dictionary<string, TokenCredential> credentials = new()
        {
            ["IdentityA"] = CreateTokenCredential("token-a"),
            ["IdentityB"] = CreateTokenCredential("token-b"),
        };
        this.mockAzureComponentFactory
            .Setup(f => f.CreateTokenCredential(It.IsAny<IConfiguration>()))
            .Returns((IConfiguration section) => credentials[((IConfigurationSection)section).Path]);

        RecordingHandler handler = new();
        OpenAIClientFactory factory = this.CreateRecordingFactory(BuildConfiguration(settings), handler);

        // Act
        SendChat(factory.GetChatClient("IdentityA", "gpt-4o"));
        SendChat(factory.GetChatClient("IdentityB", "gpt-4o"));

        // Assert
        RecordedRequest[] requests = handler.Requests.ToArray();
        Assert.Equal(2, requests.Length);
        Assert.Equal("Bearer token-a", requests[0].Authorization);
        Assert.Equal("Bearer token-b", requests[1].Authorization);
        this.mockAzureComponentFactory.Verify(
            f => f.CreateTokenCredential(It.Is<IConfiguration>(s => ((IConfigurationSection)s).Path == "IdentityA")),
            Times.Once);
        this.mockAzureComponentFactory.Verify(
            f => f.CreateTokenCredential(It.Is<IConfiguration>(s => ((IConfigurationSection)s).Path == "IdentityB")),
            Times.Once);
    }

    [Fact]
    public void GetChatClient_KeyConnectionsSharingEndpoint_EachUsesOwnKey()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        IConfiguration configuration = BuildConfiguration(new()
        {
            ["ConnectionA:Endpoint"] = "https://shared.openai.azure.com/",
            ["ConnectionA:Key"] = "key-a",
            ["ConnectionB:Endpoint"] = "https://shared.openai.azure.com/",
            ["ConnectionB:Key"] = "key-b",
        });
        RecordingHandler handler = new();
        OpenAIClientFactory factory = this.CreateRecordingFactory(configuration, handler);

        SendChat(factory.GetChatClient("ConnectionA", "model"));
        SendChat(factory.GetChatClient("ConnectionB", "model"));

        RecordedRequest[] requests = handler.Requests.ToArray();
        Assert.Equal(["key-a", "key-b"], requests.Select(r => r.ApiKey).OrderBy(key => key));
        Assert.All(requests, request => Assert.Equal("shared.openai.azure.com", request.Host));
    }

    [Fact]
    public void GetClients_SameConnectionDifferentModels_UsesRequestedModels()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        Dictionary<string, string?> settings = new()
        {
            ["Conn:Endpoint"] = "https://conn.openai.azure.com/",
            ["Conn:Key"] = "key",
        };

        RecordingHandler handler = new();
        OpenAIClientFactory factory = this.CreateRecordingFactory(BuildConfiguration(settings), handler);

        // Act
        SendChat(factory.GetChatClient("Conn", "model-one"));
        SendChat(factory.GetChatClient("Conn", "model-two"));
        SendEmbedding(factory.GetEmbeddingClient("Conn", "embed-one"));
        SendEmbedding(factory.GetEmbeddingClient("Conn", "embed-two"));

        // Assert
        Assert.Collection(
            handler.Requests,
            request => Assert.Contains("/deployments/model-one/", request.Path),
            request => Assert.Contains("/deployments/model-two/", request.Path),
            request => Assert.Contains("/deployments/embed-one/", request.Path),
            request => Assert.Contains("/deployments/embed-two/", request.Path));
    }

    [Fact]
    public void GetClients_OpenAIKeyDifferentModels_UsesRequestedModels()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "openai-key");
        RecordingHandler handler = new();
        OpenAIClientFactory factory = this.CreateRecordingFactory(
            BuildConfiguration(new()),
            handler,
            key => new OpenAIClient(
                new ApiKeyCredential(key),
                new OpenAIClientOptions
                {
                    Transport = new HttpClientPipelineTransport(new HttpClient(handler, disposeHandler: false)),
                }));

        SendChat(factory.GetChatClient("Connection", "chat-one"));
        SendChat(factory.GetChatClient("Connection", "chat-two"));
        SendEmbedding(factory.GetEmbeddingClient("Connection", "embed-one"));
        SendEmbedding(factory.GetEmbeddingClient("Connection", "embed-two"));

        Assert.Collection(
            handler.Requests,
            request => Assert.Equal("chat-one", GetRequestedModel(request.Body)),
            request => Assert.Equal("chat-two", GetRequestedModel(request.Body)),
            request => Assert.Equal("embed-one", GetRequestedModel(request.Body)),
            request => Assert.Equal("embed-two", GetRequestedModel(request.Body)));

        static string? GetRequestedModel(string? body) =>
            System.Text.Json.JsonDocument.Parse(body!).RootElement.GetProperty("model").GetString();
    }

    [Fact]
    public void GetChatClient_AfterInitializationFailure_RetriesOnNextCall()
    {
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        string? endpoint = null;
        Mock<IConfigurationSection> section = CreateMockSection(true, endpoint: null, key: "test-key");
        Mock<IConfigurationSection> endpointSection = new();
        endpointSection.Setup(s => s.Value).Returns(() => endpoint!);
        section.Setup(s => s.GetSection("Endpoint")).Returns(endpointSection.Object);
        this.mockConfiguration.Setup(c => c.GetSection("TestConnection")).Returns(section.Object);

        OpenAIClientFactory factory = new(
            this.mockConfiguration.Object,
            this.mockAzureComponentFactory.Object,
            this.mockLoggerFactory.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.GetChatClient("TestConnection", "gpt-4o"));

        endpoint = "https://test-endpoint.openai.azure.com/";
        Assert.NotNull(factory.GetChatClient("TestConnection", "gpt-4o"));
    }

    OpenAIClientFactory CreateRecordingFactory(
        IConfiguration configuration,
        RecordingHandler handler,
        Func<string, OpenAIClient>? openAIClientFactory = null)
    {
        AzureOpenAIClientOptions CreateOptions() => new()
        {
            Transport = new HttpClientPipelineTransport(new HttpClient(handler, disposeHandler: false)),
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
        };

        return new OpenAIClientFactory(
            configuration,
            this.mockAzureComponentFactory.Object,
            this.mockLoggerFactory.Object,
            (endpoint, credential) => new AzureOpenAIClient(endpoint, credential, CreateOptions()),
            (endpoint, credential) => new AzureOpenAIClient(endpoint, credential, CreateOptions()),
            openAIClientFactory);
    }

    OpenAIClientFactory CreateCredentialInspectingFactory(
        IConfiguration configuration,
        AzureComponentFactory componentFactory,
        Action<Uri, TokenCredential> inspectCredential) =>
        new(
            configuration,
            componentFactory,
            this.mockLoggerFactory.Object,
            (endpoint, credential) => new AzureOpenAIClient(endpoint, credential),
            (endpoint, credential) =>
            {
                inspectCredential(endpoint, credential);
                return new AzureOpenAIClient(endpoint, credential);
            });

    static IConfiguration BuildConfiguration(Dictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

    static TokenCredential CreateTokenCredential(string token)
    {
        AccessToken accessToken = new(token, DateTimeOffset.UtcNow.AddHours(1));
        Mock<TokenCredential> credential = new();
        credential
            .Setup(c => c.GetToken(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
            .Returns(accessToken);
        credential
            .Setup(c => c.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);
        return credential.Object;
    }

    static void SendChat(ChatClient client)
    {
        Assert.Throws<ClientResultException>(() => client.CompleteChat([new UserChatMessage("hello")]));
    }

    static void SendEmbedding(EmbeddingClient client)
    {
        Assert.Throws<ClientResultException>(() => client.GenerateEmbedding("hello"));
    }

    sealed record RecordedRequest(string Host, string Path, string? ApiKey, string? Authorization, string? Body);

    sealed class RecordingHandler : HttpMessageHandler
    {
        public ConcurrentQueue<RecordedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            this.Requests.Enqueue(new RecordedRequest(
                request.RequestUri!.Host,
                request.RequestUri.AbsolutePath,
                request.Headers.TryGetValues("api-key", out IEnumerable<string>? apiKeys) ? apiKeys.Single() : null,
                request.Headers.Authorization?.ToString(),
                body));

            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":{\"message\":\"test\"}}"),
            };
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
            this.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    sealed class EnvironmentVariableScope : IDisposable
    {
        readonly Dictionary<string, string?> originalValues = new();

        EnvironmentVariableScope(params string[] names)
        {
            foreach (string name in names)
            {
                this.originalValues[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, null);
            }
        }

        public static EnvironmentVariableScope ClearOpenAIVariables() =>
            new("OPENAI_API_KEY", "AZURE_OPENAI_ENDPOINT", "AZURE_OPENAI_KEY");

        public void Dispose()
        {
            foreach (KeyValuePair<string, string?> entry in this.originalValues)
            {
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            }
        }
    }

    static Mock<IConfigurationSection> CreateMockSection(bool exists, string? endpoint = null, string? key = null)
    {
        var mockSection = new Mock<IConfigurationSection>();

        if (!exists)
        {
            mockSection.Setup(s => s.Value).Returns("");
            mockSection.Setup(s => s.GetChildren()).Returns([]);
        }
        else
        {
            mockSection.Setup(s => s.Value).Returns("some-value");
        }

#nullable disable
        var endpointSection = new Mock<IConfigurationSection>();
        endpointSection.Setup(s => s.Value).Returns(endpoint);
        mockSection.Setup(s => s.GetSection("Endpoint")).Returns(endpointSection.Object);
        var keySection = new Mock<IConfigurationSection>();
        keySection.Setup(s => s.Value).Returns(key);
#nullable restore
        mockSection.Setup(s => s.GetSection("Key")).Returns(keySection.Object);

        return mockSection;
    }

    [Fact]
    public void GetChatClient_TokenCredentialConnectionsWithSameEndpoint_CreateSeparateClients()
    {
        // Arrange
        using EnvironmentVariableScope env = EnvironmentVariableScope.ClearOpenAIVariables();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IdentityA:Endpoint"] = "https://shared.openai.azure.com/",
                ["IdentityA:credential"] = "managedidentity",
                ["IdentityA:clientId"] = "client-a",
                ["IdentityB:Endpoint"] = "https://shared.openai.azure.com/",
                ["IdentityB:credential"] = "managedidentity",
                ["IdentityB:clientId"] = "client-b",
            })
            .Build();
        this.mockAzureComponentFactory
            .Setup(f => f.CreateTokenCredential(It.IsAny<IConfigurationSection>()))
            .Returns(() => new Mock<TokenCredential>().Object);

        OpenAIClientFactory factory = new(configuration, this.mockAzureComponentFactory.Object, this.mockLoggerFactory.Object);

        // Act
        ChatClient first = factory.GetChatClient("IdentityA", "model-1");
        ChatClient second = factory.GetChatClient("IdentityB", "model-1");

        // Assert
        Assert.NotSame(first.Pipeline, second.Pipeline);
        Assert.Same(first.Pipeline, factory.GetEmbeddingClient("IdentityA", "model-2").Pipeline);
        Assert.Same(second.Pipeline, factory.GetEmbeddingClient("IdentityB", "model-2").Pipeline);
        this.mockAzureComponentFactory.Verify(f => f.CreateTokenCredential(It.Is<IConfigurationSection>(s => s.Key == "IdentityA")), Times.Once);
        this.mockAzureComponentFactory.Verify(f => f.CreateTokenCredential(It.Is<IConfigurationSection>(s => s.Key == "IdentityB")), Times.Once);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task GetChatAndEmbeddingClients_ConcurrentRequests_UseConfiguredAuthentication(bool firstUsesToken, bool secondUsesToken)
    {
        var connections = new Dictionary<string, bool>
        {
            ["first"] = firstUsesToken,
            ["second"] = secondUsesToken,
        };
        var settings = new Dictionary<string, string?>();
        foreach (var (name, usesToken) in connections)
        {
            settings[$"{name}:Endpoint"] = $"https://{name}.example.invalid/";
            settings[$"{name}:{(usesToken ? "clientId" : "Key")}"] = usesToken ? name : $"test-key-{name}";
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        this.mockAzureComponentFactory
            .Setup(f => f.CreateTokenCredential(It.IsAny<IConfigurationSection>()))
            .Returns((IConfigurationSection section) => new TestTokenCredential($"test-token-{section["clientId"]}"));

        using var handler = new RecordingOpenAIHandler();
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        var options = new AzureOpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) };
        var factory = new OpenAIClientFactory(
            configuration,
            this.mockAzureComponentFactory.Object,
            this.mockLoggerFactory.Object,
            (endpoint, credential) => new AzureOpenAIClient(endpoint, credential, options),
            (endpoint, credential) => new AzureOpenAIClient(endpoint, credential, options));
        string? originalOpenAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        string? originalAzureKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_KEY", null);

            var calls = Enumerable.Range(0, 64).SelectMany(i => connections.Keys.Select(name => (Name: name, Model: $"{name}-{i}")));
            await Parallel.ForEachAsync(calls, new ParallelOptions { MaxDegreeOfParallelism = 32 }, async (call, cancellationToken) =>
            {
                await Task.WhenAll(
                    factory.GetChatClient(call.Name, call.Model).CompleteChatAsync([new UserChatMessage("test")], cancellationToken: cancellationToken),
                    factory.GetEmbeddingClient(call.Name, call.Model).GenerateEmbeddingAsync("test", cancellationToken: cancellationToken));
            });
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalOpenAIKey);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_KEY", originalAzureKey);
        }

        var requestsByPath = handler.Requests.ToLookup(request => request.Uri.AbsolutePath, StringComparer.Ordinal);
        Assert.Equal(256, handler.Requests.Count);
        foreach (var (name, usesToken) in connections)
        {
            for (int i = 0; i < 64; i++)
            {
                foreach (string operation in new[] { "chat/completions", "embeddings" })
                {
                    var request = Assert.Single(requestsByPath[$"/openai/deployments/{name}-{i}/{operation}"]);
                    Assert.Equal($"{name}.example.invalid", request.Uri.Host);
                    Assert.Equal(usesToken ? null : $"test-key-{name}", request.ApiKey);
                    Assert.Equal(usesToken ? $"Bearer test-token-{name}" : null, request.Authorization);
                }
            }
        }
    }

    sealed class TestTokenCredential(string token) : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new(token, DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }

    sealed class RecordingOpenAIHandler : HttpMessageHandler
    {
        public ConcurrentQueue<(Uri Uri, string? ApiKey, string? Authorization)> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri uri = request.RequestUri!;
            Requests.Enqueue((uri, request.Headers.TryGetValues("api-key", out var keys) ? keys.Single() : null, request.Headers.Authorization?.ToString()));
            string json = uri.AbsolutePath.EndsWith("/embeddings", StringComparison.Ordinal)
                ? """{"object":"list","data":[{"object":"embedding","index":0,"embedding":"AAAAPw=="}],"model":"test","usage":{"prompt_tokens":1,"total_tokens":1}}"""
                : """{"id":"test","object":"chat.completion","created":0,"model":"test","choices":[{"index":0,"message":{"role":"assistant","content":"test"},"finish_reason":"stop"}],"usage":{"prompt_tokens":1,"completion_tokens":1,"total_tokens":2}}""";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }
}