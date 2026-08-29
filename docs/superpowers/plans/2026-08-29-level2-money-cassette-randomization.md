# Level 2 Money Cassette Randomization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn Level 2's first-earned Money cassette into a separate Archipelago location and item while preserving Level 2 completion, Stars, replay behavior, and normal Music Lab insertion.

**Architecture:** The APWorld adds one permanent source location and one progression item. A pure client policy recognizes only the verified default `Level_06` first-award payload, queues the source check, and changes `WasCollected` to false before native result persistence; a separate reconciler converts AP ownership into native `ePlayableSong.I_GOT_MONEY` (`110`) status `HAVE_IN_BAG`. Existing level completion and Music Lab medal paths remain unchanged.

**Tech Stack:** Python Archipelago world, C#/.NET client policy tests, BepInEx IL2CPP reflection/Harmony runtime integration.

**Spec:** Approved conversation design for campaign-earned cassette randomization, bounded to Level 2.

## Global Constraints

- Native mapping is verified from shipped assets: `Level_06_Data.variants[*].songCassettes[0] = 110`, and `ePlayableSong.I_GOT_MONEY = 110`.
- Player-facing item name is `Money Cassette`; do not use the separate `MONEY_DUB = 128` entry.
- Native AP receipt grants `HAVE_IN_BAG`, never `HAVE_DEPOSITED`; the player must use the Music Lab machine normally.
- The Level 2 completion and Star locations remain independent and unchanged.
- The Bee Mode variant is excluded.
- Replays and already-owned cassette results do not resend the source check.
- No automatic merge, push, release, or plugin deployment.

---

### Task 1: Register the AP source and item

