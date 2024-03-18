/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation;

/**
 * Assistant post request which is used to relay post requests.
 */
public class AssistantPostRequest {

    private String userMessage;
    private String id = "";
    private String model;

    /**
     * Initializes a new instance of the AssistantPostRequest class.
     */
    public AssistantPostRequest(String userMessage, String id, String model) {
        this.userMessage = userMessage;
        this.id = id;
        this.model = model;
    }

    /**
     * Initializes a new instance of the AssistantPostRequest class.
     */
    public AssistantPostRequest(String userMessage, String id) {
        this.userMessage = userMessage;
        this.id = id;
    }

    /**
     * Gets user message that user has entered for assistant to respond to.
     */
    public String getUserMessage() {
        return userMessage;
    }

    /**
     * Sets user message that user has entered for assistant to respond to.
     */
    public void setUserMessage(String userMessage) {
        this.userMessage = userMessage;
    }

    /**
     * Gets the ID of the assistant to update.
     */
    public String getId() {
        return id;
    }

    /**
     * Sets the ID of the assistant to update.
     */
    public void setId(String id) {
        this.id = id;
    }

    /**
     * Gets the OpenAI chat model to use.
     * <p>
     * When using Azure OpenAI, then should be the name of the model <em>deployment</em>.
     * </p>
     */
    public String getModel() {
        return model;
    }

    /**
     * Sets the OpenAI chat model to use.
     * <p>
     * When using Azure OpenAI, then should be the name of the model <em>deployment</em>.
     * </p>
     */
    public void setModel(String model) {
        this.model = model;
    }
}
