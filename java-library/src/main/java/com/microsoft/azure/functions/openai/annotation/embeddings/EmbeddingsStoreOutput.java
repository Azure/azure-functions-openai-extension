/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation.embeddings;

import com.microsoft.azure.functions.annotation.CustomBinding;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.PARAMETER)
@CustomBinding(direction = "out", name = "", type = "embeddingsStore")
public @interface EmbeddingsStoreOutput {

    /**
     * The variable name used in function.json.
     *
     * @return The variable name used in function.json.
     */
    String name();

    /**
     * The ID of the model to use.
     * Changing the default embeddings model is a breaking change, since any changes
     * will be stored in a vector database for lookup.
     * Changing the default model can cause the lookups to start misbehaving if they
     * don't match the data that was previously ingested into the vector database.
     *
     * @return The model ID.
     */
    String embeddingsModel() default "text-embedding-ada-002";

    /**
     * The maximum number of characters to chunk the input into.
     * At the time of writing, the maximum input tokens allowed for
     * second-generation input embedding models
     * like text-embedding-ada-002 is 8191. 1 token is ~4 chars in English, which
     * translates to roughly 32K
     * characters of English input that can fit into a single chunk.
     * 
     * @return The maximum number of characters to chunk the input into.
     */
    int maxChunkLength() default 8 * 1024;

    /**
     * The maximum number of characters to overlap between chunks.
     * 
     * @return The maximum number of characters to overlap between chunks.
     */
    int maxOverlap() default 128;

    /**
     * The input string to generate embeddings for.
     * 
     * @return The input string to generate embeddings for.
     */
    String input();

    /**
     * The input type.
     * 
     * @return The input type.
     */
    InputType inputType();

    /**
     * The name of the configuration section for AI service connectivity settings.
     * 
     * @return The name of the configuration section for AI service connectivity
     *         settings.
     */
    String aiConnectionName() default "";

    /**
     * The name of an app setting or environment variable which contains a
     * connection string value.
     * This property supports binding expressions.
     *
     * @return The store connection name.
     */
    String storeConnectionName();

    /**
     * The name of the collection or table to search.
     * This property supports binding expressions.
     *
     * @return The collection or table name.
     */
    String collection();
}
