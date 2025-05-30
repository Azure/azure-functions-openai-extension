/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.azfs;

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
import com.microsoft.azure.functions.openai.annotation.assistant.AssistantCreate;
import com.microsoft.azure.functions.openai.annotation.assistant.AssistantQuery;
import com.microsoft.azure.functions.openai.annotation.assistant.AssistantPost;
import com.microsoft.azure.functions.openai.annotation.assistant.AssistantCreateRequest;
import com.microsoft.azure.functions.openai.annotation.assistant.AssistantState;
import com.microsoft.azure.functions.openai.annotation.assistant.AssistantMessage;

import java.util.List;
import java.util.Optional;

/**
 * Azure Functions ChatBot sample allows you to create chat bots with a specified set of initial instructions.
 */
public class ChatBot {
    final String DEFAULT_CHATSTORAGE = "AzureWebJobsStorage";
    final String DEFAULT_COLLECTION = "ChatState";

    @FunctionName("CreateChatBot")
    public HttpResponseMessage createChatBot(
        @HttpTrigger(
            name = "req", 
            methods = {HttpMethod.PUT},
            authLevel = AuthorizationLevel.ANONYMOUS, 
            route = "chats/{chatId}") 
            HttpRequestMessage<Optional<CreateRequest>> request,
        @BindingName("chatId") String chatId,
        @AssistantCreate(name = "ChatBotCreate") OutputBinding<AssistantCreateRequest> message,
        final ExecutionContext context) {
            
            if (request.getBody() == null)
            {
                throw new IllegalArgumentException("Invalid request body. Make sure that you pass in {\"instructions\": value } as the request body.");
            }
               
            AssistantCreateRequest assistantCreateRequest = new AssistantCreateRequest(chatId, request.getBody().get().getInstructions());
            assistantCreateRequest.setChatStorageConnectionSetting(DEFAULT_CHATSTORAGE);
            assistantCreateRequest.setCollectionName(DEFAULT_COLLECTION);

            message.setValue(assistantCreateRequest);
            JSONObject response = new JSONObject();
            response.put("chatId", chatId);
            return request.createResponseBuilder(HttpStatus.ACCEPTED)
                .header("Content-Type", "application/json")
                .body(response.toString())
                .build();

    }

    public class CreateRequest {
        public String instructions;
        public String getInstructions() {
            return instructions;
        }
        public void setInstructions(String instructions) {
            this.instructions = instructions;
        }
    }
   
    @FunctionName("GetChatState")
    public HttpResponseMessage getChatState(
        @HttpTrigger(
            name = "req",
            methods = {HttpMethod.GET}, 
            authLevel = AuthorizationLevel.ANONYMOUS,
            route = "chats/{chatId}") 
            HttpRequestMessage<Optional<String>> request,
        @BindingName("chatId") String chatId,        
        @AssistantQuery(name = "ChatBotState", id = "{chatId}", timestampUtc = "{Query.timestampUTC}") AssistantState state,
        final ExecutionContext context) {
            return request.createResponseBuilder(HttpStatus.OK)
                .header("Content-Type", "application/json")
                .body(state)
                .build();
    }

    @FunctionName("PostUserResponse")
    public HttpResponseMessage postUserResponse(
        @HttpTrigger(
            name = "req",
            methods = {HttpMethod.POST}, 
            authLevel = AuthorizationLevel.ANONYMOUS,
            route = "chats/{chatId}") 
            HttpRequestMessage<Optional<String>> request,
        @BindingName("chatId") String chatId,        
        @AssistantPost(name="newMessages", id = "{chatId}", chatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", userMessage = "{Query.message}") AssistantState state,
        final ExecutionContext context) {

            List<AssistantMessage> recentMessages = state.getRecentMessages();
            String response = recentMessages.isEmpty() ? "No response returned." : recentMessages.get(recentMessages.size() - 1).getContent();
            
            return request.createResponseBuilder(HttpStatus.OK)
                .header("Content-Type", "application/json")
                .body(response)
                .build();
    }
}

