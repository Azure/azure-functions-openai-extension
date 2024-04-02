// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAISDK = Azure.AI.OpenAI;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;

/// <summary>
/// Embeddings JSON converter needed to serialize the EmbeddingsContext object.
/// </summary>
class EmbeddingsContextConverter : JsonConverter<EmbeddingsContext>
{
    static readonly ModelReaderWriterOptions modelReaderWriterOptions = new("J");
    public override EmbeddingsContext Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, EmbeddingsContext value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("request"u8);
        ((IJsonModel<OpenAISDK.EmbeddingsOptions>)value.Request).Write(writer, modelReaderWriterOptions);

        writer.WritePropertyName("response"u8);
        ((IJsonModel<OpenAISDK.Embeddings>)value.Response).Write(writer, modelReaderWriterOptions);

        writer.WritePropertyName("count"u8);
        writer.WriteNumberValue(value.Count);

        writer.WriteEndObject();
    }
}
