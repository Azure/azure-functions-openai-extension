// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using System.Reflection;
using Azure.Core;
using Microsoft.Azure.WebJobs.Extensions.OpenAI;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
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
        Mock<IConfigurationSection> mockSection = CreateMockSection(
            exists: true,
            endpoint: "https://test-endpoint.openai.azure.com/",
            key: null);

        this.mockConfiguration.Setup(c => c.GetSection("TestConnection")).Returns(mockSection.Object);

        TokenCredential tokenCredential = new Mock<TokenCredential>().Object;
        this.mockAzureComponentFactory.Setup(f => f.CreateTokenCredential(It.IsAny<IConfigurationSection>()))
            .Returns(tokenCredential);

        OpenAIClientFactory factory = new(
            this.mockConfiguration.Object,
            this.mockAzureComponentFactory.Object,
            this.mockLoggerFactory.Object);

        // Act
        ChatClient chatClient = factory.GetChatClient("TestConnection", "gpt-35-turbo");

        // Assert
        Assert.NotNull(chatClient);
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
}