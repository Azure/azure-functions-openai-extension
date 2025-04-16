// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Chat;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;
class ChatCompletionsJsonConverter : JsonConverter<ChatCompletion>
{
    static readonly ModelReaderWriterOptions modelReaderWriterOptions = new("J");
    public override ChatCompletion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, ChatCompletion value, JsonSerializerOptions options)
    {
        ((IJsonModel<ChatCompletion>)value).Write(writer, modelReaderWriterOptions);
    }
}
