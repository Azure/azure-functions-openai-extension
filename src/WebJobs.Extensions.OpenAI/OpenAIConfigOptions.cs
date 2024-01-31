// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WebJobs.Extensions.OpenAI;

/// <summary>
/// Open AI Configuration Options used for reading host.json values.
/// </summary>
public class OpenAIConfigOptions
{
    /// <summary>
    /// Gets the storage connection name for storage account.
    /// </summary>
    public string StorageConnectionName { get; set; }

    /// <summary>
    /// Gets the storage collection name, which will be the name of the table.
    /// </summary>
    public string? CollectionName { get; set; } = "ChatBotRequests";
}
