﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBSearch;

/// <summary>
/// Open AI Configuration Options used for reading host.json values.
/// </summary>
public class CosmosDBSearchConfigOptions
{
<<<<<<< HEAD
<<<<<<< HEAD
    // Database name for the vector store
    public string DatabaseName { get; set; } = "openai-functions-database";

    // Index name for your vector store collection
    public string IndexName { get; set; } = "openai-functions-index";

    // Application name for the client for tracking and logging
    public string ApplicationName { get; set; } = "openai-function-appName";
=======
=======
    // Database name for the vector store
    public string DatabaseName { get; set; } = "openai-functions-database";

>>>>>>> a3290f8 (Fixing build issues)
    // Index name for your vector store collection
    public string IndexName { get; set; } = "openai-functions-index";

    // Application name for the client for tracking and logging
<<<<<<< HEAD
    public string ApplicationName { get; set; } = "functionsAppName";
>>>>>>> 94c2ade (resolving comments)
=======
    public string ApplicationName { get; set; } = "openai-function-appName";
>>>>>>> a3290f8 (Fixing build issues)

    // Name of the field property which will contain the embeddings
    public string EmbeddingKey { get; set; } = "embedding";

    // Name of the field property which will contain the text which is embedded.
    public string TextKey { get; set; } = "text";

    // Number of dimensions for vector similarity. The maximum number of supported dimensions is 2000.
    public int VectorSearchDimensions { get; set; } = 1536;

    // This integer is the number of clusters that the inverted file (IVF) index uses to group the vector data
    public int NumLists { get; set; } = 1;

    /// <summary>
    /// Type of vector index to create.
    ///     Possible options are:
    ///         - vector-ivf
    ///         - vector-hnsw
    /// </summary>
<<<<<<< HEAD
<<<<<<< HEAD
    public string Kind { get; set; } = "vector-ivf";
=======
    public String Kind { get; set; } = "vector-ivf";
>>>>>>> ad8a4dd (Updating readme file)
=======
    public string Kind { get; set; } = "vector-ivf";
>>>>>>> 94c2ade (resolving comments)

    /// <summary>
    /// Similarity metric to use with the IVF index.
    ///     Possible options are:
    ///         - COS (cosine distance, default),
    ///         - L2 (Euclidean distance), and
    ///         - IP (inner product).
    /// </summary>
<<<<<<< HEAD
<<<<<<< HEAD
    public string Similarity { get; set; } = "COS";
=======
    public String Similarity { get; set; } = "COS";
>>>>>>> ad8a4dd (Updating readme file)
=======
    public string Similarity { get; set; } = "COS";
>>>>>>> 94c2ade (resolving comments)

    /// <summary>
    /// The max number of connections per layer (16 by default, minimum value is 2, maximum value is
    /// 100). Higher m is suitable for datasets with high dimensionality and/or high accuracy requirements.
    /// </summary>
    public int NumberOfConnections { get; set; } = 16;

    /// <summary>
    /// The size of the dynamic candidate list for constructing the graph (64 by default, minimum value is 4,
    /// maximum value is 1000). Higher ef_construction will result in better index quality and higher accuracy, but it will
    /// also increase the time required to build the index. EfConstruction has to be at least 2 * m
    /// </summary>
    public int EfConstruction { get; set; } = 64;

    /// <summary>
    /// The size of the dynamic candidate list for search (40 by default). A higher value provides better recall at
    /// the cost of speed.
    /// </summary>
    public int EfSearch { get; set; } = 40;
}
