function ConvertTo-RedactedData {
    param([AllowNull()] $Value)

    if ($null -eq $Value) { return $null }
    if ($Value -is [string]) {
        $secret = $env:OPENWEBUI_API_KEY
        if ($secret -and $Value.Contains($secret, [StringComparison]::Ordinal)) {
            return $Value.Replace($secret, '[REDACTED]', [StringComparison]::Ordinal)
        }
        return $Value
    }
    if ($Value -is [Collections.IDictionary]) {
        $copy = [ordered]@{}
        foreach ($key in $Value.Keys) {
            $copy[[string] $key] = ConvertTo-RedactedData $Value[$key]
        }
        return $copy
    }
    if ($Value -is [Collections.IEnumerable] -and $Value -isnot [string]) {
        return @($Value | ForEach-Object { ConvertTo-RedactedData $_ })
    }
    if ($Value -is [psobject] -and @($Value.PSObject.Properties).Count -gt 0) {
        $copy = [ordered]@{}
        foreach ($property in $Value.PSObject.Properties) {
            $copy[$property.Name] = ConvertTo-RedactedData $property.Value
        }
        return $copy
    }
    return $Value
}

function Write-AtomicJson {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] $Value
    )

    $directory = Split-Path -Parent $Path
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $temporary = Join-Path $directory ('.' + [IO.Path]::GetFileName($Path) + '.' + [guid]::NewGuid().ToString('N') + '.tmp')
    try {
        $json = ConvertTo-RedactedData $Value | ConvertTo-Json -Depth 20
        [IO.File]::WriteAllText($temporary, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
        [IO.File]::Move($temporary, $Path, $true)
    } finally {
        if (Test-Path -LiteralPath $temporary) {
            Remove-Item -LiteralPath $temporary -Force
        }
    }
}

function Add-LocalAiTaskEvent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Task,
        [Parameter(Mandatory)] [string] $Operation,
        [hashtable] $Data = @{}
    )

    $event = [ordered]@{
        SchemaVersion = 1
        TaskId = $Task.TaskId
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        State = $Task.State
        Operation = $Operation
        Data = ConvertTo-RedactedData $Data
    }
    $line = $event | ConvertTo-Json -Depth 20 -Compress
    Add-Content -LiteralPath (Join-Path $Task.TaskDirectory 'events.jsonl') -Value $line -Encoding utf8
}

function Set-LocalAiTaskState {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Task,
        [Parameter(Mandatory)]
        [ValidateSet('created','context_ready','awaiting_model','model_complete','awaiting_review','accepted','failed','cancelled')]
        [string] $State,
        [hashtable] $Data
    )

    $next = @{
        created = @('context_ready','failed','cancelled')
        context_ready = @('awaiting_model','failed','cancelled')
        awaiting_model = @('model_complete','failed','cancelled')
        model_complete = @('awaiting_review','failed','cancelled')
        awaiting_review = @('accepted','failed','cancelled')
        accepted = @()
        failed = @()
        cancelled = @()
    }
    if ($State -notin $next[[string] $Task.State]) {
        throw "Invalid task state transition: $($Task.State) -> $State"
    }

    $copy = $Task | ConvertTo-Json -Depth 20 | ConvertFrom-Json
    $copy.State = $State
    $copy.UpdatedAtUtc = [DateTime]::UtcNow.ToString('o')
    if ($Data) {
        $copy.Data = [pscustomobject] (ConvertTo-RedactedData $Data)
    }
    Write-AtomicJson -Path (Join-Path $copy.TaskDirectory 'task.json') -Value $copy
    Add-LocalAiTaskEvent -Task $copy -Operation 'state-transition' -Data @{ From = $Task.State; To = $State; Data = $Data }
    return $copy
}

function New-TaskSlug {
    param([Parameter(Mandatory)] [string] $Goal)
    $slug = $Goal.ToLowerInvariant() -replace '[^a-z0-9]+','-'
    $slug = $slug.Trim('-')
    if (-not $slug) { $slug = 'task' }
    if ($slug.Length -gt 64) { $slug = $slug.Substring(0, 64).TrimEnd('-') }
    return $slug
}

function Get-GitBaselineCommit {
    param([Parameter(Mandatory)] [string] $RepositoryRoot)
    $output = & git -C $RepositoryRoot rev-parse HEAD 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Could not resolve Git baseline: $($output -join ' ')"
    }
    return ([string] ($output | Select-Object -Last 1)).Trim()
}
