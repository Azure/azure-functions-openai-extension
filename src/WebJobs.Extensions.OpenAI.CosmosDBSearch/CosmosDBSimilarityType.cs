// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBSearch;

// Similarity metric to use with the index. Possible options are COS (cosine distance), L2 (Euclidean distance), and IP (inner product).
public enum CosmosDBSimilarityType
{
    // Cosine similarity
    [BsonElement("COS")]
    Cosine,

    // Inner Product similarity
    [BsonElement("IP")]
    InnerProduct,

    // Euclidean similarity
    [BsonElement("L2")]
    Euclidean
}

internal static class CosmosDBSimilarityTypeExtensions
{
    public static string GetCustomName(this CosmosDBSimilarityType type)
    {
        var attribute = type.GetType()
            .GetField(type.ToString())
            ?.GetCustomAttribute<BsonElementAttribute>();
        return attribute?.ElementName ?? type.ToString();
    }
}
