using namespace System.Net

param($Request, $TriggerMetadata)

$chatID = $Request.params.ChatId
$inputJson = $Request.Body
Write-Host "Creating chat $chatID from input parameters $($inputJson)"

$createRequest = @{
    id           = $chatID
    instructions = $inputJson.Instructions
}

Push-OutputBinding -Name ChatBotCreate -Value $createRequest

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::Accepted
        Body       = @{
            chatId = $chatID
        }
    })