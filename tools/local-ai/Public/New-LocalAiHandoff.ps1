function New-LocalAiHandoff {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$TaskId,[Parameter(Mandatory)][string]$RepositoryRoot,[string]$ConfigPath)
    $task=Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    $findings=Get-TaskFindings -Task $task
    $worktree=$null;$changed=@();$diffSummary=@()
    if($task.PSObject.Properties['Worktree']) {
        $worktree=Get-LocalAiWorktreeStatus -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
        $status=@(Invoke-BridgeGit -Repository $worktree.Path -Arguments @('status','--short'))
        $changed=@($status|ForEach-Object{if($_.Length -gt 3){$_.Substring(3).Trim()}}|Where-Object{$_}|Sort-Object -Unique)
        $diffSummary=@(Invoke-BridgeGit -Repository $worktree.Path -Arguments @('diff','--stat'))
    }
    $unresolved=if($findings -and $findings.PSObject.Properties['uncertainties']){@($findings.uncertainties)}else{@()}
    $handoff=[pscustomobject][ordered]@{
        SchemaVersion=1;TaskId=$task.TaskId;Goal=$task.Goal;Mode=$task.Mode;State=$task.State;BaselineCommit=$task.BaselineCommit;
        CreatedAtUtc=[DateTime]::UtcNow.ToString('o');Worktree=$worktree;ChangedFiles=@($changed);DiffSummary=@($diffSummary);Findings=$findings;
        Operations=if($task.PSObject.Properties['Operations']){@($task.Operations)}else{@()};
        Deployments=if($task.PSObject.Properties['Deployments']){@($task.Deployments)}else{@()};
        Risks=@();UnresolvedQuestions=@($unresolved);RecommendedNextActions=@('Review findings and diff','Accept or reject explicitly','Merge or push manually only if approved')
    }
    Write-AtomicJson -Path (Join-Path $task.TaskDirectory 'handoff.json') -Value $handoff
    $markdown=ConvertTo-RedactedData (Convert-HandoffToMarkdown -Handoff $handoff)
    [IO.File]::WriteAllText((Join-Path $task.TaskDirectory 'handoff.md'),$markdown,[Text.UTF8Encoding]::new($false))
    Add-LocalAiTaskEvent -Task $task -Operation 'handoff-created' -Data @{ChangedFiles=@($changed)}
    return $handoff
}
