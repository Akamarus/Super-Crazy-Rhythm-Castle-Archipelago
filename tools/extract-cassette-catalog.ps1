[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$GameDir,

    [switch]$ValidateOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$requiredEnums = @(
    'ePlayableSong',
    'eSongCassetteStatus',
    'eLevelCassetteEarnResult',
    'eGameProgressionFlag'
)

$candidateMethods = @(
    'LevelLogic.EvaluatePlayerLevelSongCassettes',
    'LevelScoringEnquiries.GetCurrentLevelVariantSongCassettes',
    'CurrentPlayerSaveEnquiries.GetSongCassetteStatus',
    'SongCassetteEnquiries.GetSongCassetteStatus',
    'GameProgressionSaveDataState.SetSongCassetteStatus'
)

# Check the same display names used by the client and APWorld catalog tests
# against the native identifiers in this installation.
$contractPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'contracts/cassette-contract-v1.json'
$contract = Get-Content -LiteralPath $contractPath -Encoding UTF8 -Raw | ConvertFrom-Json
$expectedDisplaySongs = @($contract.entries.display_song)
if ($contract.schema -ne 1 -or $expectedDisplaySongs.Count -ne 30) {
    throw 'Expected the version 1 contract with all 30 display songs.'
}

# These names are not normalized aliases: the existing Music Lab variant
# mapping and retained gameplay evidence name their native enum counterparts.
$displaySongAliases = @{
    'Epical' = 'THE_EPICAL'
    'Money' = 'MONEY_DUB'
    'Sneaking' = 'SNEAKING_LOOP'
}

function Get-NormalizedIdentifier {
    param([Parameter(Mandatory = $true)][string]$Value)

    return ($Value -replace '[^A-Za-z0-9]', '').ToUpperInvariant()
}

function Get-AllTypes {
    param([Parameter(Mandatory = $true)]$Types)

    foreach ($type in $Types) {
        $type
        if ($type.NestedTypes.Count -gt 0) {
            Get-AllTypes -Types $type.NestedTypes
        }
    }
}

function Get-EnumEntry {
    param([Parameter(Mandatory = $true)]$Type)

    $values = @(
        $Type.Fields |
            Where-Object { $_.IsStatic -and $_.HasConstant } |
            ForEach-Object {
                [pscustomobject][ordered]@{
                    name = $_.Name
                    value = [int64]$_.Constant
                }
            } |
            Sort-Object value, name
    )

    return [pscustomobject][ordered]@{
        name = $Type.Name
        fullName = $Type.FullName
        values = $values
    }
}

function Get-MethodEntry {
    param(
        [Parameter(Mandatory = $true)][string]$Candidate,
        [Parameter(Mandatory = $true)]$AllTypes
    )

    $separator = $Candidate.LastIndexOf('.')
    $typeName = $Candidate.Substring(0, $separator)
    $methodName = $Candidate.Substring($separator + 1)
    $matches = @(
        $AllTypes |
            Where-Object { $_.Name -eq $typeName -or $_.FullName -eq $typeName } |
            ForEach-Object {
                $declaringType = $_
                $declaringType.Methods |
                    Where-Object { $_.Name -eq $methodName } |
                    ForEach-Object {
                        [pscustomobject][ordered]@{
                            candidate = $Candidate
                            fullName = $_.FullName
                            returnType = $_.ReturnType.FullName
                            parameters = @($_.Parameters | ForEach-Object { $_.ParameterType.FullName })
                        }
                    }
            } |
            Sort-Object fullName
    )

    return $matches
}

$cecilPath = Join-Path $GameDir 'BepInEx\core\Mono.Cecil.dll'
$assemblyPath = Join-Path $GameDir 'BepInEx\interop\Assembly-CSharp.dll'
foreach ($path in @($cecilPath, $assemblyPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required game file was not found: $path"
    }
}

Add-Type -Path $cecilPath
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath)
try {
    $allTypes = @(Get-AllTypes -Types $assembly.MainModule.Types)
    $enumEntries = @(
        foreach ($requiredEnum in $requiredEnums) {
            $enumType = @($allTypes | Where-Object { $_.Name -eq $requiredEnum })
            if ($enumType.Count -ne 1) {
                throw "Expected exactly one enum named '$requiredEnum'; found $($enumType.Count)."
            }
            if (-not $enumType[0].IsEnum) {
                throw "Type '$($enumType[0].FullName)' is not an enum."
            }
            Get-EnumEntry -Type $enumType[0]
        }
    ) | Sort-Object name

    $playableSongs = @($enumEntries | Where-Object { $_.name -eq 'ePlayableSong' })[0]
    $songByNormalizedName = @{}
    foreach ($song in $playableSongs.values) {
        $normalized = Get-NormalizedIdentifier -Value $song.name
        if ($songByNormalizedName.ContainsKey($normalized)) {
            throw "ePlayableSong contains a duplicate normalized identifier '$normalized'."
        }
        $songByNormalizedName[$normalized] = $song
    }

    $levelCandidates = @(
        foreach ($displaySong in $expectedDisplaySongs) {
            $nativeName = if ($displaySongAliases.ContainsKey($displaySong)) {
                $displaySongAliases[$displaySong]
            }
            else {
                $displaySong
            }
            $normalized = Get-NormalizedIdentifier -Value $nativeName
            if ($songByNormalizedName.ContainsKey($normalized)) {
                [pscustomobject][ordered]@{
                    displaySong = $displaySong
                    nativeSong = $songByNormalizedName[$normalized].name
                    nativeValue = $songByNormalizedName[$normalized].value
                }
            }
        }
    ) | Sort-Object displaySong

    $sourceCandidates = @(
        foreach ($candidateMethod in $candidateMethods) {
            Get-MethodEntry -Candidate $candidateMethod -AllTypes $allTypes
        }
    ) | Sort-Object fullName

    $resolvedDisplaySongs = @(foreach ($candidate in $levelCandidates) { $candidate.displaySong })
    $unresolved = @(@(
        foreach ($displaySong in $expectedDisplaySongs) {
            if ($displaySong -notin $resolvedDisplaySongs) {
                [pscustomobject][ordered]@{
                    kind = 'display-song'
                    value = $displaySong
                    reason = 'No exact normalized ePlayableSong identifier exists.'
                }
            }
        }
        foreach ($candidateMethod in $candidateMethods) {
            if (@($sourceCandidates | Where-Object { $_.candidate -eq $candidateMethod }).Count -eq 0) {
                [pscustomobject][ordered]@{
                    kind = 'candidate-method'
                    value = $candidateMethod
                    reason = 'Method was not found in Assembly-CSharp.dll.'
                }
            }
        }
    ) | Sort-Object kind, value)

    $result = [pscustomobject][ordered]@{
        schema = 1
        nativeEnums = $enumEntries
        levelCandidates = $levelCandidates
        sourceCandidates = $sourceCandidates
        unresolved = $unresolved
    }

    if ($ValidateOnly) {
        if ($levelCandidates.Count -ne $expectedDisplaySongs.Count) {
            throw "Expected $($expectedDisplaySongs.Count) display songs to resolve, but found $($levelCandidates.Count)."
        }
        if (@($levelCandidates.nativeSong | Select-Object -Unique).Count -ne $expectedDisplaySongs.Count) {
            throw 'Expected every display song to resolve to a unique native ePlayableSong identifier.'
        }

        $cassetteStatus = @($enumEntries | Where-Object { $_.name -eq 'eSongCassetteStatus' })[0].values.name
        foreach ($requiredStatus in @('HAVE_IN_BAG', 'HAVE_DEPOSITED')) {
            if ($cassetteStatus -notcontains $requiredStatus) {
                throw "eSongCassetteStatus is missing '$requiredStatus'."
            }
        }
        if ($cassetteStatus -notcontains 'INVALID' -and $cassetteStatus -notcontains 'HAVE_NOT_EARNED') {
            throw 'eSongCassetteStatus has no recognized unowned representation (INVALID or HAVE_NOT_EARNED).'
        }
    }

    $result | ConvertTo-Json -Depth 8 -Compress
}
finally {
    $assembly.Dispose()
}
