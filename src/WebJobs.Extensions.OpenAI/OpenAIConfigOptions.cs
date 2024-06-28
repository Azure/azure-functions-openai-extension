// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI;

/// <summary>
/// Open AI Configuration Options used for reading host.json values.
/// </summary>
public class OpenAIConfigOptions
{
    /// <summary>
    /// Gets the storage connection name for storage account. Default to "AzureWebJobsStorage".
    /// </summary>
    public string StorageConnectionName { get; set; } = "AzureWebJobsStorage";

    /// <summary>
    /// Gets the storage collection name, which will be the name of the table. Default to "OpenAIChatState".
    /// </summary>
    public string? CollectionName { get; set; } = "OpenAIChatState";

    /// <summary>
    /// The section of configuration related to search providers for semantic search.
    /// </summary>
    public IDictionary<string, object> SearchProvider { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets the name of the storage account.
    /// </summary>
    public string? StorageAccountUri { get; set; }
}
