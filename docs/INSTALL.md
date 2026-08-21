# Public development-test installation

> [!WARNING]
> This is an unofficial, experimental development build—not a release. Back up your save and expect incomplete logic. Do not install files or custom worlds from sources you do not trust.

This is the canonical installation guide for public development testing. The component guides keep their developer details: [client notes](../client/README.md), [APWorld notes](../apworld/README.md), and the [APWorld setup page](../apworld/scrc/docs/setup_en.md).

## Before you begin

You need:

- Windows and a legally installed PC copy of *Super Crazy Rhythm Castle*.
- [BepInEx 6 IL2CPP](https://builds.bepinex.dev/projects/bepinex_be) for `win-x64`, installed in the game's directory. Launch the game normally once after installing BepInEx, then close it, so BepInEx can generate its IL2CPP interop files.
- The .NET 6 SDK, PowerShell, and Git.
- Archipelago 0.6.7 or a compatible newer local installation.

An `.apworld` is executable custom-world code. Build it from this repository or obtain it only from a source you trust.

## Build and install the client

Clone the repository and run the client build script with the directory that contains `Rhythm Castle.exe`. The default Steam path is shown below; games installed in another Steam library commonly use a path such as `D:\SteamLibrary\steamapps\common\Titus` instead.

```powershell
git clone https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago.git
cd .\Super-Crazy-Rhythm-Castle-Archipelago\client
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\build.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Titus"
```

To compile without changing the game installation, add `-SkipInstall`:

```powershell
.\build.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Titus" -SkipInstall
```

Without `-SkipInstall`, the script installs the client and its runtime dependencies only to:

```text
<GameDir>\BepInEx\plugins\RhythmCastleAP
```

It removes stale `.dll` files from that plugin directory before copying the new plugin files. It does **not** remove BepInEx core files, BepInEx itself, game files, saves, or IL2CPP assemblies.

Launch the game normally once and inspect `<GameDir>\BepInEx\LogOutput.log`. A successful client load includes:

```text
[SCRC-AP] v0.67.59 loading.
```

## Configure the current development client

After the first launch, edit `<GameDir>\BepInEx\config\jack.rhythmcastle.archipelago.cfg`. For the current Roots-first APWorld v0.15 development flow, use these values and replace the server and slot placeholders with the room's connection values:

```ini
[Archipelago]
Enabled = true
Server = HOST:PORT
Slot = SLOT_NAME
Password =
ApplyReceivedProgression = true

[QualityOfLife]
DirectStartAtPhoneHub = true
DirectStartAtLevelOne = false

[Developer]
EnableAreaAccessPrototype = true
PrototypeStartingArea = AP
```

Keep `RandomizeEarlyProgression = false` if that existing entry is present. It is a retired per-level-access prototype; Area Access routing replaces it for this development flow. BepInEx represents the section and key names with the case shown above; use those names exactly and do not rename them.

Treat the room password as a secret. Put it in the local config only when the room requires it; do not commit or publicly attach the config file.

## Build, install, and generate the APWorld

From the repository root, build the custom world:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\tools\build-apworld.ps1
```

The build creates `dist\scrc.apworld`. In Archipelago Launcher, choose **Install APWorld** and select that file. Double-clicking the file or dragging it onto the launcher can also install it. Restart Archipelago Launcher after installing or replacing the APWorld.

Use the Launcher to generate a template, then configure the generated YAML. The repository includes [SCRC-AreaRouting-PlantPipes.yaml](../apworld/examples/SCRC-AreaRouting-PlantPipes.yaml) as the current Roots-first v0.15 example. Generate the seed locally with Archipelago Launcher; generation produces an `AP_XXXXX.zip` output. Custom worlds generate locally, and a generated seed can be uploaded to a compatible hosting website afterward.

Host the generated `AP_XXXXX.zip` with a local Archipelago server or an appropriate hosting website. Enter that room's host and port in `Server`, your player name in `Slot`, and the room password in `Password` only if required. The current APWorld is **v0.15** with slot-data implementation `area-routing-plant-pipes-0.15`; it forces Roots as the starter area.

## Updating or uninstalling

Update the client and APWorld together: rebuild/install both, restart Archipelago Launcher after replacing `scrc.apworld`, and generate a **fresh seed**. Existing generated seeds retain their old world data and slot data.

To uninstall, remove only:

- `<GameDir>\BepInEx\plugins\RhythmCastleAP`
- The installed `scrc.apworld` from Archipelago's custom-world installation

Keep your saves and BepInEx installation intact. Do not delete BepInEx core files, game files, or IL2CPP assemblies.
