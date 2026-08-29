# Full Music Lab Cassette Randomization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Activate all 30 Music Lab song cassettes as individual Archipelago items with evidence-backed sources, solver-safe placement, native inventory persistence, and player-controlled Music Lab insertion.

**Architecture:** A checked-in cassette catalog is the single source of truth for APWorld items, locations, logic, client interception, native receipt reconciliation, and documentation. A metadata/evidence extraction task must produce exactly 30 reviewed entries before activation; generic client policies then replace the Money-only special case while preserving existing AP locations for chest and other already-represented sources.

**Tech Stack:** C#/.NET 6, BepInEx IL2CPP, Harmony, Mono.Cecil, Python 3, Archipelago custom-world APIs, `unittest`, PowerShell, Archipelago 0.6.7 tooling

**Spec:** `docs/superpowers/specs/2026-08-29-full-cassette-randomization-design.md`

## Global Constraints

- Target Client version is exactly `0.68.0`; target APWorld version is exactly `0.22.0`.
- Full activation requires a new implementation-version suffix and schema; v0.21 permanent IDs and meanings remain unchanged.
- `Money Cassette` remains item ID `187256123`; `Level 2 - Money Cassette` remains location ID `187256186`.
- New cassette item IDs begin at `187256124`; new source location IDs begin at `187256187`.
- The catalog must contain exactly the existing 30 Music Lab medal songs with no duplicates or omissions.
- Reuse an existing AP location when it already represents the cassette's physical reward event; never emit two checks for one interaction.
- The 32/64/89/111/140-point chests source Quicksand/Flamenco/Ten-Four Good Buddy/Zen/Wiggle through their existing locations.
- Received cassettes grant native `HAVE_IN_BAG`; `HAVE_DEPOSITED` is terminal and must never be restored to the bag.
- Receiving a cassette never forces its Music Lab song open; insertion remains a normal player action.
- Preserve vanilla behavior outside compatible AP sessions and preserve unrelated rewards when a runtime identity is unreadable.
- Game Garage cartridges are outside this feature; do not alter the physical vanilla Vampire Killer route or treat cartridges as cassettes.
- All 30 cassettes activate in one release, but individually unplayed routes remain labeled manual verification pending.
- Do not merge, push, publish, install, or create a release without explicit project-owner approval.

---

## File Structure

- `tools/extract-cassette-catalog.ps1` — read-only extraction of native cassette enums, level/variant reward metadata, and source candidates from the installed game.
- `docs/testing/2026-08-29-cassette-source-catalog.md` — reviewed 30-entry evidence report and per-song manual status.
- `apworld/scrc/cassettes.py` — immutable Python catalog, source reuse, item/location IDs, regions, requirements, and validation.
- `apworld/scrc/items.py` — imports cassette item IDs/classifications into the permanent item registry.
- `apworld/scrc/__init__.py` — registers source locations, creates cassette items, applies source and medal rules, and emits v0.22 slot data.
- `apworld/tests/test_cassettes.py` — pure catalog, ID, source-reuse, and validation contracts.
- `apworld/tests/test_world_integration.py` — instantiated locations, pool capacity, rules, generation, and slot-data contracts.
- `client/CassetteCatalog.cs` — immutable client-side 30-entry native/source catalog.
- `client/CassetteRandomizationPolicy.cs` — pure source interception and receipt-reconciliation decisions.
- `client/Plugin.cs` — Harmony wiring, runtime native enquiries/requests, AP check dispatch, and Unity-thread reconciliation.
- `client/tests/CassetteRandomization/*` — pure policy/catalog/wiring regression project.
- `docs/IDS.md`, current public documentation, and `CHANGELOG.md` — v0.22/v0.68 behavior, IDs, compatibility, testing, and known limitations.
- `docs/testing/2026-08-29-full-cassette-acceptance.md` — automated, generation, and representative live acceptance evidence.

---

### Task 1: Extract and Review the Complete Native Cassette Catalog

**Files:**
- Create: `tools/extract-cassette-catalog.ps1`
- Create: `docs/testing/2026-08-29-cassette-source-catalog.md`
- Modify: `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md` only when the existing playthrough record needs a factual cross-reference

