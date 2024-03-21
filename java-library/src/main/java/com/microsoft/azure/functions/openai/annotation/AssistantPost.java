/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation;

import com.microsoft.azure.functions.annotation.CustomBinding;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;


/**
 * <p>
 * Assistant post output attribute which is used to update the assistant.
 * </p>
 * 
 * @since 1.0.0
 */
@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.PARAMETER)
@CustomBinding(direction = "out", name = "", type = "assistantPost")
public @interface AssistantPost {

    /**
     * The variable name used in function.json.
     * 
     * @return The variable name used in function.json.
     */ 
    String name();
      
    /**
     * The ID of the Assistant to query.
     * 
     * @return The ID of the Assistant to query.
     */   
    String id();

    /**
     * The OpenAI chat model to use.
     * When using Azure OpenAI, this should be the name of the model deployment.
     * 
     * @return The OpenAI chat model to use.
     */
    String model();

}
