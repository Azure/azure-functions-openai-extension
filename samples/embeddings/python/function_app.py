import json
import logging
import azure.functions as func

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)


@app.function_name("GenerateEmbeddingsHttpRequest")
@app.route(route="embeddings", methods=["POST"])
@app.embeddings_input(arg_name="embeddings", input="{rawText}", input_type="rawText", model="%EMBEDDING_MODEL_DEPLOYMENT_NAME%")
def generate_embeddings_http_request(req: func.HttpRequest, embeddings: str) -> func.HttpResponse:
    user_message = req.get_json()
    embeddings_json = json.loads(embeddings)
    embeddings_request = {
        "raw_text": user_message.get("RawText"),
        "file_path": user_message.get("FilePath")
    }
    logging.info(f'Received {embeddings_json.get("count")} embedding(s) for input text '
        f'containing {len(embeddings_request.get("raw_text"))} characters.')
    # TODO: Store the embeddings into a database or other storage.
    return func.HttpResponse(status_code=200)


@app.function_name("GetEmbeddingsHttpFilePath")
@app.route(route="embeddings-from-file", methods=["POST"])
@app.embeddings_input(arg_name="embeddings", input="{filePath}", input_type="filePath", max_chunk_length=512, model="%EMBEDDING_MODEL_DEPLOYMENT_NAME%")
def generate_embeddings_http_request(req: func.HttpRequest, embeddings: str) -> func.HttpResponse:
    user_message = req.get_json()
    embeddings_json = json.loads(embeddings)
    embeddings_request = {
        "raw_text": user_message.get("RawText"),
        "file_path": user_message.get("FilePath")
    }
    logging.info(f'Received {embeddings_json.get("count")} embedding(s) for input file {embeddings_request.get("file_path")}.')
    # TODO: Store the embeddings into a database or other storage.
    return func.HttpResponse(status_code=200)