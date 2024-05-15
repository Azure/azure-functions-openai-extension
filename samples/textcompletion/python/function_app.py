import json
import azure.functions as func

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)


@app.route(route="whois/{name}", methods=["GET"])
@app.text_completion_input(arg_name="response", prompt="Who is {name}?", max_tokens="100", model = "%CHAT_MODEL_DEPLOYMENT_NAME%")
def whois(req: func.HttpRequest, response: str) -> func.HttpResponse:
    response_json = json.loads(response)
    return func.HttpResponse(response_json["content"], status_code=200)


@app.route(route="genericcompletion", methods=["POST"])
@app.text_completion_input(arg_name="response", prompt="{Prompt}", model = "%CHAT_MODEL_DEPLOYMENT_NAME%")
def genericcompletion(req: func.HttpRequest, response: str) -> func.HttpResponse:
    response_json = json.loads(response)
    return func.HttpResponse(response_json["content"], status_code=200)