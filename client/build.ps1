param(
    [Parameter(Mandatory=$true)]
    [string]$GameDir,

    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"

Write-Host "Building RhythmCastleAP v0.67.65 against:" $GameDir

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

if ($SkipInstall) {
    Write-Host "Build complete; installation skipped."
    return
}

$pluginDir = Join-Path $GameDir "BepInEx\plugins\RhythmCastleAP"
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null

# Remove stale plugin-local DLLs.
Get-ChildItem $pluginDir -File -Filter "*.dll" -ErrorAction SilentlyContinue |
    Remove-Item -Force

# Copy the plugin and NuGet runtime dependencies from the build output.
# Do NOT copy BepInEx/Harmony/Il2CppInterop because those come from BepInEx\core.
Get-ChildItem $outDir -File | Where-Object {
    $_.Extension -eq ".dll" -and
    $_.Name -notlike "BepInEx.*" -and
    $_.Name -ne "0Harmony.dll" -and
    $_.Name -ne "Il2CppInterop.Runtime.dll"
} | ForEach-Object {
    $dest = Join-Path $pluginDir $_.Name
    Copy-Item $_.FullName $dest -Force
    Write-Host "Installed:" $dest
}

Write-Host ""
Write-Host "Next:"
Write-Host "1. Launch Rhythm Castle.exe normally (File Explorer is fine)."
Write-Host "2. Confirm BepInEx\LogOutput.log says: [SCRC-AP] v0.67.65 loading."
Write-Host "3. Set DirectStartAtPhoneHub=true, EnableAreaAccessPrototype=true, and PrototypeStartingArea=AP."
Write-Host "4. Use a fresh compatible APWorld v0.19 seed."
