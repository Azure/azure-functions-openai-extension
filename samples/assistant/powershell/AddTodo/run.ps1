using namespace System.Net

param($TaskDescription, $TriggerMetadata)

$ErrorActionPreference = "Stop"

if (-not $TaskDescription) {
    throw "Task description cannot be empty"
}

Write-Information "Adding todo: $TaskDescription"

$todoID = [Guid]::NewGuid().ToString().Substring(0, 5)

# TODO: Add the todo to a list of todos
Add-Todo $todoId $TaskDescription