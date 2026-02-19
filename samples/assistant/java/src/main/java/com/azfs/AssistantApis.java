/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.azfs;

import java.util.List;
import java.util.Optional;

import com.microsoft.azure.functions.openai.annotation.assistant.*;
import org.json.JSONObject;

import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.HttpMethod;
import com.microsoft.azure.functions.HttpRequestMessage;
import com.microsoft.azure.functions.HttpResponseMessage;
import com.microsoft.azure.functions.HttpStatus;
import com.microsoft.azure.functions.OutputBinding;
import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.BindingName;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;

/**
 * Defines HTTP APIs for interacting with assistants.
 */
public class AssistantApis {

    /**
     * The default storage account setting for the table storage account.
     * This constant is used to specify the connection string for the table storage
     * account
     * where chat data will be stored.
     */
    final String DEFAULT_CHATSTORAGE = "AzureWebJobsStorage";

    /**
     * The default collection name for the table storage account.
     * This constant is used to specify the collection name for the table storage
     * account
     * where chat data will be stored.
     */
    final String DEFAULT_COLLECTION = "ChatState";

    /*
     * HTTP PUT function that creates a new assistant chat bot with the specified ID.
     */
    @FunctionName("CreateAssistant")
    public HttpResponseMessage createAssistant(
        @HttpTrigger(
            name = "req", 
            methods = {HttpMethod.PUT}, 
            authLevel = AuthorizationLevel.ANONYMOUS, 
            route = "assistants/{assistantId}") 
            HttpRequestMessage<Optional<String>> request,
        @BindingName("assistantId") String assistantId,
        @AssistantCreate(name = "AssistantCreate") OutputBinding<AssistantCreateRequest> message,
        final ExecutionContext context) {
            context.getLogger().info("Java HTTP trigger processed a request.");
            
            String instructions = "Don't make assumptions about what values to plug into functions.\n" +
                    "Ask for clarification if a user request is ambiguous.";
            boolean preserveChatHistory = false;

            if (request.getBody().isPresent()) {
                JSONObject body = new JSONObject(request.getBody().get());
                if (body.has("instructions")) {
                    instructions = body.getString("instructions");
                }
                if (body.has("preserveChatHistory")) {
                    preserveChatHistory = body.getBoolean("preserveChatHistory");
                }
            }

            AssistantCreateRequest assistantCreateRequest = new AssistantCreateRequest(assistantId, instructions);
            assistantCreateRequest.setChatStorageConnectionSetting(DEFAULT_CHATSTORAGE);
            assistantCreateRequest.setCollectionName(DEFAULT_COLLECTION);
            try {
                assistantCreateRequest.getClass()
                    .getMethod("setPreserveChatHistory", boolean.class)
                    .invoke(assistantCreateRequest, preserveChatHistory);
            } catch (NoSuchMethodException ex) {
                // Older SDK version that does not support preserveChatHistory; continue without it.
                context.getLogger().warning(
                    "PreserveChatHistory not supported by current SDK version (method setPreserveChatHistory not found): "
                        + ex.getMessage());
            } catch (ReflectiveOperationException ex) {
                // Method exists but could not be invoked due to a reflection error.
                context.getLogger().warning(
                    "Failed to invoke setPreserveChatHistory via reflection: "
                        + ex.getClass().getSimpleName() + ": " + ex.getMessage());
            }

            message.setValue(assistantCreateRequest);
            JSONObject response = new JSONObject();
            response.put("assistantId", assistantId);
            
            return request.createResponseBuilder(HttpStatus.CREATED)
                .header("Content-Type", "application/json")
                .body(response.toString())
                .build();    
    }

    /*
     * HTTP GET function that queries the conversation history of the assistant chat bot.
     */   
    @FunctionName("GetChatState")
    public HttpResponseMessage getChatState(
        @HttpTrigger(
            name = "req",
            methods = {HttpMethod.GET}, 
            authLevel = AuthorizationLevel.ANONYMOUS,
            route = "assistants/{assistantId}") 
            HttpRequestMessage<Optional<String>> request,
        @BindingName("assistantId") String assistantId,        
        @AssistantQuery(name = "AssistantState", id = "{assistantId}", timestampUtc = "{Query.timestampUTC}", chatStorageConnectionSetting = DEFAULT_CHATSTORAGE, collectionName = DEFAULT_COLLECTION) AssistantState state,
        final ExecutionContext context) {
            return request.createResponseBuilder(HttpStatus.OK)
                .header("Content-Type", "application/json")
                .body(state)
                .build();
    }

    /*
     * HTTP POST function that sends user prompts to the assistant chat bot.
     */ 
    @FunctionName("PostUserResponse")
    public HttpResponseMessage postUserResponse(
        @HttpTrigger(
            name = "req",
            methods = {HttpMethod.POST}, 
            authLevel = AuthorizationLevel.ANONYMOUS,
            route = "assistants/{assistantId}") 
            HttpRequestMessage<Optional<String>> request,
        @BindingName("assistantId") String assistantId,        
        @AssistantPost(name="newMessages", id = "{assistantId}", chatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", userMessage = "{Query.message}", chatStorageConnectionSetting = DEFAULT_CHATSTORAGE, collectionName = DEFAULT_COLLECTION) AssistantState state,
        final ExecutionContext context) {
            
            List<AssistantMessage> recentMessages = state.getRecentMessages();
            String response = recentMessages.isEmpty() ? "No response returned." : recentMessages.get(recentMessages.size() - 1).getContent();
            
            return request.createResponseBuilder(HttpStatus.OK)
                .header("Content-Type", "application/json")
                .body(response)
                .build();
    }
}

