param(
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceDir = Join-Path $repoRoot "apworld\scrc"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "dist"
}

if (!(Test-Path $sourceDir)) {
    throw "APWorld source directory not found: $sourceDir"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("scrc-apworld-" + [guid]::NewGuid().ToString("N"))
$tempPackage = Join-Path $tempRoot "scrc"
New-Item -ItemType Directory -Force -Path $tempPackage | Out-Null

try {
    Copy-Item (Join-Path $sourceDir "*") $tempPackage -Recurse -Force

    $zipPath = Join-Path $OutputDir "scrc.zip"
    $apworldPath = Join-Path $OutputDir "scrc.apworld"

    Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
    Remove-Item $apworldPath -Force -ErrorAction SilentlyContinue

    Compress-Archive -Path $tempPackage -DestinationPath $zipPath -CompressionLevel Optimal
    Move-Item $zipPath $apworldPath -Force

    Write-Host "Built:" $apworldPath
}
finally {
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
