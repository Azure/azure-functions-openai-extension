using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embedding;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using Azure;
using System.ClientModel.Primitives;

namespace EmbeddingsIsolated
{
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

        public class EmbeddingsContext : IJsonModel<EmbeddingsContext>
        {
            public EmbeddingsContext(EmbeddingsOptions Request, Embeddings Response)
            {
                this.Request = Request;
                this.Response = Response;
            }

            public EmbeddingsOptions Request { get; set; }
            public Embeddings Response { get; set; }
            /// <summary>
            /// Gets the number of embeddings that were returned in the response.
            /// </summary>
            public int Count => this.Response.Data?.Count ?? 0;

            public EmbeddingsContext Create(BinaryData data, ModelReaderWriterOptions options)
            {
                return ModelReaderWriter.Read<EmbeddingsContext>(data, options);
            }

            public void Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("request"u8);
                ((IJsonModel<EmbeddingsOptions>)Request).Write(writer, options);

                writer.WritePropertyName("response"u8);
                ((IJsonModel<Embeddings>)Response).Write(writer, options);

                writer.WritePropertyName("count"u8);
                writer.WriteNumberValue(Count);

                writer.WriteEndObject();
            }

            EmbeddingsContext IJsonModel<EmbeddingsContext>.Create(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
            {
                using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);

                foreach (JsonProperty item in jsonDocument.RootElement.EnumerateObject())
                {
                    if (item.NameEquals("request"u8))
                    {
                        this.Request = ModelReaderWriter.Read<EmbeddingsOptions>(BinaryData.FromString(item.Value.GetString()));
                    }

                    if (item.NameEquals("response"u8))
                    {
                        this.Response = ModelReaderWriter.Read<Embeddings>(BinaryData.FromString(item.Value.GetString()));
                    }
                }
                return this;
            }

            string IPersistableModel<EmbeddingsContext>.GetFormatFromOptions(ModelReaderWriterOptions options)
            {
                return "J";
            }

            BinaryData IPersistableModel<EmbeddingsContext>.Write(ModelReaderWriterOptions options)
            {
                throw new NotImplementedException();
            }
        }

        public class EmbeddingsRequest
        {
            [JsonPropertyName("RawText")]
            public string RawText { get; set; }

            [JsonPropertyName("FilePath")]
            public string FilePath { get; set; }
        }

        /// <summary>
        /// Example showing how to use the <see cref="EmbeddingsAttribute"/> input binding to generate embeddings 
        /// for a raw text string.
        /// </summary>
        [Function(nameof(GenerateEmbeddings_Http_RequestAsync))]
        public static async Task GenerateEmbeddings_Http_RequestAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings")] HttpRequestData req,
            [EmbeddingsInput("{RawText}", InputType.RawText)] Embeddings embeddings,
            ILogger logger)
        {
            using StreamReader reader = new(req.Body);

            string request = await reader.ReadToEndAsync();

            EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

            logger.LogInformation(
                "Received {count} embedding(s) for input text containing {length} characters.",
               embeddings,
                requestBody.RawText.Length);

            // TODO: Store the embeddings into a database or other storage.
        }

        /// <summary>
        /// Example showing how to use the <see cref="EmbeddingsAttribute"/> input binding to generate embeddings
        /// for text contained in a file on the file system.
        /// </summary>
        [Function(nameof(GetEmbeddings_Http_FilePath))]
        public static void GetEmbeddings_Http_FilePath(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings-from-file")] EmbeddingsRequest req,
            [EmbeddingsInput("{FilePath}", InputType.FilePath, MaxChunkLength = 512)] EmbeddingsContext embeddings,
            ILogger logger)
        {
            logger.LogInformation(
                "Received {count} embedding(s) for input file '{path}'.",
                embeddings.Response,
                req.FilePath);

            // TODO: Store the embeddings into a database or other storage.
        }
    }
}
