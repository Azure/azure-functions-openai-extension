using namespace System.Net

param($InputIgnored, $TriggerMetadata)

$ErrorActionPreference = "Stop"

Write-Information "Fetching list of todos"

# TODO: is writing to logging really the correct approach?
Get-Todos