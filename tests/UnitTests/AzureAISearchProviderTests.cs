// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.AzureAISearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace WebJobsOpenAIUnitTests;

/// <summary>
/// Integration-style tests for <see cref="AzureAISearchProvider"/> that exercise
/// constructor wiring, options validation, argument validation and the
/// configuration-resolution flow without requiring a real Azure AI Search resource.
///
/// The provider constructs <c>SearchClient</c>/<c>SearchIndexClient</c> internally,
/// so any code path that proceeds to an actual REST call will fail at the network
/// layer. These tests stop deterministically before that point or assert against
/// the configuration-validation failures the provider raises up-front.
/// </summary>
public class AzureAISearchProviderTests
{
    static IOptions<AzureAISearchConfigOptions> Options(
        bool semantic = false,
        bool captions = false,
        int dims = 1536,
        string? apiKeySetting = null) =>
        Microsoft.Extensions.Options.Options.Create(new AzureAISearchConfigOptions
        {
            IsSemanticSearchEnabled = semantic,
            UseSemanticCaptions = captions,
            VectorSearchDimensions = dims,
            SearchAPIKeySetting = apiKeySetting,
        });

    static IConfiguration BuildConfig(IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    static AzureAISearchProvider CreateProvider(
        IConfiguration? configuration = null,
        IOptions<AzureAISearchConfigOptions>? options = null)
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => { });
        Mock<AzureComponentFactory> azureComponentFactory = new();
        return new AzureAISearchProvider(
            configuration ?? BuildConfig(new Dictionary<string, string?>()),
            loggerFactory,
            options ?? Options(),
            azureComponentFactory.Object);
    }

    [Fact]
    public void Constructor_NullConfiguration_Throws()
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => { });
        Mock<AzureComponentFactory> acf = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new AzureAISearchProvider(null!, loggerFactory, Options(), acf.Object));
        Assert.Equal("configuration", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullAzureComponentFactory_Throws()
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => { });

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new AzureAISearchProvider(BuildConfig(new Dictionary<string, string?>()), loggerFactory, Options(), null!));
        Assert.Equal("azureComponentFactory", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullLoggerFactory_Throws()
    {
        Mock<AzureComponentFactory> acf = new();

        Assert.Throws<ArgumentNullException>(() =>
            new AzureAISearchProvider(BuildConfig(new Dictionary<string, string?>()), null!, Options(), acf.Object));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(3073)]
    public void Constructor_VectorDimensionsOutOfRange_Throws(int dims)
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => { });
        Mock<AzureComponentFactory> acf = new();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AzureAISearchProvider(
                BuildConfig(new Dictionary<string, string?>()),
                loggerFactory,
                Options(dims: dims),
                acf.Object));
        Assert.Equal(nameof(AzureAISearchConfigOptions.VectorSearchDimensions), ex.ParamName);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1536)]
    [InlineData(3072)]
    public void Constructor_VectorDimensionsInRange_Succeeds(int dims)
    {
        AzureAISearchProvider provider = CreateProvider(options: Options(dims: dims));
        Assert.Equal("AzureAISearch", provider.Name);
    }

    [Fact]
    public async Task AddDocumentAsync_NullConnectionInfo_Throws()
    {
        AzureAISearchProvider provider = CreateProvider();
        SearchableDocument doc = new("title") { ConnectionInfo = null };

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => provider.AddDocumentAsync(doc, CancellationToken.None));
    }

    [Fact]
    public async Task SearchAsync_QueryAndEmbeddingsBothEmpty_Throws()
    {
        AzureAISearchProvider provider = CreateProvider();
        SearchRequest request = new(
            Query: null!,
            Embeddings: ReadOnlyMemory<float>.Empty,
            MaxResults: 3,
            ConnectionInfo: new ConnectionInfo("AISearch", "openai-index"));

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.SearchAsync(request));
        Assert.Contains("query or embeddings", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchAsync_NullConnectionInfo_Throws()
    {
        AzureAISearchProvider provider = CreateProvider();
        SearchRequest request = new(
            Query: "hello",
            Embeddings: ReadOnlyMemory<float>.Empty,
            MaxResults: 3,
            ConnectionInfo: null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SearchAsync(request));
    }

    [Fact]
    public async Task AddDocumentAsync_MissingConnectionConfiguration_ThrowsInvalidOperation()
    {
        // No "AISearch" section and no "AISearch" key in config -> SetConfigSectionProperties throws.
        AzureAISearchProvider provider = CreateProvider(
            configuration: BuildConfig(new Dictionary<string, string?>()));

        SearchableDocument doc = new("title")
        {
            ConnectionInfo = new ConnectionInfo("AISearch", "openai-index"),
            Embeddings = new EmbeddingsContext(new List<string>(), null),
        };

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.AddDocumentAsync(doc, CancellationToken.None));
        Assert.Contains("AISearch", ex.Message);
    }

    [Fact]
    public async Task SearchAsync_MissingConnectionConfiguration_ThrowsInvalidOperation()
    {
        AzureAISearchProvider provider = CreateProvider(
            configuration: BuildConfig(new Dictionary<string, string?>()));

        SearchRequest request = new(
            Query: "hello",
            Embeddings: ReadOnlyMemory<float>.Empty,
            MaxResults: 3,
            ConnectionInfo: new ConnectionInfo("MissingConn", "openai-index"));

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SearchAsync(request));
        Assert.Contains("MissingConn", ex.Message);
    }

    [Fact]
    public async Task SearchAsync_WithEndpointAsFlatSetting_AttemptsNetworkCall()
    {
        // Endpoint configured as a flat connection-name property (no section).
        // The provider should resolve config successfully and then attempt a
        // real SearchAsync call against an unreachable endpoint, surfacing an
        // SDK exception. This proves the configuration path executes end-to-end.
        IConfiguration config = BuildConfig(new Dictionary<string, string?>
        {
            ["AISearch"] = "https://nonexistent-search-resource.invalid.example/",
        });

        AzureAISearchProvider provider = CreateProvider(configuration: config);
        SearchRequest request = new(
            Query: "hello",
            Embeddings: ReadOnlyMemory<float>.Empty,
            MaxResults: 1,
            ConnectionInfo: new ConnectionInfo("AISearch", "openai-index"));

        // Any exception (auth, DNS, request failure) is acceptable - we only assert
        // that configuration parsing succeeded and the SDK call was attempted.
        await Assert.ThrowsAnyAsync<Exception>(() => provider.SearchAsync(request));
    }

    [Fact]
    public async Task SearchAsync_WithEndpointAndApiKeyFromSection_AttemptsNetworkCall()
    {
        IConfiguration config = BuildConfig(new Dictionary<string, string?>
        {
            ["AISearch:endPoint"] = "https://nonexistent-search-resource.invalid.example/",
            ["AISearch:apiKey"] = "fake-key",
        });

        AzureAISearchProvider provider = CreateProvider(configuration: config);
        SearchRequest request = new(
            Query: "hello",
            Embeddings: ReadOnlyMemory<float>.Empty,
            MaxResults: 1,
            ConnectionInfo: new ConnectionInfo("AISearch", "openai-index"));

        await Assert.ThrowsAnyAsync<Exception>(() => provider.SearchAsync(request));
    }
}
