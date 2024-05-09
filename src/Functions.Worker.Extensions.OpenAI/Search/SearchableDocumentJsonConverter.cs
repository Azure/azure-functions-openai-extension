﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAISDK = Azure.AI.OpenAI;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;

class SearchableDocumentJsonConverter : JsonConverter<SearchableDocument>
{
    static readonly ModelReaderWriterOptions modelReaderWriterOptions = new("J");
    public override SearchableDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, SearchableDocument value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("embeddingsContext"u8);
        writer.WriteStartObject();

        if (value.EmbeddingsContext?.Request is IJsonModel<OpenAISDK.EmbeddingsOptions> request)
        {
            writer.WritePropertyName("request"u8);
            request.Write(writer, modelReaderWriterOptions);
        }
        if (value.EmbeddingsContext?.Response is IJsonModel<OpenAISDK.Embeddings> response)
        {
            writer.WritePropertyName("response"u8);
            response.Write(writer, modelReaderWriterOptions);
        }
        if (value.EmbeddingsContext != null)
        {
            writer.WritePropertyName("count"u8);
            writer.WriteNumberValue(value.EmbeddingsContext.Count);
        }

        writer.WriteEndObject();

        writer.WritePropertyName("connectionInfo"u8);
        writer.WriteStartObject();
        writer.WritePropertyName("connectionName"u8);

        if (value.ConnectionInfo == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.ConnectionInfo.ConnectionName);
        }

        writer.WritePropertyName("collectionName"u8);

        if (value.ConnectionInfo == null)
        {
            writer.WriteNullValue();
        }
        else if (value.ConnectionInfo.CollectionName == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.ConnectionInfo.CollectionName);
        }
        writer.WriteEndObject();


        writer.WritePropertyName("title");
        writer.WriteStringValue(value.Title);
        writer.WriteEndObject();
    }
}
