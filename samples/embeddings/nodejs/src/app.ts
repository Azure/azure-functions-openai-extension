import { app, input } from "@azure/functions";

interface EmbeddingsRequest {
    RawText?: string;
}

const openAIEmbeddingsInput = input.generic({
    input: '{RawText}',
    inputType: 'RawText',
    type: 'embeddings',
    model: '%EMBEDDINGS_MODEL_DEPLOYMENT_NAME%'
})

app.http('generateEmbeddingsHttpRequest', {
    methods: ['POST'],
    route: 'embeddings',
    authLevel: 'function',
    extraInputs: [openAIEmbeddingsInput],
    handler: async (request, context) => {
        let requestBody: EmbeddingsRequest = await request.json();
        let response: any = context.extraInputs.get(openAIEmbeddingsInput);

        context.log(
            `Received ${response.count} embedding(s) for input text containing ${requestBody.RawText.length} characters.`
        );
        
        // TODO: Store the embeddings into a database or other storage.

        return {status: 202}
    }
});