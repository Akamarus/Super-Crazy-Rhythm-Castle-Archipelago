function New-LocalAiTask {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [ValidateNotNullOrEmpty()] [string] $Goal,
        [Parameter(Mandatory)] [ValidateSet('investigation','implementation')] [string] $Mode,
        [Parameter(Mandatory)] [string] $RepositoryRoot,
        [string] $ConfigPath
    )

    $configuration = Get-LocalAiConfiguration -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    New-Item -ItemType Directory -Path $configuration.StateRoot -Force | Out-Null
    do {
        $taskId = [guid]::NewGuid().ToString('N').Substring(0, 12)
        $taskDirectory = Join-Path $configuration.StateRoot $taskId
    } while (Test-Path -LiteralPath $taskDirectory)
    New-Item -ItemType Directory -Path $taskDirectory | Out-Null

    $now = [DateTime]::UtcNow.ToString('o')
    $task = [pscustomobject] [ordered]@{
        SchemaVersion = 1
        TaskId = $taskId
        Slug = New-TaskSlug -Goal $Goal
        Goal = $Goal
        Mode = $Mode
        State = 'created'
        RepositoryRoot = $configuration.RepositoryRoot
        TaskDirectory = $taskDirectory
        BaselineCommit = Get-GitBaselineCommit -RepositoryRoot $configuration.RepositoryRoot
        CreatedAtUtc = $now
        UpdatedAtUtc = $now
        Data = [pscustomobject] @{}
    }
    Write-AtomicJson -Path (Join-Path $taskDirectory 'task.json') -Value $task
    New-Item -ItemType File -Path (Join-Path $taskDirectory 'events.jsonl') | Out-Null
    Add-LocalAiTaskEvent -Task $task -Operation 'task-created' -Data @{ Goal = $Goal; Mode = $Mode }
    return $task
}
