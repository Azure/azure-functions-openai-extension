/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.microsoft.azure.functions.openai.annotation.search;

import com.azure.ai.openai.models.ChatCompletions;
import com.microsoft.azure.functions.openai.annotation.embeddings.EmbeddingsContext;

public class SemanticSearchContext {

    private EmbeddingsContext embeddings;

    private ChatCompletions chat;

    public EmbeddingsContext getEmbeddings() {
        return embeddings;
    }

    public void setEmbeddings(EmbeddingsContext embeddings) {
        this.embeddings = embeddings;
    }

    public ChatCompletions getChat() {
        return chat;
    }

    public void setChat(ChatCompletions chat) {
        this.chat = chat;
    }

    public String getResponse() {
        if (this.chat != null && this.chat.getChoices() != null && !this.chat.getChoices().isEmpty()) {
            return this.chat.getChoices().get(this.chat.getChoices().size() - 1).getMessage().getContent();
        } else {
            return "";
        }
    }
    
}
