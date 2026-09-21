# Cassette source audit candidate — September 20, 2026

The approved v0.75.6 candidate was installed, but its cassette reward hook failed to resolve at startup. The corrected native-name candidate below is not yet installed or gameplay accepted. Published client remains v0.75.4; the latest leak was reported on v0.75.5. Existing APWorld, seed identity and native save are unchanged.

## Confirmed chest bypass

The 32-point chest sent its randomized Star while Quicksand was playable without its AP cassette. The retained seed places that item at Level 2 Completion. The five native chest definitions share the same grant path:

`Root/GameRoom_Hub6_Logic/GameRoom_Hub6_Script/MiscSequences/ChestRewards/SongRewardSequence_{Quicksand,Flamenco,TenFourGoodBuddy,Zen,Wiggle}/AwardCassette`

Read-only asset inspection resolves each reward component to `ObtainSongCassetteOnTrigger`; it has only its song field. `TriggerReactor_OnTrigger` at native RVA `0x7db890` checks unearned/invalid ownership and dispatches `RecordSongCassetteStatusInSaveDataRequest` with `HAVE_IN_BAG` (1), not `HAVE_DEPOSITED` (2). The dispatched request's `AcceptProcessor` at `0x7dba60` contains an inlined copy of the save-change logic and never calls the separately hooked `PlayerSaveRequestProcessor.ProcessRequest` (`0x7e2c20`). That existing lower-level Harmony hook therefore does not intercept this native path. The collected flag itself is not cassette ownership: `GetCassetteStatusForSong` at `0x7e7a50` reads pending cassette changes and the cassette-status dictionary, not source flags.

A dedicated native void hook now intercepts the exact five reward reactor paths after the existing selected-save gate proves slot, epoch, processor pointer and gameplay readiness. The native controller still sets each chest's collected flag, sends its source check and runs its separate persistence step. AP receipt and the Cassette Lord insertion routine are unchanged. The old request guard remains defensive coverage for paths which actually call it.

The matching five `CassettePopup` steps are suppressed to avoid falsely announcing the vanilla cassette. This does not skip sequence completion: interop metadata identifies the popup base as `SIUtil.Scripting.InstantEventSequenceStep`. Its native `Begin` at `0x9a89c0` calls virtual `Trigger`, then sets `hasFinished` at `0x9a89d9`. Popup `Trigger` (`0x844e30`) only dispatches its UI request and does not own completion. The hook returns normally to native `Begin`. Popup interception is installed only after reward interception succeeds, so a missing reward hook cannot silently hide a leaked native grant. Both hook signatures are verified as nonstatic, nongeneric instance void methods with zero parameters before installation; original calls use direct trampolines.

The fix performs no save repair. Previously leaked deposited songs remain deposited, and no cassette is removed or returned to the bag. Native gameplay must verify the entrypoint hook and chest sequence advancement.

## Early AP receipt erased level sources

A second defect tied successful level-source checks to unowned native cassette statuses. A cassette received before its source clear made the source impossible to report through that evaluator, including mixed levels where only some songs were already owned. The policy now queues every mapped source after a successful result with readable statuses. It suppresses only newly earned native rewards. The runtime queues these locations before the `AllowNative` early return; AP location queueing already handles replay and alias deduplication. Failed results, unrelated levels/variants and unreadable-status fallback remain unchanged.

## Verification

- A new chest-source regression failed before interception and passed afterward.
- The early-receipt regression failed with an empty source list before the fix; every level source and configured alias now retains its check after early receipt, including mixed owned/unowned rewards.
- `CassetteChestRewards.Tests` exercises the production admission helper: exact five paths, wrong/unreadable paths, disabled contracts, save boundaries, no save probe for unrelated rewards, logging/identity failures and original exceptions without duplicate native invocation.
- `CassetteRandomization.Tests` passes, preserving AP grant, deposited-state, save epoch and persistence coverage.
- Client compilation succeeds without installation; native runtime acceptance remains pending.

## Focused native acceptance

With an unreceived chest cassette, open an unopened corresponding chest: one existing AP source check, no native cassette reward or misleading popup, normal sequence/persistence completion. Receive that AP cassette later, confirm it reaches the bag, insert normally, and confirm song access survives restart. Repeat one early-cassette source clear (bag and deposited cases) and a mixed-song source. Verify another native save/non-AP contract stays vanilla. Inspect all five source hook paths, including a later chest, before claiming full cassette-chest acceptance. Do not reset the retained save to repeat already consumed sources.

## v0.75.6 startup hook failure: corrected native explicit-interface name

The live error `Method 'ObtainSongCassetteOnTrigger.TriggerReactor_OnTrigger' not found` came from using the generated interop wrapper name as the IL2CPP method name. Read-only inspection of installed `Assembly-CSharp.dll` shows the generated static constructor resolves native token `100694870` (`0x06007B56`) through `GetIl2CppMethodByToken`. The installed `global-metadata.dat` is version 29; that token's native method definition names `TriggerReactor.OnTrigger`, with zero parameters, private/final/virtual flags and a void return confirmed by the generated wrapper and native disassembly. The underscore is an interop naming transformation, not an actual native method name.

The hook now resolves `ObtainSongCassetteOnTrigger` / `TriggerReactor.OnTrigger` with zero parameters. Its instance-plus-method-info void ABI and existing signature checks are unchanged. The native target mapping has a regression which failed with the underscore spelling and passes with the verified dotted spelling. Popup installation still requires successful reward-hook installation. No running process or save was modified by this investigation; root controls the separate rebuild/install/retest.
