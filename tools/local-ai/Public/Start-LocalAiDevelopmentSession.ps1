function Start-LocalAiDevelopmentSession {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$TaskId,[Parameter(Mandatory)][string]$RepositoryRoot,[string]$ConfigPath,
        [Parameter(Mandatory)][string]$GameDir,[string[]]$Pattern=@(),[ValidateRange(0.001,1440)][double]$TimeoutMinutes=120
    )
    $task=Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    Assert-TaskOperation -Mode $task.Mode -Operation 'watch-log'
    if($task.PSObject.Properties['DevelopmentSession'] -and $task.DevelopmentSession.Active) {
        $existing=Get-Job -Id $task.DevelopmentSession.JobId -ErrorAction SilentlyContinue
        if($existing -and $existing.State -eq 'Running'){throw 'A development session is already active for this task.'}
    }
    $logPath=Resolve-GameLogPath -GameDir $GameDir
    if(-not (Test-Path -LiteralPath $logPath -PathType Leaf)){throw "LogOutput.log does not exist at: $logPath"}
    $capture=Join-Path $task.TaskDirectory 'log-capture.log'
    [IO.File]::WriteAllText($capture,'',[Text.UTF8Encoding]::new($false))
    $offset=(Get-Item -LiteralPath $logPath).Length
    $deadline=[DateTime]::UtcNow.AddMinutes($TimeoutMinutes)
    $job=Start-Job -ScriptBlock $script:LogWatcherScript -ArgumentList $logPath,$capture,$offset,@($Pattern),$deadline,(Join-Path $task.TaskDirectory 'task.json')
    $session=[pscustomobject]@{
        Active=$true; JobId=$job.Id; OwningProcessId=$PID; LogPath=$logPath; CapturePath=$capture;
        Patterns=@($Pattern); StartedAtUtc=[DateTime]::UtcNow.ToString('o'); DeadlineUtc=$deadline.ToString('o')
    }
    Save-DevelopmentSessionRecord -Task $task -Session $session
    Add-LocalAiTaskEvent -Task $task -Operation 'development-session-started' -Data @{JobId=$job.Id;LogPath=$logPath;CapturePath=$capture}
    return $session
}
