function Invoke-BridgeGit {
    param(
        [Parameter(Mandatory)] [string] $Repository,
        [Parameter(Mandatory)] [string[]] $Arguments,
        [string] $FailureMessage = 'Git operation failed'
    )
    $output = & git -C $Repository @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "$FailureMessage`: $($output -join ' ')"
    }
    return @($output)
}

function Get-AbsoluteGitPath {
    param([string] $Repository, [string] $GitPath)
    if ([IO.Path]::IsPathFullyQualified($GitPath)) {
        return [IO.Path]::GetFullPath($GitPath)
    }
    return [IO.Path]::GetFullPath((Join-Path $Repository $GitPath))
}

function Get-LocalAiWorktreeStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $TaskId,
        [Parameter(Mandatory)] [string] $RepositoryRoot,
        [string] $ConfigPath
    )
    $task = Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    if (-not $task.PSObject.Properties['Worktree']) {
        throw 'Task has no recorded worktree.'
    }
    $path = [string] $task.Worktree.Path
    if (-not (Test-Path -LiteralPath $path -PathType Container)) {
        throw "Recorded worktree does not exist: $path"
    }
    $branch = [string] ((Invoke-BridgeGit -Repository $path -Arguments @('branch','--show-current') -FailureMessage 'Could not read worktree branch') | Select-Object -Last 1)
    if ($branch -ne $task.Worktree.Branch) {
        throw "Worktree branch mismatch: expected $($task.Worktree.Branch), got $branch"
    }
    $head = [string] ((Invoke-BridgeGit -Repository $path -Arguments @('rev-parse','HEAD') -FailureMessage 'Could not read worktree HEAD') | Select-Object -Last 1)
    if ($head -ne $task.BaselineCommit) {
        throw "Worktree baseline mismatch: expected $($task.BaselineCommit), got $head"
    }
    $repoCommonRaw = [string] ((Invoke-BridgeGit -Repository $task.RepositoryRoot -Arguments @('rev-parse','--git-common-dir')) | Select-Object -Last 1)
    $worktreeCommonRaw = [string] ((Invoke-BridgeGit -Repository $path -Arguments @('rev-parse','--git-common-dir')) | Select-Object -Last 1)
    $repoCommon = Get-AbsoluteGitPath -Repository $task.RepositoryRoot -GitPath $repoCommonRaw
    $worktreeCommon = Get-AbsoluteGitPath -Repository $path -GitPath $worktreeCommonRaw
    if (-not $repoCommon.Equals($worktreeCommon, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Worktree Git metadata does not belong to the task repository.'
    }
    return [pscustomobject]@{
        Verified = $true
        Branch = $branch
        Path = [IO.Path]::GetFullPath($path)
        BaselineCommit = $head
        GitCommonDirectory = $worktreeCommon
    }
}
