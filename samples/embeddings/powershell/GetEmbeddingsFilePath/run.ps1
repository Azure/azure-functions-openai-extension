using namespace System.Net

param($Request, $TriggerMetadata, $Embeddings)

$requestBody = (ConvertFrom-Json $Request.Body)

Write-Host "Received $($Embeddings.Count) embedding(s) for input file '$($requestBody.FilePath)'."

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::Accepted
})