**Interfaces:**
- Consumes: `D:\SteamLibrary\steamapps\common\Titus\BepInEx\interop\Assembly-CSharp.dll`, Mono.Cecil, existing Music Lab song names, historical playthrough evidence, and retained logs.
- Produces: a deterministic JSON object on stdout with `schema`, `nativeEnums`, `levelCandidates`, `sourceCandidates`, and `unresolved` arrays.
- Produces: a reviewed evidence table containing exactly 30 rows and the fields required by Spec section 2.

- [ ] **Step 1: Write a failing extractor contract test**

Add a `-ValidateOnly` mode whose first implementation intentionally does not exist, then run:

```powershell
& .\tools\extract-cassette-catalog.ps1 `
  -GameDir 'D:\SteamLibrary\steamapps\common\Titus' `
  -ValidateOnly
```

Expected: FAIL because the script does not exist.

- [ ] **Step 2: Implement deterministic enum and method extraction**

Create the script with mandatory `GameDir` and optional `ValidateOnly` parameters. Load Mono.Cecil from `BepInEx\core\Mono.Cecil.dll`, read `BepInEx\interop\Assembly-CSharp.dll`, and emit sorted entries for:

```powershell
$requiredEnums = @(
    'ePlayableSong',
    'eSongCassetteStatus',
    'eLevelCassetteEarnResult',
    'eGameProgressionFlag'
)

$candidateMethods = @(
    'LevelLogic.EvaluatePlayerLevelSongCassettes',
    'LevelScoringEnquiries.GetCurrentLevelVariantSongCassettes',
    'CurrentPlayerSaveEnquiries.GetSongCassetteStatus',
    'SongCassetteEnquiries.GetSongCassetteStatus',
    'GameProgressionSaveDataState.SetSongCassetteStatus'
)
```

Normalize output ordering by enum type/value and method full name so two runs produce byte-identical JSON.

- [ ] **Step 3: Add the exact 30-song validation input**

Embed the approved expected display list in the validation section:

```text
The Little Things; No Plan B; Jolt City; Quieres Bailar; Quicksand; Gold;
I Got Money; Hippo and Frog; On the Way; Badass; Heavy Metal; AOK;
Rainbow Melodies; Sneaking; The Heist; Money; Lets Go; Bounce; Epical;
Hollywood Trailer; False Data; Gotta Get Up; Fumblin Around; Party Non Stop;
Keep On Hustlin; Another Day In Paradise; Flamenco; Ten-Four Good Buddy; Zen; Wiggle
```

`-ValidateOnly` must fail unless all 30 resolve to unique native song identifiers and the three cassette-status enum values `HAVE_IN_BAG`, `HAVE_DEPOSITED`, and at least one unowned representation (`INVALID` or `HAVE_NOT_EARNED`) exist.

- [ ] **Step 4: Run extraction twice and compare outputs**

```powershell
$first = & .\tools\extract-cassette-catalog.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus'
$second = & .\tools\extract-cassette-catalog.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus'
if (($first -join "`n") -cne ($second -join "`n")) { throw 'Cassette extraction is nondeterministic.' }
```

Expected: no exception and no write to the game directory.

- [ ] **Step 5: Build the reviewed evidence table**

Create one row per approved song with columns:

```text
Display song | Native song | Source type | Level | Variant | Existing AP location |
New source name | Region | Requirements | Native source identity | Replay behavior |
Evidence | Manual status
```

Use `verified` only for identities proven by metadata/logs. Use `mapped; manual verification pending` for catalog-complete entries not yet played. Do not use `unknown`, `TBD`, or guessed values. If a row cannot be completed, stop this task and keep production activation blocked.

- [ ] **Step 6: Verify the five reused Music Lab chest mappings**

The evidence table must contain these exact existing locations and no new source ID for them:

```text
Quicksand -> Music Lab - 32 Point Chest
Flamenco -> Music Lab - 64 Point Chest
Ten-Four Good Buddy -> Music Lab - 89 Point Chest
Zen -> Music Lab - 111 Point Chest
Wiggle -> Music Lab - 140 Point Chest
```

