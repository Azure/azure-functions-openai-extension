// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WebJobs.Extensions.OpenAI;

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
    /// The input data is a URL that can be invoked to get the text.
    /// </summary>
    URL,
}
