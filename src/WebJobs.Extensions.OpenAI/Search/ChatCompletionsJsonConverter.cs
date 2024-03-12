// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;

namespace WebJobs.Extensions.OpenAI.Search;
public class ChatCompletionsJsonConverter : JsonConverter<ChatCompletions>
{
    static readonly ModelReaderWriterOptions modelReaderWriterOptions = new("J");
    public override ChatCompletions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, ChatCompletions value, JsonSerializerOptions options)
    {
        ((IJsonModel<ChatCompletions>)value).Write(writer, modelReaderWriterOptions);
    }
}
