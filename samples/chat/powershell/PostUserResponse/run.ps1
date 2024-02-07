using namespace System.Net

param($Request, $TriggerMetadata)

$chatID = $Request.params.ChatId
$userMessage = $Request.Body

Write-Host "ChatID: $chatID"
Write-Host "UserMessage: $userMessage"

# Check if the userMessage is empty
if (-not $userMessage)
{
    Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
            StatusCode = [HttpStatusCode]::BadRequest
            message    = "Request body is empty"
        })
    return
}

# Create a custom object to represent the ChatBotPostRequest
$chatBotPostRequest = [PSCustomObject]@{
    UserMessage = $userMessage
}


# Add the ChatBotPostRequest to the output binding
Push-OutputBinding -Name newMessages -Value $chatBotPostRequest

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::Accepted
    })