/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.azfs;

import java.util.Optional;

import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.HttpMethod;
import com.microsoft.azure.functions.HttpRequestMessage;
import com.microsoft.azure.functions.HttpResponseMessage;
import com.microsoft.azure.functions.HttpStatus;
import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.BindingName;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.openai.annotation.textcompletion.TextCompletion;
import com.microsoft.azure.functions.openai.annotation.textcompletion.TextCompletionResponse;
/**
  * These samples show how to use the OpenAI Chat Completions API for Text Completion. For more details on the Completions APIs, see
  * https://platform.openai.com/docs/guides/text-generation/chat-completions-vs-completions.
 */
public class TextCompletions {
    
    /**
     * This sample demonstrates the "templating" pattern, where the function takes a parameter
     * and embeds it into a text prompt, which is then sent to the OpenAI completions API.
     */
    @FunctionName("WhoIs")
    public HttpResponseMessage whoIs(
        @HttpTrigger(
            name = "req", 
            methods = {HttpMethod.GET},
            authLevel = AuthorizationLevel.ANONYMOUS, 
            route = "whois/{name}") 
            HttpRequestMessage<Optional<String>> request,
        @BindingName("name") String name,
        @TextCompletion(prompt = "Who is {name}?", chatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", name = "response") TextCompletionResponse response,
        final ExecutionContext context) {
        return request.createResponseBuilder(HttpStatus.OK)
            .header("Content-Type", "application/json")
            .body(response.getContent())
            .build();
    }
    
    /**
     * This sample takes a prompt as input, sends it directly to the OpenAI completions API, and results the 
     * response as the output.
     */
    @FunctionName("GenericCompletion")
    public HttpResponseMessage genericCompletion(
        @HttpTrigger(
            name = "req", 
            methods = {HttpMethod.POST},
            authLevel = AuthorizationLevel.ANONYMOUS) 
            HttpRequestMessage<Optional<String>> request,
        @TextCompletion(prompt = "{prompt}", chatModel = "%CHAT_MODEL_DEPLOYMENT_NAME%", name = "response") TextCompletionResponse response,
        final ExecutionContext context) {
        return request.createResponseBuilder(HttpStatus.OK)
            .header("Content-Type", "application/json")
            .body(response.getContent())
            .build();
    }
}

