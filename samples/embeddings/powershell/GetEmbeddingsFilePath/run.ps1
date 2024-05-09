using namespace System.Net

param($Request, $TriggerMetadata, $Embeddings)

Write-Host "Received $($Embeddings.Count) embedding(s) for input file '$($Request.Body.FilePath)'."

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::Accepted
})