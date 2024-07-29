// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const { app, trigger } = require("@azure/functions");
const { TodoItem, CreateTodoManager } = require("../services/todoManager");
const { randomUUID } = require('crypto');

const todoManager = CreateTodoManager()

app.generic('AddTodo', {
    trigger: trigger.generic({
        type: 'assistantSkillTrigger',
        functionDescription: 'Create a new todo task'
    }),
    handler: async (taskDescription, context) => {
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
    handler: async (_, context) => {
        context.log('Fetching list of todos')

        return todoManager.GetTodos()
    }
})