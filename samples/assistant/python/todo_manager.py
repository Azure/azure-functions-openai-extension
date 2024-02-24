import abc
import logging
import os

from azure.cosmos import CosmosClient


class TodoItem:
    def __init__(self, id, task):
        self.id = id
        self.task = task


class ITodoManager(metaclass=abc.ABCMeta):
    @abc.abstractmethod
    def add_todo(self, todo: TodoItem):
        raise NotImplementedError()

    @abc.abstractmethod
    def get_todos(self):
        raise NotImplementedError()


class InMemoryTodoManager(ITodoManager):
    def __init__(self):
        self.todos = []

    def add_todo(self, todo: TodoItem):
        self.todos.append(todo)

    def get_todos(self):
        return [item.__dict__ for item in self.todos]


class CosmosDbTodoManager(ITodoManager):
    def __init__(self, cosmos_client: CosmosClient):
        self.cosmos_client = cosmos_client
        self.database = self.cosmos_client.get_database_client("testdb")
        self.container = self.database.get_container_client("my-todos")

    def add_todo(self, todo: TodoItem):
        logging.info(
            f"Adding todo ID = {todo.id} to container '{self.container.id}'.")
        self.container.create_item(todo.__dict__)

    def get_todos(self):
        logging.info(
            f"Getting all todos from container '{self.container.id}'.")
        results = [item for item in self.container.query_items(
            "SELECT * FROM c", enable_cross_partition_query=True)]
        logging.info(
            f"Found {len(results)} todos in container '{self.container.id}'.")
        return results


def CreateTodoManager() -> ITodoManager:
    if not os.environ.get("CosmosDbConnectionString"):
        return InMemoryTodoManager()
    else:
        cosmos_client = CosmosClient.from_connection_string(
            os.environ["CosmosDbConnectionString"])
        return CosmosDbTodoManager(cosmos_client)
