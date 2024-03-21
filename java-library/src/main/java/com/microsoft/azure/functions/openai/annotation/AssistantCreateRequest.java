/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation;

import java.time.LocalDateTime;

/**
 * Assistant create request which is used to create a assistant.
 */
public class AssistantCreateRequest {

    private String id;
    private String instructions = "You are a helpful assistant.";
    private LocalDateTime expiresAt;
    
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
     * @param instructions The instructions that are provided to assistant to follow.
     */
    public void setInstructions(String instructions) {
        this.instructions = instructions;
    }

    /**
     * Gets time when assistant request is set to expire
     * 
     * @return The time when assistant request is set to expire
     */
    public LocalDateTime getExpiresAt() {
        return expiresAt;
    }

    /**
     * Sets time when assistant request is set to expire
     * 
     * @param expiresAt The time when assistant request is set to expire
     */
    public void setExpiresAt(LocalDateTime expiresAt) {
        this.expiresAt = expiresAt;
    }   
    
}
