// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;


namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embedding;

/// <summary>
/// Embeddings JSON converter needed to serialize and deserialize the Embeddings object with the dotnet worker.
/// </summary>
class EmbeddingsJsonConverter : JsonConverter<Embeddings>
{
    public override Embeddings Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);
        return ModelReaderWriter.Read<Embeddings>(BinaryData.FromString(jsonDocument.RootElement.GetRawText()))!;
    }

    public override void Write(Utf8JsonWriter writer, Embeddings value, JsonSerializerOptions options)
    {
        ((IJsonModel<Embeddings>)value).Write(writer, new ModelReaderWriterOptions("J"));
    }
}
