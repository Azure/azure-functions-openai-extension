using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Xunit;
using Xunit.Abstractions;

namespace SampleValidation;

public class FilePromptTests
{
    readonly ITestOutputHelper output;
    readonly string baseAddress;
    readonly HttpClient client;
    readonly CancellationTokenSource cts;

    public FilePromptTests(ITestOutputHelper output)
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
    public async Task IngestFile_ValidUrl_ReturnsSuccess()
    {        
        // Prepare the request
        var request = new { url = "https://github.com/Azure/azure-functions-openai-extension/blob/main/README.md" };

        // Send the POST request to IngestFile
        using HttpResponseMessage response = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/IngestFile",
            request,
            cancellationToken: this.cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType);

        // Validate the response content
        string responseContent = await response.Content.ReadAsStringAsync(this.cts.Token);
        JsonNode? jsonResponse = JsonNode.Parse(responseContent);
        Assert.NotNull(jsonResponse);
        Assert.Equal("success", jsonResponse!["status"]?.GetValue<string>());
        Assert.Equal("README.md", jsonResponse!["title"]?.GetValue<string>());
    }

    [Fact]
    public async Task PromptFile_ValidPrompt_ReturnsResponse()
    {
        // Prepare the request
        var request = new { prompt = "How can the textCompletion input binding be used from Azure Functions OpenAI extension?" };

        // Send the POST request to PromptFile
        using HttpResponseMessage response = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/PromptFile",
            request,
            cancellationToken: this.cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/plain", response.Content.Headers.ContentType?.MediaType);

        // Validate the response content
        string responseContent = await response.Content.ReadAsStringAsync(this.cts.Token);
        Assert.False(string.IsNullOrWhiteSpace(responseContent));
        Assert.Contains("OpenAI Chat Completions API", responseContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("README", responseContent, StringComparison.OrdinalIgnoreCase);
    }
}