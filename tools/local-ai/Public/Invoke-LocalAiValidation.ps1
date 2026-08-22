function Invoke-LocalAiValidation {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$TaskId,[Parameter(Mandatory)][string]$RepositoryRoot,[string]$ConfigPath)
    $task = Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    Assert-TaskOperation -Mode $task.Mode -Operation validate
    $scriptPath = Join-Path $task.Worktree.Path 'tools\validate-repo.py'
    return Invoke-NamedLocalAiOperation -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath -Operation validate -Component repository -FilePath py -ArgumentList @('-3',$scriptPath)
}
