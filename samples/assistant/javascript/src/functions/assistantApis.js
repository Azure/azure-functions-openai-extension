// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const { app, input, output } = require("@azure/functions");

const CHAT_STORAGE_CONNECTION_SETTING = "AzureWebJobsStorage";
const COLLECTION_NAME = "ChatState";

const chatBotCreateOutput = output.generic({
    type: 'assistantCreate'
})
app.http('CreateAssistant', {
    methods: ['PUT'],
    route: 'assistants/{assistantId}',
    authLevel: 'anonymous',
    extraOutputs: [chatBotCreateOutput],
    handler: async (request, context) => {
        const assistantId = request.params.assistantId
        const instructions =
            `
            Don't make assumptions about what values to plug into functions.
            Ask for clarification if a user request is ambiguous.
            `
        const createRequest = {
            id: assistantId,
            instructions: instructions,
            chatStorageConnectionSetting: CHAT_STORAGE_CONNECTION_SETTING,
            collectionName: COLLECTION_NAME
        }
        context.extraOutputs.set(chatBotCreateOutput, createRequest)
        return { status: 202, jsonBody: { assistantId: assistantId } }
    }
})


const assistantPostInput = input.generic({
    type: 'assistantPost',
    id: '{assistantId}',
    chatModel: '%CHAT_MODEL_DEPLOYMENT_NAME%',
    userMessage: '{Query.message}',
    chatStorageConnectionSetting: CHAT_STORAGE_CONNECTION_SETTING,
    collectionName: COLLECTION_NAME
})
app.http('PostUserResponse', {
    methods: ['POST'],
    route: 'assistants/{assistantId}',
    authLevel: 'anonymous',
    extraInputs: [assistantPostInput],
    handler: async (_, context) => {
        const chatState = context.extraInputs.get(assistantPostInput)
        const content = chatState.recentMessages[0].content
        return {
            status: 200,
            body: content,
            headers: {
                'Content-Type': 'text/plain'
            }
        };
    }
})


const chatBotQueryInput = input.generic({
    type: 'assistantQuery',
    id: '{assistantId}',
    timestampUtc: '{Query.timestampUTC}',
    chatStorageConnectionSetting: CHAT_STORAGE_CONNECTION_SETTING,
    collectionName: COLLECTION_NAME
})
app.http('GetChatState', {
    methods: ['GET'],
    route: 'assistants/{assistantId}',
    authLevel: 'anonymous',
    extraInputs: [chatBotQueryInput],
    handler: async (_, context) => {
        const state = context.extraInputs.get(chatBotQueryInput)
        return { status: 200, jsonBody: state }
    }
})
