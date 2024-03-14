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
import com.microsoft.azure.functions.annotation.CosmosDBOutput;
import com.microsoft.azure.functions.annotation.CustomBinding;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.openai.annotation.AssistantCreate;
import com.microsoft.azure.functions.openai.annotation.AssistantQuery;
import com.microsoft.azure.functions.openai.annotation.ChatBotState;

import java.util.Optional;

/**
 * Azure Functions ChatBot.
 */
public class ChatBot2 {

    @FunctionName("CreateChatBot")
    public HttpResponseMessage createChatBot(
        @HttpTrigger(
            name = "req", 
            methods = {HttpMethod.PUT},
            authLevel = AuthorizationLevel.ANONYMOUS, 
            route = "chats/{chatId}") 
            HttpRequestMessage<Optional<CreateRequest>> request,
        @BindingName("chatId") String chatId,
        @AssistantCreate(name = "ChatBotCreate") OutputBinding<ChatBotCreateRequest> message,
        //@CustomBinding(direction = "out", name = "ChatBotCreate", type = "chatBotCreate") OutputBinding<ChatBotCreateRequest> message,
        final ExecutionContext context) {
               
            ChatBotCreateRequest chatBotCreateRequest = new ChatBotCreateRequest(chatId, request.getBody().get().getInstructions());
            message.setValue(chatBotCreateRequest);

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

    public class ChatBotCreateRequest {

        public String id;
        public String instructions;

        public ChatBotCreateRequest(String id, String instructions) {
            this.id = id;
            this.instructions = instructions;
        }

        public String getChatId() {
            return id;
        }

        public void setChatId(String id) {
            this.id = id;
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
        @AssistantQuery(name = "ChatBotState", id = "{chatId}", timestampUtc = "{Query.timestampUTC}") ChatBotState state,
        final ExecutionContext context) {
            // ChatBotCreateRequest chatBotCreateRequest = new ChatBotCreateRequest(chatId, request.getBody().get().getInstructions());
            // message.setValue(chatBotCreateRequest);
            JSONObject response = new JSONObject();
            response.put("chatId", chatId);
            response.put("state", state);
            return request.createResponseBuilder(HttpStatus.OK)
                .header("Content-Type", "application/json")
                .body(response.toString())
                .build();
    }

}
