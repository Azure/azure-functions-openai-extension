/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.azfs;

import com.microsoft.azure.functions.*;
import org.json.JSONObject;

import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.openai.annotation.embeddings.EmbeddingsInput;
import com.microsoft.azure.functions.openai.annotation.embeddings.InputType;

/**
 * Azure Functions embeddings sample to generate embeddings.
 */
public class EmbeddingsGenerator {

    /**
     * Example showing how to use the EmbeddingsInput input binding to generate embeddings
     * for a raw text string.
     */
    @FunctionName("GenerateEmbeddingsHttpRequest")
    public HttpResponseMessage generateEmbeddingsHttpRequest(
            @HttpTrigger(
                name = "req", 
                methods = {HttpMethod.POST},
                authLevel = AuthorizationLevel.ANONYMOUS,
                route = "embeddings")
            HttpRequestMessage<EmbeddingsRequest> request,
            @EmbeddingsInput(name = "Embeddings", input = "{RawText}", inputType = InputType.RawText, embeddings_model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%") String embeddingsContext,
            final ExecutionContext context) {

        if (request.getBody() == null) 
        {
            throw new IllegalArgumentException(
                    "Invalid request body. Make sure that you pass in {\"rawText\": value } as the request body.");
        }

        JSONObject embeddingsContextJsonObject = new JSONObject(embeddingsContext);

        context.getLogger().info(String.format("Received %d embedding(s) for input text containing %s characters.",
                embeddingsContextJsonObject.get("count"),
                request.getBody().getRawText().length()));

        // TODO: Store the embeddings into a database or other storage.
        return request.createResponseBuilder(HttpStatus.ACCEPTED)
                .header("Content-Type", "application/json")
                .build();
    }

    /**
     * Example showing how to use the EmbeddingsInput input binding to generate embeddings
     * for text contained in a file on the file system.
     */
    @FunctionName("GenerateEmbeddingsHttpFilePath")
    public HttpResponseMessage generateEmbeddingsHttpFilePath(
        @HttpTrigger(
            name = "req", 
            methods = {HttpMethod.POST},
            authLevel = AuthorizationLevel.ANONYMOUS,
            route = "embeddings-from-file")
        HttpRequestMessage<EmbeddingsRequest> request,
        @EmbeddingsInput(name = "Embeddings", input = "{FilePath}", inputType = InputType.FilePath, maxChunkLength = 512, embeddings_model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%") String embeddingsContext,
        final ExecutionContext context) {

        if (request.getBody() == null) 
        {
            throw new IllegalArgumentException(
                    "Invalid request body. Make sure that you pass in {\"filePath\": value } as the request body.");
        }

        JSONObject embeddingsContextJsonObject = new JSONObject(embeddingsContext);

        context.getLogger().info(String.format("Received %d embedding(s) for input file %s.",
                embeddingsContextJsonObject.get("count"),
                request.getBody().getFilePath()));

        // TODO: Store the embeddings into a database or other storage.
        return request.createResponseBuilder(HttpStatus.ACCEPTED)
                .header("Content-Type", "application/json")
                .build();
    }

    /**
     * Example showing how to use the EmbeddingsInput input binding to generate embeddings
     * for text contained in a file on the file system.
     */
    @FunctionName("GenerateEmbeddingsHttpUrl")
    public HttpResponseMessage generateEmbeddingsHttpUrl(
        @HttpTrigger(
            name = "req", 
            methods = {HttpMethod.POST},
            authLevel = AuthorizationLevel.ANONYMOUS,
            route = "embeddings-from-url")
        HttpRequestMessage<EmbeddingsRequest> request,
        @EmbeddingsInput(name = "Embeddings", input = "{Url}", inputType = InputType.Url, maxChunkLength = 512, embeddings_model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%") String embeddingsContext,
        final ExecutionContext context) {

        if (request.getBody() == null) 
        {
            throw new IllegalArgumentException(
                    "Invalid request body. Make sure that you pass in {\"url\": value } as the request body.");
        }

        JSONObject embeddingsContextJsonObject = new JSONObject(embeddingsContext);

        context.getLogger().info(String.format("Received %d embedding(s) for input url %s.",
                embeddingsContextJsonObject.get("count"),
                request.getBody().getUrl()));

        // TODO: Store the embeddings into a database or other storage.
        return request.createResponseBuilder(HttpStatus.ACCEPTED)
                .header("Content-Type", "application/json")
                .build();
    }

    public class EmbeddingsRequest {
        private String rawText;
        private String filePath;
        private String url;

        public String getRawText() {
            return rawText;
        }

        public void setRawText(String rawText) {
            this.rawText = rawText;
        }

        public String getFilePath() {
            return filePath;
        }

        public void setFilePath(String filePath) {
            this.filePath = filePath;
        }

        public String getUrl() {
            return url;
        }

        public void setUrl(String url) {
            this.url = url;
        }
    }

}
