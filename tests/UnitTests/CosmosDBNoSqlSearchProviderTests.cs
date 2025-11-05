// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBNoSqlSearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace WebJobsOpenAIUnitTests;

public class CosmosDBNoSqlSearchProviderTests
{
    readonly Mock<ILoggerFactory> mockLoggerFactory;
    readonly Mock<ILogger<CosmosDBNoSqlSearchProvider>> mockLogger;
    readonly Mock<IConfiguration> mockConfiguration;
    readonly Mock<IOptions<CosmosDBNoSqlSearchConfigOptions>> mockOptions;
    readonly Mock<AzureComponentFactory> mockAzureComponentFactory;

    public CosmosDBNoSqlSearchProviderTests()
    {
        this.mockLoggerFactory = new Mock<ILoggerFactory>();
        this.mockLogger = new Mock<ILogger<CosmosDBNoSqlSearchProvider>>();
        this.mockConfiguration = new Mock<IConfiguration>();
        this.mockOptions = new Mock<IOptions<CosmosDBNoSqlSearchConfigOptions>>();
        this.mockAzureComponentFactory = new Mock<AzureComponentFactory>();

        this.mockLoggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(this.mockLogger.Object);

        // Setup valid default options
        var options = new CosmosDBNoSqlSearchConfigOptions
        {
            DatabaseName = "TestDB",
            VectorDimensions = 1536,
            EmbeddingKey = "/embedding",
            ApplicationName = "TestApp"
        };
        this.mockOptions.Setup(o => o.Value).Returns(options);
    }

    [Fact]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        // Act
        var provider = new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockAzureComponentFactory.Object);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("CosmosDBNoSqlSearch", provider.Name);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
#nullable disable
        Assert.Throws<ArgumentNullException>(() => new CosmosDBNoSqlSearchProvider(
            null,
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockAzureComponentFactory.Object));
#nullable restore
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
#nullable disable
        Assert.Throws<ArgumentNullException>(() => new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            null,
            this.mockOptions.Object,
            this.mockAzureComponentFactory.Object));
#nullable restore
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
#nullable disable
        Assert.Throws<ArgumentNullException>(() => new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            this.mockLoggerFactory.Object,
            null,
            this.mockAzureComponentFactory.Object));
#nullable restore
    }

    [Fact]
    public void Constructor_WithNullAzureComponentFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
#nullable disable
        Assert.Throws<ArgumentNullException>(() => new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            null));
#nullable restore
    }

    [Fact]
    public void Constructor_WithEmptyDatabaseName_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new CosmosDBNoSqlSearchConfigOptions
        {
            DatabaseName = "",
            VectorDimensions = 1536,
            EmbeddingKey = "/embedding",
            ApplicationName = "TestApp"
        };
        this.mockOptions.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockAzureComponentFactory.Object));
    }

    [Fact]
    public void Constructor_WithInvalidVectorDimensions_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new CosmosDBNoSqlSearchConfigOptions
        {
            DatabaseName = "TestDB",
            VectorDimensions = 0,
            EmbeddingKey = "/embedding",
            ApplicationName = "TestApp"
        };
        this.mockOptions.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockAzureComponentFactory.Object));
    }

    [Fact]
    public void Constructor_WithEmptyEmbeddingKey_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new CosmosDBNoSqlSearchConfigOptions
        {
            DatabaseName = "TestDB",
            VectorDimensions = 1536,
            EmbeddingKey = "",
            ApplicationName = "TestApp"
        };
        this.mockOptions.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockAzureComponentFactory.Object));
    }

    [Fact]
    public void Constructor_WithEmptyApplicationName_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new CosmosDBNoSqlSearchConfigOptions
        {
            DatabaseName = "TestDB",
            VectorDimensions = 1536,
            EmbeddingKey = "/embedding",
            ApplicationName = ""
        };
        this.mockOptions.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockAzureComponentFactory.Object));
    }

    [Fact]
    public void Constructor_WithLowDatabaseThroughput_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new CosmosDBNoSqlSearchConfigOptions
        {
            DatabaseName = "TestDB",
            VectorDimensions = 1536,
            EmbeddingKey = "/embedding",
            ApplicationName = "TestApp",
            DatabaseThroughput = 300 // Less than minimum of 400
        };
        this.mockOptions.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockAzureComponentFactory.Object));
    }

    [Fact]
    public void Constructor_WithLowContainerThroughput_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new CosmosDBNoSqlSearchConfigOptions
        {
            DatabaseName = "TestDB",
            VectorDimensions = 1536,
            EmbeddingKey = "/embedding",
            ApplicationName = "TestApp",
            ContainerThroughput = 300 // Less than minimum of 400
        };
        this.mockOptions.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockAzureComponentFactory.Object));
    }

    [Fact]
    public async Task SearchAsync_WithEmptyEmbeddings_ThrowsArgumentException()
    {
        // Arrange
        var provider = new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockAzureComponentFactory.Object);

        var request = new SearchRequest(
            "test query",
            ReadOnlyMemory<float>.Empty,
            10,
            new ConnectionInfo("TestConnection", "TestCollection"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => provider.SearchAsync(request));
    }

    [Fact]
    public async Task SearchAsync_WithNullConnectionInfo_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = new CosmosDBNoSqlSearchProvider(
            this.mockConfiguration.Object,
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockAzureComponentFactory.Object);

#nullable disable
        var request = new SearchRequest(
            "test query",
            new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f }),
            10,
            null);
#nullable restore

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SearchAsync(request));
    }
}
