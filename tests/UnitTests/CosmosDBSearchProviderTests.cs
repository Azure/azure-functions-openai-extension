// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBSearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace WebJobsOpenAIUnitTests;

public class CosmosDBSearchProviderTests
{
    readonly Mock<ILoggerFactory> mockLoggerFactory;
    readonly Mock<ILogger<CosmosDBSearchProvider>> mockLogger;
    readonly Mock<IConfiguration> mockConfiguration;
    readonly Mock<IOptions<CosmosDBSearchConfigOptions>> mockOptions;

    public CosmosDBSearchProviderTests()
    {
        this.mockLoggerFactory = new Mock<ILoggerFactory>();
        this.mockLogger = new Mock<ILogger<CosmosDBSearchProvider>>();
        this.mockConfiguration = new Mock<IConfiguration>();
        this.mockOptions = new Mock<IOptions<CosmosDBSearchConfigOptions>>();

        this.mockLoggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(this.mockLogger.Object);

        // Setup valid default options
        var options = new CosmosDBSearchConfigOptions
        {
            VectorSearchDimensions = 1536,
            DatabaseName = "TestDB",
            IndexName = "TestIndex",
            Kind = "vector-ivf"
        };
        this.mockOptions.Setup(o => o.Value).Returns(options);
    }

    [Fact]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        // Act
        var provider = new CosmosDBSearchProvider(
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockConfiguration.Object);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("CosmosDBSearch", provider.Name);
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
#nullable disable
        Assert.Throws<ArgumentNullException>(() => new CosmosDBSearchProvider(
            null,
            this.mockOptions.Object,
            this.mockConfiguration.Object));
#nullable restore
    }

    [Fact]
    public void Constructor_WithInvalidVectorDimensions_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidOptions = new CosmosDBSearchConfigOptions
        {
            VectorSearchDimensions = 1, // Invalid: less than 2
            DatabaseName = "TestDB",
            IndexName = "TestIndex"
        };
        this.mockOptions.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new CosmosDBSearchProvider(
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockConfiguration.Object));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(4000)]
    public void Constructor_WithInvalidVectorDimensionsRange_ThrowsArgumentOutOfRangeException(int dimensions)
    {
        // Arrange
        var invalidOptions = new CosmosDBSearchConfigOptions
        {
            VectorSearchDimensions = dimensions,
            ApplicationName = "TestApp",
            DatabaseName = "TestDB",
            IndexName = "TestIndex"
        };
        this.mockOptions.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new CosmosDBSearchProvider(
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockConfiguration.Object));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1536)]
    [InlineData(3072)]
    public void Constructor_WithValidVectorDimensionsRange_CreatesInstance(int dimensions)
    {
        // Arrange
        var validOptions = new CosmosDBSearchConfigOptions
        {
            VectorSearchDimensions = dimensions,
            DatabaseName = "TestDB",
            IndexName = "TestIndex",
            Kind = "vector-ivf"
        };
        this.mockOptions.Setup(o => o.Value).Returns(validOptions);

        // Act
        var provider = new CosmosDBSearchProvider(
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockConfiguration.Object);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyEmbeddings_ThrowsArgumentException()
    {
        // Arrange
        var provider = new CosmosDBSearchProvider(
            this.mockLoggerFactory.Object,
            this.mockOptions.Object,
            this.mockConfiguration.Object);

        var request = new SearchRequest(
            "test query",
            ReadOnlyMemory<float>.Empty,
            10,
            new ConnectionInfo("TestConnection", "TestCollection"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => provider.SearchAsync(request));
    }
}
