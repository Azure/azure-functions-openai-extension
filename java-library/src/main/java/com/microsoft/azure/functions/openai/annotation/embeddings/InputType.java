/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation.embeddings;


/**
 * <p>
 * Options for interpreting embeddings input binding data.
 * </p>
 */
public enum InputType {
    /**
     * The input data is raw text.
     */
    RawText,

    /**
     * The input data is a file path that contains the text.
     */
    FilePath,

    /**
     * The input data is a URL.
     */
    Url
}
