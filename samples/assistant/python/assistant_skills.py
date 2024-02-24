import json
import logging
import uuid
import azure.functions as func

from todo_manager import CreateTodoManager, TodoItem

bp = func.Blueprint()

todo_manager = CreateTodoManager()


@bp.function_name("AddTodo")
@bp.generic_trigger(arg_name="taskDescription", type="assistantSkillTrigger", data_type=func.DataType.STRING, functionDescription="Create a new todo task")
def add_todo(taskDescription: str) -> None:
    if not taskDescription:
        raise ValueError("Task description cannot be empty")

    logging.info(f"Adding todo: {taskDescription}")

    todo_id = str(uuid.uuid4())[0:6]
    todo_manager.add_todo(TodoItem(id=todo_id, task=taskDescription))
    return


@bp.function_name("GetTodos")
@bp.generic_trigger(arg_name="inputIgnored", type="assistantSkillTrigger", data_type=func.DataType.STRING, functionDescription="Fetch the list of previously created todo tasks")
def get_todos(inputIgnored: str) -> str:
    logging.info("Fetching list of todos")
    results = todo_manager.get_todos()
    return json.dumps(results)
