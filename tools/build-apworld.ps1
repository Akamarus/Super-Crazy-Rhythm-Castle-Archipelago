param(
    [string]$OutputDir = "",
    [string]$Python = "python",
    [string]$SourceRevision = ""
)
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDir)) { $OutputDir = Join-Path $repoRoot "dist" }
if ([string]::IsNullOrWhiteSpace($SourceRevision)) {
    $SourceRevision = & git -C $repoRoot rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw "Specify -SourceRevision when building outside Git" }
    $dirty = & git -C $repoRoot status --porcelain
    if ($LASTEXITCODE -ne 0) { throw "Cannot determine source state" }
    if ($dirty) { $SourceRevision += "+dirty-local" }
}
$sourceDir = Join-Path $repoRoot "apworld/scrc"
$metadata = Get-Content (Join-Path $sourceDir "archipelago.json") -Raw | ConvertFrom-Json
$release = "APWorld " + $metadata.world_version
& $Python (Join-Path $PSScriptRoot "package_release.py") --kind apworld --input $sourceDir --output (Join-Path $OutputDir "scrc.apworld") --source-revision $SourceRevision --release $release
if ($LASTEXITCODE -ne 0) { throw "APWorld package validation failed" }
