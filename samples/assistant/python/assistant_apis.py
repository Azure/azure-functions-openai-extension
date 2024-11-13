import json
import azure.functions as func

apis = func.Blueprint()

DEFAULT_CHAT_STORAGE_SETTING = "AzureWebJobsStorage"
DEFAULT_CHAT_COLLECTION_NAME = "ChatState"

@apis.function_name("CreateAssistant")
@apis.route(route="assistants/{assistantId}", methods=["PUT"])
@apis.assistant_create_output(arg_name="requests")
def create_assistant(req: func.HttpRequest, requests: func.Out[str]) -> func.HttpResponse:
    assistantId = req.route_params.get("assistantId")
    instructions = """
            Don't make assumptions about what values to plug into functions.
            Ask for clarification if a user request is ambiguous.
            """
    create_request = {
        "id": assistantId,
        "instructions": instructions,
        "chatStorageConnectionSetting": DEFAULT_CHAT_STORAGE_SETTING,
        "collectionName": DEFAULT_CHAT_COLLECTION_NAME
    }
    requests.set(json.dumps(create_request))
    response_json = {"assistantId": assistantId}
    return func.HttpResponse(json.dumps(response_json), status_code=202, mimetype="application/json")


@apis.function_name("PostUserQuery")
@apis.route(route="assistants/{assistantId}", methods=["POST"])
@apis.assistant_post_input(arg_name="state", id="{assistantId}", user_message="{Query.message}", model="%CHAT_MODEL_DEPLOYMENT_NAME%", chat_storage_connection_setting=DEFAULT_CHAT_STORAGE_SETTING, collection_name=DEFAULT_CHAT_COLLECTION_NAME)
def post_user_response(req: func.HttpRequest, state: str) -> func.HttpResponse:
    # Parse the JSON string into a dictionary
    data = json.loads(state)

    # Extract the content of the recentMessage
    recent_message_content = data['recentMessages'][0]['content']
    return func.HttpResponse(recent_message_content, status_code=200, mimetype="text/plain")


@apis.function_name("GetChatState")
@apis.route(route="assistants/{assistantId}", methods=["GET"])
@apis.assistant_query_input(arg_name="state", id="{assistantId}", timestamp_utc="{Query.timestampUTC}", chat_storage_connection_setting=DEFAULT_CHAT_STORAGE_SETTING, collection_name=DEFAULT_CHAT_COLLECTION_NAME)
def get_chat_state(req: func.HttpRequest, state: str) -> func.HttpResponse:
    return func.HttpResponse(state, status_code=200, mimetype="application/json")
