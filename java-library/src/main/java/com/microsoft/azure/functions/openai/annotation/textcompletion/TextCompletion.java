/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation.textcompletion;

import com.microsoft.azure.functions.annotation.CustomBinding;
import com.microsoft.azure.functions.openai.constants.ModelDefaults;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * <p>
 * Assistant query input attribute which is used query the Assistant to get
 * current state.
 * </p>
 * 
 * @since 1.0.0
 */
@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.PARAMETER)
@CustomBinding(direction = "in", name = "", type = "textCompletion")
public @interface TextCompletion {

    /**
     * The variable name used in function.json.
     * 
     * @return The variable name used in function.json.
     */
    String name();

    /**
     * The prompt to generate completions for, encoded as a string.
     *
     * @return The prompt string.
     */
    String prompt();

    /**
     * The name of the configuration section for AI service connectivity settings.
     * 
     * @return The name of the configuration section for AI service connectivity
     *         settings.
     */
    String aiConnectionName() default "";

    /**
     * The ID of the model to use.
     *
     * @return The model ID.
     */
    String chatModel() default ModelDefaults.DEFAULT_CHAT_MODEL;

    /**
     * The sampling temperature to use, between 0 and 2. Higher values like 0.8 will
     * make the output
     * more random, while lower values like 0.2 will make it more focused and
     * deterministic.
     * It's generally recommended to use this or {@link #topP()} but not both.
     *
     * @return The sampling temperature value.
     */
    String temperature() default "0.5";

    /**
     * An alternative to sampling with temperature, called nucleus sampling, where
     * the model considers
     * the results of the tokens with top_p probability mass. So 0.1 means only the
     * tokens comprising the top 10%
     * probability mass are considered.
     * It's generally recommended to use this or {@link #temperature()} but not
     * both.
     *
     * @return The topP value.
     */
    String topP() default "";

    /**
     * The maximum number of tokens to generate in the completion.
     * The token count of your prompt plus max_tokens cannot exceed the model's
     * context length.
     * Most models have a context length of 2048 tokens (except for the newest
     * models, which support 4096).
     *
     * @return The maxTokens value.
     */
    String maxTokens() default "100";

    /**
     * Indicates whether the chat completion api uses a reasoning model.
     *
     * @return {@code true} if the chat completion api is based on a reasoning model; {@code false} otherwise.
     */
    boolean isReasoningModel() default false;
}
