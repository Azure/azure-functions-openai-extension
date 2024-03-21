/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation;

import java.util.List;

/**
 * <p>
 * Assistant state.
 * </p>
 */
public class AssistantState {
    private String id;
    private boolean exists;
    private String status;
    private String createdAt;
    private String lastUpdatedAt;
    private int totalMessages;
    private int totalTokens;
    private List<ChatMessage> recentMessages;

    public AssistantState(String id, boolean exists, String status,
                        String createdAt, String lastUpdatedAt,
                        int totalMessages, int totalTokens, List<ChatMessage> recentMessages) {
        this.id = id;
        this.exists = exists;
        this.status = status;
        this.createdAt = createdAt;
        this.lastUpdatedAt = lastUpdatedAt;
        this.totalMessages = totalMessages;
        this.totalTokens = totalTokens;
        this.recentMessages = recentMessages;
    }

	/**
     * Gets the ID of the assistant.
     * 
     * @return The ID of the assistant.
     */
    public String getId() {
        return id;
    }

    /**
     * Gets if assistant exists.
     * 
     * @return If assistant exists.
     */
    public boolean isExists() {
        return exists;
    }

    /**
     * Gets status of assistant. Options are Uninitialzied, Active, or Expired.
     * 
     * @return The status of the assistant.
     */
    public String getStatus() {
        return status;
    }

    /**
     * Gets timestamp of when assistant is created.
     * 
     * @return The timestamp of when assistant is created.
     */ 
    public String getCreatedAt() {
        return createdAt;
    }

    /**
     * Gets timestamp of when assistant is last updated.
     * 
     * @return The timestamp of when assistant is last updated.
     */
    public String getLastUpdatedAt() {
        return lastUpdatedAt;
    }

    /**
     * Gets number of total messages for assistant.
     * 
     * @return The number of total messages for assistant.
     */
    public int getTotalMessages() {
        return totalMessages;
    }

    /**
     * Gets number of total tokens for assistant.
     * 
     * @return The number of total tokens for assistant.
     */
    public int getTotalTokens() {
        return totalTokens;
    }

    /**
     * Gets a list of the recent messages from the assistant.
     * 
     * @return A list of the recent messages from the assistant.
     */
    public List<ChatMessage> getRecentMessages() {
        return recentMessages;
    }

    /**
     * Sets the ID of the assistant.
     * 
     * @param id The ID of the assistant.
     */
    public void setId(String id) {
        this.id = id;
    }

    /**
     * Sets if assistant exists.
     * 
     * @param exists If assistant exists.
     */
    public void setExists(boolean exists) {
        this.exists = exists;
    }

    /**
     * Sets status of assistant. Options are Uninitialzied, Active, or Expired.
     * 
     * @param status The status of the assistant.
     */
    public void setStatus(String status) {
        this.status = status;
    }

    /**
     * Sets timestamp of when assistant is created.
     * 
     * @param createdAt The timestamp of when assistant is created.
     */
    public void setCreatedAt(String createdAt) {
        this.createdAt = createdAt;
    }

    /**
     * Sets timestamp of when assistant is last updated.
     * 
     * @param lastUpdatedAt The timestamp of when assistant is last updated.
     */
    public void setLastUpdatedAt(String lastUpdatedAt) {
        this.lastUpdatedAt = lastUpdatedAt;
    }

    /**
     * Sets number of total messages for assistant.
     * 
     * @param totalMessages The number of total messages for assistant.
     */
    public void setTotalMessages(int totalMessages) {
        this.totalMessages = totalMessages;
    }

    /**
     * Sets number of total tokens for assistant.
     * 
     * @param totalTokens The number of total tokens for assistant.
     */
    public void setTotalTokens(int totalTokens) {
        this.totalTokens = totalTokens;
    }

    /**
     * Sets a list of the recent messages from the assistant.
     * 
     * @param recentMessages A list of the recent messages from the assistant.
     */
    public void setRecentMessages(List<ChatMessage> recentMessages) {
        this.recentMessages = recentMessages;
    }
    
}
