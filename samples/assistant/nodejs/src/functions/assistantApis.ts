// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { HttpRequest, InvocationContext, app, input, output } from "@azure/functions"


const chatBotCreateOutput = output.generic({
    type: 'assistantCreate'
})
app.http('CreateAssistant', {
    methods: ['PUT'],
    route: 'assistants/{assistantId}',
    authLevel: 'anonymous',
    extraOutputs: [chatBotCreateOutput],
    handler: async (request: HttpRequest, context: InvocationContext) => {
        const assistantId = request.params.assistantId
        const instructions =
            `
            Don't make assumptions about what values to plug into functions.
            Ask for clarification if a user request is ambiguous.
            `
        const createRequest = {
            id: assistantId,
            instructions: instructions,
        }
        context.extraOutputs.set(chatBotCreateOutput, createRequest)
        return { status: 202, jsonBody: { assistantId: assistantId } }
    }
})


const assistantPostInput = output.generic({
    type: 'assistantPost',
    id: '{assistantId}',
    model: '%CHAT_MODEL_DEPLOYMENT_NAME%',
    userMessage: '{Query.message}'
})
app.http('PostUserResponse', {
    methods: ['POST'],
    route: 'assistants/{assistantId}',
    authLevel: 'anonymous',
    extraInputs: [assistantPostInput],
    handler: async (_, context) => {
        const chatState: any = context.extraInputs.get(assistantPostInput)
        return { status: 200, jsonBody: chatState.recentMessages[0].content }
    }
})


const chatBotQueryInput = input.generic({
    type: 'assistantQuery',
    id: '{assistantId}',
    timestampUtc: '{Query.timestampUTC}'
})
app.http('GetChatState', {
    methods: ['GET'],
    route: 'assistants/{assistantId}',
    authLevel: 'anonymous',
    extraInputs: [chatBotQueryInput],
    handler: async (_, context) => {
        const state: any = context.extraInputs.get(chatBotQueryInput)
        return { status: 200, jsonBody: state }
    }
})
