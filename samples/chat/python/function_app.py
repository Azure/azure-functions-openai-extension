import json
import logging
import azure.functions as func

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)


@app.function_name("CreateChatBot")
@app.route(route="chats/{chatID}", methods=["PUT"])
@app.generic_output_binding(arg_name="requests", type="assistantCreate", data_type=func.DataType.STRING)
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
@app.generic_input_binding(arg_name="state", type="assistantQuery", data_type=func.DataType.STRING, id="{chatID}", timestampUtc="{Query.timestampUTC}")
def get_chat_state(req: func.HttpRequest, state: str) -> func.HttpResponse:
    return func.HttpResponse(state, status_code=200, mimetype="application/json")


@app.function_name("PostUserResponse")
@app.route(route="chats/{chatID}", methods=["POST"])
@app.generic_input_binding(arg_name="state", type="assistantPost", data_type=func.DataType.STRING, id="{chatID}", userMessage="{Query.message}", model="%CHAT_MODEL_DEPLOYMENT_NAME%")
def post_user_response(req: func.HttpRequest, state: str) -> func.HttpResponse:
    # Parse the JSON string into a dictionary
    data = json.loads(state)

    # Extract the content of the recentMessage
    recent_message_content = data['recentMessages'][0]['content']
    return func.HttpResponse(recent_message_content, status_code=200, mimetype="text/plain")