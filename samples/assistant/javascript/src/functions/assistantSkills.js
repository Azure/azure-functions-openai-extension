// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { InvocationContext, app, trigger } from "@azure/functions"
import { TodoItem, ITodoManager, CreateTodoManager } from "../services/todoManager"
import { randomUUID } from 'crypto';

const todoManager: ITodoManager = CreateTodoManager()

app.generic('AddTodo', {
    trigger: trigger.generic({
        type: 'assistantSkillTrigger',
        functionDescription: 'Create a new todo task'
    }),
    handler: async (taskDescription: string, context: InvocationContext) => {
        if (!taskDescription) {
            throw new Error('Task description cannot be empty')
        }

        context.log(`Adding todo: ${taskDescription}`)

        const todoId = randomUUID().substring(0, 6)
        return todoManager.AddTodo(new TodoItem(todoId, taskDescription))
    }
})

app.generic('GetTodos', {
    trigger: trigger.generic({
        type: 'assistantSkillTrigger',
        functionDescription: 'Fetch the list of previously created todo tasks'
    }),
    handler: async (_, context: InvocationContext) => {
        context.log('Fetching list of todos')

        return todoManager.GetTodos()
    }
})