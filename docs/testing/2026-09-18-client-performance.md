# Client v0.75.3 performance review - 2026-09-18

Status: v0.75.3 installed and tested; user reports improvement with residual lag. Follow-up v0.75.4 built but not installed; native acceptance pending. User paused the full Hard/40 playthrough to address lag in all areas. Existing seed/save and all progression rules are retained. Quicksand reward leak remains deferred.

## Review scope and findings

Reviewed the runtime Update/LateUpdate/OnGUI entry points, their reconciliation calls, shared reflection helpers, native save/status adapter, condition-hook routing, notification rendering, and diagnostic/scene-scan entry points. This is a hot-path review across the client, not a claim that every code path has been profiled live.

- CassetteSaveTransactionAdapter.FindType enumerated every type in the preferred assembly before consulting its cache, once per native cassette read. It now caches positive results per assembly/name and attempts exact type lookup first. ObtainState method metadata is also cached by type and binding flags. Native state and pointer are still read on every invocation.
- ReflectionUtil repeatedly enumerated loaded assemblies/types and resolved the same fields/properties. It now caches positive game-assembly lookup, immutable type/member metadata, and missing members. Partial type loads retry after an assembly-load event; dynamic assembly catalogs are never cached. Throwing property getters retain field fallback. Moved the utility into its own source file without changing callers.
- Cassette readiness performed a full-path Hub6 phone-bank search every frame in all rooms. The keeper now searches only in Hub6, reuses a live active object, and retries missing/destroyed objects at most four times per second. Existing fresh marker incarnation, save generation, and pointer verification gates remain authoritative.
- Character quest item ownership snapshots cloned a HashSet each frame before checking the one-second timer. Revision is now checked before cloning; changed receipts still reconcile immediately.
- Notification overlay entered GUI setup even with no visible content. Empty/disabled views now return immediately; empty visible snapshots reuse Array.Empty.
- Area baseline and phone keepers, native interaction guards, packet synchronization, journal writes, and required save checks remain active. Periodic reconciliation still reads current save state; no blanket throttling or gameplay-condition suppression was introduced.
- Diagnostic scans inspected are manual, lifecycle-bound or room/bounded-poll paths. The obsolete MusicLabPointBoundaryDiagnostics config key is no longer read by current code. Remaining periodic reflection/native I/O may still contribute stalls and requires measurements.

## Measurements and validation

Offline benchmark against installed Assembly-CSharp interop metadata (6,982 loadable types): old repeated type scan took 6,689.3 ms for 1,000 calls and allocated 492,354 bytes/call; the revised shared cached catalog plus the same type search took 1.3 ms and 32 bytes/call. This standalone process experiences partial type-load failures and is not the game's frame-time environment. Do not translate these numbers into an FPS improvement claim.

34/34 client regression projects pass, including 20 new reflection/probe/profiling checks, save isolation, receipt persistence, reconnect, quest handling and Star Eater reference lifecycle. Release build succeeds with zero errors; four existing nullable warnings and the existing NuGet vulnerability-metadata network warning remain.

## Native acceptance

Enable Developer.EnablePerformanceDiagnostics for the approved test installation. It is off by default. Measures all keeper Update/LateUpdate methods, notification Update/OnGUI, and individual reconciliation sections. Reports once per 30-second window or on leaving a room after at least five seconds; no per-frame log I/O. Component timings and nested section timings overlap and must not be summed as independent totals. Frame intervals include native game/GPU/OS work; transition intervals are excluded. This does not measure every native hook or all game code.

Preserve the current seed and slot 4. Test ordinary movement for at least 30 seconds, then a Music Lab song including 10-4 Good Buddy. Inspect worst update-section time versus frame stalls. Verify pending AP item delivery and normal scene transition/re-entry still work. If lag persists outside measured sections, profile native hooks/rendering next instead of assuming the optimization solved it. Gameplay acceptance is pending.

## v0.75.4 follow-up: cassette persistence polling

Live v0.75.3 Music Lab windows measured Reconcile.Cassettes at approximately 9.65-9.92 ms per frame while cassette persistence verification remained pending. Other measured reconciliation sections were small. These are observed section timings, not attribution of every frame stall to the plugin.

The adapter still rebuilt the preferred assembly type catalog when resolving types from other assemblies, before checking its global positive cache. Its GetLoadableTypes now delegates to the shared ReflectionUtil catalog cache. Partial catalogs remain revision-aware and dynamic assemblies remain uncached. Native save identity, status and persistence acceptance are still checked on every existing poll; no save state is cached.

Regression first failed because repeated adapter catalog lookups returned distinct arrays, then passed with the shared catalog. CassetteRandomization and all 20 RuntimePerformance checks pass. Release build succeeds with zero errors and the existing warnings; repository validation and diff whitespace checks pass. Existing seed and slot 4 are retained. Installation/restart approval and a repeat Music Lab timing sample are next; the remaining lag is not yet claimed fixed.

### v0.75.4 first native sample

Installed with approval; startup confirms v0.75.4, three Star hooks and connection to the retained Jack seed on port 38287. User reports much better performance, but a few remaining hitches cost notes. Acceptance therefore remains incomplete.

GameRoom_10 cassette reconciliation now totals roughly 14.6-15.1 ms over 2,250 frames (about 0.007 ms/frame), compared with roughly 9.7 ms/frame previously. Seven middle 30-second windows report no frames above 33 ms; some still peak at 27-29 ms. First and final song-room windows include larger stalls (187/159 ms) while measured keeper sections remain small. Those windows include entry/result activity and cannot identify exactly which stalls occurred during active notes. Final result identifies No Plan B Silver. No exception is reported in the captured plugin log.

The remaining hitches are not explained by measured reconciliation cost. Native hooks, rendering, scheduling and collection outside these sections remain unmeasured; do not attribute the remainder to any one of them without further evidence. Next comparison: replay the same song in the same session and distinguish early, mid-song and result-screen hitches. Preserve this capture locally as live-0754-first-song.log; do not commit the log.

### Second native sample: Keep on Hustlin

User reports one noticeable hitch about a quarter through a different song. Captured result is KEEP_ON_HUSTLIN. The eight middle 30-second windows have no frames above 33 ms; peaks range 18.91-28.31 ms. Cassette section remains approximately 0.007 ms/frame with maxima 0.02-0.08 ms during these windows. Measured parent reconciliation maxima are about 1.1-1.3 ms. This confirms the repeated cassette scan remains resolved, but does not localize the reported hitch or establish that the plugin is otherwise uninvolved.

Performance reporting itself emits a long synchronous log message every 30 seconds; console logging is enabled and disk logging uses buffered flushing. Next controlled comparison is the same installed build/song with EnablePerformanceDiagnostics=false, retaining ordinary logs and all gameplay behavior. This requires a game restart because the setting is read only during plugin Load. No further speculative gameplay changes are justified by the aggregate timings alone. If hitches persist, narrower hook/allocation or native frame profiling is needed.
