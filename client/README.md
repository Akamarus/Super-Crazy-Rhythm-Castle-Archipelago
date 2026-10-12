# RhythmCastleAP client

C# BepInEx/IL2CPP client for Super Crazy Rhythm Castle. Current client: **0.76.1**, released with v0.29.1 and compatible with unchanged APWorld 0.29.0.

For downloaded releases, follow [Install and connect](../docs/INSTALL.md); no SDK is needed. Everyone uses `jack.rhythmcastle.archipelago.cfg`; set your generated player name in its Slot field.

For source builds, use the .NET SDK and your separately installed game/BepInEx runtime:

```powershell
./build.ps1 -GameDir "D:/SteamLibrary/steamapps/common/Titus" -SkipInstall
```

Replace the example path with your game folder. Review and explicitly approve deployment separately. Never commit game DLLs, interop assemblies, logs or generated binaries.

[Workflow](../docs/WORKFLOW.md) · [Test evidence](../docs/TESTING.md) · [Known issues](../docs/KNOWN_ISSUES.md) · [Historical client README](../docs/archive/checkpoints/client-README.md)
