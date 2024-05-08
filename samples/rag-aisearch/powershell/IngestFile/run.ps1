using namespace System.Net

param($Request, $TriggerMetadata)

$ErrorActionPreference = 'Stop'

$inputJson = $Request.Body | ConvertFrom-Json

if (-not $inputJson -or -not $inputJson.Url) {
    throw 'Invalid request body. Make sure that you pass in {\"Url\": value } as the request body.'
}

$uri = [URI]$inputJson.Url

$title = [System.IO.Path]::GetExtension($uri)

Push-OutputBinding -Name EmbeddingsStoreOutput -Value @{
    "title" = $uri
}

$response = @{
    "status" = "success"
    "title" = $title
}

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::OK
        Body = $response
        Headers    = @{
            "Content-Type" = "application/json"
        }
})