// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.Kusto;
using Xunit;

namespace WebJobsOpenAIUnitTests;

public class KustoSearchProviderTests
{
    [Theory]
    [InlineData("Documents", "['Documents']")]
    [InlineData("  Documents  ", "['Documents']")]
    [InlineData("my_table-1.v2", "['my_table-1.v2']")]
    [InlineData("My Table", "['My Table']")]
    [InlineData("Données", "['Données']")]
    public void GetQuotedTableName_ValidName_ReturnsQuotedIdentifier(string input, string expected)
    {
        Assert.Equal(expected, KustoSearchProvider.GetQuotedTableName(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Documents']")]
    [InlineData("Documents\"")]
    [InlineData("Documents|take 1")]
    [InlineData("Documents|union(SyntheticPrivateDocuments)")]
    [InlineData("Documents\nOther")]
    [InlineData("Documents\tOther")]
    [InlineData("Documents;")]
    [InlineData("Documents/")]
    [InlineData("Documents\\")]
    [InlineData("Documents()")]
    [InlineData("Documents,Other")]
    [InlineData("Documents\u200B")]
    [InlineData("Documents\u2028Other")]
    [InlineData("Documente\u0301")]
    public void GetQuotedTableName_InvalidName_Throws(string? input)
    {
        Assert.Throws<InvalidOperationException>(() => KustoSearchProvider.GetQuotedTableName(input));
    }

    [Fact]
    public void GetQuotedTableName_MaxLength_Succeeds()
    {
        string name = new('a', 1024);
        Assert.Equal($"['{name}']", KustoSearchProvider.GetQuotedTableName(name));
    }

    [Fact]
    public void GetQuotedTableName_TooLong_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => KustoSearchProvider.GetQuotedTableName(new string('a', 1025)));
    }
}
