// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Embeddings;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
class EmbeddingsOptionsJsonConverter : JsonConverter<EmbeddingGenerationOptions>
{
    static readonly ModelReaderWriterOptions modelReaderWriterOptions = new("J");
    public override EmbeddingGenerationOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, EmbeddingGenerationOptions value, JsonSerializerOptions options)
    {
        ((IJsonModel<EmbeddingGenerationOptions>)value).Write(writer, modelReaderWriterOptions);
    }
}
