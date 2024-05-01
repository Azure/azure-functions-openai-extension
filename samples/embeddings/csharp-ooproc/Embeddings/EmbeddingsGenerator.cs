using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace EmbeddingsGenerator;

public class EmbeddingsGenerator
{
    readonly ILogger<EmbeddingsGenerator> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingsGenerator"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is called by the Azure Functions runtime's dependency injection container.
    /// </remarks>
    public EmbeddingsGenerator(ILogger<EmbeddingsGenerator> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal class EmbeddingsRequest
    {
        [JsonPropertyName("RawText")]
        public string? RawText { get; set; }

        [JsonPropertyName("FilePath")]
        public string? FilePath { get; set; }

        [JsonPropertyName("URL")]
        public string? URL { get; set; }
    }

    /// <summary>
    /// Example showing how to use the <see cref="EmbeddingsAttribute"/> input binding to generate embeddings 
    /// for a raw text string.
    /// </summary>
    [Function(nameof(GenerateEmbeddings_Http_RequestAsync))]
    public async Task GenerateEmbeddings_Http_RequestAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings")] HttpRequestData req,
        [EmbeddingsInput("{RawText}", InputType.RawText, Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] EmbeddingsContext embeddings)
    {
        using StreamReader reader = new(req.Body);
        string request = await reader.ReadToEndAsync();

        EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

        this.logger.LogInformation(
            "Received {count} embedding(s) for input text containing {length} characters.",
            embeddings.Count,
            requestBody?.RawText?.Length);

        // TODO: Store the embeddings into a database or other storage.
    }

    /// <summary>
    /// Example showing how to use the <see cref="EmbeddingsAttribute"/> input binding to generate embeddings
    /// for text contained in a file on the file system.
    /// </summary>
    [Function(nameof(GetEmbeddings_Http_FilePath))]
    public async Task GetEmbeddings_Http_FilePath(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings-from-file")] HttpRequestData req,
        [EmbeddingsInput("{FilePath}", InputType.FilePath, MaxChunkLength = 512, Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] EmbeddingsContext embeddings)
    {
        using StreamReader reader = new(req.Body);
        string request = await reader.ReadToEndAsync();

        EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);
        this.logger.LogInformation(
            "Received {count} embedding(s) for input file '{path}'.",
            embeddings.Count,
            requestBody?.FilePath);

        // TODO: Store the embeddings into a database or other storage.
    }

    /// <summary>
    /// Example showing how to use the <see cref="EmbeddingsAttribute"/> input binding to generate embeddings
    /// for text contained in a file on the file system.
    /// </summary>
    [Function(nameof(GetEmbeddings_Http_URL))]
    public async Task GetEmbeddings_Http_URL(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings-from-url")] HttpRequestData req,
        [EmbeddingsInput("{URL}", InputType.URL, MaxChunkLength = 512, Model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%")] EmbeddingsContext embeddings)
    {
        using StreamReader reader = new(req.Body);
        string request = await reader.ReadToEndAsync();

        EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);
        this.logger.LogInformation(
            "Received {count} embedding(s) for input file '{path}'.",
            embeddings.Count,
            requestBody?.URL);

        // TODO: Store the embeddings into a database or other storage.
    }
}