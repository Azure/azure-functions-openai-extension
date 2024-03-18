/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation;

import java.time.LocalDateTime;
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
    private LocalDateTime createdAt;
    private LocalDateTime lastUpdatedAt;
    private int totalMessages;
    private int totalTokens;
    private List<ChatMessage> recentMessages;

    public AssistantState(String id, boolean exists, String status,
                        LocalDateTime createdAt, LocalDateTime lastUpdatedAt,
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
     */
    public String getId() {
        return id;
    }

    /**
     * Gets if assistant exists.
     */
    public boolean isExists() {
        return exists;
    }

    /**
     * Gets status of assistant. Options are Uninitialzied, Active, or Expired.
     */
    public String getStatus() {
        return status;
    }

    /**
     * Gets timestamp of when assistant is created.
     */ 
    public LocalDateTime getCreatedAt() {
        return createdAt;
    }

    /**
     * Gets timestamp of when assistant is last updated.
     */
    public LocalDateTime getLastUpdatedAt() {
        return lastUpdatedAt;
    }

    /**
     * Gets number of total messages for assistant.
     */
    public int getTotalMessages() {
        return totalMessages;
    }

    /**
     * Gets number of total tokens for assistant.
     */
    public int getTotalTokens() {
        return totalTokens;
    }

    /**
     * Gets a list of the recent messages from the assistant.
     */
    public List<ChatMessage> getRecentMessages() {
        return recentMessages;
    }

    /**
     * Sets the ID of the assistant.
     */
    public void setId(String id) {
        this.id = id;
    }

    /**
     * Sets if assistant exists.
     */
    public void setExists(boolean exists) {
        this.exists = exists;
    }

    /**
     * Sets status of assistant. Options are Uninitialzied, Active, or Expired.
     */
    public void setStatus(String status) {
        this.status = status;
    }

    /**
     * Sets timestamp of when assistant is created.
     */
    public void setCreatedAt(LocalDateTime createdAt) {
        this.createdAt = createdAt;
    }

    /**
     * Sets timestamp of when assistant is last updated.
     */
    public void setLastUpdatedAt(LocalDateTime lastUpdatedAt) {
        this.lastUpdatedAt = lastUpdatedAt;
    }

    /**
     * Sets number of total messages for assistant.
     */
    public void setTotalMessages(int totalMessages) {
        this.totalMessages = totalMessages;
    }

    /**
     * Sets number of total tokens for assistant.
     */
    public void setTotalTokens(int totalTokens) {
        this.totalTokens = totalTokens;
    }

    /**
     * Sets a list of the recent messages from the assistant.
     */
    public void setRecentMessages(List<ChatMessage> recentMessages) {
        this.recentMessages = recentMessages;
    }
    
}
