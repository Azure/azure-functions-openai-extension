import json
import logging
import azure.functions as func

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)

DEFAULT_CHAT_STORAGE_SETTING = "AzureWebJobsStorage"
DEFAULT_CHAT_COLLECTION_NAME = "ChatState"


@app.function_name("CreateChatBot")
@app.route(route="chats/{chatId}", methods=["PUT"])
@app.assistant_create_output(arg_name="requests")
def create_chat_bot(
    req: func.HttpRequest, requests: func.Out[str]
) -> func.HttpResponse:
    chatId = req.route_params.get("chatId")
    input_json = req.get_json()
    logging.info(
        f"Creating chat ${chatId} from input parameters "
        f"${json.dumps(input_json)}"
    )
    create_request = {
        "id": chatId,
        "instructions": input_json.get("instructions"),
        "chatStorageConnectionSetting": DEFAULT_CHAT_STORAGE_SETTING,
        "collectionName": DEFAULT_CHAT_COLLECTION_NAME,
    }
    requests.set(json.dumps(create_request))
    response_json = {"chatId": chatId}
    return func.HttpResponse(
        json.dumps(response_json), status_code=202, mimetype="application/json"
    )


@app.function_name("GetChatState")
@app.route(route="chats/{chatId}", methods=["GET"])
@app.assistant_query_input(
    arg_name="state",
    id="{chatId}",
    timestamp_utc="{Query.timestampUTC}",
    chat_storage_connection_setting=DEFAULT_CHAT_STORAGE_SETTING,
    collection_name=DEFAULT_CHAT_COLLECTION_NAME,
)
def get_chat_state(req: func.HttpRequest, state: str) -> func.HttpResponse:
    return func.HttpResponse(
        state, status_code=200, mimetype="application/json"
    )


@app.function_name("PostUserResponse")
@app.route(route="chats/{chatId}", methods=["POST"])
@app.assistant_post_input(
    arg_name="state",
    id="{chatId}",
    user_message="{Query.message}",
    chat_model="%CHAT_MODEL_DEPLOYMENT_NAME%",
    chat_storage_connection_setting=DEFAULT_CHAT_STORAGE_SETTING,
    collection_name=DEFAULT_CHAT_COLLECTION_NAME,
)
def post_user_response(req: func.HttpRequest, state: str) -> func.HttpResponse:
    # Parse the JSON string into a dictionary
    data = json.loads(state)

    # Extract the content of the recentMessage
    recent_message_content = data["recentMessages"][0]["content"]
    return func.HttpResponse(
        recent_message_content, status_code=200, mimetype="text/plain"
    )
