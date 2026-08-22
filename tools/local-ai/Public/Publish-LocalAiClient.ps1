function Publish-LocalAiClient {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$TaskId,
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [string]$ConfigPath,
        [Parameter(Mandatory)][string]$GameDir,
        [switch]$ConfirmDeployment
    )
    if (-not $ConfirmDeployment) { throw 'Client deployment requires -ConfirmDeployment.' }
    $task = Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    Assert-TaskOperation -Mode $task.Mode -Operation deploy
    $worktree = Get-LocalAiWorktreeStatus -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
    $successfulBuild = @()
    if ($task.PSObject.Properties['Operations']) {
        $successfulBuild = @($task.Operations | Where-Object { $_.Operation -eq 'build' -and $_.Component -eq 'client' -and $_.Succeeded })
    }
    if ($successfulBuild.Count -eq 0) { throw 'Deployment requires a successful client build record.' }

    $outputRoot = Resolve-ContainedPath -Root $worktree.Path -Path 'client\bin\Release\net6.0' -MustExist
    $excludedNames = @('0Harmony.dll','Il2CppInterop.Runtime.dll')
    $sources = @(Get-ChildItem -LiteralPath $outputRoot -File -Filter '*.dll' | Where-Object {
        $_.Name -notlike 'BepInEx.*' -and $_.Name -notin $excludedNames
    } | Sort-Object Name)
    if ($sources.Name -notcontains 'RhythmCastleAP.dll') { throw 'Release output is missing RhythmCastleAP.dll.' }

    $destination = Resolve-PluginDestination -GameDir $GameDir
    $stage = Join-Path $task.TaskDirectory ('deploy-stage-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage | Out-Null
    try {
        $manifest = foreach ($source in $sources) {
            $staged = Join-Path $stage $source.Name
            Copy-Item -LiteralPath $source.FullName -Destination $staged
            [pscustomobject]@{ Name=$source.Name; Source=$source.FullName; Sha256=(Get-FileHash -LiteralPath $staged -Algorithm SHA256).Hash }
        }
        $destinationCheck = Resolve-PluginDestination -GameDir $GameDir
        if (-not $destination.Equals($destinationCheck,[StringComparison]::OrdinalIgnoreCase)) { throw 'Plugin destination changed during deployment.' }
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        foreach ($file in $manifest) { Copy-Item -LiteralPath (Join-Path $stage $file.Name) -Destination (Join-Path $destination $file.Name) -Force }
        $record = [pscustomobject]@{
            TimestampUtc=[DateTime]::UtcNow.ToString('o'); Succeeded=$true; Destination=$destination; Files=@($manifest)
        }
    } catch {
        $record = [pscustomobject]@{ TimestampUtc=[DateTime]::UtcNow.ToString('o'); Succeeded=$false; Destination=$destination; Error=(ConvertTo-RedactedData $_.Exception.Message); Files=@() }
        throw
    } finally {
        if ($record) {
            $current = Get-LocalAiTask -TaskId $TaskId -RepositoryRoot $RepositoryRoot -ConfigPath $ConfigPath
            $existing = if ($current.PSObject.Properties['Deployments']) { @($current.Deployments) } else { @() }
            if ($current.PSObject.Properties['Deployments']) { $current.Deployments=@($existing+$record) }
            else { $current | Add-Member -NotePropertyName Deployments -NotePropertyValue @($record) }
            $current.UpdatedAtUtc=[DateTime]::UtcNow.ToString('o')
            Write-AtomicJson -Path (Join-Path $current.TaskDirectory 'task.json') -Value $current
            Add-LocalAiTaskEvent -Task $current -Operation deploy -Data @{ Record=$record }
        }
        if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
    }
    return $record
}
