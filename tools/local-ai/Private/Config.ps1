function Assert-LoopbackUri {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [uri] $Uri
    )

    if ($Uri.Scheme -ne 'http' -or -not $Uri.IsLoopback) {
        throw "OpenWebUiBaseUri must be an HTTP loopback URI. Received: $Uri"
    }

    return $Uri
}

function Get-AllowedLocalAiModelId {
    return @('jacks-assistant', 'jacks-assistant-fast')
}

function Resolve-ConfigurationPath {
    param(
        [Parameter(Mandatory)] [string] $BasePath,
        [Parameter(Mandatory)] [string] $Value
    )

    if ([IO.Path]::IsPathFullyQualified($Value)) {
        return [IO.Path]::GetFullPath($Value)
    }

    return [IO.Path]::GetFullPath((Join-Path $BasePath $Value))
}

function Test-PathInsideRoot {
    param(
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string] $Path
    )

    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $pathFull = [IO.Path]::GetFullPath($Path)
    return $pathFull.Equals($rootFull, [StringComparison]::OrdinalIgnoreCase) -or
        $pathFull.StartsWith($rootFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Get-LocalAiConfiguration {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $RepositoryRoot,

        [string] $ConfigPath
    )

    $repositoryFull = [IO.Path]::GetFullPath($RepositoryRoot)
    if (-not (Test-Path -LiteralPath $repositoryFull -PathType Container)) {
        throw "RepositoryRoot does not exist: $repositoryFull"
    }

    $values = [ordered]@{
        OpenWebUiBaseUri = 'http://127.0.0.1:8080'
        ModelId = 'jacks-assistant'
        StateRoot = '.local-ai'
        WorktreeRoot = '..\SCRC-Archipelago-GitLab-Repo-ai-worktrees'
        GameDir = $null
    }

    if ($ConfigPath) {
        $localValues = Import-PowerShellDataFile -LiteralPath $ConfigPath
        foreach ($key in $localValues.Keys) {
            if (-not $values.Contains($key)) {
                throw "Unknown configuration key: $key"
            }
            $values[$key] = $localValues[$key]
        }
    }

    $baseUri = Assert-LoopbackUri -Uri ([uri] $values.OpenWebUiBaseUri)
    $modelId = [string] $values.ModelId
    $isAllowlisted = @(
        Get-AllowedLocalAiModelId | Where-Object {
            $_.Equals($modelId, [StringComparison]::Ordinal)
        }
    ).Count -gt 0
    if (-not $isAllowlisted) {
        throw "ModelId must be allowlisted. Allowed values: 'jacks-assistant', 'jacks-assistant-fast'."
    }

    $stateRoot = Resolve-ConfigurationPath -BasePath $repositoryFull -Value ([string] $values.StateRoot)
    if (-not (Test-PathInsideRoot -Root $repositoryFull -Path $stateRoot)) {
        throw 'StateRoot must resolve inside RepositoryRoot.'
    }

    $worktreeRoot = Resolve-ConfigurationPath -BasePath $repositoryFull -Value ([string] $values.WorktreeRoot)
    $gameDir = if ($values.GameDir) {
        Resolve-ConfigurationPath -BasePath $repositoryFull -Value ([string] $values.GameDir)
    } else {
        $null
    }

    [pscustomobject]@{
        RepositoryRoot = $repositoryFull
        OpenWebUiBaseUri = $baseUri
        ModelId = $modelId
        StateRoot = $stateRoot
        WorktreeRoot = $worktreeRoot
        GameDir = $gameDir
    }
}