- [ ] **Step 7: Run validation and repository checks**

```powershell
& .\tools\extract-cassette-catalog.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -ValidateOnly
py tools\validate-repo.py
git diff --check
```

Expected: all commands PASS and the report contains exactly 30 song rows.

- [ ] **Step 8: Commit the evidence extractor and report**

```powershell
git add tools/extract-cassette-catalog.ps1 docs/testing/2026-08-29-cassette-source-catalog.md docs/HISTORICAL_GAMEPLAY_EVIDENCE.md
git commit -m "test: catalog native cassette sources"
```

---

### Task 2: Create the Authoritative APWorld Cassette Catalog and Permanent IDs

**Files:**
- Create: `apworld/scrc/cassettes.py`
- Create: `apworld/tests/test_cassettes.py`
- Modify: `apworld/scrc/items.py`
- Modify: `apworld/scrc/__init__.py`
- Modify: `docs/IDS.md`

**Interfaces:**
- Consumes: the reviewed 30-row evidence report from Task 1.
- Produces: immutable `CassetteDefinition` values with `display_song`, `native_song`, `item_name`, `item_id`, `source_name`, `source_id`, `reused_location`, `region`, `requirements`, `source_type`, `level`, and `variant`.
- Produces: `CASSETTES: tuple[CassetteDefinition, ...]`, `CASSETTE_BY_ITEM`, `CASSETTE_BY_NATIVE_SONG`, and `NEW_CASSETTE_SOURCE_IDS`.
- Produces: `validate_cassette_catalog(registered_medal_songs, registered_locations) -> None`.

- [ ] **Step 1: Write failing pure catalog tests**

Create `test_cassettes.py` with assertions that:

```python
self.assertEqual(len(CASSETTES), 30)
self.assertEqual(len({entry.display_song for entry in CASSETTES}), 30)
self.assertEqual(len({entry.native_song for entry in CASSETTES}), 30)
self.assertEqual({entry.display_song for entry in CASSETTES}, set(CASSETTE_SONGS))
self.assertEqual(CASSETTE_BY_ITEM["Money Cassette"].item_id, BASE_ID + 123)
self.assertEqual(CASSETTE_BY_ITEM["Money Cassette"].source_id, BASE_ID + 186)
```

Also assert that non-Money item IDs are consecutive from `BASE_ID + 124`, new source IDs are consecutive from `BASE_ID + 187`, and reused source names have `source_id is None`.

- [ ] **Step 2: Run the pure tests and observe failure**

```powershell
py -m unittest apworld.tests.test_cassettes -v
```

Expected: FAIL because `apworld/scrc/cassettes.py` does not exist.

- [ ] **Step 3: Implement the immutable catalog types and validators**

Use frozen dataclasses and tuples:

```python
@dataclass(frozen=True)
class CassetteRequirement:
    item: str


@dataclass(frozen=True)
class CassetteDefinition:
    display_song: str
    native_song: str
    item_name: str
    item_id: int
    source_name: str
    source_id: int | None
    reused_location: bool
    region: str
    requirements: tuple[CassetteRequirement, ...]
    source_type: str
    level: str | None
    variant: str | None
```

Populate all 30 definitions from the reviewed evidence table. Implement validation that raises `ValueError` with the offending song/source/ID for count, set equality, duplicates, reused-location registration, new-ID collision, unknown region, or missing native identity.

- [ ] **Step 4: Register items and only genuinely new source locations**

Import catalog data into `items.py` and `__init__.py`. Preserve Money's existing entries. Add 29 new item IDs/classifications and add only `NEW_CASSETTE_SOURCE_IDS` to `LOCATION_NAME_TO_ID`; reused locations remain registered exactly once under their historical IDs.

- [ ] **Step 5: Update the permanent registry**

Add every new cassette item and source ID to `docs/IDS.md` in numeric order. Include an explicit table of the five reused source events whose existing IDs remain authoritative.

- [ ] **Step 6: Add invalid-fixture tests**

