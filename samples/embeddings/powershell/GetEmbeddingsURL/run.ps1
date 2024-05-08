using namespace System.Net

param($Request, $TriggerMetadata, $Embeddings)

Write-Host "Received $($Embeddings.Count) embedding(s) for input url '$($Request.Body.Url)'."

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::Accepted
})