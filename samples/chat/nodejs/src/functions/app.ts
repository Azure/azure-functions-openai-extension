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


const chatBotPostOutput = output.generic({
    type: 'assistantPost',
    id: '{chatID}'
})
app.http('PostUserResponse', {
    methods: ['POST'],
    route: 'chats/{chatID}',
    authLevel: 'function',
    extraOutputs: [chatBotPostOutput],
    handler: async (request, context) => {
        const userMessage = await request.text()
        if (!userMessage) {
            return { status: 400, bodyJson: { message: 'No message provided' } }
        }
        const chatPostRequest = {
            chatId: request.params.chatID,
            userMessage: userMessage
        }
        context.log(`Creating post request with parameters: ${JSON.stringify(chatPostRequest)}`)
        context.extraOutputs.set(chatBotPostOutput, chatPostRequest)
        return { status: 202 }
    }
});
