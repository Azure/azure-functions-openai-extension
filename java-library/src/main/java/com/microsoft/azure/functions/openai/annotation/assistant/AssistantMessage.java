/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation.assistant;

/**
 * <p>
 * Chat Message Entity which contains the content of the message, the role of the chat agent, and the name of the calling function if applicable.
 * </p>
 */
public class AssistantMessage {

    private String content;
    private String role;
    private String toolCalls;

    /**
     * Initializes a new instance of the AssistantMessage class.
     *
     * @param content   The content of the message.
     * @param role      The role of the chat agent.
     * @param toolCalls The toolCalls of the calling function if applicable.
     */
    public AssistantMessage(String content, String role, String toolCalls) {
        this.content = content;
        this.role = role;
        this.toolCalls = toolCalls;
    }

    /**
     * Gets the content of the message.
     * 
     * @return The content of the message.
     */
    public String getContent() {
        return content;
    }

    /**
     * Sets the content of the message.
     * 
     * @param content The content of the message.
     */
    public void setContent(String content) {
        this.content = content;
    }

    /**
     * Gets the role of the chat agent.
     * 
     * @return The role of the chat agent.
     */
    public String getRole() {
        return role;
    }

    /**
     * Sets the role of the chat agent.
     * 
     * @param role The role of the chat agent.
     */
    public void setRole(String role) {
        this.role = role;
    }

    /**
     * Gets the toolCalls of the calling function if applicable.
     * 
     * @return The toolCalls of the calling function if applicable.
     */
    public String getToolCalls() {
        return toolCalls;
    }

    /**
     * Sets the toolCalls of the calling function if applicable.
     * 
     * @param toolCalls The toolCalls of the calling function if applicable.
     */
    public void setToolCalls(String toolCalls) {
        this.toolCalls = toolCalls;
    }
}
