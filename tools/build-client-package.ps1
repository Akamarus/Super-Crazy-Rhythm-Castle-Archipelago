param(
    [string]$InputDir = "",
    [Parameter(Mandatory=$true)][string]$OutputPath,
    [Parameter(Mandatory=$true)][string]$SourceRevision,
    [Parameter(Mandatory=$true)][string]$Release,
    [string]$Python = "python"
)
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($InputDir)) {
    $InputDir = Join-Path $repoRoot "client/bin/Release/net6.0"
}
& $Python (Join-Path $PSScriptRoot "package_release.py") --kind client --input $InputDir --output $OutputPath --source-revision $SourceRevision --release $Release
if ($LASTEXITCODE -ne 0) { throw "Client package validation failed" }
