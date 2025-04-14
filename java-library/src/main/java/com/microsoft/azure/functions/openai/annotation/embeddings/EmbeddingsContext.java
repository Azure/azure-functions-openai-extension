/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation.embeddings;

import com.azure.ai.openai.models.Embeddings;
import java.util.List;

public class EmbeddingsContext {

    private List<String> input;
    private Embeddings response;
    private int count = 0;

    public List<String> getInput() {
        return input;
    }

    public void setInput(List<String> input) {
        this.input = input;
    }

    public Embeddings getResponse() {
        return response;
    }

    public void setResponse(Embeddings response) {
        this.response = response;
    }

    /**
     * Gets the number of embeddings that were returned in the response.
     *
     * @return The number of embeddings that were returned in the response.
     */
    public int getCount() {
        return this.response != null && this.response.getData() != null
                ? this.count : 0;
    }

}
