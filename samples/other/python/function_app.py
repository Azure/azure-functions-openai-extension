import json
import azure.functions as func

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)


@app.route(route="whois/{name}", methods=["GET"])
@app.generic_input_binding(arg_name="response", type="textCompletion", data_type=func.DataType.STRING, prompt="Who is {name}?", maxTokens="100", model = "gpt-3.5-turbo")
def whois(req: func.HttpRequest, response: str) -> func.HttpResponse:
    response_json = json.loads(response)
    return func.HttpResponse(response_json["content"], status_code=200)


@app.route(route="genericcompletion", methods=["POST"])
@app.generic_input_binding(arg_name="response", type="textCompletion", data_type=func.DataType.STRING, prompt="{Prompt}", model = "gpt-3.5-turbo")
def genericcompletion(req: func.HttpRequest, response: str) -> func.HttpResponse:
    response_json = json.loads(response)
    return func.HttpResponse(response_json["content"], status_code=200)