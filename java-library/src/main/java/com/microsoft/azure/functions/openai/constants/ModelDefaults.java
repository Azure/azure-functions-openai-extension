/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.constants;

/**
 * Constants for default model values used throughout the library.
 * Centralizing these values makes it easier to update them across the codebase.
 * 
 * @since 1.0.0
 */
public final class ModelDefaults {

    /**
     * Private constructor to prevent instantiation.
     */
    private ModelDefaults() {
        // Utility class should not be instantiated
    }

    /**
     * The default chat completion model used for text generation.
     */
    public static final String DEFAULT_CHAT_MODEL = "gpt-3.5-turbo";

    /**
     * The default embeddings model used for vector embeddings.
     */
    public static final String DEFAULT_EMBEDDINGS_MODEL = "text-embedding-ada-002";
}
