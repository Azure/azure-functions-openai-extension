import json
import logging
import azure.functions as func

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)


@app.function_name("CreateChatBot")
@app.route(route="chats/{chatID}", methods=["PUT"])
@app.generic_output_binding(arg_name="requests", type="chatBotCreate", data_type=func.DataType.STRING)
def create_chat_bot(req: func.HttpRequest, requests: func.Out[str]) -> func.HttpResponse:
    chatID = req.route_params.get("chatID")
    input_json = req.get_json()
    logging.info(
        f"Creating chat ${chatID} from input parameters ${json.dumps(input_json)}")
    create_request = {
        "id": chatID,
        "instructions": input_json.get("instructions")
    }
    requests.set(json.dumps(create_request))
    response_json = {"chatId": chatID}
    return func.HttpResponse(json.dumps(response_json), status_code=202, mimetype="application/json")


@app.function_name("GetChatState")
@app.route(route="chats/{chatID}", methods=["GET"])
@app.generic_input_binding(arg_name="state", type="chatBotQuery", data_type=func.DataType.STRING, id="{chatID}", timestampUtc="{Query.timestampUTC}")
def get_chat_state(req: func.HttpRequest, state: str) -> func.HttpResponse:
    return func.HttpResponse(state, status_code=200, mimetype="application/json")


@app.function_name("PostUserResponse")
@app.route(route="chats/{chatID}", methods=["POST"])
@app.generic_output_binding(arg_name="messages", type="chatBotPost", data_type=func.DataType.STRING, id="{chatID}", model = "%AZURE_DEPLOYMENT_NAME%")
def post_user_response(req: func.HttpRequest, messages: func.Out[str]) -> func.HttpResponse:
    userMessage = req.get_body().decode("utf-8")
    if not userMessage:
        return func.HttpResponse(json.dumps({"message": "No message provided"}), status_code=400, mimetype="application/json")
    chat_post_request = {
        "chatId": req.route_params.get("chatID"),
        "userMessage": userMessage
    }
    logging.info(
        f"Creating post request with parameters: ${json.dumps(chat_post_request)}")
    messages.set(json.dumps(chat_post_request))
    return func.HttpResponse(status_code=202)

@app.route(route="http_trigger", auth_level=func.AuthLevel.ANONYMOUS)
def http_trigger(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    name = req.params.get('name')
    if not name:
        try:
            req_body = req.get_json()
        except ValueError:
            pass
        else:
            name = req_body.get('name')

    if name:
        return func.HttpResponse(f"Hello, {name}. This HTTP triggered function executed successfully.")
    else:
        return func.HttpResponse(
             "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.",
             status_code=200
        )