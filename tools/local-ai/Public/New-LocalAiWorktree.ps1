function New-LocalAiWorktree {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $TaskId,
        [Parameter(Mandatory)] [string] $RepositoryRoot,
        [string] $ConfigPath
    )
    $task = Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    if ($task.Mode -ne 'implementation') {
        throw 'New-LocalAiWorktree requires an implementation task.'
    }
    if ($task.PSObject.Properties['Worktree']) {
        throw 'Task already has a recorded worktree.'
    }
    $configuration = Get-LocalAiConfiguration -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    if (Test-PathInsideRoot -Root $task.RepositoryRoot -Path $configuration.WorktreeRoot) {
        throw 'WorktreeRoot must resolve outside the main repository checkout.'
    }
    $baselineCheck = & git -C $task.RepositoryRoot cat-file -e "$($task.BaselineCommit)^{commit}" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Recorded baseline cannot be resolved: $($task.BaselineCommit)"
    }
    $branch = "ai/$($task.Slug)-$($task.TaskId.Substring(0,6))"
    $destination = [IO.Path]::GetFullPath((Join-Path $configuration.WorktreeRoot "$($task.Slug)-$($task.TaskId)"))
    if (Test-Path -LiteralPath $destination) {
        throw "Worktree destination already exists: $destination"
    }
    New-Item -ItemType Directory -Path $configuration.WorktreeRoot -Force | Out-Null
    Invoke-BridgeGit -Repository $task.RepositoryRoot -Arguments @('worktree','add','-b',$branch,$destination,$task.BaselineCommit) -FailureMessage 'Could not create isolated worktree' | Out-Null

    $record = [pscustomobject]@{
        Branch = $branch
        Path = $destination
        BaselineCommit = $task.BaselineCommit
        CreatedAtUtc = [DateTime]::UtcNow.ToString('o')
    }
    $task | Add-Member -NotePropertyName Worktree -NotePropertyValue $record
    $task.UpdatedAtUtc = [DateTime]::UtcNow.ToString('o')
    Write-AtomicJson -Path (Join-Path $task.TaskDirectory 'task.json') -Value $task
    Add-LocalAiTaskEvent -Task $task -Operation 'worktree-created' -Data @{ Branch = $branch; Path = $destination; BaselineCommit = $task.BaselineCommit }

    return Get-LocalAiWorktreeStatus -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
}
