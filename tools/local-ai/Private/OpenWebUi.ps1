function Invoke-OpenWebUiChat {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Configuration,
        [Parameter(Mandatory)] [AllowEmptyCollection()] [object[]] $Messages,
        [ValidateRange(1, 600)] [int] $TimeoutSec = 120
    )

    $baseUri = Assert-LoopbackUri -Uri ([uri] $Configuration.OpenWebUiBaseUri)
    $modelId = [string] $Configuration.ModelId
    $isAllowlisted = @(
        Get-AllowedLocalAiModelId | Where-Object {
            $_.Equals($modelId, [StringComparison]::Ordinal)
        }
    ).Count -gt 0
    if (-not $isAllowlisted) {
        throw "Open WebUI model must be allowlisted. Allowed values: 'jacks-assistant', 'jacks-assistant-fast'."
    }
    $apiKey = $env:OPENWEBUI_API_KEY
    if ([string]::IsNullOrWhiteSpace($apiKey)) {
        throw 'OPENWEBUI_API_KEY is required for Open WebUI calls.'
    }

    $uri = $baseUri.AbsoluteUri.TrimEnd('/') + '/api/chat/completions'
    $body = [ordered]@{
        model = $modelId
        messages = $Messages
    } | ConvertTo-Json -Depth 20
    $headers = @{ Authorization = "Bearer $apiKey" }

    try {
        $response = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -ContentType 'application/json' -Body $body -TimeoutSec $TimeoutSec
    } catch {
        $safeMessage = $_.Exception.Message.Replace($apiKey, '[REDACTED]', [StringComparison]::Ordinal)
        throw "Open WebUI request failed: $safeMessage"
    }

    $choicesProperty = $response.PSObject.Properties['choices']
    if (-not $choicesProperty -or @($choicesProperty.Value).Count -lt 1) {
        throw 'Open WebUI returned a malformed response: choices[0] is missing.'
    }
    $choice = @($choicesProperty.Value)[0]
    $messageProperty = $choice.PSObject.Properties['message']
    $contentProperty = if ($messageProperty) { $messageProperty.Value.PSObject.Properties['content'] } else { $null }
    if (-not $contentProperty -or $contentProperty.Value -isnot [string] -or [string]::IsNullOrWhiteSpace($contentProperty.Value)) {
        throw 'Open WebUI returned a malformed response: choices[0].message.content is missing.'
    }
    $idProperty = $response.PSObject.Properties['id']

    return [pscustomobject]@{
        Content = [string] $contentProperty.Value
        ResponseId = if ($idProperty) { [string] $idProperty.Value } else { $null }
        ModelId = $modelId
    }
}
