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
    /// Similar capabilities as GPT-3 era models. Compatible with legacy Completions endpoint and not Chat Completions.
    /// </summary>
    internal const string Gpt_35_Turbo_Instruct = "gpt-3.5-turbo-instruct";

    /// <summary>
    /// Embeddings are a numerical representation of text that can be used to measure the relatedness between two pieces of text.
    /// Our second generation embedding model, text-embedding-ada-002 is a designed to replace the previous 16 first-generation
    /// embedding models at a fraction of the cost. Embeddings are useful for search, clustering, recommendations, 
    /// anomaly detection, and classification tasks.
    /// </summary>
    internal const string Text_Embedding_Ada_002 = "text-embedding-ada-002";
}
