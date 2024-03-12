﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Text;
using Azure.AI.OpenAI;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embedding;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Search;
class SearchableDocumentJsonConverter : JsonConverter<SearchableDocument>
{
    static readonly ModelReaderWriterOptions modelReaderWriterOptions = new("J");
    public override SearchableDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);

        // Properties for SearchableDocument
        EmbeddingsOptions embeddingsOptions = null;
        Embeddings embeddings = null;
        int count;
        string title = null;
        string connectionName = null;
        string collectionName = null;

        foreach (JsonProperty item in jsonDocument.RootElement.EnumerateObject())
        {
            if (item.NameEquals("embeddingsContext"u8))
            {
                foreach (JsonProperty embeddingContextItem in item.Value.EnumerateObject())
                {
                    if (embeddingContextItem.NameEquals("request"u8))
                    {
                        embeddingsOptions = ModelReaderWriter.Read<EmbeddingsOptions>(BinaryData.FromString(embeddingContextItem.Value.GetRawText()))!;
                    }
                    if (embeddingContextItem.NameEquals("response"u8))
                    {
                        embeddings = ModelReaderWriter.Read<Embeddings>(BinaryData.FromString(embeddingContextItem.Value.GetRawText()))!;
                    }
                    if (embeddingContextItem.NameEquals("count"u8))
                    {
                        count = embeddingContextItem.Value.GetInt32();
                    }
                }
            }
            if (item.NameEquals("connectionInfo"u8))
            {
                foreach (JsonProperty connectionInfoItem in item.Value.EnumerateObject())
                {
                    if (connectionInfoItem.NameEquals("connectionName"u8))
                    {
                        connectionName = connectionInfoItem.Value.GetString();
                    }
                    if (connectionInfoItem.NameEquals("collectionName"u8))
                    {
                        collectionName = connectionInfoItem.Value.GetString();
                    }
                }
            }

            if (item.NameEquals("title"u8))
            {
                title = item.Value.GetString();
            }
        }
        SearchableDocument searchableDocument = new SearchableDocument(title, new EmbeddingsContext(embeddingsOptions, embeddings));
        searchableDocument.ConnectionInfo = new ConnectionInfo(connectionName, collectionName);
        return searchableDocument;
    }

    public override void Write(Utf8JsonWriter writer, SearchableDocument value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("embeddingsContext"u8);
        writer.WriteStartObject();

        writer.WritePropertyName("request"u8);
        ((IJsonModel<EmbeddingsOptions>)value.Embeddings.Request).Write(writer, modelReaderWriterOptions);

        writer.WritePropertyName("response"u8);
        ((IJsonModel<Embeddings>)value.Embeddings.Response).Write(writer, modelReaderWriterOptions);

        writer.WritePropertyName("count"u8);
        writer.WriteNumberValue(value.Embeddings.Count);
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