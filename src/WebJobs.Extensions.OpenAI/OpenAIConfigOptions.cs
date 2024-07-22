// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

/// <summary>
/// Open AI Configuration Options used for reading host.json values.
/// </summary>
public class OpenAIConfigOptions
{
    /// <summary>
    /// The section of configuration related to search providers for semantic search.
    /// </summary>
    public IDictionary<string, object> SearchProvider { get; set; } = new Dictionary<string, object>();
}
