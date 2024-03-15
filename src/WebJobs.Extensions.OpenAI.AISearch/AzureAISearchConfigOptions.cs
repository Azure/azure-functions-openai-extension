// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WebJobs.Extensions.OpenAI.AzureAISearch;

/// <summary>
/// Open AI Configuration Options used for reading host.json values.
/// </summary>
public class AzureAISearchConfigOptions
{
    
    public string IsSemanticSearchEnabled { get; set; } = "IsSemanticSearchEnabled";

    public string UseSemanticCaptions { get; set; } = "UseSemanticCaptions";

    public string VectorSearchDimensions { get; set; } = "vectorSearchDimensions";
}
