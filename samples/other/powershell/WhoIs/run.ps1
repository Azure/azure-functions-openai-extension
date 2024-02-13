using namespace System.Net

param($Request, $TriggerMetadata, $TextCompletionResponse)

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::OK
        Body       = $TextCompletionResponse.Content
    })