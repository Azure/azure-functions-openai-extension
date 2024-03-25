/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation.textcompletion;

/**
 * Text Completion Response Class
 */
public class TextCompletionResponse {
    private String content;
    private int totalTokens;

    /**
     * Initializes a new instance of the TextCompletionResponse class.
     *
     * @param content     The text completion message content.
     * @param totalTokens The total token usage.
     */
    public TextCompletionResponse(String content, int totalTokens) {
        this.content = content;
        this.totalTokens = totalTokens;
    }

    /**
     * Gets the text completion message content.
     *
     * @return The content.
     */
    public String getContent() {
        return content;
    }

    /**
     * Sets the text completion message content.
     *
     * @param content The content to set.
     */
    public void setContent(String content) {
        this.content = content;
    }

    /**
     * Gets the total token usage.
     *
     * @return The total tokens.
     */
    public int getTotalTokens() {
        return totalTokens;
    }

    /**
     * Sets the total token usage.
     *
     * @param totalTokens The total tokens to set.
     */
    public void setTotalTokens(int totalTokens) {
        this.totalTokens = totalTokens;
    }
}
