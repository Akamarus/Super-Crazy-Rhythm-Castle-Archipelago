function Stop-LocalAiDevelopmentSession {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$TaskId,[Parameter(Mandatory)][string]$RepositoryRoot,[string]$ConfigPath)
    $task=Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    if($task.PSObject.Properties['DevelopmentSession']) {
        $job=Get-Job -Id $task.DevelopmentSession.JobId -ErrorAction SilentlyContinue
        if($job -and $job.State -eq 'Running'){Stop-Job -Job $job}
        $task.DevelopmentSession.Active=$false
        $task.DevelopmentSession | Add-Member -NotePropertyName StoppedAtUtc -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force
        Save-DevelopmentSessionRecord -Task $task -Session $task.DevelopmentSession
        Add-LocalAiTaskEvent -Task $task -Operation 'development-session-stopped' -Data @{JobId=$task.DevelopmentSession.JobId}
    }
    return [pscustomobject]@{Stopped=$true;TaskId=$TaskId}
}
