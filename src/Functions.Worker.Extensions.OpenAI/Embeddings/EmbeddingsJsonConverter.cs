// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Embeddings;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;

/// <summary>
/// OpenAIEmbeddingCollection JSON converter needed to serialize and deserialize the OpenAIEmbeddingCollection object with the dotnet worker.
/// </summary>
class EmbeddingsJsonConverter : JsonConverter<OpenAIEmbeddingCollection>
{
    static readonly ModelReaderWriterOptions JsonOptions = new("J");

    public override OpenAIEmbeddingCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);
        return ModelReaderWriter.Read<OpenAIEmbeddingCollection>(BinaryData.FromString(jsonDocument.RootElement.GetRawText()))!;
    }

    public override void Write(Utf8JsonWriter writer, OpenAIEmbeddingCollection value, JsonSerializerOptions options)
    {
        ((IJsonModel<OpenAIEmbeddingCollection>)value).Write(writer, JsonOptions);
    }
}
