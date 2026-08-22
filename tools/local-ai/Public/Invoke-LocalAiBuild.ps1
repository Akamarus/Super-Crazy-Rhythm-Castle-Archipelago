function Invoke-LocalAiBuild {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$TaskId,[Parameter(Mandatory)][string]$RepositoryRoot,[string]$ConfigPath,
        [Parameter(Mandatory)][ValidateSet('client','apworld')][string]$Component,[string]$GameDir
    )
    $task = Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    Assert-TaskOperation -Mode $task.Mode -Operation build
    if ($Component -eq 'client') {
        if (-not $GameDir) { throw 'GameDir is required for a client build.' }
        $scriptPath = Join-Path $task.Worktree.Path 'client\build.ps1'
        $arguments = @('-NoProfile','-File',$scriptPath,'-GameDir',[IO.Path]::GetFullPath($GameDir),'-SkipInstall')
    } else {
        $scriptPath = Join-Path $task.Worktree.Path 'tools\build-apworld.ps1'
        $arguments = @('-NoProfile','-File',$scriptPath)
    }
    return Invoke-NamedLocalAiOperation -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath -Operation build -Component $Component -FilePath pwsh -ArgumentList $arguments
}