Use `dataclasses.replace` to prove validation rejects a duplicate song, duplicate native identifier, duplicate ID, missing medal song, reused unknown location, new source with no ID, and unsupported region. Each assertion must check the diagnostic contains the affected value.

- [ ] **Step 7: Run pure catalog and repository tests**

```powershell
py -m unittest apworld.tests.test_cassettes -v
py -m unittest apworld.tests.test_repository_contract -v
py tools\validate-repo.py
```

Expected: all commands PASS and validator prints the new next-safe item/location IDs.

- [ ] **Step 8: Commit the catalog and permanent IDs**

```powershell
git add apworld/scrc/cassettes.py apworld/scrc/items.py apworld/scrc/__init__.py apworld/tests/test_cassettes.py docs/IDS.md
git commit -m "feat(apworld): register full cassette catalog"
```

---

### Task 3: Activate Cassette Items, Sources, Medal Rules, and Generation Safety

**Files:**
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `apworld/tests/test_cassettes.py`

**Interfaces:**
- Consumes: `CASSETTES`, `CASSETTE_BY_ITEM`, and `NEW_CASSETTE_SOURCE_IDS` from Task 2.
- Produces: one live item per cassette definition in `create_items()`.
- Produces: source locations placed in their catalog regions and rules derived from `requirements`.
- Produces: matching cassette-item rules for all active `Music Lab Cassette - {song} - {tier}` locations.

- [ ] **Step 1: Write failing world-integration tests**

Add tests that build each difficulty and assert:

```python
names = [item.name for item in world.multiworld.itempool]
for entry in CASSETTES:
    self.assertEqual(names.count(entry.item_name), 1)

for entry in CASSETTES:
    source = world.multiworld.get_location(entry.source_name, world.player)
    self.assertIsNotNone(source)
```

For reused sources, assert the world contains only one location with that name. For new sources, assert the address equals the catalog ID.

- [ ] **Step 2: Add failing reachability-rule tests**

For each cassette, construct a state without its item and assert its active Bronze location is unreachable; add only its matching item and assert the cassette gate clears when all source-independent prerequisites are satisfied. Prove a different cassette item does not unlock it.

Add exact tests that required progression is rejected at all nine Music Lab point chests under the current point model, including the five cassette-reward chests.

- [ ] **Step 3: Run the integration tests and observe failure**

```powershell
py -m unittest apworld.tests.test_world_integration apworld.tests.test_cassettes -v
```

Expected: FAIL because only Money is in the pool and only Money medals have an item rule.

- [ ] **Step 4: Instantiate catalog source locations**

During `create_regions()`, add new catalog sources to `entry.region`. Reused sources are not instantiated again. Apply every `CassetteRequirement(item=name)` with `add_rule(location, lambda state, item=name: state.has(item, self.player))`; bind loop variables through default arguments.

- [ ] **Step 5: Build the full cassette item pool**

Replace the Money-only append with one item per catalog entry. Derive filler count from active unfilled capacity after adding all progression items. Raise `ValueError` containing difficulty, active capacity, and required count when capacity is insufficient.

- [ ] **Step 6: Gate all Music Lab medals by matching cassette item**

For every active medal tier and catalog song, add the matching cassette requirement. Preserve cumulative difficulty behavior and existing medal-result handling.

- [ ] **Step 7: Add deterministic generation and self-lock regression coverage**

Run a fixed seed matrix across four difficulties and both supported `starting_area` values. Assert each generated world reaches completion, every cassette appears once, no progression occupies restricted point chests, and no cassette's only reachable sphere requires its own medal checks.

- [ ] **Step 8: Run the complete APWorld suite**

```powershell
py -m unittest discover apworld/tests -v
```

Expected: all APWorld tests PASS.

- [ ] **Step 9: Commit live APWorld cassette logic**

```powershell
git add apworld/scrc/__init__.py apworld/tests/test_world_integration.py apworld/tests/test_cassettes.py
git commit -m "feat(apworld): activate full cassette logic"
```

---

### Task 4: Generalize Client Source Interception to the 30-Entry Catalog

