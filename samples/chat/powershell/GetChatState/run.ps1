using namespace System.Net

param($Request, $TriggerMetadata, $ChatBotState)

Write-Host "Query state for chat $($ChatBotState.Id) by timeStampUtc $($ChatBotState.TimeStampUtc)"

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::OK
        Body       = $ChatBotState
    })