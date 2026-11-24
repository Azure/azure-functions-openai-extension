// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.WebJobs.Extensions.OpenAI;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.AzureAISearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBNoSqlSearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.CosmosDBSearch;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Kusto;
using Xunit;

namespace WebJobsOpenAIUnitTests;

/// <summary>
/// Verifies instance fields on shared service types.
/// </summary>
public class SingletonStateTests
{
    // Mutable fields that are intentionally allowed, with the reason. Keep this list short and justified.
    static readonly Dictionary<(Type Type, string Field), string> AllowedMutableFields = new()
    {
        // ISearchProvider.Name has a public setter by contract.
        [(typeof(AzureAISearchProvider), "<Name>k__BackingField")] = "ISearchProvider.Name",
        [(typeof(CosmosDBSearchProvider), "<Name>k__BackingField")] = "ISearchProvider.Name",
        [(typeof(CosmosDBNoSqlSearchProvider), "<Name>k__BackingField")] = "ISearchProvider.Name",
        [(typeof(KustoSearchProvider), "<Name>k__BackingField")] = "ISearchProvider.Name",
    };

    public static TheoryData<Type> SingletonTypes => new()
    {
        typeof(AzureAISearchProvider),
        typeof(CosmosDBSearchProvider),
        typeof(CosmosDBNoSqlSearchProvider),
        typeof(KustoSearchProvider),
        typeof(OpenAIClientFactory),
    };

    [Theory]
    [MemberData(nameof(SingletonTypes))]
    public void SharedSingleton_HasNoMutableInstanceFields(Type type)
    {
        List<string> mutableFields = new();
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (!field.IsInitOnly && !AllowedMutableFields.ContainsKey((current, field.Name)))
                {
                    mutableFields.Add($"{current.Name}.{field.Name} ({field.FieldType.Name})");
                }
            }
        }

        Assert.True(
            mutableFields.Count == 0,
            $"{type.Name} is a shared singleton but has mutable instance fields: {string.Join(", ", mutableFields)}. " +
            "Make them readonly, or resolve per-connection settings into locals instead of instance fields.");
    }
}
