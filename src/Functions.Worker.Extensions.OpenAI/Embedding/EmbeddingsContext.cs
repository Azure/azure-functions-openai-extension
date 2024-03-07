// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Xml.Linq;
using Azure;
using Azure.AI.OpenAI;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embedding;

/// <summary>
/// Binding target for the <see cref="EmbeddingsAttribute"/>.
/// </summary>
/// <param name="Request">The embeddings request that was sent to OpenAI.</param>
/// <param name="Response">The embeddings response that was received from OpenAI.</param>
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
