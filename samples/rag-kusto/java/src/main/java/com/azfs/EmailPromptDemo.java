/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.azfs;

import java.net.MalformedURLException;
import java.nio.file.Paths;

import org.json.JSONObject;

import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.openai.annotation.embeddings.EmbeddingsStoreOutput;
import com.microsoft.azure.functions.openai.annotation.embeddings.InputType;
import com.microsoft.azure.functions.openai.annotation.search.SearchableDocument;
import com.microsoft.azure.functions.openai.annotation.search.SemanticSearch;
import com.sun.jndi.toolkit.url.Uri;

public class EmailPromptDemo {

    @FunctionName("IngestEmail")
    public HttpResponseMessage ingestEmail(
        @HttpTrigger(
            name = "req", 
            methods = {HttpMethod.POST},
            authLevel = AuthorizationLevel.ANONYMOUS)
            HttpRequestMessage<EmbeddingsRequest> request,
        @EmbeddingsStoreOutput(name="EmbeddingsStoreOutput", input = "{url}", inputType = InputType.Url,
                connectionName = "KustoConnectionString", collection = "Documents",
                model = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%") OutputBinding<EmbeddingsStoreOutputResponse> output,
        final ExecutionContext context) throws MalformedURLException {

        if (request.getBody() == null || request.getBody().getUrl() == null)
        {
            throw new IllegalArgumentException("Invalid request body. Make sure that you pass in {\"url\": value } as the request body.");
        }

        Uri uri = new Uri(request.getBody().getUrl());
        String filename = Paths.get(uri.getPath()).getFileName().toString();

        EmbeddingsStoreOutputResponse embeddingsStoreOutputResponse = new EmbeddingsStoreOutputResponse(new SearchableDocument(filename));

        output.setValue(embeddingsStoreOutputResponse);

        JSONObject response = new JSONObject();
        response.put("status", "success");
        response.put("title", filename);

        return request.createResponseBuilder(HttpStatus.CREATED)
                .header("Content-Type", "application/json")
                .body(response)
                .build();
    }

    public class EmbeddingsStoreOutputResponse {
        private SearchableDocument searchableDocument;

        public EmbeddingsStoreOutputResponse(SearchableDocument searchableDocument) {
            this.searchableDocument = searchableDocument;
        }
        public SearchableDocument getSearchableDocument() {
            return searchableDocument;
        }

    }
    
    @FunctionName("PromptEmail")
    public HttpResponseMessage promptEmail(
        @HttpTrigger(
            name = "req", 
            methods = {HttpMethod.POST},
            authLevel = AuthorizationLevel.ANONYMOUS)
            HttpRequestMessage<SemanticSearchRequest> request,
        @SemanticSearch(name = "search", connectionName = "KustoConnectionString", collection = "Documents", query = "{Prompt}", chatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", embeddingsModel = "%EMBEDDING_MODEL_DEPLOYMENT_NAME%" ) String semanticSearchContext,
        final ExecutionContext context) {
            String response = new JSONObject(semanticSearchContext).getString("Response");
            return request.createResponseBuilder(HttpStatus.OK)
            .header("Content-Type", "application/json")
            .body(response)
            .build();       
    }

    public class EmbeddingsRequest {
        public String url;
        public String getUrl() {
            return url;
        }
        public void setUrl(String url) {
            this.url = url;
        }
    }

    public class SemanticSearchRequest {
        public String prompt;
        public String getPrompt() {
            return prompt;
        }
        public void setPrompt(String prompt) {
            this.prompt = prompt;
        }        
    }
}

