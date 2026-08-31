# Cassette Save-Transaction Design

**Date:** 2026-08-30  
**Status:** Revised boundary approved after live diagnostics
**Related design:** `2026-08-29-full-cassette-randomization-design.md`

## Problem

Archipelago cassette ownership is session-wide, while the game's cassette state belongs to the currently loaded save. The current client can stage `HAVE_IN_BAG` before a slot finishes loading or during a level-result transaction, observe that transient state, and cache the cassette as satisfied for the rest of the process. A later slot load or enclosing save transaction can replace that state. The client then refuses to revalidate, leaving the received cassette absent from the player's inventory.

`VerifiedBag` therefore cannot mean “persisted.” It only means the current in-memory processor state contains `HAVE_IN_BAG` at that instant.

## Goals

- Apply every AP-owned cassette to the actual loaded save.
- Persist receipts through the game's native semantic player-save request lifecycle.
- Revalidate after every load, reload, and save switch, including reselecting the same slot.
- Never return or duplicate a cassette already marked `HAVE_DEPOSITED`.
- Support receipts arriving during campaign and Music Lab result processing.
- Avoid private save APIs, forced flushes, and the crashing selected-slot event hook.

## Non-goals

- Changing cassette item placement, source routing, medal logic, or machine insertion.
- Creating a custom save file or independent persistence layer.
- Automatically depositing cassettes or force-unlocking Music Lab levels.
- Repairing unrelated difficulty/HUD behavior.

## Architecture

### Loaded-save epochs

The client maintains a monotonically increasing loaded-save epoch. A safe postfix on the existing save-selection/load request boundary advances the epoch only after the game has installed the selected save state. Reselecting the same slot creates a new epoch because the in-memory state may have been replaced.

No cassette is considered terminal across epochs. `HAVE_IN_BAG` and `HAVE_DEPOSITED` are terminal only within the epoch in which an authoritative read observed them.

Reconciliation remains inactive before the first confirmed loaded-save epoch.

### Pending AP ownership

AP receipt history remains the source of session-wide cassette ownership. Each owned cassette is evaluated against the active epoch:

- `HAVE_DEPOSITED`: mark satisfied for this epoch and never submit a bag grant.
- `HAVE_IN_BAG`: mark satisfied for this epoch.
- missing: keep pending until the selected save is readable and a compatible player-save processor is available.
- authoritative read unavailable or unknown: fail closed and keep pending.

### Revised semantic request boundary

Live diagnostics disproved the earlier assumption that `PersistSaveChangeBundleRequest` or `PersistAllSaveChangeBundlesRequest` provides a reachable cassette boundary. Neither prefix ran during save load, area travel, return to title, difficulty change, shutdown, nor a proven durable AP Plant Pipes grant. Cassette reconciliation must not wait for those absent callbacks.

After a loaded-save epoch is confirmed, reconciliation runs only on the Unity thread at existing safe lifecycle points. For each pending AP-owned cassette it:

1. Confirms compatible synchronized slot data and an active epoch.
2. Authoritatively reads the selected save through `SongCassetteEnquiries.GetSongCassetteStatus`.
3. Leaves `HAVE_IN_BAG` and `HAVE_DEPOSITED` unchanged and satisfies them only for the current epoch.
4. Leaves unreadable or unknown state pending and fails closed.
5. For `INVALID` or `HAVE_NOT_EARNED`, constructs the exact semantic `RecordSongCassetteStatusInSaveDataRequest(song, HAVE_IN_BAG, DEFAULT)`.
6. Invokes the matching `PlayerSaveRequestProcessor.ProcessRequest(...)` overload through the compatible captured processor, or the proven stateless processor fallback only after selected-save enquiries are readable.
7. Treats a successful invocation as `verification-pending`, never as persisted or satisfied.
8. Re-reads after a bounded delay. Only authoritative `HAVE_IN_BAG` or `HAVE_DEPOSITED` satisfies the current epoch.

This mirrors the durable Plant Pipes lifecycle. The native record request participates in the game's own save/change-bundle behavior; the client never calls a private save manager, flush method, or synthetic persist request.

The exact cassette request mechanism already has live evidence: after the three-argument constructor fixed the earlier parameterless-wrapper defect, a received cassette appeared in native inventory, deposited normally, and remained deposited after full restart. The remaining repair is safe loaded-save timing and epoch-scoped verification, not a new persistence API.

### Load and switch behavior

Every confirmed load/reload/switch:

- advances the epoch;
- clears per-epoch satisfaction and request-attempt markers;
- preserves AP ownership;
- re-reads all owned cassettes;
- keeps missing cassettes pending for post-epoch semantic request reconciliation.

The client must not use `SelectedPlayerSaveSlotChangedEvent.HandleEvent`; the prior diagnostic established that unsafe IL2CPP event/reflection hooks can corrupt the runtime.

## Logging

Logs distinguish submission from verified native state:

- `pending`: AP-owned but missing from the active save;
- `grant submitted`: exact semantic request returned normally; verification remains pending;
- `verified bag`: observed during a later authoritative read;
- `verified deposited`: terminal for the current epoch;
- `deferred`: no active epoch, selected save unreadable, compatible processor unavailable, or bounded retry pending.

No log may call a cassette persisted merely because request invocation returned without an exception.

## Failure handling

- Missing save state or reader: do not submit; retry at a later safe boundary.
- Unknown native status: do not overwrite; log once per epoch/status transition.
- Native semantic request failure: leave pending and retry only at the next bounded safe lifecycle point.
- Save load during pending work: discard epoch-local markers and evaluate the new epoch.
- Duplicate AP receipt: idempotent; it does not create another native request.

## Testing

Automated tests must prove:

1. No native request occurs before a confirmed loaded-save epoch.
2. A first load, same-slot reload, and different-slot switch each advance the epoch.
3. Process-wide AP ownership survives epoch changes while native satisfaction does not.
4. Missing cassettes submit only after an active loaded-save epoch and an authoritative unearned read.
5. Submission uses the exact semantic three-argument request with `HAVE_IN_BAG` and `DEFAULT`.
6. `HAVE_IN_BAG` and `HAVE_DEPOSITED` prevent submission within an epoch.
7. Deposited cassettes remain deposited across reloads.
8. Authoritative-read failures fail closed.
9. A receipt arriving during campaign result processing waits for a safe post-epoch Unity lifecycle point rather than writing inside the result transaction.
10. A receipt arriving during Music Lab result processing follows the same deferred, bounded reconciliation.
11. Same-slot reload after a transient bag observation revalidates and repairs the active save.
12. Source wiring contains no selected-slot event hook, private save flush/save-manager API, forced deposit/unlock, or process-wide satisfaction cache.

Live acceptance uses the existing seed without replaying completed checks:

- AP history restores Badass and The Heist into the loaded save;
- both appear in the top-right inventory;
- a full restart before insertion restores both as `HAVE_IN_BAG` without duplicate requests or replayed checks;
- normal machine insertion unlocks their corresponding Music Lab levels;
- a second full restart reports them `HAVE_DEPOSITED` and does not return them;
- no repeated receipt loop or crash occurs.

## Acceptance boundary

This repair may be installed into the existing authorized test targets after automated verification and independent review. It does not authorize merging, pushing, publishing, or releasing the feature branch.
