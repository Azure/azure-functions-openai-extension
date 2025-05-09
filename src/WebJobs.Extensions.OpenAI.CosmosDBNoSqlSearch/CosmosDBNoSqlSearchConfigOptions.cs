// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBNoSqlSearch;

/// <summary>
/// Open AI Configuration Options used for reading host.json values.
/// </summary>
public class CosmosDBNoSqlSearchConfigOptions
{
    // The application name to be used in the requests
    public string ApplicationName { get; set; } = "OpenAI-Functions-CDBNoSql-VectorStore";

    // The vector data type for the embeddings, value can be int8, int16, float32
    public VectorDataType VectorDataType { get; set; } = VectorDataType.Float32;

    // The distance function to be used to calculate the similarity between vectors, value can be cosine, euclidean, dotproduct
    public DistanceFunction VectorDistanceFunction { get; set; } = DistanceFunction.Cosine;

    // The dimensions for the vectors.
    public int VectorDimensions { get; set; } = 1536;

    // The vector index type, value can be flat, quantizedFlat, diskAnn
    public VectorIndexType VectorIndexType { get; set; } = VectorIndexType.QuantizedFlat;

    // Database Name for the vector store.
    public string DatabaseName { get; set; } = "openai-functions-database";

    // Provisioned throughput for the database.
    public int DatabaseThroughput { get; set; } = 400;

    // Provisioned throughput for the container.
    public int ContainerThroughput { get; set; } = 400;

    // Name of the field property which will contain the embeddings
    public string EmbeddingKey { get; set; } = "embedding";

    // Name of the field property which will contain the text which is embedded.
    public string TextKey { get; set; } = "text";

    // Where filter for query.
    public string WhereFilterClause { get; set; } = "";

    // Limit Offset filter for query.
    public string LimitOffsetFilterClause { get; set; } = "";
}
