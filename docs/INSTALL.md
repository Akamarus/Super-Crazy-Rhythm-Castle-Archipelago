# Public development-test installation

> [!WARNING]
> This is an unofficial, experimental development build—not a release. Back up your save and expect incomplete logic. Do not install files or custom worlds from sources you do not trust.

This is the canonical installation guide for public development testing. The component guides keep their developer details: [client notes](../client/README.md), [APWorld notes](../apworld/README.md), and the [APWorld setup page](../apworld/scrc/docs/setup_en.md).

Current testing release: **Client v0.70.0 / APWorld v0.24.0**. It maps all 22 normal campaign identities while retaining AP Music Lab Points and the full cassette candidate. Use the two matching components with a fresh v0.24 seed and fresh native save. Broad campaign gameplay acceptance remains pending; see the [v0.24 acceptance record](testing/2026-09-10-full-level-mapping-acceptance.md).

## Before you begin

You need:

- Windows and a legally installed PC copy of *Super Crazy Rhythm Castle*.
- [BepInEx 6 IL2CPP's official installation guide](https://github.com/BepInEx/bepinex-docs/blob/master/articles/user_guide/installation/unity_il2cpp.md?plain=1). From its [Bleeding Edge download page](https://builds.bepinex.dev/projects/bepinex_be), download the Windows 64-bit IL2CPP archive designated `BepInEx-Unity.IL2CPP-win-x64-6.0.0-....zip` (the trailing build identifier varies); do **not** choose Mono, x86, Linux, or macOS builds. Extract the archive's contents directly into the game root—the directory containing `Rhythm Castle.exe`—not into a nested folder. Launch the game normally once after installing BepInEx, then close it, so BepInEx can generate its IL2CPP interop files.
- The .NET 6 SDK, PowerShell, and Git.
- Archipelago 0.6.7 or a compatible newer local installation. Follow the [official Archipelago setup guide](https://archipelago.gg/tutorial/Archipelago/setup_en) or download the installer from the [official latest release](https://github.com/ArchipelagoMW/Archipelago/releases/latest), then run the downloaded Windows installer. The default installation is commonly `C:\ProgramData\Archipelago`; this guide calls that location `<Archipelago>`.

An `.apworld` is executable custom-world code. Build it from this repository or obtain it only from a source you trust.

## Install the client

Download `RhythmCastleAP-v0.70.0.zip` from the [v0.24.0-dev testing release](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/tag/v0.24.0-dev). Close the game, then extract the three DLLs from the archive directly into:

```text
<GameDir>\BepInEx\plugins\RhythmCastleAP
```

Replace the existing files when prompted. Do not put the ZIP itself or an extra `RhythmCastleAP-v0.70.0` directory inside the plugin directory. The release archive contains only the SCRC plugin and its two application dependencies; BepInEx, Harmony, and IL2CPP interop files continue to come from the BepInEx installation.

### Build from source instead

Clone the repository and run the client build script with the directory that contains `Rhythm Castle.exe`. The default Steam path is shown below; games installed in another Steam library commonly use a path such as `D:\SteamLibrary\steamapps\common\Titus` instead.

```powershell
git clone https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago.git
cd .\Super-Crazy-Rhythm-Castle-Archipelago\client
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\build.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Titus" -SkipInstall
```

The command above builds without installation. After artifact review and live-test approval, rerun it without `-SkipInstall`. The build script then installs the client and its runtime dependencies only to:

```text
<GameDir>\BepInEx\plugins\RhythmCastleAP
```

It removes stale `.dll` files from that plugin directory before copying the new plugin files. It does **not** remove BepInEx core files, BepInEx itself, game files, saves, or IL2CPP assemblies.

Launch the game normally once and inspect `<GameDir>\BepInEx\LogOutput.log`. A successful client load includes:

```text
[SCRC-AP] v0.70.0 loading.
```

## Configure the current development client

After the first launch, edit `<GameDir>\BepInEx\config\jack.rhythmcastle.archipelago.cfg`. For the current Roots-first APWorld v0.24 testing flow, use these values and replace the server and slot placeholders with the room's connection values.

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

## Install and generate the APWorld

Download `scrc.apworld` from the [v0.24.0-dev testing release](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/tag/v0.24.0-dev). In Archipelago Launcher, choose **Install APWorld**, select the downloaded file, and restart Archipelago Launcher.

### Build from source instead

From the repository root, build the custom world:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\tools\build-apworld.ps1
```

The build creates `dist\scrc.apworld`. In Archipelago Launcher, choose **Install APWorld** and select that file. Double-clicking the file or dragging it onto the launcher can also install it. Restart Archipelago Launcher after installing or replacing the APWorld.

Use one of these YAML starting points: click **Generate Template Options** in Archipelago Launcher, which writes template YAMLs to `<Archipelago>\Players\Templates`, or use the repository's [SCRC-AreaRouting-PlantPipes.yaml](../apworld/examples/SCRC-AreaRouting-PlantPipes.yaml) Roots-first example. Copy the selected YAML to `<Archipelago>\Players` (not `Players\Templates`) and keep it as an uncompressed `.yaml` file. Open the copied file and change its top-level `name:` value to your intended slot name; the repository example starts as `name: Jack`, so replace `Jack`. Use the same slot name in the client's `Slot` setting, including its capitalization.

In Archipelago Launcher, click **Generate**. On success, take the generated archive from `<Archipelago>\output\AP_XXXXX.zip`. Custom worlds generate locally, and the resulting zip can be uploaded to a compatible hosting website afterward.

Host the generated `AP_XXXXX.zip` with a local Archipelago server or an appropriate hosting website. Enter that room's host and port in `Server`, your player name in `Slot`, and the room password in `Password` only if required. The current APWorld is **v0.24.0** with slot-data implementation `area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23-full-level-mapping-0.24`; it forces Roots as the starter area, retains AP Music Lab Points and all 30 cassette mappings, maps all 22 normal campaign identities, and preserves the physical vanilla Vampire Killer pickup required for normal Game Garage entry.

Set the YAML `difficulty` to `normal`, `hard`, `expert`, or `perfection` to filter AP performance locations. In v0.24, Normal addresses 121 locations; Hard 179; Expert 237; Perfection 273. Inactive checks are absent from the seed, not filler. This does not alter the native REG/PRO choice.

Install APWorld v0.24, generate a newly created v0.24 seed, and start a fresh in-game save for acceptance. Replacing the world does not upgrade an old seed or make an old save an acceptance baseline. Client v0.70.0 requires top-level schema 15, campaign-mapping schema 1, and point schema 1. Recognized v0.23/schema-14 seeds retain their historical mapping and AP Music Lab Points behavior; recognized v0.22 and non-AP play retain native Music Lab scoring. Malformed or unsupported AP contracts report incompatibility and keep the affected randomized system inactive.

The 10 one-point items, 3 ten-point bundles, and 7 twenty-point large bundles total 180 points. They replace 20 Stardust; the last chest costs 140, leaving 40 slack. Thresholds are 5/10/20/32/46/64/89/111/140, with no milestone or duplicate cassette/cartridge checks. AP totals apply only through the existing managed getter in `GameRoom_Hub6`, remain zero before synchronization, and retain the last synchronized total during a temporary disconnect. Native medals add no points. The diagnostic-first investigation rejected chest/native detours; no native score/save write or forced chest interaction is used. The existing display, all thresholds, and persistence still need live acceptance; the developer Shift+F4 override cannot supersede compatible AP points.

Incomplete, older, or mismatched cassette slot data separately preserves vanilla cassette awards. Many individual cassette routes remain manual verification pending. A historical v0.21 seed used with historical Client v0.67.95 retains the Money-only pilot. Receiving a cassette puts it in the native bag, but the player must use its Music Lab machine to deposit/unlock the song.

## Updating or uninstalling

Update the client and APWorld together: rebuild/install both, restart Archipelago Launcher after replacing `scrc.apworld`, and generate a **fresh seed**. Existing generated seeds retain their old world data and slot data.

To uninstall, remove only:

- `<GameDir>\BepInEx\plugins\RhythmCastleAP`
- `<Archipelago>\custom_worlds\scrc.apworld`—commonly `C:\ProgramData\Archipelago\custom_worlds\scrc.apworld` when using the default Archipelago installation

Do not delete the repository's `dist\scrc.apworld` when uninstalling; that is the build output, not the installed custom world. Keep your saves and BepInEx installation intact. Do not delete BepInEx core files, game files, or IL2CPP assemblies.
