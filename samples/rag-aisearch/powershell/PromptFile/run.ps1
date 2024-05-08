using namespace System.Net

param($Request, $TriggerMetadata, $SemanticSearchInput)

$chatID = $Request.params.ChatId
$inputJson = $Request.Body

Write-Host "Creating chat $chatID from input parameters $($inputJson)"

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::OK
        Body       = $SemanticSearchInput.Response
    })