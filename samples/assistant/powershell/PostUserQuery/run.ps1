using namespace System.Net

param($Request, $TriggerMetadata, $State)

$recent_message_content = "No recent messages!"

if ($State.recentMessages.Count -gt 0) {
    $recent_message_content = $State.recentMessages[0].content
}

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::OK
    Body       = $recent_message_content
    Headers    = @{
        "Content-Type" = "application/json"
    }
})