**Files:**
- Modify: `apworld/scrc/items.py`
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_items.py`
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `docs/IDS.md`

**Interfaces:**
- Produces: item `Money Cassette` at permanent ID `187256123`.
- Produces: location `Level 2 - Money Cassette` at permanent ID `187256186` in Roots.
- Produces: slot-data key `randomize_level_2_money_cassette: true`.

- [ ] **Step 1: Write failing APWorld tests**

Assert the new item/location IDs, uniqueness, progression classification, item-pool presence, Roots placement, and slot-data opt-in. Assert the source location is reachable with `Roots Access` and that Music Lab `I Got Money` medal locations require `Money Cassette`.

- [ ] **Step 2: Run the focused tests and verify RED**

Run: `python -m unittest apworld.tests.test_items apworld.tests.test_world_integration -v`

Expected: FAIL because `Money Cassette`, its source location, and its rule do not exist.

- [ ] **Step 3: Add the minimal APWorld definitions**

Add these constants without renumbering any existing entry:

```python
MONEY_CASSETTE_ITEM_NAME = "Money Cassette"
NEW_ITEM_NAME_TO_ID[MONEY_CASSETTE_ITEM_NAME] = BASE_ID + 123
LEVEL_2_MONEY_CASSETTE_SOURCE = "Level 2 - Money Cassette"
LOCATION_NAME_TO_ID[LEVEL_2_MONEY_CASSETTE_SOURCE] = BASE_ID + 186
```

Classify the item as progression, add exactly one copy to `progression_items`, create the source in Roots, require the item for all active `Music Lab Cassette - I Got Money - {tier}` locations, and emit the opt-in slot-data boolean.

- [ ] **Step 4: Run the focused tests and verify GREEN**

Run: `python -m unittest apworld.tests.test_items apworld.tests.test_world_integration -v`

Expected: PASS.

### Task 2: Define client result and reconciliation policies

**Files:**
- Create: `client/Level2MoneyCassettePolicy.cs`
- Create: `client/tests/Level2MoneyCassette/Level2MoneyCassette.Tests.csproj`
- Create: `client/tests/Level2MoneyCassette/Program.cs`
- Modify: `client/tests/DiagnosticHotkeyRouting.Tests.csproj`

**Interfaces:**
- Produces: `Level2MoneyCassettePolicy.IsSourceAward(level, variant, wasCollected)`.
- Produces: `Level2MoneyCassetteRuntime` with `Configure`, `NoteReceivedCount`, `ObserveNativeStatus`, and retry-safe reconciliation decisions.
- Uses native status names `HAVE_IN_BAG`, `HAVE_DEPOSITED`, and `HAVE_NOT_EARNED` as pure strings so policy tests require no game assemblies.

- [ ] **Step 1: Write failing client tests**

Cover: default `Level_06` plus `WasCollected=true` is the source; replay false, Bee Mode, and unrelated levels are not. Cover: zero received items never grant; received plus not-earned requests `HAVE_IN_BAG`; either owned status is idempotently complete; unavailable save/processor retries without losing ownership.

- [ ] **Step 2: Run the new test project and verify RED**

Run: `dotnet run --project client/tests/Level2MoneyCassette/Level2MoneyCassette.Tests.csproj -c Release`

Expected: FAIL because the policy/runtime types do not exist.

- [ ] **Step 3: Implement the pure policy and state machine**

Use exact case-insensitive comparisons and keep all native reflection outside these pure types. Exclude the Bee Mode variant even if `WasCollected` is true.

- [ ] **Step 4: Run the new test project and verify GREEN**

Run: `dotnet run --project client/tests/Level2MoneyCassette/Level2MoneyCassette.Tests.csproj -c Release`

Expected: PASS.

### Task 3: Integrate native suppression, AP check, and item receipt

**Files:**
- Modify: `client/Plugin.cs`
- Remove: `client/Level2CassetteDiagnosticPolicy.cs`
- Remove: `client/tests/Level2CassetteDiagnostic/Level2CassetteDiagnostic.Tests.csproj`
- Remove: `client/tests/Level2CassetteDiagnostic/Program.cs`

**Interfaces:**
- Consumes: Task 2 policy/runtime.
- Produces: one `Level 2 - Money Cassette` queue on the first award payload.
- Produces: native cassette status for `ePlayableSong.I_GOT_MONEY` through `RecordSongCassetteStatusInSaveDataRequest`.

- [ ] **Step 1: Add failing integration-facing policy assertions**

Extend the Task 2 harness to assert that source handling returns both `queueLocation=true` and `replacementWasCollected=false`, while all excluded results preserve their original value.

- [ ] **Step 2: Run the focused client test and verify RED**

Run: `dotnet run --project client/tests/Level2MoneyCassette/Level2MoneyCassette.Tests.csproj -c Release`

Expected: FAIL because the source decision does not yet expose the replacement value.

- [ ] **Step 3: Replace the diagnostic with production integration**

In the Harmony prefix for `ApplyLevelResultToSaveDataRequest`, read `LevelIdentifier`, `LevelVariantIdentifier`, and `WasCollected`. When the policy recognizes the source, queue `Level 2 - Money Cassette` and write `WasCollected=false` before the native processor runs. Do not suppress result persistence or existing completion/Star processing.

Configure the feature only when slot data opts in. Route `Money Cassette` from `NativeProgression.ApplyArchipelagoItem` to the reconciler. Query native ownership through `SongCassetteEnquiries.GetSongCassetteStatus(I_GOT_MONEY)` and submit `RecordSongCassetteStatusInSaveDataRequest(I_GOT_MONEY, HAVE_IN_BAG)` through the captured `PlayerSaveRequestProcessor`; verify the selected save and retry at bounded lifecycle points. Treat both `HAVE_IN_BAG` and `HAVE_DEPOSITED` as already owned.

Delete the temporary Level 2 trace hooks and diagnostic policy after the production path is covered.

- [ ] **Step 4: Run client regression verification**

Run every `client/tests/**/*.csproj` in Release, then `powershell -ExecutionPolicy Bypass -File client/build.ps1`.

Expected: all test projects pass; build succeeds with no new errors.

### Task 4: Complete APWorld and documentation verification

**Files:**
- Modify: `docs/PROJECT_OVERVIEW.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/TESTING_AND_ISSUES.md`
- Modify: `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md`

**Interfaces:**
- Documents the one-cassette pilot truthfully; does not claim all campaign cassettes are randomized.

- [ ] **Step 1: Update documentation**

Record the verified `Level_06 -> 110 -> I_GOT_MONEY` mapping, explain that the separate internal `MONEY_DUB=128` entry is not this source, list the new location/item, and add a future manual acceptance sequence: fresh Level 2 clear sends the source check but does not retain the cassette; AP receipt puts it in the bag; normal Music Lab interaction deposits it; replay sends nothing.

- [ ] **Step 2: Run full repository verification**

Run all APWorld unit tests, all client test projects, the repository validator, and the Release client build.

Expected: all automated checks pass; no plugin is deployed.

- [ ] **Step 3: Review the diff**

Confirm permanent IDs are append-only, diagnostic code is removed, `Level 2 - Completion` remains unchanged, only `I Got Money` Music Lab medals gain the cassette rule, and no release/push/deployment files were changed unintentionally.
