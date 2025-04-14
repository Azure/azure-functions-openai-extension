/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation.assistant;

import com.microsoft.azure.functions.annotation.CustomBinding;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * <p>
 * Assistant post input attribute which is used to update the assistant.
 * </p>
 * 
 * @since 1.0.0
 */
@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.PARAMETER)
@CustomBinding(direction = "in", name = "", type = "assistantPost")
public @interface AssistantPost {

    /**
     * The default storage account setting for the table storage account.
     * This constant is used to specify the connection string for the table storage
     * account
     * where chat data will be stored.
     */
    String DEFAULT_CHATSTORAGE = "AzureWebJobsStorage";

    /**
     * The default collection name for the table storage account.
     * This constant is used to specify the collection name for the table storage
     * account
     * where chat data will be stored.
     */
    String DEFAULT_COLLECTION = "ChatState";

    /**
     * The variable name used in function.json.
     * 
     * @return The variable name used in function.json.
     */
    String name();

    /**
     * The ID of the Assistant to query.
     * 
     * @return The ID of the Assistant to query.
     */
    String id();

    /**
     * The OpenAI chat model to use.
     * When using Azure OpenAI, this should be the name of the model deployment.
     * 
     * @return The OpenAI chat model to use.
     */
    String chatModel();

    /**
     * The user message that user has entered for assistant to respond to.
     * 
     * @return The user message that user has entered for assistant to respond to.
     */
    String userMessage();

    /**
     * The name of the configuration section for AI service connectivity settings.
     * 
     * @return The name of the configuration section for AI service connectivity
     *         settings.
     */
    String aiConnectionName() default "";

    /**
     * The configuration section name for the table settings for assistant chat
     * storage.
     * 
     * @return The configuration section name for the table settings for assistant
     *         chat storage. By default, it returns {@code DEFAULT_CHATSTORAGE}.
     */
    String chatStorageConnectionSetting() default DEFAULT_CHATSTORAGE;

    /**
     * The table collection name for assistant chat storage.
     * 
     * @return the table collection name for assistant chat storage.By default, it
     *         returns {@code DEFAULT_COLLECTION}.
     */
    String collectionName() default DEFAULT_COLLECTION;

    /**
     * The sampling temperature to use, between 0 and 2. Higher values like 0.8 will
     * make the output
     * more random, while lower values like 0.2 will make it more focused and
     * deterministic.
     * It's generally recommended to use this or {@link #topP()} but not both.
     *
     * @return The sampling temperature value.
     */
    String temperature() default "0.5";

    /**
     * An alternative to sampling with temperature, called nucleus sampling, where
     * the model considers
     * the results of the tokens with top_p probability mass. So 0.1 means only the
     * tokens comprising the top 10%
     * probability mass are considered.
     * It's generally recommended to use this or {@link #temperature()} but not
     * both.
     *
     * @return The topP value.
     */
    String topP() default "";

    /**
     * The maximum number of tokens to generate in the completion.
     * The token count of your prompt plus max_tokens cannot exceed the model's
     * context length.
     * Most models have a context length of 2048 tokens (except for the newest
     * models, which support 4096).
     *
     * @return The maxTokens value.
     */
    String maxTokens() default "100";
}