**Files:**
- Create: `client/CassetteCatalog.cs`
- Create: `client/CassetteRandomizationPolicy.cs`
- Create: `client/tests/CassetteRandomization/CassetteRandomization.Tests.csproj`
- Create: `client/tests/CassetteRandomization/Program.cs`
- Modify: `client/RhythmCastleAP.csproj`
- Modify: `client/Plugin.cs`
- Remove after replacement: `client/Level2MoneyCassettePolicy.cs`
- Remove after replacement: `client/tests/Level2MoneyCassette/*`

**Interfaces:**
- Consumes: the exact catalog values from Task 2, represented as immutable C# `CassetteDefinition` entries.
- Produces: `CassetteCatalog.All`, `ByItemName`, `ByNativeSong`, and `ForLevelSource(level, variant)`.
- Produces: `CassetteRandomizationPolicy.DecideLevelEvaluation(level, variant, succeeded, nativeStatuses) -> CassetteSourceDecision`.
- Produces: `CassetteSourceDecision.AllowNative` and immutable `SourceLocationsToQueue`/`NativeSongsToSuppress` collections.

- [ ] **Step 1: Write failing catalog-parity tests**

Create a console test project linked to the two new production files. Assert 30 unique display songs, native songs, item names, and source identities; exact Money identity; five reused chest locations; and exact equality with a checked expected 30-song array.

- [ ] **Step 2: Write failing pure interception tests**

Cover:

```csharp
Decision("Level_06", "LevelVariant_Default", true,
    new Dictionary<string, string> { ["I_GOT_MONEY"] = "INVALID" })
```

Expected: queue `Level 2 - Money Cassette` and suppress `I_GOT_MONEY`.

Add cases for `HAVE_NOT_EARNED`, `HAVE_IN_BAG`, `HAVE_DEPOSITED`, failed results, wrong variants, unrelated levels, unreadable statuses, and a catalog source containing multiple distinct mapped rewards. Unreadable and unrelated cases must allow native behavior.

- [ ] **Step 3: Run the focused test and observe failure**

```powershell
dotnet run --project client\tests\CassetteRandomization\CassetteRandomization.Tests.csproj -c Release
```

Expected: FAIL because the catalog and policy do not exist.

- [ ] **Step 4: Implement immutable client catalog and pure policy**

Use records and read-only dictionaries:

```csharp
internal sealed record CassetteDefinition(
    string DisplaySong,
    string NativeSong,
    string ItemName,
    string SourceName,
    CassetteSourceType SourceType,
    string? Level,
    string? Variant,
    bool ReusesExistingLocation);

internal sealed record CassetteSourceDecision(
    bool AllowNative,
    IReadOnlyList<string> SourceLocationsToQueue,
    IReadOnlyList<string> NativeSongsToSuppress,
    string Detail);
```

The catalog static constructor validates the same uniqueness/count invariants as Python and throws `InvalidOperationException` with the conflicting value.

- [ ] **Step 5: Replace the Money-only level hook**

Retain the verified `EvaluatePlayerLevelSongCassettes(bool)` Harmony target. In the prefix, read current level/variant and `GetCurrentLevelVariantSongCassettes()`, convert the native dictionary to `nativeSong -> status`, then call the pure policy.

Queue mapped AP locations only after the complete decision is valid. Suppress the original evaluator only when every newly earned catalog entry for that physical event can be represented safely. Log one structured source line per queued location.

- [ ] **Step 6: Add exact non-level source adapters**

For each catalog source type found in Task 1, add a narrowly named adapter in `Plugin.cs` that extracts the exact verified request/flag/event identity and calls a shared `QueueCatalogSource(nativeSong, sourceIdentity)` method. Existing-location adapters send the historical location name. No broad object-name scans are allowed.

- [ ] **Step 7: Remove the Money-only policy and unsafe hook path**

Delete the replaced source policy/project. Preserve the regression assertion that `SelectedPlayerSaveSlotChangedEvent.HandleEvent` is not Harmony-patched. Keep Money behavior covered in the generic test project.

- [ ] **Step 8: Run focused and full client tests**

