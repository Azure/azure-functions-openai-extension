// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Azure.WebJobs.Extensions.OpenAI;

namespace WebJobs.Extensions.OpenAI;
public class EmbeddingsJsonConverter : JsonConverter<EmbeddingsContext>
{
    public override EmbeddingsContext Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);

        EmbeddingsOptions embeddingsOptions = null!;
        Embeddings embeddings = null!;

        foreach (JsonProperty item in jsonDocument.RootElement.EnumerateObject())
        {
            if (item.NameEquals("request"u8))
            {
                embeddingsOptions = ModelReaderWriter.Read<EmbeddingsOptions>(BinaryData.FromString(item.Value.GetString()));
            }

            if (item.NameEquals("response"u8))
            {
                embeddings = ModelReaderWriter.Read<Embeddings>(BinaryData.FromString(item.Value.GetString()));
            }
        }
        return new EmbeddingsContext(embeddingsOptions, embeddings);
    }

    public override void Write(Utf8JsonWriter writer, EmbeddingsContext value, JsonSerializerOptions options)
    {

        writer.WriteStartObject();
        writer.WritePropertyName("request"u8);
        ((IJsonModel<EmbeddingsOptions>)value.Request).Write(writer, new ModelReaderWriterOptions("J"));

        writer.WritePropertyName("response"u8);
        ((IJsonModel<Embeddings>)value.Response).Write(writer, new ModelReaderWriterOptions("J"));

        writer.WritePropertyName("count"u8);
        writer.WriteNumberValue(value.Count);

        writer.WriteEndObject();
    }
}
