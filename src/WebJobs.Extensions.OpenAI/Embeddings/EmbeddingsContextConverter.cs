// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Embeddings;

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
        writer.WritePropertyName("input"u8);

        if (value.Input is List<string> inputList)
        {
            var inputWrapper = JsonModelListWrapper.FromList(inputList);
            inputWrapper.Write(writer, modelReaderWriterOptions);
        }

        if (value.Response is IJsonModel<OpenAIEmbeddingCollection> response)
        {
            writer.WritePropertyName("response"u8);
            response.Write(writer, modelReaderWriterOptions);
        }

        writer.WritePropertyName("count"u8);
        writer.WriteNumberValue(value.Count);

        writer.WriteEndObject();
    }
}
