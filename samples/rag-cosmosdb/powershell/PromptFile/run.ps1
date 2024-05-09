using namespace System.Net

param($Request, $TriggerMetadata, $SemanticSearchInput)

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::OK
        Body       = $SemanticSearchInput.Response
    })