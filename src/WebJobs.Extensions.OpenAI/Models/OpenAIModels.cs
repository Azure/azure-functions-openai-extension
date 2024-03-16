// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

static class OpenAIModels
{
    // Reference - https://platform.openai.com/docs/models

    /// <summary>
    /// GPT 3 Turbo, refer to https://platform.openai.com/docs/models/continuous-model-upgrades for exact model being pointed to
    /// </summary>
    internal const string Gpt_35_Turbo = "gpt-3.5-turbo";

    /// <summary>
    /// The default embeddings model, currently pointing to text-embedding-3-small
    /// </summary>
    /// <remarks>
    /// Changing the default embeddings model is a breaking change, since any changes will be stored in a vector database for lookup. Changing the default model can cause the lookups to start misbehaving if they don't match the data that was previously ingested into the vector database.
    /// </remarks>
    internal const string DefaultEmbeddingsModel = "text-embedding-3-small";
}
