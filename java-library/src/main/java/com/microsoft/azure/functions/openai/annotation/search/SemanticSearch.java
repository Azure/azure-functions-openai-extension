/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.openai.annotation.search;

import com.microsoft.azure.functions.annotation.CustomBinding;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.PARAMETER)
@CustomBinding(direction = "in", name = "", type = "semanticSearch")
public @interface SemanticSearch {

    /**
     * The variable name used in function.json.
     *
     * @return The variable name used in function.json.
     */
    String name();

    /**
     * The name of the configuration section for AI service connectivity settings.
     * 
     * @return The name of the configuration section for AI service connectivity
     *         settings.
     */
    String aiConnectionName() default "";

    /**
     * The name of an app setting or environment variable which contains a
     * connection string value.
     * This property supports binding expressions.
     *
     * @return The connection name.
     */
    String searchConnectionName();

    /**
     * The name of the collection or table to search.
     * This property supports binding expressions.
     *
     * @return The collection or table name.
     */
    String collection();

    /**
     * the semantic query text to use for searching.
     * This property is only used for the semantic search input binding.
     * This property supports binding expressions.
     *
     * @return The semantic query text.
     */
    String query() default "";

    /**
     * The model to use for embeddings.
     * The default value is "text-embedding-ada-002".
     * This property supports binding expressions.
     *
     * @return The model to use for embeddings.
     */
    String embeddingsModel() default "text-embedding-ada-002";

    /**
     * the name of the Large Language Model to invoke for chat responses.
     * The default value is "gpt-3.5-turbo".
     * This property supports binding expressions.
     *
     * @return The name of the Large Language Model to invoke for chat responses.
     */
    String chatModel() default "gpt-3.5-turbo";

    /**
     * The system prompt to use for prompting the large language model.
     * The system prompt will be appended with knowledge that is fetched as a result
     * of the Query.
     * The combined prompt will then be sent to the OpenAI Chat API.
     * This property supports binding expressions.
     *
     * @return The system prompt to use for prompting the large language model.
     */
    String systemPrompt() default "You are a helpful assistant. You are responding to requests from a user about internal emails and documents.\n"
            + "You can and should refer to the internal documents to help respond to requests. If a user makes a request that's\n"
            + "not covered by the internal emails and documents, explain that you don't know the answer or that you don't have\n"
            + "access to the information.\n"
            + "\n"
            + "The following is a list of documents that you can refer to when answering questions. The documents are in the format\n"
            + "[filename]: [text] and are separated by newlines. If you answer a question by referencing any of the documents,\n"
            + "please cite the document in your answer. For example, if you answer a question by referencing info.txt,\n"
            + "you should add \"Reference: info.txt\" to the end of your answer on a separate line.";

    /**
     * The number of knowledge items to inject into the systemPrompt
     *
     * @return The number of knowledge items to inject into the SystemPrompt.
     */
    int maxKnowledgeLength() default 1;

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
    String maxTokens() default "2048";
}
