# Runtime diagnostic cleanup (2026-10-28)

The F8 movement recorder and its native timing-probe implementation are removed. The accepted `PlayerMovementTiming` and `MontageAnimalTick` repairs retain their install/reset/shutdown lifecycle. Existing saved captures were not touched. Historical formatting/buffer/profiler helpers remain available to regression tests, but are excluded from the production project.

Developer test-harness and verbose/performance/acceptance-diagnostic settings and live hotkey registration are removed. Roots-owner, bottom-HUD, selector-availability, mouse binding/condition and cassette ownership inspection probes are disconnected. Logging-only quest-action and special-variant dispatch is removed. Ordinary connection/save warnings and errors remain.

Some legacy diagnostic definitions and their historical tests remain in source without production entry points. This is runtime-hook cleanup, not deletion of every diagnostic-named type: cassette persistence identity/read adapters and functional progression dispatch still share historical diagnostic names.

`MusicLabDiagnosticKeeper` is now `MusicLabProgressionKeeper`, enabled with the AP connection option. Its nine reward-chest reconciliation and Garage song-identity polling remain functional. `DeveloperHarness` retains room identity and native transition capture used by movement/progression, with its optional test harness disabled.

Read-only installed-helper inventory: `D:/SteamLibrary/steamapps/common/Titus/BepInEx/plugins/SCRC.BunkerTest/SCRC.BunkerTest.dll`, 28,672 bytes, modified 2026-10-28 11:47 local. The installed helper and installed client were not changed. Removing that separate helper requires an explicit deployment step.

Regression smoke: `node tools/diagnostic-cleanup-smoke.cjs` from the repository root. The original smoke failed on the still-registered movement recorder before cleanup. Existing policy/adapter tests remain for movement timing, scene resets, cassette persistence, Music Lab checks and reflection-cache performance. Native gameplay acceptance remains separate from compilation/regression tests.
