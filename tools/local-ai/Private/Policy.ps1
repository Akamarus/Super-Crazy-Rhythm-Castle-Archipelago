function Resolve-ContainedPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string] $Path,
        [switch] $MustExist
    )

    if ([Management.Automation.WildcardPattern]::ContainsWildcardCharacters($Root) -or
        [Management.Automation.WildcardPattern]::ContainsWildcardCharacters($Path)) {
        throw 'Path values must not contain wildcard characters.'
    }

    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $rootFull -PathType Container)) {
        throw "Root does not exist: $rootFull"
    }

    $candidate = if ([IO.Path]::IsPathFullyQualified($Path)) {
        [IO.Path]::GetFullPath($Path)
    } else {
        [IO.Path]::GetFullPath((Join-Path $rootFull $Path))
    }

    if (-not (Test-PathInsideRoot -Root $rootFull -Path $candidate)) {
        throw "Path resolves outside approved root: $candidate"
    }

    $cursor = $candidate
    while ($cursor -and (Test-PathInsideRoot -Root $rootFull -Path $cursor)) {
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Path contains a reparse point: $($item.FullName)"
            }
        }
        if ($cursor.Equals($rootFull, [StringComparison]::OrdinalIgnoreCase)) { break }
        $cursor = Split-Path -Parent $cursor
    }

    if ($MustExist -and -not (Test-Path -LiteralPath $candidate)) {
        throw "Path does not exist: $candidate"
    }

    return $candidate
}

function Resolve-PluginDestination {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string] $GameDir)

    $gameRoot = [IO.Path]::GetFullPath($GameDir)
    if (-not (Test-Path -LiteralPath $gameRoot -PathType Container)) {
        throw "GameDir does not exist: $gameRoot"
    }
    return Resolve-ContainedPath -Root $gameRoot -Path 'BepInEx\plugins\RhythmCastleAP'
}

function Resolve-GameLogPath {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string] $GameDir)

    $gameRoot = [IO.Path]::GetFullPath($GameDir)
    if (-not (Test-Path -LiteralPath $gameRoot -PathType Container)) {
        throw "GameDir does not exist: $gameRoot"
    }
    return Resolve-ContainedPath -Root $gameRoot -Path 'BepInEx\LogOutput.log'
}

function Assert-TaskOperation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('investigation', 'implementation')]
        [string] $Mode,

        [Parameter(Mandatory)]
        [string] $Operation
    )

    $readOperations = @('read-context', 'invoke-model', 'create-handoff')
    $implementationOperations = @('edit', 'validate', 'build', 'deploy', 'watch-log', 'create-worktree')
    $known = $readOperations + $implementationOperations
    if ($Operation -notin $known) {
        throw "Unknown bridge operation: $Operation"
    }
    if ($Mode -eq 'investigation' -and $Operation -in $implementationOperations) {
        throw "Operation '$Operation' is not allowed for investigation tasks."
    }
}