```powershell
dotnet run --project client\tests\CassetteRandomization\CassetteRandomization.Tests.csproj -c Release
$projects = Get-ChildItem client\tests -Recurse -Filter '*.Tests.csproj' | Sort-Object FullName
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
```

Expected: every client test project PASS.

- [ ] **Step 9: Commit generic source interception**

```powershell
git add client/CassetteCatalog.cs client/CassetteRandomizationPolicy.cs client/RhythmCastleAP.csproj client/Plugin.cs client/tests/CassetteRandomization client/Level2MoneyCassettePolicy.cs client/tests/Level2MoneyCassette
git commit -m "feat(client): intercept all cassette sources"
```

---

### Task 5: Generalize Native Receipt, Save, and Insertion Reconciliation

**Files:**
- Modify: `client/CassetteRandomizationPolicy.cs`
- Modify: `client/Plugin.cs`
- Modify: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Consumes: `CassetteCatalog.ByItemName` and received-item history.
- Produces: `CassetteReceiptRuntime.Configure(enabled)`, `NoteReceived(itemName)`, `OnLifecyclePoint()`, `TryBeginReconcileAttempt()`, and `ObserveNativeStatus(nativeSong, status, saveAvailable, processorAvailable)`.
- Produces: requested native status `HAVE_IN_BAG` only for AP-owned unowned cassettes.

- [ ] **Step 1: Write failing receipt-state tests for all 30 entries**

For every definition, assert zero received count makes no request; one received item converts `INVALID` and `HAVE_NOT_EARNED` to `HAVE_IN_BAG`; `HAVE_IN_BAG` and `HAVE_DEPOSITED` make no request; a different item does not grant it.

- [ ] **Step 2: Add lifecycle and idempotence tests**

Cover unavailable save, unavailable request processor, bounded retries, later lifecycle rearm, duplicate receipt history, later compatible save, reconnect replay, full restart reconstruction, and multiple cassettes received in one history batch. Assert deposited entries are never regranted.

- [ ] **Step 3: Run the focused tests and observe failure**

```powershell
dotnet run --project client\tests\CassetteRandomization\CassetteRandomization.Tests.csproj -c Release
```

Expected: FAIL because receipt runtime still supports only Money.

- [ ] **Step 4: Implement catalog-driven receipt state**

Track AP ownership as a `HashSet<string>` of native songs plus a per-lifecycle bounded retry set. `TryApplyItem(itemName)` resolves through `ByItemName`, records ownership idempotently, and requests Unity-thread reconciliation without calling native APIs on the network callback.

- [ ] **Step 5: Generalize native status reads and requests**

Resolve `ePlayableSong` from each catalog entry, call `SongCassetteEnquiries.GetSongCassetteStatus`, unwrap the nullable, and submit `RecordSongCassetteStatusInSaveDataRequest(song, HAVE_IN_BAG)` through the captured or safely constructed `PlayerSaveRequestProcessor`. Verify the state on a later Unity tick before closing its retry window.

- [ ] **Step 6: Preserve normal insertion and consumption**

Treat `HAVE_DEPOSITED` as complete. Do not intercept the cassette machine's deposit request, reopen the song directly, or restore the bag item after insertion. Add source logging that differentiates `received`, `requested`, `verified bag`, and `verified deposited`.

- [ ] **Step 7: Wire every safe lifecycle trigger**

Reuse connection synchronization, selected-save readability, room transition, result persistence, and periodic bounded Unity reconciliation. Do not patch `SelectedPlayerSaveSlotChangedEvent.HandleEvent`.

- [ ] **Step 8: Run the complete client suite and release build**

```powershell
$projects = Get-ChildItem client\tests -Recurse -Filter '*.Tests.csproj' | Sort-Object FullName
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
& .\client\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall
```

Expected: all tests PASS and the release build exits 0.

- [ ] **Step 9: Commit native receipt reconciliation**

```powershell
git add client/CassetteRandomizationPolicy.cs client/Plugin.cs client/tests/CassetteRandomization/Program.cs
git commit -m "feat(client): persist all received cassettes"
```

---

### Task 6: Activate the v0.22/v0.68 Contract and Documentation

