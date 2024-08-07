// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBNoSqlSearch;

/// <summary>
/// Open AI Configuration Options used for reading host.json values.
/// </summary>
public class CosmosDBNoSqlSearchConfigOptions
{
    // The application name to be used in the requests
    public string ApplicationName { get; set; } = "openai-functions-nosql";

    // Vector Embedding Policy for the Database.
    public VectorEmbeddingPolicy VectorEmbeddingPolicy { get; set; } =
        new(new Collection<Embedding>());

    // Indexing Policy for the Database.
    public IndexingPolicy IndexingPolicy { get; set; } = new();

    // Database Name for the vector store.
    public string DatabaseName { get; set; } = "openai-functions-database";

    // Provisioned throughput for the database.
    public int DatabaseThroughput { get; set; } = 400;

    // Request Options for database creation.
    public RequestOptions DatabaseRequestOptions { get; set; } = new();

    // Container properties for the container to be created.
    public ContainerProperties ContainerProperties { get; set; } = new();

    // Provisioned throughput for the container.
    public int ContainerThroughput { get; set; } = 400;

    // Request Options for container creation.
    public RequestOptions ContainerRequestOptions { get; set; } = new();

    // Name of the field property which will contain the embeddings
    public string EmbeddingKey { get; set; } = "embedding";

    // Name of the field property which will contain the text which is embedded.
    public string TextKey { get; set; } = "text";

    // Filters for query
    public ConcurrentDictionary<string, string> PreFilters { get; set; } = new();
}
