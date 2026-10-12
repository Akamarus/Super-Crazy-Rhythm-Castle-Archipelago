# Item delivery audit — October 6, 2026

## Scope and evidence

Source-only candidate. The running async client, configuration, native saves and
server remain untouched. Gameplay acceptance is pending.

The user reported Weed Killer received by Archipelago but absent from the native
inventory until restart, and a similar Zen cassette symptom. The Weed Killer
source explains a concrete failure path: receipt callbacks and progression hooks
submitted native requests using any captured processor, without the stable
selected-save or Unity-thread gate. A successful reflection invocation set a
process-wide delivery latch even when the native inventory never acquired the
item. Later retries were suppressed by that latch. Restart cleared the latch and
replayed receipts. This reproduces the reported behavior in a controlled runtime
fixture; the exact live failure timing is not established from a retained trace.

The native consumption mapping is already recorded in PROJECT_OVERVIEW.md,
section 14: `WEED_KILLER_BAG_ITEM=False` and
`ROOTS_HUB_LEVEL_07_DOOR_REVEALED=True`. Gecko's collected source flag is not
consumption. The change reads the door consequence; it never sets the door flag.
Permanent Weed Killer item ID `187256116` is recorded in IDS.md.

## Candidate behavior

- Receipt notifications do no native work. A contiguous index-zero received-item
  history from the current connection is the ownership authority; incomplete or
  gapped history revokes authority until a new complete history arrives.
- Connection generations prevent stale/ended callbacks from publishing ownership.
  History can arrive before login configuration, but cannot authorize delivery
  until configuration completes. A new connection clears the prior authority.
- A dedicated Unity tick reconciles at most once per second through the existing
  stable selected-save and gameplay-ready gate, using its verified processor.
- Both held inventory and the native consumed-door flag must be readable. Held or
  consumed items are not granted. Absent unconsumed items receive the native bag
  flag, followed by readback. Unverified writes remain eligible on later ticks.
- Native scene overwrites and new selected saves are reconciled from current
  history rather than a process-wide success latch. Delivery logs distinguish
  pending readback, verified held, consumed, and unreadable state.

## Automated evidence

`dotnet run --project client/tests/WeedKillerRuntime/WeedKillerRuntime.Tests.csproj`
extracts the real WeedKillerRandomization class from Plugin.cs and compiles it
with the real history adapter. Fixtures replace the external native processor,
selected-save gate and AP packet boundary only.

Before the fix, six runtime assertions failed: native access before readiness,
silent-submission retry, native overwrite repair, consumed-item restart replay,
unknown-consumption deferral, and selected-save replacement. All ten runtime
assertions passed after the delivery change. The initial history stub then failed
complete-history authorization; the completed adapter passes all history cases.
The combined fixture covers held-item idempotence, bounded polling, consumed
replay, incomplete/gapped history, terminal and stale generations, and notification
callbacks failing to bypass the ownership authority.

Full repository checks and the client build are recorded by the coordinating
change. No automated fixture proves real in-game delivery acceptance.

## Remaining acceptance and audit findings

Test receipt while already playing, including an initially unavailable save;
observe delivery without restart. Confirm normal consumption at the Level 3 door,
then reconnect, switch scenes and restart without restoring consumed Weed Killer.
Confirm a different server/slot's empty receipt history does not grant it.

The Zen cassette symptom requires its own trace and is not repaired by this
Weed Killer change. Cassette delivery already has a separate selected-save,
transaction and gameplay-ready pipeline, so its failure mechanism cannot be
inferred from the Weed Killer latch.

RootsBucketRandomization still treats successful request submission as held via
`_hipGrantAppliedThisProcess` and `_chickenGrantAppliedThisProcess`; a silently
lost live grant can therefore remain latched until selected-save identity changes.
The general CharacterQuestItemSessionHistory adapter marks its local history
unready on a gap without revoking the already-published state. Those broader
issues are outside this focused change and remain open for targeted work.
