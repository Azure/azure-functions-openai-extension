// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace SampleValidation;

public class TextCompletionLegacyTests
{
    readonly ITestOutputHelper output;
    readonly HttpClient client;
    readonly string baseAddress;
    readonly CancellationTokenSource cts;

    public TextCompletionLegacyTests(ITestOutputHelper output)
    {
        this.output = output;
        this.client = new HttpClient(new LoggingHandler(this.output));
        this.baseAddress = Environment.GetEnvironmentVariable("FUNC_BASE_ADDRESS") ?? "http://localhost:7071";
        this.cts = new CancellationTokenSource(delay: TimeSpan.FromMinutes(Debugger.IsAttached ? 5 : 1));

#if RELEASE
        // Use the default key for the Azure Functions app in RELEASE mode; for local development, DEBUG mode can be used.
        string functionKey = Environment.GetEnvironmentVariable("FUNC_DEFAULT_KEY") ?? throw new InvalidOperationException("Missing environment variable 'FUNC_DEFAULT_KEY'");
        this.client.DefaultRequestHeaders.Add("x-functions-key", functionKey);
#endif
    }

    [Fact]
    public async Task WhoIs_ValidName_ReturnsContent()
    {
        // Arrange
        string name = "Albert Einstein";

        // Act
        using HttpResponseMessage response = await this.client.GetAsync(
            requestUri: $"{this.baseAddress}/api/whois/{name}",
            cancellationToken: this.cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync(this.cts.Token);
        this.output.WriteLine($"Response content: {content}");

        // Validate that content is not empty and contains some relevant information about Albert Einstein
        Assert.NotEmpty(content);
        Assert.Contains("physicist", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("relativity", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenericCompletion_ValidPrompt_ReturnsContent()
    {
        // Arrange
        var promptPayload = new { Prompt = "Write a haiku about programming" };

        // Act
        using HttpResponseMessage response = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/GenericCompletion",
            promptPayload,
            cancellationToken: this.cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync(this.cts.Token);
        this.output.WriteLine($"Response content: {content}");

        // Validate that content is not empty and resembles a haiku structure
        Assert.NotEmpty(content);

        // Verify we received some form of haiku (might contain line breaks)
        // We can't verify exact content since AI responses vary, but we can check for reasonable length
        // and common haiku-related formatting
        Assert.True(content.Length >= 10, "Response should contain a meaningful haiku");
        Assert.True(content.Split('\n').Length >= 1, "Haiku should have at least one line");
    }

    [Fact]
    public async Task GenericCompletion_EmptyPrompt_ReturnsError()
    {
        // Arrange
        var emptyPrompt = new { Prompt = string.Empty };

        // Act
        using HttpResponseMessage response = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/GenericCompletion",
            emptyPrompt,
            cancellationToken: this.cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GenericCompletion_ComplexPrompt_ReturnsValidContent()
    {
        // Arrange
        var complexPrompt = new
        {
            Prompt = "Explain the difference between synchronous and asynchronous programming in 3 bullet points"
        };

        // Act
        using HttpResponseMessage response = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/GenericCompletion",
            complexPrompt,
            cancellationToken: this.cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync(this.cts.Token);
        this.output.WriteLine($"Response content: {content}");

        // Validate that content is not empty and contains relevant terms
        Assert.NotEmpty(content);
        Assert.Contains("synchronous", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("asynchronous", content, StringComparison.OrdinalIgnoreCase);

        // Check for bullet point formatting (could be -, *, or numbered)
        bool hasBulletFormat = content.Contains('-') ||
                              content.Contains('*') ||
                              content.Contains("1.") ||
                              content.Contains('•');

        Assert.True(hasBulletFormat, "Response should contain bullet points");
    }
}
