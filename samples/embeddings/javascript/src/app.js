const { app, input } = require("@azure/functions");

const embeddingsHttpInput = input.generic({
    input: '{rawText}',
    inputType: 'RawText',
    type: 'embeddings',
    embeddingsModel: '%EMBEDDING_MODEL_DEPLOYMENT_NAME%'
})

app.http('generateEmbeddings', {
    methods: ['POST'],
    route: 'embeddings',
    authLevel: 'function',
    extraInputs: [embeddingsHttpInput],
    handler: async (request, context) => {
        let requestBody = await request.json();
        let response = context.extraInputs.get(embeddingsHttpInput);

        context.log(
            `Received ${response.count} embedding(s) for input text containing ${requestBody.RawText.length} characters.`
        );
        
        // TODO: Store the embeddings into a database or other storage.

        return {status: 202}
    }
});

const embeddingsFilePathInput = input.generic({
    input: '{filePath}',
    inputType: 'FilePath',
    type: 'embeddings',
    maxChunkLength: 512,
    embeddingsModel: '%EMBEDDING_MODEL_DEPLOYMENT_NAME%'
})

app.http('getEmbeddingsFilePath', {
    methods: ['POST'],
    route: 'embeddings-from-file',
    authLevel: 'function',
    extraInputs: [embeddingsFilePathInput],
    handler: async (request, context) => {
        let requestBody = await request.json();
        let response = context.extraInputs.get(embeddingsFilePathInput);

        context.log(
            `Received ${response.count} embedding(s) for input file ${requestBody.FilePath}.`
        );
        
        // TODO: Store the embeddings into a database or other storage.

        return {status: 202}
    }
});

const embeddingsUrlInput = input.generic({
    input: '{url}',
    inputType: 'Url',
    type: 'embeddings',
    maxChunkLength: 512,
    embeddingsModel: '%EMBEDDING_MODEL_DEPLOYMENT_NAME%'
})

app.http('getEmbeddingsUrl', {
    methods: ['POST'],
    route: 'embeddings-from-url',
    authLevel: 'function',
    extraInputs: [embeddingsUrlInput],
    handler: async (request, context) => {
        let requestBody = await request.json();
        let response = context.extraInputs.get(embeddingsUrlInput);

        context.log(
            `Received ${response.count} embedding(s) for input url ${requestBody.Url}.`
        );
        
        // TODO: Store the embeddings into a database or other storage.

        return {status: 202}
    }
});
