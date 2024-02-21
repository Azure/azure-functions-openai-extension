// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { HttpRequest, InvocationContext, app, input, output } from "@azure/functions"


const chatBotCreateOutput = output.generic({
    type: 'chatBotCreate'
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


const chatBotPostOutput = output.generic({
    type: 'chatBotPost',
    id: '{assistantId}',
    model: 'gpt-4'
})
app.http('PostUserQuery', {
    methods: ['POST'],
    route: 'assistants/{assistantId}',
    authLevel: 'anonymous',
    extraOutputs: [chatBotPostOutput],
    handler: async (request, context) => {
        const userMessage = await request.text()
        if (!userMessage) {
            return { status: 400, bodyJson: { message: 'Request body is empty' } }
        }
        context.extraOutputs.set(chatBotPostOutput, { userMessage: userMessage })
        return { status: 202 }
    }
})


const chatBotQueryInput = input.generic({
    type: 'chatBotQuery',
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
