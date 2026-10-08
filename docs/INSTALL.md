# Install and connect

This guide covers the published **v0.28.2 release**: **Client v0.75.13 / APWorld v0.28.2**.
Unreleased development candidates are not required for these steps.

The integration is experimental. Back up your native save before starting a new
randomized run, and see [known issues](KNOWN_ISSUES.md).

## 1. Find your game folder

In Steam, right-click **Super Crazy Rhythm Castle → Manage → Browse local files**.
This opens the folder containing `Rhythm Castle.exe`. Below, `<GameDir>` means
that folder; do not create a folder literally named `<GameDir>`.

## 2. Install BepInEx

1. Close the game.
2. Open the [BepInEx Bleeding Edge download page](https://builds.bepinex.dev/projects/bepinex_be).
   Our tested installation uses **6.0.0-be.785**. Choose its **Unity.IL2CPP-win-x64**
   ZIP. This is the Windows 64-bit IL2CPP build, not Mono or x86.
   The tested build is **be.785**, rather than the GitHub releases named pre.1 or pre.2;
   those two prereleases are not the setup verified here.
3. Extract the ZIP's contents directly beside `Rhythm Castle.exe`.
4. Launch the game once, allow BepInEx to generate its files, then close it.

See the [official IL2CPP installation guide](https://github.com/BepInEx/bepinex-docs/blob/master/articles/user_guide/installation/unity_il2cpp.md?plain=1)
if BepInEx does not start.

## 3. Install the SCRC client

1. Download **RhythmCastleAP-v0.75.13.zip** from the
   [v0.28.2 release](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/tag/v0.28.2).
2. With the game closed, create this folder if it does not exist:

   ```text
   <GameDir>\BepInEx\plugins\RhythmCastleAP
   ```

3. Extract the archive's three DLL files directly into that folder. Do not leave
   them inside the ZIP or an extra nested folder.
4. Launch the game once to generate the mod's configuration, then close it again.

**You do not need Git or the .NET SDK to install the downloaded client.**
Those tools are only needed if you choose to build from source.

## 4. Enter your room details

Open this file in Notepad:

```text
<GameDir>\BepInEx\config\jack.rhythmcastle.archipelago.cfg
```

**Everyone uses this exact filename.** `jack` is part of the mod's internal
identifier, not your player name. Do not rename the file or replace `jack`.
Your Archipelago player name goes in the **Slot** setting inside the file.

Find the following settings in their existing sections and edit them. Keep other
settings as they are; do not paste duplicate sections at the bottom of the file.

```ini
[Archipelago]
Enabled = true
Server = HOST:PORT
Slot = YOUR_PLAYER_NAME
Password =
ApplyReceivedProgression = true

[QualityOfLife]
DirectStartAtPhoneHub = true
DirectStartAtLevelOne = false

[Developer]
EnableAreaAccessPrototype = true
PrototypeStartingArea = AP
```

Replace the two placeholders:

| Setting | What to enter |
| --- | --- |
| `Server` | The **connection address and port** shown by your room or supplied by the host. For example, `archipelago.gg:12345` illustrates the format; it is not your room's address. Do not paste the room's web-page URL. |
| `Slot` | Your exact player name from the **generated room** (originally your YAML's `name:`). Match capitalization. This is not automatically your Steam or Discord name. |
| `Password` | The room password, only if required. Otherwise leave it blank after `=`. |

**`localhost:38281` means a server running on your own computer.** It is the
default value, not a public Archipelago server. If you are joining someone else's
room, replace it with that room's address. If you are hosting locally, start the
Archipelago server with your generated seed first, and use its actual port.
Opening the game or Archipelago Launcher alone does not host the seed.

If an older configuration contains `RandomizeEarlyProgression`, leave it `false`.
Save the file, then launch the game again. Keep any password private; do not post
the whole configuration file publicly.

## 5. Connect before loading your save

Wait for the mod to report a successful Archipelago connection before loading a
native save. Use a fresh native save for a new seed. For an existing run, keep its
associated save; do not erase it just to reconnect or update a compatible client.

If you are joining an already-generated room, you do **not** need to generate a
second seed. Get the connection address and your assigned slot name from its host.

### If it will not connect

| Symptom | Check |
| --- | --- |
| No configuration file | Confirm BepInEx and the client DLLs are installed, launch once, then close the game. Check the log for client loading errors. |
| Connection refused / keeps retrying | Check `Server`, including the port. A local server must be running; a hosted room must be online. Do not leave `localhost` when joining a remote room. |
| Invalid slot or password | Copy the player name from the generated room and check whether the room requires a password. |
| Still cannot connect | Attach `BepInEx\LogOutput.log` from the game folder when asking for help. Readable log text is more useful than a screenshot of the console. |

Successful client loading includes `[SCRC-AP] v0.75.13 loading.` in the log.
**That confirms the mod loaded, not that it connected to your room.**

## Generating or hosting your own seed

Skip this section if a host has already generated your room.

1. Install Archipelago 0.6.7 or a compatible newer version using the
   [official setup guide](https://archipelago.gg/tutorial/Archipelago/setup_en).
   Its usual Windows installation folder is `C:\ProgramData\Archipelago`.
2. Download **scrc.apworld** from the
   [v0.28.2 release](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/tag/v0.28.2).
   In Archipelago Launcher, select **Install APWorld**, choose that file, then
   restart the launcher. Custom worlds contain executable code; use trusted downloads.
3. Select **Generate Template Options**, or use the repository's
   [example YAML](../apworld/examples/SCRC-AreaRouting-PlantPipes.yaml).
   Copy your YAML into Archipelago's `Players` folder, not `Players\Templates`.
4. Set the YAML's top-level `name:` to your player name. The example uses `Jack`;
   replace that name here. This is separate from the fixed configuration filename.
5. Choose your settings and select **Generate**. The generated ZIP appears in
   Archipelago's `output` folder.
6. Host that generated ZIP on a compatible hosting service or start a local
   Archipelago server for it. Follow step 4 above to configure the game client.

The published APWorld has **Normal 164 / Hard 222 / Expert 280 / Perfection 316**
checks. AP difficulty determines which performance checks exist; it does not
change the game's REG/PRO selection. Victory requires your configured AP Star
goal followed by a successful Level 22 clear. An earlier clear must be replayed
after reaching the goal. See [progression](PROGRESSION.md) for details.

For **Universal Tracker**, install the appropriate APWorld on the tracker computer
and keep your YAML in the folder required by your tracker setup. Follow
[the tracker guide](UNIVERSAL_TRACKER.md).

## Updating or uninstalling

- Follow the specific release's update instructions. v0.28.2 updates the APWorld;
  existing client v0.75.13 users do not need to replace the client DLLs.
- Close the game before replacing client DLLs. Restart Archipelago after replacing
  its APWorld. An APWorld update does not rewrite an already-generated seed.
- To uninstall the integration, remove only `BepInEx\plugins\RhythmCastleAP` from
  the game folder and `custom_worlds\scrc.apworld` from the Archipelago installation.
  Keep your saves, game files and BepInEx core files.

## Optional: build from source

Only this route requires Git, PowerShell and the .NET 6 SDK. Use the matching
release tag to build the published version:

```powershell
git clone https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago.git
cd .\Super-Crazy-Rhythm-Castle-Archipelago
git checkout v0.28.2
.\client\build.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Titus" -SkipInstall
.\tools\build-apworld.ps1
```

Replace the example game path with your actual installation path. `-SkipInstall`
builds without changing the installed client. To install your reviewed build, close
the game and rerun the client build command without `-SkipInstall`. The APWorld
build writes `dist\scrc.apworld`; install it through Archipelago Launcher.

Further developer details: [client notes](../client/README.md),
[APWorld notes](../apworld/README.md), and [workflow](WORKFLOW.md).
