# Cassette Save-Transaction Design

**Date:** 2026-08-30  
**Status:** Approved in chat; written-spec review pending  
**Related design:** `2026-08-29-full-cassette-randomization-design.md`

## Problem

Archipelago cassette ownership is session-wide, while the game's cassette state belongs to the currently loaded save. The current client can stage `HAVE_IN_BAG` before a slot finishes loading or during a level-result transaction, observe that transient state, and cache the cassette as satisfied for the rest of the process. A later slot load or enclosing save transaction can replace that state. The client then refuses to revalidate, leaving the received cassette absent from the player's inventory.

`VerifiedBag` therefore cannot mean “persisted.” It only means the current in-memory processor state contains `HAVE_IN_BAG` at that instant.

## Goals

- Apply every AP-owned cassette to the actual loaded save.
- Persist receipts through the game's normal save transaction.
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

- `HAVE_DEPOSITED`: mark satisfied for this epoch and never stage a bag grant.
- `HAVE_IN_BAG`: mark satisfied for this epoch.
- missing: keep pending until a natural save-bundle persistence boundary.
- authoritative read unavailable or unknown: fail closed and keep pending.

### Natural transaction integration

The client hooks the existing `PersistSaveChangeBundleRequest` and `PersistAllSaveChangeBundlesRequest` processing boundaries without replacing their native behavior.

Immediately before the original native persistence operation:

1. Confirm a loaded-save epoch is active.
2. Re-read each pending cassette from the authoritative state used by `PlayerSaveRequestProcessor`.
3. Leave deposited or bagged cassettes unchanged.
4. Stage only missing AP-owned cassettes as `HAVE_IN_BAG` using the bundle that the game is about to persist.
5. Allow the original persistence request to commit and write the bundle normally.

The client never calls a save manager, flush method, or extra persistence request directly.

After native persistence completes, reconciliation re-reads status on a later Unity tick. A cassette becomes satisfied for the current epoch only after this post-persistence read returns `HAVE_IN_BAG` or `HAVE_DEPOSITED`.

### Load and switch behavior

Every confirmed load/reload/switch:

- advances the epoch;
- clears per-epoch satisfaction and staged-attempt markers;
- preserves AP ownership;
- re-reads all owned cassettes;
- keeps missing cassettes pending for the next natural persistence boundary.

The client must not use `SelectedPlayerSaveSlotChangedEvent.HandleEvent`; the prior diagnostic established that unsafe IL2CPP event/reflection hooks can corrupt the runtime.

## Logging

Logs distinguish staging from persistence:

- `pending`: AP-owned but missing from the active save;
- `staged`: added to the bundle currently being persisted;
- `verified bag`: observed after native persistence;
- `verified deposited`: terminal for the current epoch;
- `deferred`: no active epoch, no persistence boundary, or authoritative read unavailable.

No log may call a cassette persisted merely because request invocation returned without an exception.

## Failure handling

- Missing save state or reader: do not submit; retry at a later safe boundary.
- Unknown native status: do not overwrite; log once per epoch/status transition.
- Native staging failure: leave pending and allow the game's persistence request to continue.
- Save load during pending work: discard epoch-local markers and evaluate the new epoch.
- Duplicate AP receipt: idempotent; it does not create another native request.

## Testing

Automated tests must prove:

1. No staging occurs before a confirmed loaded-save epoch.
2. A first load, same-slot reload, and different-slot switch each advance the epoch.
3. Process-wide AP ownership survives epoch changes while native satisfaction does not.
4. Missing cassettes stage only at a natural bundle-persist boundary.
5. The exact native bundle being persisted is used.
6. `HAVE_IN_BAG` and `HAVE_DEPOSITED` prevent staging within an epoch.
7. Deposited cassettes remain deposited across reloads.
8. Authoritative-read failures fail closed.
9. A receipt arriving during campaign result processing persists through that transaction.
10. A receipt arriving during Music Lab result processing persists through that transaction.
11. Same-slot reload after a transient bag observation revalidates and repairs the active save.
12. Source wiring contains no selected-slot event hook, private save flush, or diagnostic postfix/state reflection.

Live acceptance uses the existing seed without replaying completed checks:

- AP history restores Badass and The Heist into the loaded save;
- both appear in the top-right inventory;
- normal machine insertion unlocks their corresponding Music Lab levels;
- a full restart reports them deposited and does not return them;
- no repeated receipt loop or crash occurs.

## Acceptance boundary

This repair may be installed into the existing authorized test targets after automated verification and independent review. It does not authorize merging, pushing, publishing, or releasing the feature branch.
