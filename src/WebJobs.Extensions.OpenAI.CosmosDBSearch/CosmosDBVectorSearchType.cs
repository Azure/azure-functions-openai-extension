// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using System.Reflection;
using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBSearch;

// Type of vector index to create. The options are vector-ivf and vector-hnsw.
public enum CosmosDBVectorSearchType
{
    // vector-ivf is available on all cluster tiers
    [BsonElement("vector-ivf")]
    VectorIVF,

    // vector-hnsw is available on M40 cluster tiers and higher.
    [BsonElement("vector-hnsw")]
    VectorHNSW
}

internal static class CosmosDBVectorSearchTypeExtensions
{
    public static string GetCustomName(this CosmosDBVectorSearchType type)
    {
        var attribute = type.GetType()
            .GetField(type.ToString())
            ?.GetCustomAttribute<BsonElementAttribute>();
        return attribute?.ElementName ?? type.ToString();
    }
}
