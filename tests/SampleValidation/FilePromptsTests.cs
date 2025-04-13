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
        this.cts = new CancellationTokenSource(delay: TimeSpan.FromMinutes(Debugger.IsAttached ? 5 : 2));

        this.baseAddress = Environment.GetEnvironmentVariable("FUNC_BASE_ADDRESS") ?? "http://localhost:7071";

#if RELEASE
        // Use the default key for the Azure Functions app in RELEASE mode; for local development, DEBUG mode can be used.
        string functionKey = Environment.GetEnvironmentVariable("FUNC_DEFAULT_KEY") ?? throw new InvalidOperationException("Missing environment variable 'FUNC_DEFAULT_KEY'");
        client.DefaultRequestHeaders.Add("x-functions-key", functionKey);
#endif
    }

    [Fact]
    public async Task Ingest_Prompt_File_Test()
    {
        // Step 1: Test IngestFile
        var ingestRequest = new { url = "https://github.com/Azure/azure-functions-openai-extension/blob/main/README.md" };

        using HttpResponseMessage ingestResponse = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/IngestFile",
            ingestRequest,
            cancellationToken: this.cts.Token);

        Assert.Equal(HttpStatusCode.OK, ingestResponse.StatusCode);
        Assert.StartsWith("application/json", ingestResponse.Content.Headers.ContentType?.MediaType);

        string ingestResponseContent = await ingestResponse.Content.ReadAsStringAsync(this.cts.Token);
        JsonNode? ingestJsonResponse = JsonNode.Parse(ingestResponseContent);
        Assert.NotNull(ingestJsonResponse);
        Assert.Equal("success", ingestJsonResponse!["status"]?.GetValue<string>());
        Assert.Equal("README.md", ingestJsonResponse!["title"]?.GetValue<string>());

        // Step 2: Test PromptFile
        var promptRequest = new { prompt = "How can the textCompletion input binding be used from Azure Functions OpenAI extension?" };

        using HttpResponseMessage promptResponse = await this.client.PostAsJsonAsync(
            requestUri: $"{this.baseAddress}/api/PromptFile",
            promptRequest,
            cancellationToken: this.cts.Token);

        Assert.Equal(HttpStatusCode.OK, promptResponse.StatusCode);
        Assert.StartsWith("text/plain", promptResponse.Content.Headers.ContentType?.MediaType);

        string promptResponseContent = await promptResponse.Content.ReadAsStringAsync(this.cts.Token);
        Assert.False(string.IsNullOrWhiteSpace(promptResponseContent));
        Assert.Contains("OpenAI Chat Completions API", promptResponseContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("README", promptResponseContent, StringComparison.OrdinalIgnoreCase);
    }
}