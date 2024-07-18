// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBNoSqlSearch;

/// <summary>
/// Open AI Configuration Options used for reading host.json values.
/// </summary>
public class CosmosDBNoSqlSearchConfigOptions
{
    // Preferred location for the cosmos client
    public string preferredlocations { get; set; } = null;

    // The application name to be used in the requests
    public string applicationName { get; set; } = "openai-functions-nosql";

    // Vector Embedding Policy for the Database.
    public VectorEmbeddingPolicy vectorEmbeddingPolicy { get; set; } = null;

    // Indexing Policy for the Database.
    public IndexingPolicy indexingPolicy { get; set; } = null;

    // Container Properties for the Database.
    public ContainerProperties containerProperties { get; set; } = null;

    // Database Properties for the Database.
    public DatabaseProperties databaseProperties { get; set; } = null;

    // Name of the field property which will contain the embeddings
    public string EmbeddingKey { get; set; } = "embedding";

    // Name of the field property which will contain the text which is embedded.
    public string TextKey { get; set; } = "text";

    // Filters for query
    public ConcurrentDictionary<string, string> PreFilters { get; set; } = new();
}
