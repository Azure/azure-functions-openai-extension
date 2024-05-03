import json
import os
import azure.functions as func

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)


# @app.function_name("IngestFile")
# @app.route(methods=["POST"])  # no route?
# # TODO: WAIT FOR MANVIR'S PR FOR TYPE
# @app.generic_input_binding(arg_name="embeddings", type="embeddings", data_type=func.DataType.STRING, input="{filePath}", input_type="filePath", model="%EMBEDDING_MODEL_DEPLOYMENT_NAME%")
# @app.generic_output_binding(arg_name="requests", type="semanticSearch", data_type=func.DataType.STRING)
# def ingest_file(req: func.HttpRequest, embeddings: str, requests: func.Out[str]) -> func.HttpResponse:
#     user_message = req.get_json()
#     embeddings_json = json.loads(embeddings)
#
#     if not user_message:
#         return func.HttpResponse(json.dumps({"message": "No message provided"}), status_code=400, mimetype="application/json")
#
#     file_name_with_extension = os.path.basename(user_message["file_path"])  # Get the base name (filename with extension)
#     title = os.path.splitext(file_name_with_extension)[0]
#
#
#     create_request = {
#         "title": title,
#         "embeddings": embeddings_json
#     }
#
#     requests.set(json.dumps(create_request))
#
#     response_json = {
#         "status": "success",
#         "title": title,
#         "chunks": embeddings_json.get("count")
#     }
#
#     return func.HttpResponse(json.dumps(response_json), status_code=200, mimetype="application/json")
#






@app.function_name("PromptFile")
@app.route(methods=["POST"])
@app.generic_input_binding(arg_name="result", type="semanticSearch", data_type=func.DataType.STRING, connection_name="AISearchEndpoint", collection="openai-index", query="{Prompt}", chat_model="%CHAT_MODEL_DEPLOYMENT_NAME%", model="%EMBEDDING_MODEL_DEPLOYMENT_NAME%")
def prompt_file(req: func.HttpRequest, result: str) -> func.HttpResponse:
    result_json = json.loads(result)
    response_json = {
        "content": result_json.get("response"),
        "content_type": "text/plain"
    }

    return func.HttpResponse(json.dumps(response_json), status_code=200, mimetype="application/json")





