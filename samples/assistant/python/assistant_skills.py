import json
import logging
import uuid
import azure.functions as func

from todo_manager import CreateTodoManager, TodoItem

skills = func.Blueprint()

todo_manager = CreateTodoManager()


@skills.function_name("AddTodo")
@skills.assistant_skill_trigger(
    arg_name="taskDescription", function_description="Create a new todo task"
)
def add_todo(taskDescription: str) -> None:
    if not taskDescription:
        raise ValueError("Task description cannot be empty")

    logging.info(f"Adding todo: {taskDescription}")

    todo_id = str(uuid.uuid4())[0:6]
    todo_manager.add_todo(TodoItem(id=todo_id, task=taskDescription))
    return


@skills.function_name("GetTodos")
@skills.assistant_skill_trigger(
    arg_name="inputIgnored",
    function_description="Fetch the list of previously created todo tasks",
)
def get_todos(inputIgnored: str) -> str:
    logging.info("Fetching list of todos")
    results = todo_manager.get_todos()
    return json.dumps(results)
