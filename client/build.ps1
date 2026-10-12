param(
    [Parameter(Mandatory=$true)]
    [string]$GameDir,

    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"

Write-Host "Building RhythmCastleAP v0.76.0 against:" $GameDir

$project = Join-Path $PSScriptRoot "RhythmCastleAP.csproj"

# GameDir MUST be passed to MSBuild because the project references BepInEx directly
# from the installed game's BepInEx\core directory.
dotnet build $project -c Release -p:GameDir="$GameDir"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

$outDir = Join-Path $PSScriptRoot "bin\Release\net6.0"
$dll = Join-Path $outDir "RhythmCastleAP.dll"
if (!(Test-Path $dll)) {
    throw "Build succeeded but RhythmCastleAP.dll was not found."
}

# Keep notices alongside build and installed DLLs, including local installs.
$repoRoot = Split-Path -Parent $PSScriptRoot
foreach ($notice in @('LICENSE', 'THIRD_PARTY_NOTICES.md', 'licenses')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $notice) -Destination $outDir -Recurse -Force
}
$approvedDlls = @('RhythmCastleAP.dll', 'Archipelago.MultiClient.Net.dll', 'Newtonsoft.Json.dll')
$unexpectedDlls = @(Get-ChildItem $outDir -File -Filter '*.dll' | Where-Object { $_.Name -notin $approvedDlls })
if ($unexpectedDlls.Count) { throw 'Unexpected DLL in build output; review before packaging or installing.' }

if ($SkipInstall) {
    Write-Host "Build complete; installation skipped."
    return
}

$pluginDir = Join-Path $GameDir "BepInEx\plugins\RhythmCastleAP"
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null

# Remove stale plugin-local DLLs.
Get-ChildItem $pluginDir -File -Filter "*.dll" -ErrorAction SilentlyContinue |
    Remove-Item -Force

# Install only approved DLLs and their accompanying notices.
foreach ($name in $approvedDlls) {
    Copy-Item -LiteralPath (Join-Path $outDir $name) -Destination $pluginDir -Force
}
foreach ($notice in @('LICENSE', 'THIRD_PARTY_NOTICES.md', 'licenses')) {
    Copy-Item -LiteralPath (Join-Path $outDir $notice) -Destination $pluginDir -Recurse -Force
}
Write-Host ""
Write-Host "Next:"
Write-Host "1. Launch Rhythm Castle.exe normally (File Explorer is fine)."
Write-Host "2. Confirm BepInEx\LogOutput.log says: [SCRC-AP] v0.76.0 loading."
Write-Host "3. Set DirectStartAtPhoneHub=true, EnableAreaAccessPrototype=true, and PrototypeStartingArea=AP."
Write-Host "4. Use a fresh compatible APWorld v0.28 seed and fresh save; Hard40 Victory is verified; broader gameplay acceptance remains pending."
