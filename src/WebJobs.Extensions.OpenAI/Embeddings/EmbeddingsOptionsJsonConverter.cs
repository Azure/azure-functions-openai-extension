// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
class EmbeddingsOptionsJsonConverter : JsonConverter<EmbeddingsOptions>
{
    static readonly ModelReaderWriterOptions modelReaderWriterOptions = new("J");
    public override EmbeddingsOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, EmbeddingsOptions value, JsonSerializerOptions options)
    {
        ((IJsonModel<EmbeddingsOptions>)value).Write(writer, modelReaderWriterOptions);
    }
}
