using namespace System.Net

param($Request, $TriggerMetadata)

$ErrorActionPreference = 'Stop'

$inputJson = $Request.Body

if (-not $inputJson -or -not $inputJson.Url) {
    throw 'Invalid request body. Make sure that you pass in {\"url\": value } as the request body.'
}

$uri = [URI]$inputJson.Url
$filename = [System.IO.Path]::GetFileName($uri.AbsolutePath)


Push-OutputBinding -Name EmbeddingsStoreOutput -Value @{
    "title" = $filename
}

$response = @{
    "status" = "success"
    "title" = $filename
}

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::OK
        Body = $response
        Headers    = @{
            "Content-Type" = "application/json"
        }
})