import json
import azure.functions as func

from assistant_skills import bp

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)

app.register_functions(bp)


@app.function_name("CreateAssistant")
@app.route(route="assistants/{assistantId}", methods=["PUT"])
@app.generic_output_binding(arg_name="requests", type="chatBotCreate", data_type=func.DataType.STRING)
def create_assistant(req: func.HttpRequest, requests: func.Out[str]) -> func.HttpResponse:
    assistantId = req.route_params.get("assistantId")
    instructions = """
            Don't make assumptions about what values to plug into functions.
            Ask for clarification if a user request is ambiguous.
            """
    create_request = {
        "id": assistantId,
        "instructions": instructions
    }
    requests.set(json.dumps(create_request))
    response_json = {"assistantId": assistantId}
    return func.HttpResponse(json.dumps(response_json), status_code=202, mimetype="application/json")


@app.function_name("PostUserQuery")
@app.route(route="assistants/{assistantId}", methods=["POST"])
@app.generic_output_binding(arg_name="requests", type="chatBotPost", data_type=func.DataType.STRING, id="{assistantId}", model="gpt-4")
def post_user_query(req: func.HttpRequest, requests: func.Out[str]) -> func.HttpResponse:
    userMessage = req.get_body().decode("utf-8")
    if not userMessage:
        return func.HttpResponse(json.dumps({"message": "Request body is empty"}), status_code=400, mimetype="application/json")

    requests.set(json.dumps({"userMessage": userMessage}))
    return func.HttpResponse(status_code=202)


@app.function_name("GetChatState")
@app.route(route="assistants/{assistantId}", methods=["GET"])
@app.generic_input_binding(arg_name="state", type="chatBotQuery", data_type=func.DataType.STRING, id="{assistantId}", timestampUtc="{Query.timestampUTC}")
def get_chat_state(req: func.HttpRequest, state: str) -> func.HttpResponse:
    return func.HttpResponse(state, status_code=200, mimetype="application/json")
