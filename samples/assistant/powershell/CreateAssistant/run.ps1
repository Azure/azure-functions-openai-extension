using namespace System.Net

param($Request, $TriggerMetadata)

$assistantId = $Request.params.assistantId

$instructions = "Don't make assumptions about what values to plug into functions."
$instructions += "\nAsk for clarification if a user request is ambiguous."

$create_request = @{
    "id" = $assistantId
    "instructions" = $instructions
    "chatStorageConnectionSetting" = "AzureWebJobsStorage"
    "collectionName" = "ChatState"
}

Push-OutputBinding -Name Requests -Value (ConvertTo-Json $create_request)

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::Accepted
    Body       = (ConvertTo-Json @{ "assistantId" = $assistantId})
    Headers    = @{
        "Content-Type" = "application/json"
    }
})