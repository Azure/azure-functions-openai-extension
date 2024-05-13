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

            AssistantCreateRequest assistantCreateRequest = new AssistantCreateRequest(assistantId, instructions);
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
        @AssistantQuery(name = "AssistantState", id = "{assistantId}", timestampUtc = "{Query.timestampUTC}") AssistantState state,
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
        @AssistantPost(name="newMessages", id = "{assistantId}", model = "%CHAT_MODEL_DEPLOYMENT_NAME%", userMessage = "{Query.message}") AssistantState state,
        final ExecutionContext context) {
            
            List<ChatMessage> recentMessages = state.getRecentMessages();
            String response = recentMessages.isEmpty() ? "No response returned." : recentMessages.get(recentMessages.size() - 1).getContent();
            
            return request.createResponseBuilder(HttpStatus.OK)
                .header("Content-Type", "application/json")
                .body(response)
                .build();
    }
}

