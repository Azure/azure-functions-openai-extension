import { app, input } from "@azure/functions";

interface EmbeddingsHttpRequest {
    rawText?: string;
}

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
        let requestBody: EmbeddingsHttpRequest = await request.json();
        let response: any = context.extraInputs.get(embeddingsHttpInput);

        context.log(
            `Received ${response.count} embedding(s) for input text containing ${requestBody.rawText?.length ?? 0} characters.`
        );
        
        // TODO: Store the embeddings into a database or other storage.

        return {status: 202}
    }
});

interface EmbeddingsFilePath {
    filePath?: string;
}

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
        let requestBody: EmbeddingsFilePath = await request.json();
        let response: any = context.extraInputs.get(embeddingsFilePathInput);

        context.log(
            `Received ${response.count} embedding(s) for input file ${requestBody.filePath}.`
        );
        
        // TODO: Store the embeddings into a database or other storage.

        return {status: 202}
    }
});

interface EmbeddingsUrlPath {
    url?: string;
}

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
        let requestBody: EmbeddingsUrlPath = await request.json();
        let response: any = context.extraInputs.get(embeddingsUrlInput);

        context.log(
            `Received ${response.count} embedding(s) for input url ${requestBody.url}.`
        );
        
        // TODO: Store the embeddings into a database or other storage.

        return {status: 202}
    }
});
