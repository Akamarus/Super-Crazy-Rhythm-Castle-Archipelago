# v0.22 Full Music Lab Cassette Randomization Acceptance

**Client:** `0.68.0`

**APWorld:** `0.22.0`

**Implementation:** `area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22`

This record separates automated acceptance from installation and representative gameplay. Do not mark a manual row complete without retained live evidence. Full cassette acceptance requires a fresh v0.22 seed and a fresh in-game save.

## Automated verification

- [x] The source catalog validates exactly 30 evidence-backed cassette mappings.
- [x] All 94 APWorld tests pass.
- [x] Every client test project passes:
  - [x] `DiagnosticHotkeyRouting.Tests.csproj`
  - [x] `BottomHudDiagnostic.Tests.csproj`
  - [x] `CassetteCatalogDiagnostic.Tests.csproj`
  - [x] `CassetteRandomization.Tests.csproj`
  - [x] `DifficultyAvailability.Tests.csproj`
  - [x] `GarageAvailability.Tests.csproj`
  - [x] `LevelCompletion.Tests.csproj`
  - [x] `MusicLabBarrier.Tests.csproj`
  - [x] `NativeIdentityDiagnostic.Tests.csproj`
  - [x] `NewSaveDirectStart.Tests.csproj`
  - [x] `PlantPipesReconciler.Tests.csproj`
  - [x] `PreviewAbilities.Tests.csproj`
  - [x] `ReconnectPolicy.Tests.csproj`
  - [x] `RepairCompatibility.Tests.csproj`
  - [x] `RootsBucketRandomization.Tests.csproj`
  - [x] `RootsPresentation.Tests.csproj`
  - [x] `StarHud.Tests.csproj`
- [x] Repository validation passes.
- [x] The release client builds with installation disabled: 7 nullable warnings and 0 errors.
- [x] The APWorld package contains only distribution files and reports `0.22.0`.
- [x] `git diff --check` passes.

## Deterministic real-generator matrix

| Difficulty | Starting area | Seed | Generated archive and spoiler | 30 items / 30 sources | Chest reuse | Tier filtering | Reachability / no self-lock | Restricted chests | Result |
| --- | --- | ---: | --- | --- | --- | --- | --- | --- | --- |
| Normal | Roots | 43001 | `AP_90851994406477544873.zip`; 95 locations, 30 active medals | [x] | [x] | [x] | [x] | [x] | PASS |
| Normal | Random | 43002 | `AP_36629074115365137020.zip`; 95 locations, 30 active medals | [x] | [x] | [x] | [x] | [x] | PASS |
| Hard | Roots | 43003 | `AP_88005521961561927782.zip`; 132 locations, 60 active medals | [x] | [x] | [x] | [x] | [x] | PASS |
| Hard | Random | 43004 | `AP_65026037859613384723.zip`; 132 locations, 60 active medals | [x] | [x] | [x] | [x] | [x] | PASS |
| Expert | Roots | 43005 | `AP_20861406327301829531.zip`; 169 locations, 90 active medals | [x] | [x] | [x] | [x] | [x] | PASS |
| Expert | Random | 43006 | `AP_10620698428626906874.zip`; 169 locations, 90 active medals | [x] | [x] | [x] | [x] | [x] | PASS |
| Perfection | Roots | 43007 | `AP_59464169391464172114.zip`; 205 locations, 120 active medals | [x] | [x] | [x] | [x] | [x] | PASS |
| Perfection | Random | 43008 | `AP_29916127257521243454.zip`; 205 locations, 120 active medals | [x] | [x] | [x] | [x] | [x] | PASS |

For every row, the spoiler audit must confirm completion reachability; every cassette item exactly once; every logical cassette source exactly once; reuse of the 32, 64, 89, 111, and 140 Music Lab point-chest locations; matching active medal tiers; no progression placed in restricted point chests; no cassette self-lock; and absence of inactive difficulty tiers.

## Representative live acceptance

- [x] Money level-completion source sends once and suppresses only its vanilla cassette award.
- [ ] One additional ordinary level-completion cassette source sends once.
- [ ] One Music Lab point-chest cassette reuses its existing AP check and sends no duplicate source.
- [ ] One non-level story, pickup, or quest source is exercised if the final catalog contains one.
- [ ] A received cassette appears in the native inventory without forcing its song open. **FAILED in the first live run:** `Quieres Bailar Cassette` reached AP receipt routing, but the native selected-save state remained unearned. The request used the synthetic parameterless IL2CPP constructor and omitted its mandatory bundle. The client now uses the exact `(song, status, bundle)` constructor with `HAVE_IN_BAG` and `DEFAULT`; live retest remains pending.
- [ ] Normal Music Lab insertion deposits the cassette and enables its matching medals.
- [ ] Replaying a source sends no duplicate check and does not leak a vanilla cassette.
- [ ] Save reload, Archipelago reconnect, and full game restart preserve bag/deposited state correctly.
- [ ] Local cooperative play receives a smoke test when practical.

## Installation boundary

No acceptance build may replace either installed target without explicit owner approval:

```text
C:\ProgramData\Archipelago\custom_worlds\scrc.apworld
D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP\
```

## Automated evidence

