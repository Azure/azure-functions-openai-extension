/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation.assistant;

import com.microsoft.azure.functions.annotation.CustomBinding;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
  * <p>
  * Assistant skill trigger attribute.
  * </p>
  * 
  * @since 1.0.0
  */
 @Retention(RetentionPolicy.RUNTIME)
 @Target(ElementType.PARAMETER)
 @CustomBinding(direction = "in", name = "", type = "assistantSkillTrigger")
 public @interface AssistantSkillTrigger {
    
    /**
     * The variable name used in function.json.
     * 
     * @return The variable name used in function.json.
     */ 
    String name();

    /**
     * The name of the function to be invoked by the assistant.
     * 
     * @return The name of the function to be invoked by the assistant.
     */
    String functionName() default "";

    /**
     * The description of the assistant function, which is provided to the LLM.
     * 
     * @return The description of the assistant function.
     */
    String functionDescription();

    /**
     * The JSON description of the function parameter, which is provided to the LLM.
     * If no description is provided, the description will be autogenerated.
     * For more information on the syntax of the parameter description JSON, see the OpenAI API documentation:
     * https://platform.openai.com/docs/api-reference/chat/create#chat-create-tools.
     * 
     * @return The JSON description of the function parameter.
     */
    String parameterDescriptionJson() default "";

    /**
     * The OpenAI chat model to use.
     * When using Azure OpenAI, this should be the name of the model deployment.
     * 
     * @return The OpenAI chat model to use.
     */
    String model() default "gpt-3.5-turbo";
 
 }
