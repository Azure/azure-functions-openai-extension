// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Embeddings;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;

/// <summary>
/// EmbeddingsOptions JSON converter needed to serialize and deserialize the EmbeddingsOptions object with the dotnet worker.
/// </summary>
class EmbeddingsOptionsJsonConverter : JsonConverter<EmbeddingGenerationOptions>
{
    static readonly ModelReaderWriterOptions JsonOptions = new("J");

    public override EmbeddingGenerationOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);
        return ModelReaderWriter.Read<EmbeddingGenerationOptions>(BinaryData.FromString(jsonDocument.RootElement.GetRawText()))!;
    }

    public override void Write(Utf8JsonWriter writer, EmbeddingGenerationOptions value, JsonSerializerOptions options)
    {
        ((IJsonModel<EmbeddingGenerationOptions>)value).Write(writer, JsonOptions);
    }
}
