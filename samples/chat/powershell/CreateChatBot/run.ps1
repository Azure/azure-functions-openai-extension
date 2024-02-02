using namespace System.Net

param($Request, $TriggerMetadata)

$chatID = $Request.Query.ChatId
$inputJson = $Request.Body
Wait-Debugger
Write-Host "Creating chat $chatID from input parameters $($inputJson)"

$createRequest = @{
    id = $chatID
    instructions = $inputJson.Instructions
}

$Response.SetContent([PSCustomObject]@{
    status = 202
    jsonBody = @{
        chatId = $chatID
    }
})

Push-OutputBinding -Name Response -Value $createRequest