**Files:**
- Modify: `client/Plugin.cs`
- Modify: `client/build.ps1`
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/scrc/archipelago.json`
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `apworld/tests/test_repository_contract.py`
- Modify: `tools/validate-repo.py`
- Modify: `README.md`
- Modify: `apworld/README.md`
- Modify: `apworld/scrc/docs/en_Super Crazy Rhythm Castle.md`
- Modify: `apworld/scrc/docs/setup_en.md`
- Modify: `docs/INSTALL.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/PROJECT_OVERVIEW.md`
- Modify: `docs/PROGRESSION.md`
- Modify: `docs/TESTING_AND_ISSUES.md`
- Modify: `docs/NEXT_RELEASE_BUG_FIXES.md`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: final catalog and runtime contract from Tasks 2–5.
- Produces: APWorld `0.22.0`, Client `0.68.0`, new cassette implementation suffix, and slot fields `cassette_schema`, `full_cassette_randomization`, `cassette_count`, `cassette_items`, `cassette_sources`, and `cassette_reused_locations`.

- [ ] **Step 1: Write failing version and slot-data tests**

Require:

```python
self.assertEqual(data["cassette_schema"], 1)
self.assertTrue(data["full_cassette_randomization"])
self.assertEqual(data["cassette_count"], 30)
self.assertEqual(len(data["cassette_items"]), 30)
self.assertEqual(len(data["cassette_sources"]), 30)
self.assertEqual(data["cassette_reused_locations"]["Quicksand"], "Music Lab - 32 Point Chest")
self.assertTrue(data["implementation_version"].endswith("full-cassettes-0.22"))
```

Update validator expectations first and confirm validation fails against the old version.

- [ ] **Step 2: Apply exact version and slot contract**

Set client/version text to `0.68.0`, APWorld metadata to `0.22.0`, increment the slot schema, and append `full-cassettes-0.22` without removing historical implementation prefixes. Serialize catalog mappings in deterministic display-song order.

- [ ] **Step 3: Enforce client compatibility before routing**

Enable the manager only when slot data reports schema `1`, count `30`, and exact item/source mappings matching the client catalog. A mismatch logs the differing key/value and leaves native cassette behavior enabled.

- [ ] **Step 4: Update permanent and public documentation**

Document all 30 items, source types, reused locations, insertion flow, fresh-seed requirement, v0.21 compatibility boundary, and tester evidence fields. Replace Money-pilot status with full activation/manual verification pending. Keep the separate Game Garage native-inventory blocker open.

- [ ] **Step 5: Add per-song manual status table**

In `docs/TESTING_AND_ISSUES.md`, list each song with source type, source location, and one of:

```text
live verified
mapped; representative path verified
mapped; manual verification pending
```

Do not label a song live verified without its own recorded gameplay evidence.

- [ ] **Step 6: Run version, documentation, and repository checks**

```powershell
py -m unittest apworld.tests.test_world_integration apworld.tests.test_repository_contract -v
py tools\validate-repo.py
git diff --check
```

Expected: all commands PASS and validator reports Client v0.68.0 / APWorld v0.22.0.

- [ ] **Step 7: Commit the versioned release contract**

```powershell
git add client/Plugin.cs client/build.ps1 apworld/scrc apworld/tests tools/validate-repo.py README.md apworld/README.md docs CHANGELOG.md
git commit -m "docs: activate v0.22 full cassette testing"
```

---

### Task 7: Package, Generate, and Run Representative Live Acceptance

**Files:**
- Create: `docs/testing/2026-08-29-full-cassette-acceptance.md`
- Modify after evidence: `docs/testing/2026-08-29-cassette-source-catalog.md`
- Generated but do not commit: `dist/scrc.apworld`
- Temporary and do not commit: isolated YAML/output directories under `$env:TEMP`

**Interfaces:**
- Consumes: complete v0.22 APWorld, v0.68 client, source catalog, and manual matrix.
- Produces: package hash/content audit, eight real generated seeds, and representative live source/receipt/insertion/reload evidence.

- [ ] **Step 1: Create the acceptance record before running commands**

Include unchecked sections for catalog validation, APWorld tests, every client test project, repository validation, package contents, four difficulties × two supported starting-area values, and the nine representative gameplay checks from Spec section 10.

- [ ] **Step 2: Run the complete automated verification**

```powershell
$env:PYTHONDONTWRITEBYTECODE = '1'
py -m unittest discover apworld/tests -v
$projects = Get-ChildItem client\tests -Recurse -Filter '*.Tests.csproj' | Sort-Object FullName
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
py tools\validate-repo.py
& .\client\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall
git diff --check
```

Record exact test counts, build warnings/errors, and command exit codes.

- [ ] **Step 3: Package and inspect the APWorld**

```powershell
& .\tools\build-apworld.ps1
```

Inspect `dist\scrc.apworld` as a ZIP, confirm only distribution files are present, confirm `archipelago.json` is `0.22.0`, and record SHA-256.

- [ ] **Step 4: Generate the eight-seed matrix**

Use deterministic seeds `43001..43008`, covering Normal/Hard/Expert/Perfection once with each supported starting-area option value. Run `ArchipelagoGenerate.exe` with spoiler level 3 in isolated temporary directories.

For every spoiler, verify completion reachability, all 30 cassette items exactly once, all 30 source events represented once, five chest sources reused, matching medal gates, no progression in restricted point chests, no self-lock, and no inactive difficulty tiers.

- [ ] **Step 5: Request explicit installation approval**

Before changing either installed target, request approval to replace only:

```text
C:\ProgramData\Archipelago\custom_worlds\scrc.apworld
D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP\
```

Do not install, launch, merge, or push as part of an approval request.

- [ ] **Step 6: Generate a fresh acceptance seed and verify startup**

After approval, install only the approved targets, generate a fresh v0.22 seed, start its server, launch through Steam, and confirm BepInEx v0.68.0, schema/count/catalog acceptance, and Archipelago connection before asking for gameplay.

- [ ] **Step 7: Run the representative manual matrix**

Record evidence for:

```text
Money source; one other ordinary level source; one point-chest source;
one story/pickup/quest source when present; received inventory visibility;
normal Music Lab insertion; medal availability; replay deduplication;
save reload; reconnect; full restart; local-co-op smoke test when practical.
```

Use retained saves or targeted server item sends when they reduce replay without bypassing the source behavior under test. Never ask for a replay until diagnostics are installed and the exact expected log marker is stated.

- [ ] **Step 8: Mark only evidenced statuses complete**

Update the source catalog and acceptance record. Untested songs remain `mapped; manual verification pending`; they do not block the experimental testing release after the representative matrix passes.

- [ ] **Step 9: Run final clean verification**

```powershell
py -m unittest discover apworld/tests -q
$projects = Get-ChildItem client\tests -Recurse -Filter '*.Tests.csproj' | Sort-Object FullName
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
py tools\validate-repo.py
git diff --check
git status --short
```

Expected: tests and validation PASS; only intended acceptance-document changes remain uncommitted; `WordFactori/`, generated packages, logs, saves, and temporary outputs are unstaged.

- [ ] **Step 10: Commit acceptance evidence**

```powershell
git add docs/testing/2026-08-29-full-cassette-acceptance.md docs/testing/2026-08-29-cassette-source-catalog.md
git commit -m "test: verify full cassette activation"
```

---

## Completion Gate

The feature is ready for owner review only when:

- the reviewed catalog contains exactly 30 evidence-backed native/source mappings;
- all permanent IDs are unique and historical IDs remain unchanged;
- all 30 cassette items exist once and gate only their matching Music Lab medals;
- existing physical AP locations are reused without duplicate checks;
- source interception preserves unreadable, unrelated, failed, replayed, and alternate-mode native behavior;
- received cassettes appear in native inventory and remain player-inserted;
- deposited cassettes remain deposited across every lifecycle;
- all APWorld/client tests, validator, build, packaging, and eight-seed matrix pass;
- the representative live matrix passes;
- remaining individual routes are labeled manual verification pending; and
- no install, merge, push, publication, or release occurs without explicit approval.
