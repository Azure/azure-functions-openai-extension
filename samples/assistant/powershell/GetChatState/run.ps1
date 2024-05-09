using namespace System.Net

param($Request, $TriggerMetadata, $State)

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::OK
    Body       = $State
    Headers    = @{
        "Content-Type" = "application/json"
    }
})