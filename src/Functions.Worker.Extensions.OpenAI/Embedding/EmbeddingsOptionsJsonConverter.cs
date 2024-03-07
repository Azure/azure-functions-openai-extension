// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embedding;

/// <summary>
/// EmbeddingsOptions JSON converter needed to serialize and deserialize the EmbeddingsOptions object with the dotnet worker.
/// </summary>
internal class EmbeddingsOptionsJsonConverter : JsonConverter<EmbeddingsOptions>
{
    public override EmbeddingsOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);
        return ModelReaderWriter.Read<EmbeddingsOptions>(BinaryData.FromString(jsonDocument.RootElement.GetRawText()));
    }

    public override void Write(Utf8JsonWriter writer, EmbeddingsOptions value, JsonSerializerOptions options)
    {
        ((IJsonModel<EmbeddingsOptions>)value).Write(writer, new ModelReaderWriterOptions("J"));
    }
}
