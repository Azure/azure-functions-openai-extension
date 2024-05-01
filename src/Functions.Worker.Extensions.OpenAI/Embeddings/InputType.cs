// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;

/// <summary>
/// Options for interpreting input binding data.
/// </summary>
public enum InputType
{
    /// <summary>
    /// The input data is raw text.
    /// </summary>
    RawText,

    /// <summary>
    /// The input data is a file path that contains the text.
    /// </summary>
    FilePath,

    /// <summary>
    /// The input data is a URL that contains the text.
    /// </summary>
    URL
}
