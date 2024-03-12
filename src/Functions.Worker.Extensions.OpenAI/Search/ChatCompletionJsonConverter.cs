// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Newtonsoft.Json.Linq;

namespace WebJobs.Extensions.OpenAI.Search;
public class ChatCompletionsJsonConverter : JsonConverter<ChatCompletions>
{
    static readonly ModelReaderWriterOptions modelReaderWriterOptions = new("J");
    public override ChatCompletions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);
        return ModelReaderWriter.Read<ChatCompletions>(BinaryData.FromString(jsonDocument.RootElement.GetRawText()))!;
    }

    public override void Write(Utf8JsonWriter writer, ChatCompletions value, JsonSerializerOptions options)
    {
        ((IJsonModel<ChatCompletions>)value).Write(writer, modelReaderWriterOptions);
    }
}