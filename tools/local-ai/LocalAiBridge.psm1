Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Private') -Filter '*.ps1' -File |
    Sort-Object Name |
    ForEach-Object { . $_.FullName }

Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Public') -Filter '*.ps1' -File |
    Sort-Object Name |
    ForEach-Object { . $_.FullName }
