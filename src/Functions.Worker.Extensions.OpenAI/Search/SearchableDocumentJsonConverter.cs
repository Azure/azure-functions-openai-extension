﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Search;
using System.Text.Json.Serialization;

namespace Functions.Worker.Extensions.OpenAI.Search;
class SearchableDocumentJsonConverter : JsonConverter<SearchableDocument>
{
    static readonly ModelReaderWriterOptions modelReaderWriterOptions = new("J");
    public override SearchableDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);
        return new SearchableDocument("lol", null);
    }

    public override void Write(Utf8JsonWriter writer, SearchableDocument value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("embeddingsContext"u8);
        writer.WriteStartObject();
        
        writer.WritePropertyName("request"u8);
        ((IJsonModel<EmbeddingsOptions>)value.EmbeddingsContext.Request).Write(writer, modelReaderWriterOptions);

        writer.WritePropertyName("response"u8);
        ((IJsonModel<Embeddings>)value.EmbeddingsContext.Response).Write(writer, modelReaderWriterOptions);
        
        writer.WritePropertyName("count"u8);
        writer.WriteNumberValue(value.EmbeddingsContext.Count);
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