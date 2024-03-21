// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.AzureAISearch;

/// <summary>
/// Open AI Configuration Options used for reading host.json values.
/// </summary>
public class AzureAISearchConfigOptions
{
    public bool IsSemanticSearchEnabled { get; set; }

    public bool UseSemanticCaptions { get; set; }

    public int VectorSearchDimensions { get; set; } = 1536;
}
