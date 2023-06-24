// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WebJobs.Extensions.OpenAI.Search;

/// <summary>
/// Represents the results of a semantic search.
/// </summary>
public class SearchResult
{
    public SearchResult(string sourceName, string snippet)
    {
        if (snippet == null)
        {
            throw new ArgumentNullException(nameof(snippet));
        }

        if (sourceName == null)
        {
            throw new ArgumentNullException(nameof(sourceName));
        }

        this.SourceName = Normalize(sourceName);
        this.NormalizedSnippet = Normalize(snippet);
    }

    /// <summary>
    /// Gets or sets the name of source from which the results were pulled. For example, this may be the name of a file.
    /// </summary>
    public string SourceName { get; set; }

    /// <summary>
    /// Gets or sets the snippet of text that was found in the source.
    /// </summary>
    public string NormalizedSnippet { get; set; }

    static string Normalize(string snippet)
    {
        // NOTE: .NET 6 has an optimized string.ReplaceLineEndings method. At the time of writing, we're targeting
        //       .NET Standard, so we don't have access to that more efficient implementation.
        return snippet.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
    }

    /// <summary>
    /// Returns a formatted version of the search result.
    /// </summary>
    public override string ToString()
    {
        return this.SourceName + ": " + this.NormalizedSnippet;
    }
}