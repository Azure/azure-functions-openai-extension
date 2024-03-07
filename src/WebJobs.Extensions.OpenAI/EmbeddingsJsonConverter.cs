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

/// <summary>
/// Embeddings JSON converter needed to serialize the EmbeddingsContext object.
/// </summary>
internal class EmbeddingsJsonConverter : JsonConverter<EmbeddingsContext>
{
    public override EmbeddingsContext Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
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
