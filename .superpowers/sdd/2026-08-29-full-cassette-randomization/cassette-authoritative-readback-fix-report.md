# Cassette authoritative-readback repair report

**Status:** LIVE_RETEST_PASS

## Commits

- `6706508` — reverted `9de6a65` and removed the diagnostic helper, diagnostic-only tests, postfix wiring, and Harmony `__state` transfer.
- `df70f74` — reverted `d9e2bd2` and removed the remaining collision formatter/report while restoring the pre-diagnostic client tree to the safe `8e40871` prefix-only cassette hook.
- `e7ae90e` — made reconciliation trust the processor-selected save state and made observed `HAVE_IN_BAG`/`HAVE_DEPOSITED` states terminal.

## Production repair

The scheduler still receives the public `SongCassetteEnquiries` observation for context, but it no longer uses that value to make a write decision when a `PlayerSaveRequestProcessor` is available. At decision time it calls the processor's zero-argument `ObtainState()` and then that current state's `GetCassetteStatusForSong(song)`. If the authoritative read is unavailable, reconciliation fails closed as `SaveUnavailable` and does not submit a request from the stale public value.

The reader does not call `HasSongCassetteStatusBeenChangedByAnyBundle`, construct or unwrap `Il2CppSystem.Nullable<T>`, pass a reflected by-reference/out slot, retain an IL2CPP wrapper across a native call, or use a Harmony postfix. The original AP-origin prefix suppression gate remains unchanged.

`HAVE_IN_BAG` now joins `HAVE_DEPOSITED` in the runtime's terminal-song set. Generic lifecycle notifications can still rearm unresolved receipts, but cannot requeue or resubmit a cassette after either terminal status has been observed.

No cassette request constructor, bundle, routing, persistence, flush, or save-write path changed.

## TDD evidence

The focused regression models the proven inconsistent state directly:

- public enquiry: `INVALID`;
- authoritative processor-selected save: `HAVE_IN_BAG`;
- expected decision: `VerifiedBag`;
- expected submissions: zero; and
- expected behavior after a broad lifecycle notification: no new observation and no submission.

The first RED failed for the intended reason:

```text
authoritative processor state wins over stale public enquiry: expected VerifiedBag, got RequestHaveInBag
```

After the minimal scheduler/terminal-state change, the focused suite was GREEN.

A second behavioral test exercised the real authoritative reflection adapter through native-shaped managed fakes. Its RED was:

```text
processor-selected save status is readable without the private bundle predicate: expected True, got False
```

After implementing only `ObtainState()` plus `GetCassetteStatusForSong(song)`, the focused suite was GREEN:

```text
Cassette randomization catalog and source policy tests passed.
```

## Automated verification

- Focused cassette tests: PASS.
- All 17 discovered client Release test projects: PASS (`CLIENT_TEST_PROJECTS_PASSED=17`).
- Client v0.68.0 no-install Release build: PASS, 7 pre-existing nullable warnings, 0 errors, `Build complete; installation skipped.`
- Repository validator with the documented bundled Python runtime: PASS; client `0.68.0`, APWorld `0.22.0`, next item ID `187256153`, next location ID `187256211`.
- `git diff --check`: PASS before the implementation commit.
- Scope checks: no diagnostic production/test symbols remain; the cassette hook is prefix-only; no cassette persistence, flush, request-construction, bundle, or routing change was added.

The first validator attempt used the unavailable Windows `python` app alias and exited before running the validator. The same validator then passed with the repository's documented bundled interpreter.

## Verified live evidence

The safe v0.68 client started successfully against the v0.22 slot data. For the captured `QUIERES_BAILAR` case where the public enquiry could lag but the processor-selected save already reported `HAVE_IN_BAG`, the decisive log pair appeared as designed:

```text
[SCRC-AP] CASSETTE reconciliation song='QUIERES_BAILAR' decision=VerifiedBag writeSubmitted=False.
[SCRC-AP] CASSETTE VERIFIED BAG nativeSong='QUIERES_BAILAR' status='HAVE_IN_BAG' outcome='terminal'; state preserved.
```

The user visually confirmed the cassette in carried inventory, then used the normal cassette-machine insertion flow and confirmed that Cassette 16 unlocked. After a full game restart, reconciliation observed `VerifiedDeposited` with `writeSubmitted=False`; the user confirmed Cassette 16 remained available and the deposited cassette was absent from carried inventory.

No later `CASSETTE REQUESTED nativeSong='QUIERES_BAILAR'` retry loop appeared. The removed `CASSETTE BUNDLE COLLISION DIAGNOSTIC` did not appear, and the safe build did not crash.

## Discarded diagnostic build

The diagnostic build from `d9e2bd2`/`9de6a65` is not a passing build and contributes no acceptance credit. Its single diagnostic execution was followed by a CoreCLR access violation. The entire diagnostic range was removed by `6706508`/`df70f74` before the safe authoritative-readback build was tested. The passing live evidence above applies only to the safe prefix-only build containing `e7ae90e`.

## Remaining concerns

- The focused `QUIERES_BAILAR` receipt, insertion, and full-restart path passed live. Other cassette identities and the remaining representative acceptance rows still need their own coverage.
- The authoritative reader still uses ordinary reflection across the IL2CPP wrapper boundary, but only for zero-argument state lookup and a one-enum-argument status getter. It has no nullable ref/out argument and does not retain wrapper state across a native request.
- Terminal bag state deliberately survives broad lifecycle events. The runtime does not currently identify a genuine selected-save identity change, so switching to a different save inside the same configured session will not rearm a cassette already observed terminal. This is the requested safe behavior until save identity can be tracked reliably.
- This documentation follow-up performed no install, game launch, merge, push, persistence request, direct save write, or production-code change.
