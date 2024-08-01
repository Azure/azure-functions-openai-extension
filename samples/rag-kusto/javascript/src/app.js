const { app, input, output } = require("@azure/functions");

const embeddingsStoreOutput = output.generic({
    type: "embeddingsStore",
    input: "{url}",
    inputType: "url",
    connectionName: "KustoConnectionString",
    collection: "Documents",
    model: "%EMBEDDING_MODEL_DEPLOYMENT_NAME%"
});

app.http('IngestEmail', {
    methods: ['POST'],
    authLevel: 'function',
    extraOutputs: [embeddingsStoreOutput],
    handler: async (request, context) => {
        let requestBody = await request.json();
        if (!requestBody || !requestBody.Url) {
            throw new Error("Invalid request body. Make sure that you pass in {\"Url\": value } as the request body.");
        }

        let uri = requestBody.Url;
        let filename = uri.split('/').pop();

        context.extraOutputs.set(embeddingsStoreOutput, { title: filename });

        let response = {
            status: "success",
            title: filename
        };

        return { status: 202, jsonBody: response }
    }
});

const semanticSearchInput = input.generic({
    type: "semanticSearch",
    connectionName: "KustoConnectionString",
    collection: "Documents",
    query: "{Prompt}",
    chatModel: "%CHAT_MODEL_DEPLOYMENT_NAME%",
    embeddingsModel: "%EMBEDDING_MODEL_DEPLOYMENT_NAME%"
});

app.http('PromptEmail', {
    methods: ['POST'],
    authLevel: 'function',
    extraInputs: [semanticSearchInput],
    handler: async (_request, context) => {
        var responseBody = context.extraInputs.get(semanticSearchInput)

        return { status: 200, body: responseBody.Response.trim() }
    }
});