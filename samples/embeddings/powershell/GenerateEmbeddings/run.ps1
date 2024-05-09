using namespace System.Net

param($Request, $TriggerMetadata, $Embeddings)

$input = $Request.Body.RawText

Write-Host "Received $($Embeddings.Count) embedding(s) for input text containing $($input.Length) characters."

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::Accepted
})