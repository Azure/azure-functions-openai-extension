import { Container, CosmosClient } from "@azure/cosmos"

export class TodoItem {
    constructor(public id: string, public task: string) { }
}

export interface ITodoManager {
    AddTodo: (todo: TodoItem) => Promise<void>
    GetTodos: () => Promise<TodoItem[]>
}

class InMemoryTodoManager implements ITodoManager {
    private todos: TodoItem[] = []

    public async AddTodo(todo: TodoItem) {
        this.todos.push(todo)
    }

    public async GetTodos() {
        return this.todos
    }
}

class CosmosDbTodoManager implements ITodoManager {
    private container: Container

    constructor(cosmosClient: CosmosClient) {
        this.container = cosmosClient.database('testdb').container('my-todos')
    }

    public async AddTodo(todo: TodoItem) {
        console.log(`Adding todo ID = ${todo.id} to container '${this.container.id}'.`)
        await this.container.items.create(todo)
    }

    public async GetTodos() {
        console.log(`Getting all todos from container '${this.container.id}'.`)
        const { resources } = await this.container.items.readAll<TodoItem>().fetchAll()
        console.log(`Found ${resources.length} todos in container '${this.container.id}'.`)
        return resources
    }
}

export function CreateTodoManager(): ITodoManager {
    const cosmosDbConnectionString = process.env.CosmosDbConnectionString
    if (!cosmosDbConnectionString) {
        return new InMemoryTodoManager()
    } else {
        const cosmosClient = new CosmosClient(cosmosDbConnectionString)
        return new CosmosDbTodoManager(cosmosClient)
    }
}