# Network security candidate validation

November 8, 2026. Original candidate audit and validation record; release-specific verification is recorded below.

## Changes

- `ConnectionEndpointPolicy.Normalize` supplies explicit WSS for bare remote addresses and WS for loopback. Explicit WS remains a deliberate plaintext opt-in; unsupported schemes, embedded credentials and fragments are rejected.
- `BoundedWebSocketReceive.ReadAsync` caps each decompressed message at 32 MiB before appending a fragment. A partial-message deadline starts only after the first non-final fragment and expires after 60 seconds. Idle waiting is allowed. Binary/invalid UTF-8 payloads fail, partial close payloads are discarded, and failures abort the socket so the dependency's polling loop stops.
- `NetworkReceiveGuard.EnsureInstalled` validates the pinned library's reader signature, socket field and close callback, then installs a Harmony prefix before session creation. Failed installation prevents connection. The existing dependency DLLs and licenses are unchanged. This adapter is necessary because the pinned factory does not offer a custom transport or receive-size setting.

## Evidence

Before the fix, the endpoint regression failed for a bare remote address; a local WebSocket test delivered 33 MiB through the actual pinned dependency, reproducing the missing limit. After the fix, the same actual-library reader rejects the oversized message and leaves the socket aborted.

Targeted tests cover exact size boundaries, fragmentation, UTF-8 split across frames, invalid UTF-8, binary rejection, stalled partial-message cancellation, idle waiting, and close handling. A real session parsed a fragmented 1 MiB PrintJSON message through the guard and emitted its close event. A local peer observed the TLS handshake and closed the connection; the actual library failed without opening a plaintext retry.

All 48 client regression projects pass. Some source-reading tests initially failed because the runner's working directory was the task folder; rerunning those from the repository root passed. The first restricted-environment listener failed to initialize; the local-only integration tests passed outside that restriction. These were test-environment failures, not hidden product failures.

Client Release build succeeded with zero warnings/errors and SkipInstall. Repository validator passed. All 11 tool/packaging tests passed (the duplicate-entry negative test emits its expected ZIP warning). Test builds emitted NETSDK1138 about their net6.0 target being out of support; resolving the installed mod-loader/runtime support lifecycle is separate from these two fixes.

## Limits and next acceptance

No game launch or live large-room login was performed. The 32 MiB ceiling was chosen as a finite per-message guard with room for large packets, not measured from a live 1,900-player room. Legitimate messages above it will disconnect; obtain real size evidence before changing the ceiling. This bounds one message and does not claim to stop every message-rate or parser-allocation attack.

Before deployment acceptance, verify guard installation in BepInEx, remote WSS login, local WS login, reconnect, normal item delivery and location checks. For a plaintext remote server, use an explicit WS address and recognize that traffic is unencrypted. No APWorld rule, item ID, save, seed or runtime configuration was changed.

Follow-up review found no confirmed production defect. The oversized-message assertion was strengthened to reject every successful return, including truncation. Further tests passed for unpatch/reinstall with real packet delivery and injected guard failure in the production connection flow: login is blocked, the failed generation is abandoned, and a protected retry succeeds. Compressed transport and real stalled-frame timing remain additional coverage; decompressed reader limits and scripted deadline behavior are tested.

## v0.30.1 release verification
The clean release checkout excludes the unfinished cutscene candidate. All 47 release client projects passed, along with repository validation and 11 tool tests. Client0.77.1 Release build completed with zero warnings/errors without deployment. APWorld source is unchanged; the release reuses the exact v0.30.0 scrc.apworld asset. The earlier 48-project result above includes the separate cutscene candidate and is not the release test count.
