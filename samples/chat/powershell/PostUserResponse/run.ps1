using namespace System.Net

param($Request, $TriggerMetadata, $ChatBotState)

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::OK
    Body       = $ChatBotState.recentMessages[0].content
    Headers    = @{
        "Content-Type" = "text/plain"
    }
})