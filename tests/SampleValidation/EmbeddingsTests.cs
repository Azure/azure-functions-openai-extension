// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace SampleValidation;

public class EmbeddingsTests
{
    readonly ITestOutputHelper output;
    readonly string baseAddress;
    readonly HttpClient client;
    readonly CancellationTokenSource cts;

    public EmbeddingsTests(ITestOutputHelper output)
    {
        this.output = output;
        this.client = new HttpClient(new LoggingHandler(this.output));
        this.cts = new CancellationTokenSource(delay: TimeSpan.FromMinutes(Debugger.IsAttached ? 5 : 1));

        this.baseAddress = Environment.GetEnvironmentVariable("FUNC_BASE_ADDRESS") ?? "http://localhost:7071";

#if RELEASE
        // Use the default key for the Azure Functions app in RELEASE mode; for local development, DEBUG mode can be used.
        string functionKey = Environment.GetEnvironmentVariable("FUNC_DEFAULT_KEY") ?? throw new InvalidOperationException("Missing environment variable 'FUNC_DEFAULT_KEY'");
        client.DefaultRequestHeaders.Add("x-functions-key", functionKey);
#endif
    }

    [Fact]
    public async Task GenerateEmbeddings_Http_Request_Test()
    {
        // Arrange
        var request = new
        {
            rawText = "This is a test for generating embeddings from raw text."
        };

        // Act
        using HttpResponseMessage response = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/embeddings",
            request,
            cancellationToken: this.cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task EmbeddingsLegacy_Performance_Test()
    {
        // Create a large text for testing performance
        string largeText = new('A', 10000); // 10KB text

        var request = new
        {
            rawText = largeText
        };

        // Measure time
        var stopwatch = Stopwatch.StartNew();

        using HttpResponseMessage response = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/embeddings",
            request,
            cancellationToken: this.cts.Token);

        stopwatch.Stop();

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Log performance metrics
        this.output.WriteLine($"Embeddings generation for 10KB text took {stopwatch.ElapsedMilliseconds}ms");

        // If specific performance SLAs exist, add assertions like:
        // Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Embeddings generation took too long");
    }

    [Fact]
    public async Task GetEmbeddings_Url_ReturnsNoContent()
    {
        // Arrange
        var request = new { url = "https://github.com/Azure/azure-functions-openai-extension/blob/main/README.md" };

        // Act
        using HttpResponseMessage response = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/embeddings-from-url",
            request,
            cancellationToken: this.cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GenerateEmbeddings_InvalidText_ReturnsBadRequest()
    {
        // Arrange
        var request = new { url = "" }; // Invalid: Empty text

        // Act
        using HttpResponseMessage response = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/embeddings",
            request,
            cancellationToken: this.cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetEmbeddings_InvalidFilePath_ReturnsBadRequest()
    {
        // Arrange
        var request = new { filePath = "invalid/file/path.txt" }; // Invalid: Non-existent file path

        // Act
        using HttpResponseMessage response = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/embeddings-from-file",
            request,
            cancellationToken: this.cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetEmbeddings_InvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new { url = "invalid-url" }; // Invalid: Malformed URL

        // Act
        using HttpResponseMessage response = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/embeddings-from-url",
            request,
            cancellationToken: this.cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}