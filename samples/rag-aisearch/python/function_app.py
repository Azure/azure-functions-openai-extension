import json
import os
import azure.functions as func

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)


@app.function_name("IngestFile")
@app.route(methods=["POST"])  # no route?
@app.embeddings_store_output(arg_name="requests", input="{url}", input_type="url", connection_name="AISearchEndpoint", collection="openai-index")
def ingest_file(req: func.HttpRequest, requests: func.Out[str]) -> func.HttpResponse:
    user_message = req.get_json()

    if not user_message:
        return func.HttpResponse(json.dumps({"message": "No message provided"}), status_code=400, mimetype="application/json")

    file_name_with_extension = os.path.basename(user_message["Url"])
    title = os.path.splitext(file_name_with_extension)[0]
    create_request = {
        "title": title
    }
    requests.set(json.dumps(create_request))
    response_json = {
        "status": "success",
        "title": title
    }
    return func.HttpResponse(json.dumps(response_json), status_code=200, mimetype="application/json")


@app.function_name("PromptFile")
@app.route(methods=["POST"])
@app.semantic_search_input(arg_name="result", connection_name="AISearchEndpoint", collection="openai-index", query="{Prompt}", embeddings_model="%EMBEDDING_MODEL_DEPLOYMENT_NAME%", chat_model="%CHAT_MODEL_DEPLOYMENT_NAME%")
def prompt_file(req: func.HttpRequest, result: str) -> func.HttpResponse:
    result_json = json.loads(result)
    response_json = {
        "content": result_json.get("response"),
        "content_type": "text/plain"
    }
    return func.HttpResponse(json.dumps(response_json), status_code=200, mimetype="application/json")





