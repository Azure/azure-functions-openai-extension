using namespace System.Net

param($InputIgnored, $TriggerMetadata)

$ErrorActionPreference = "Stop"

Write-Information "Fetching list of todos"

Get-Todos