- The complete APWorld suite ran with bundled Python and passed 94 tests in 30.199 seconds.
- All 17 discovered client test projects ran in Release configuration and passed.
- `tools/validate-repo.py` reported Client `0.68.0`, APWorld `0.22.0`, the full-cassette implementation suffix, next safe item ID `187256153`, and next safe location ID `187256211`.
- `client/build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall` produced `RhythmCastleAP.dll` with 7 nullable warnings, 0 errors, and explicitly skipped installation.
- `dist/scrc.apworld` is 19,477 bytes with SHA-256 `40E4D4AF07DEC5B0CB620BC01AE162E11C86DBFFF7078B7C1685F03DDD2121A4`.
- The package has exactly 11 distribution members: the nine root Python/manifest files plus two public documentation files. It contains no tests, bytecode, cache, Git, or development-only files. `scrc/archipelago.json` reports world version `0.22.0` and supported handler format `7`.
- The first real-generator attempt exposed that handler format `8` was unsupported by the installed Archipelago `0.6.7` generator. A repository-contract regression test was changed first and observed failing (`8 != 7`); the manifest was then corrected to `version: 7` and `compatible_version: 7`, and the focused test passed before the package and matrix were rebuilt.
- All eight real seeds were generated by an isolated copy of Archipelago under `C:\Users\Jack\AppData\Local\Temp\scrc-full-cassette-task7-20260829`. The copied runtime loaded the rebuilt package from its own `custom_worlds` directory. The installed APWorld was not changed.
- Every spoiler contains every cassette item and logical source exactly once, all five reused chest sources exactly once and no duplicate cassette-source locations for them, the exact active medal set for its difficulty, Stardust in all nine restricted Music Lab point chests, no cassette placed behind its own medal rule, and Victory in the generated playthrough. Archipelago's Full accessibility generation completed for each seed, providing the real-generator reachability gate in addition to the APWorld's eight-option no-self-lock test.
- Both supported starting-area option values were exercised. The explicit Random option resolves to Roots because Roots is currently the only validated starting area, matching the approved option contract.
- `git diff --check` passed after the automated evidence update.

## Fresh live-acceptance environment

- [x] The owner authorized replacement of only the documented APWorld and `RhythmCastleAP` plugin targets.
- [x] A fresh Normal/Roots seed was generated with seed `43009` as `AP_80040381594953139783.zip` in an isolated temporary directory. It has 92 active locations and 30 active Bronze cassette medals.
- [x] The spoiler contains each of the 30 cassette items and 30 logical cassette sources exactly once, the five point-chest source locations are reused, and the generated playthrough reaches `Victory` without a cassette self-lock. The first progression sphere contains `Level 2 - Money Cassette`, minimizing the owner's initial replay requirement.
- [x] The local server is hosting the fresh archive at `127.0.0.1:38281` with slot `Jack`; the installed config has networking enabled and the documented test harness enabled.
- [x] The first live startup exposed a client parsing defect: Archipelago.MultiClient.Net materializes nested slot-data maps as Newtonsoft `JObject` entries with `Name`/`Value`, while the compatibility reader accepted only `Key`/`Value`. The client therefore failed closed with `cassette_items.The Little Things ... actual='<missing>'` even though the generated multidata contained the complete maps.
- [x] A runtime-shaped `JObject` regression test was added first and observed failing with zero parsed entries. Commit `27912f2` accepts both `Key`/`Value` and `Name`/`Value`; the focused test, all 94 APWorld tests, all 17 client test projects, repository validation, non-installing v0.68 build, and `git diff --check` then passed.
- [x] The repaired client was installed and relaunched through `Rhythm Castle.exe`. Fresh logs confirm `[SCRC-AP] v0.68.0 loading.`, successful login, the exact v0.22 implementation tag, `CASSETTE RECEIPT RECONCILIATION ENABLED entries=30`, `CASSETTE SOURCE RANDOMIZATION ENABLED entries=30 levelSources=25 chestSources=5`, and `CONNECTED server=127.0.0.1:38281 slot='Jack'`.

The game and local server are now ready for representative gameplay. Every manual gameplay row remains unchecked until its own live evidence is retained.

## First live Money-source result and receipt blocker

- [x] A fresh save spawned directly in Music Lab and connected with all 30 cassette routes enabled.
- [x] Default Level 2 suppressed its native `I_GOT_MONEY` cassette award and sent `Level 2 - Money Cassette` exactly once.
- [x] AP returned `Quieres Bailar Cassette` and the client routed it to native song `QUIERES_BAILAR` on the Unity scheduler.
- [ ] Native receipt persistence failed: repeated later-tick reads remained unearned even though the processor returned normally.
- [x] Root cause was isolated to construction of `RecordSongCassetteStatusInSaveDataRequest`: the synthetic parameterless wrapper allocation set song/status members but omitted the mandatory `ePlayerSaveChangeBundleKey` semantic-constructor argument.
- [x] The production adapter now requires the exact three-argument constructor and passes `QUIERES_BAILAR`, `HAVE_IN_BAG`, and `DEFAULT`. Focused logs include song/status/bundle at submission and a later persisted/terminal verification outcome.
- [ ] Install and live-retest the corrected client before checking native inventory visibility, insertion, reload, reconnect, or restart rows. This document does not claim a live receipt pass.
