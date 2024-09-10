/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation.assistant;

/**
 * Assistant create request which is used to create a assistant.
 */
public class AssistantCreateRequest {

    private String id;
    private String instructions = "You are a helpful assistant.";
    private String chatStorageConnectionSetting;
    private String collectionName = "SampleChatState";

    public AssistantCreateRequest(String id) {
        this.id = id;
    }

    public AssistantCreateRequest(String id, String instructions) {
        this.id = id;

        if (!(instructions == null || instructions.isEmpty())) {
            this.instructions = instructions;
        }
    }

    /**
     * Gets the ID of the assistant to create.
     * 
     * @return The ID of the assistant to create.
     */
    public String getId() {
        return id;
    }

    /**
     * Sets the ID of the assistant to create.
     * 
     * @param id The ID of the assistant to create.
     */
    public void setId(String id) {
        this.id = id;
    }

    /**
     * Gets the instructions that are provided to assistant to follow.
     * 
     * @return The instructions that are provided to assistant to follow.
     */
    public String getInstructions() {
        return instructions;
    }

    /**
     * Sets the instructions that are provided to assistant to follow.
     * 
     * @param instructions The instructions that are provided to assistant to
     *                     follow.
     */
    public void setInstructions(String instructions) {
        this.instructions = instructions;
    }

    /**
     * Gets the configuration section name for the table settings for assistant chat
     * storage.
     * 
     * @return The configuration section name for the table settings for assistant
     *         chat storage.
     */
    public String getChatStorageConnectionSetting() {
        return chatStorageConnectionSetting;
    }

    /**
     * Sets the configuration section name for the table settings for assistant chat
     * storage.
     * 
     * @param chatStorageConnectionSetting The configuration section name for the
     *                                     table settings for assistant chat
     *                                     storage.
     */
    public void setChatStorageConnectionSetting(String chatStorageConnectionSetting) {
        this.chatStorageConnectionSetting = chatStorageConnectionSetting;
    }

    /**
     * Gets the table collection name for assistant chat storage.
     * 
     * @return The table collection name for assistant chat storage.
     */
    public String getCollectionName() {
        return collectionName;
    }

    /**
     * Sets the table collection name for assistant chat storage.
     * 
     * @param collectionName The table collection name for assistant
     *                                     chat storage.
     */
    public void setCollectionName(String collectionName) {
        this.collectionName = collectionName;
    }
}
