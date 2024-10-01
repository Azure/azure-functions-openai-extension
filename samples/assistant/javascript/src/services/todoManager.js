const { CosmosClient } = require("@azure/cosmos");

class TodoItem {
    id;
    task;
    constructor(id, task) {
        this.id = id;
        this.task = task;
    }
}

class InMemoryTodoManager {
    todos = []

    async AddTodo(todo) {
        this.todos.push(todo)
    }

    async GetTodos() {
        return this.todos
    }
}

class CosmosDbTodoManager {
    container;

    constructor(cosmosClient) {
        this.createContainerIfNotExists(cosmosClient);
    }

    async AddTodo(todo) {
        console.log(`Adding todo ID = ${todo.id} to container '${this.container.id}'.`)
        await this.container.items.create(todo)
    }

    async GetTodos() {
        console.log(`Getting all todos from container '${this.container.id}'.`)
        const { resources } = await this.container.items.readAll().fetchAll()
        console.log(`Found ${resources.length} todos in container '${this.container.id}'.`)
        return resources
    }

    async createContainerIfNotExists(cosmosClient) {
        const cosmosDatabaseName = process.env.CosmosDatabaseName;
        const cosmosContainerName = process.env.CosmosContainerName;

        if (!cosmosDatabaseName || !cosmosContainerName) {
            throw new Error("CosmosDatabaseName and CosmosContainerName must be set as environment variables or in local.settings.json");
        }
        await cosmosClient.databases.createIfNotExists({ id: cosmosDatabaseName });

        await cosmosClient.database(cosmosDatabaseName).containers.createIfNotExists(
            { id: cosmosContainerName, partitionKey: { paths: ["/id"] } }
        );

        this.container = cosmosClient.database(cosmosDatabaseName).container(cosmosContainerName);
    }

}

function CreateTodoManager() {
    const cosmosDbConnectionString = process.env.CosmosDbConnectionString
    if (!cosmosDbConnectionString) {
        return new InMemoryTodoManager()
    } else {
        const cosmosClient = new CosmosClient(cosmosDbConnectionString)
        return new CosmosDbTodoManager(cosmosClient)
    }
}

exports.TodoItem = TodoItem;
exports.CreateTodoManager = CreateTodoManager;