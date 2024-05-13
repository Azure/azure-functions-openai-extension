import { app, input, output } from "@azure/functions";

interface EmbeddingsRequest {
    Url?: string;
}

const embeddingsStoreOutput = output.generic({
    type: "embeddingsStore",
    input: "{url}", 
    inputType: "url", 
    connectionName: "CosmosDBMongoVCoreConnectionString", 
    collection: "openai-index", 
    model: "%EMBEDDING_MODEL_DEPLOYMENT_NAME%"
});

app.http('IngestFile', {
    methods: ['POST'],
    authLevel: 'function',
    extraOutputs: [embeddingsStoreOutput],
    handler: async (request, context) => {
        let requestBody: EmbeddingsRequest | null = await request.json();
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
    connectionName: "CosmosDBMongoVCoreConnectionString",
    collection: "openai-index",
    query: "{Prompt}",
    chatModel: "%CHAT_MODEL_DEPLOYMENT_NAME%",
    embeddingsModel: "%EMBEDDING_MODEL_DEPLOYMENT_NAME%"
});

app.http('PromptFile', {
    methods: ['POST'],
    authLevel: 'function',
    extraInputs: [semanticSearchInput],
    handler: async (_request, context) => {
        var responseBody: any = context.extraInputs.get(semanticSearchInput)

        return { status: 200, body: responseBody.Response.trim() }
    }
});