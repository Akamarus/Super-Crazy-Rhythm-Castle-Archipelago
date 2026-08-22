function Invoke-BridgeProcess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $FilePath,
        [Parameter(Mandatory)] [string[]] $ArgumentList,
        [Parameter(Mandatory)] [string] $WorkingDirectory
    )
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $FilePath
    $info.WorkingDirectory = $WorkingDirectory
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($argument in $ArgumentList) { [void] $info.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $info
    [void] $process.Start()
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        Output = @(($stdout + [Environment]::NewLine + $stderr) -split '\r?\n' | Where-Object { $_ } | Select-Object -First 500)
    }
}

function Save-LocalAiOperationResult {
    param($Task, [string] $Operation, [string] $Component, $ProcessResult)
    $record = [pscustomobject]@{
        Operation = $Operation
        Component = $Component
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        ExitCode = [int] $ProcessResult.ExitCode
        Succeeded = ([int] $ProcessResult.ExitCode -eq 0)
        Output = @(ConvertTo-RedactedData $ProcessResult.Output)
    }
    $existing = if ($Task.PSObject.Properties['Operations']) { @($Task.Operations) } else { @() }
    if ($Task.PSObject.Properties['Operations']) { $Task.Operations = @($existing + $record) }
    else { $Task | Add-Member -NotePropertyName Operations -NotePropertyValue @($record) }
    $Task.UpdatedAtUtc = [DateTime]::UtcNow.ToString('o')
    Write-AtomicJson -Path (Join-Path $Task.TaskDirectory 'task.json') -Value $Task
    Add-LocalAiTaskEvent -Task $Task -Operation $Operation -Data @{ Record = $record }
    return $record
}

function Invoke-NamedLocalAiOperation {
    param(
        [string] $TaskId, [string] $RepositoryRoot, [string] $ConfigPath,
        [string] $Operation, [string] $Component, [string] $FilePath, [string[]] $ArgumentList
    )
    $task = Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    Assert-TaskOperation -Mode $task.Mode -Operation $Operation
    $worktree = Get-LocalAiWorktreeStatus -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    $processResult = Invoke-BridgeProcess -FilePath $FilePath -ArgumentList $ArgumentList -WorkingDirectory $worktree.Path
    $record = Save-LocalAiOperationResult -Task $task -Operation $Operation -Component $Component -ProcessResult $processResult
    if (-not $record.Succeeded) { throw "$Operation failed with exit code $($record.ExitCode)." }
    return $record
}
