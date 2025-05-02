const { app, input, output } = require("@azure/functions");

const path = require('path');

const embeddingsStoreOutput = output.generic({
    type: "embeddingsStore",
    input: "{url}", 
    inputType: "url", 
    connectionName: "CosmosDBNoSqlEndpoint", 
    collection: "openai-index", 
    embeddingsModel: "%EMBEDDING_MODEL_DEPLOYMENT_NAME%"
});

app.http('IngestFile', {
    methods: ['POST'],
    authLevel: 'function',
    extraOutputs: [embeddingsStoreOutput],
    handler: async (request, context) => {
        let requestBody = await request.json();
        if (!requestBody || !requestBody.url) {
            throw new Error("Invalid request body. Make sure that you pass in {\"url\": value } as the request body.");
        }

        let uri = requestBody.url;
        let url = new URL(uri);

        let fileName = path.basename(url.pathname);
        context.extraOutputs.set(embeddingsStoreOutput, { title: fileName });

        let response = {
            status: "success",
            title: fileName
        };

        return { status: 202, jsonBody: response } 
    }
});

const semanticSearchInput = input.generic({
    type: "semanticSearch",
    connectionName: "CosmosDBNoSqlEndpoint",
    collection: "openai-index",
    query: "{prompt}",
    chatModel: "%CHAT_MODEL_DEPLOYMENT_NAME%",
    embeddingsModel: "%EMBEDDING_MODEL_DEPLOYMENT_NAME%"
});

app.http('PromptFile', {
    methods: ['POST'],
    authLevel: 'function',
    extraInputs: [semanticSearchInput],
    handler: async (_request, context) => {
        var responseBody = context.extraInputs.get(semanticSearchInput)

        return { status: 200, body: responseBody.Response.trim() }
    }
});