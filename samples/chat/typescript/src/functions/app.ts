// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { HttpRequest, InvocationContext, app, input, output } from "@azure/functions";


const chatBotCreateOutput = output.generic({
    type: 'assistantCreate'
})
app.http('CreateChatBot', {
    methods: ['PUT'],
    route: 'chats/{chatID}',
    authLevel: 'function',
    extraOutputs: [chatBotCreateOutput],
    handler: async (request: HttpRequest, context: InvocationContext) => {
        const chatID = request.params.chatID
        const inputJson: any = await request.json()
        context.log(`Creating chat ${chatID} from input parameters ${JSON.stringify(inputJson)}`)
        const createRequest = {
            id: chatID,
            instructions: inputJson.instructions,
        }
        context.extraOutputs.set(chatBotCreateOutput, createRequest)
        return { status: 202, jsonBody: { chatId: chatID } }
    }
});


const assistantQueryInput = input.generic({
    type: 'assistantQuery',
    id: '{chatId}',
    timestampUtc: '{Query.timestampUTC}'
})
app.http('GetChatState', {
    methods: ['GET'],
    route: 'chats/{chatID}',
    authLevel: 'function',
    extraInputs: [assistantQueryInput],
    handler: async (_, context) => {
        const chatState: any = context.extraInputs.get(assistantQueryInput)
        return { status: 200, jsonBody: chatState }
    }
});


const assistantPostInput = input.generic({
    type: 'assistantPost',
    id: '{chatID}',
    model: '%CHAT_MODEL_DEPLOYMENT_NAME%',
    userMessage: '{Query.message}'
})
app.http('PostUserResponse', {
    methods: ['POST'],
    route: 'chats/{chatID}',
    authLevel: 'function',
    extraInputs: [assistantPostInput],
    handler: async (_, context) => {
        const chatState: any = context.extraInputs.get(assistantPostInput)
        const content = chatState.recentMessages[0].content
        return {
            status: 200,
            body: content,
            headers: {
                'Content-Type': 'text/plain'
            }
        };
    }
});
