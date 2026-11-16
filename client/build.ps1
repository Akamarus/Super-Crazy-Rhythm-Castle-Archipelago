param(
    [Parameter(Mandatory=$true)]
    [string]$GameDir,

    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"

$GameDir = (Resolve-Path -LiteralPath $GameDir).ProviderPath
if (-not $SkipInstall -and (
    !(Test-Path -LiteralPath (Join-Path $GameDir 'Rhythm Castle.exe') -PathType Leaf) -or
    !(Test-Path -LiteralPath (Join-Path $GameDir 'BepInEx\core\BepInEx.Core.dll') -PathType Leaf))) {
    throw 'Select the Rhythm Castle installation containing Rhythm Castle.exe and BepInEx core before installing.'
}

Write-Host "Building RhythmCastleAP v0.77.2 against:" $GameDir

$project = Join-Path $PSScriptRoot "RhythmCastleAP.csproj"

# GameDir MUST be passed to MSBuild because the project references BepInEx directly
# from the installed game's BepInEx\core directory.
dotnet build $project -c Release -p:GameDir="$GameDir" -p:RestoreLockedMode=true
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
foreach ($name in $approvedDlls) {
    if (!(Test-Path -LiteralPath (Join-Path $outDir $name) -PathType Leaf)) {
        throw "Missing required build output: $name"
    }
}
$unexpectedDlls = @(Get-ChildItem $outDir -File -Filter '*.dll' | Where-Object { $_.Name -notin $approvedDlls })
if ($unexpectedDlls.Count) { throw 'Unexpected DLL in build output; review before packaging or installing.' }

if ($SkipInstall) {
    Write-Host "Build complete; installation skipped."
    return
}

$pluginDir = Join-Path $GameDir "BepInEx\plugins\RhythmCastleAP"
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null

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
Write-Host "2. Confirm BepInEx\LogOutput.log says: [SCRC-AP] v0.77.2 loading."
Write-Host "3. Set DirectStartAtPhoneHub=true, EnableAreaAccessPrototype=true, and PrototypeStartingArea=AP."
Write-Host "4. Keep the associated save for an existing compatible seed. Use a fresh native save only for a new seed; see docs/INSTALL.md."
