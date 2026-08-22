function Get-LocalAiTask {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [ValidatePattern('^[a-f0-9]{12}$')] [string] $TaskId,
        [string] $RepositoryRoot = (Get-Location).Path,
        [string] $ConfigPath
    )

    $configuration = Get-LocalAiConfiguration -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    $taskPath = Resolve-ContainedPath -Root $configuration.StateRoot -Path (Join-Path $configuration.StateRoot "$TaskId\task.json") -MustExist
    return Get-Content -Raw -LiteralPath $taskPath | ConvertFrom-Json
}
