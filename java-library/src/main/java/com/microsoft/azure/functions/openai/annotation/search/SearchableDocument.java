/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.microsoft.azure.functions.openai.annotation.search;


import com.microsoft.azure.functions.openai.annotation.embeddings.EmbeddingsContext;

/**
 * Searchable document which contains the title and embeddings context.
 */
public class SearchableDocument {
    /**
     * Title of the searchable document.
     */
    private final String title;

    /**
     * Connection info for the searchable document.
     */
    private ConnectionInfo connectionInfo;

    /**
     * Embeddings context that contains embeddings request and response from OpenAI for searchable document.
     */
    private EmbeddingsContext embeddingsContext;

    /**
     * Constructor for creating a searchable document with the given title.
     * @param title Title of the searchable document.
     */
    public SearchableDocument(String title) {
        this.title = title;
    }

    /**
     * Gets the connection info for the searchable document.
     * @return Connection info for the searchable document.
     */
    public ConnectionInfo getConnectionInfo() {
        return connectionInfo;
    }

    /**
     * Sets the connection info for the searchable document.
     * @param connectionInfo Connection info for the searchable document.
     */
    public void setConnectionInfo(ConnectionInfo connectionInfo) {
        this.connectionInfo = connectionInfo;
    }

    /**
     * Gets the title of the searchable document.
     * @return Title of the searchable document.
     */
    public String getTitle() {
        return title;
    }

    /**
     * Gets the embeddings context for the searchable document.
     * @return Embeddings context for the searchable document.
     */
    public EmbeddingsContext getEmbeddingsContext() {
        return embeddingsContext;
    }

    /**
     * Sets the embeddings context for the searchable document.
     * @param embeddingsContext Embeddings context for the searchable document.
     */
    public void setEmbeddingsContext(EmbeddingsContext embeddingsContext) {
        this.embeddingsContext = embeddingsContext;
    }
}
