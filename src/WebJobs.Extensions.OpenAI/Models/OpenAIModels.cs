// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

public static class OpenAIModels
{
    // Reference - https://platform.openai.com/docs/models

    // GPT 4 models

    /// <summary>
    /// The latest GPT-4 model with improved instruction following, JSON mode, reproducible outputs, parallel function calling, 
    /// and more. Returns a maximum of 4,096 output tokens. This preview model is not yet suited for production traffic. 
    /// </summary>
    public const string gpt_4_1106_preview = "gpt-4-1106-preview";

    /// <summary>
    /// Ability to understand images, in addition to all other GPT-4 Turbo capabilties. Returns a maximum of 4,096 output tokens.
    /// This is a preview model version and not suited yet for production traffic. 
    /// </summary>
    public const string gpt_4_vision_preview = "gpt-4-vision-preview";

    /// <summary>
    /// GPT 4, refer to https://platform.openai.com/docs/models/continuous-model-upgrades for exact model being pointed to
    /// </summary>
    public const string gpt_4 = "gpt-4";

    /// <summary>
    /// GPT 4 with 32k tokens context window, refer to https://platform.openai.com/docs/models/continuous-model-upgrades for exact model being pointed to
    /// </summary>
    public const string gpt_4_32k = "gpt-4-32k";

    /// <summary>
    /// Snapshot of gpt-4 from June 13th 2023 with improved function calling support.
    /// </summary>
    public const string gpt_4_0613 = "gpt-4-0613";

    /// <summary>
    /// Snapshot of gpt-4-32k from June 13th 2023 with improved function calling support.
    /// </summary>
    public const string gpt_4_32k_0613 = "gpt-4-32k-0613";

    // GPT 3.5 models

    /// <summary>
    /// GPT-3.5 Turbo model with improved instruction following, JSON mode, 
    /// reproducible outputs, parallel function calling, and more. Returns a maximum of 4,096 output tokens.
    /// </summary>
    public const string gpt_35_turbo_1106 = "gpt-3.5-turbo-1106";

    /// <summary>
    /// GPT 3 Turbo, refer to https://platform.openai.com/docs/models/continuous-model-upgrades for exact model being pointed to
    /// </summary>
    public const string gpt_35_turbo = "gpt-3.5-turbo";

    /// <summary>
    /// GPT 3 Turbo with 16K tokens context window, refer to https://platform.openai.com/docs/models/continuous-model-upgrades for exact model being pointed to
    /// </summary>
    public const string gpt_35_turbo_16k = "gpt-3.5-turbo-16k";

    /// <summary>
    /// Similar capabilities as GPT-3 era models. Compatible with legacy Completions endpoint and not Chat Completions.
    /// </summary>
    public const string gpt_35_turbo_instruct = "gpt-3.5-turbo-instruct";

    /// <summary>
    /// (Legacy) Snapshot of gpt-3.5-turbo from June 13th 2023.
    /// </summary>
    public const string gpt_35_turbo_0613 = "gpt-3.5-turbo-0613";

    /// <summary>
    /// (Legacy) Snapshot of gpt-3.5-16k-turbo from June 13th 2023.
    /// </summary>
    public const string gpt_35_turbo_16k_0613 = "gpt-3.5-turbo-16k-0613";

    /// <summary>
    /// (Legacy) Snapshot of gpt-3.5-turbo from March 1st 2023.
    /// </summary>
    public const string gpt_35_turbo_0301 = "gpt-3.5-turbo-0301";

    // Embeddings model

    /// <summary>
    /// Embeddings are a numerical representation of text that can be used to measure the relatedness between two pieces of text.
    /// Our second generation embedding model, text-embedding-ada-002 is a designed to replace the previous 16 first-generation
    /// embedding models at a fraction of the cost. Embeddings are useful for search, clustering, recommendations, 
    /// anomaly detection, and classification tasks.
    /// </summary>
    public const string text_embedding_ada_002 = "text-embedding-ada-002";
}
