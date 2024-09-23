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
    String DEFAULT_COLLECTION = "SampleChatState";

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
    String model();

    /**
     * The user message that user has entered for assistant to respond to.
     * 
     * @return The user message that user has entered for assistant to respond to.
     */
    String userMessage();

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
}
