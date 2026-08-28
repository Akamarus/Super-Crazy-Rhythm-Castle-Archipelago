# Connection Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover automatically from unexpected Archipelago socket loss without restarting the game or duplicating state.

**Architecture:** A pure reconnect policy owns retry timing and intent; `ArchipelagoClient` owns session replacement and login. Unity-facing work remains queued to the main thread, while the last synchronized state remains authoritative during temporary loss.

**Tech Stack:** C#/.NET 6, Archipelago.MultiClient.Net 6.7.1, .NET 8 console tests.

**Spec:** `docs/superpowers/specs/2026-08-23-consolidated-stability-preview-abilities-design.md`

## Global Constraints

- Do not reconnect after deliberate shutdown or explicit disconnect.
- Do not block the Unity thread.
- Reconnect to the same server, slot, game, and password only.
- Do not duplicate callbacks, received items, or queued checks.
- Preserve last synchronized ownership during temporary loss.

---

### Task 1: Implement bounded reconnect policy

**Files:**
- Create: `client/ReconnectPolicy.cs`
- Create: `client/tests/ReconnectPolicy/ReconnectPolicy.Tests.csproj`
- Create: `client/tests/ReconnectPolicy/Program.cs`

**Interfaces:**
- Produces: `ReconnectPolicy.OnUnexpectedDisconnect()`, `OnDeliberateShutdown()`, `OnConnected()`, and `TimeSpan? NextDelay()`.
- Retry schedule: 1, 2, 5, 10, and 30 seconds, then 30 seconds repeatedly until deliberate shutdown.

- [ ] **Step 1: Write failing literal schedule tests**

Assert `NextDelay()` is null before disconnect, returns the exact schedule after unexpected loss, resets after `OnConnected`, and stays null after `OnDeliberateShutdown`.

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet run --project client/tests/ReconnectPolicy/ReconnectPolicy.Tests.csproj`  
Expected: FAIL because `ReconnectPolicy` does not exist.

- [ ] **Step 3: Implement the policy**

Use an integer attempt counter and two booleans, `reconnectRequested` and `shutdown`. No timers or sockets belong in this class.

- [ ] **Step 4: Run focused test and commit**

Expected: `Reconnect policy tests passed.`

```powershell
git add client/ReconnectPolicy.cs client/tests/ReconnectPolicy
git commit -m "feat: add bounded reconnect policy"
```

### Task 2: Integrate session recovery

**Files:**
- Modify: `client/Plugin.cs`
- Test: `client/tests/ReconnectPolicy/Program.cs`
- Modify: `docs/NEXT_RELEASE_BUG_FIXES.md`

**Interfaces:**
- Consumes: `ReconnectPolicy` from Task 1.
- Produces: one active `ArchipelagoSession`, one registered handler set, and one reconnect worker at a time.

- [ ] **Step 1: Add failing lifecycle tests**

Model socket close → scheduled retry → failed login → next delay → successful login. Assert deliberate shutdown never schedules a retry and successful recovery resets attempts.

- [ ] **Step 2: Separate session construction from login**

Extract `CreateSessionAndRegisterHandlers()` and `TryLogin()` inside `ArchipelagoClient`. Dispose or disconnect the old session before replacement. Register handlers exactly once per new session.

- [ ] **Step 3: Schedule one background reconnect loop**

On unexpected `SocketClosed` or terminal socket error, set `_connected=false`, notify the policy, and start a single guarded worker. Delay using the policy, attempt a new login, and stop on success or deliberate shutdown.

- [ ] **Step 4: Reconcile and flush once after recovery**

Rebuild received-item counts from the new session, queue native reconciliation on the Unity thread, and flush `_pendingChecks` through the existing `_queuedOrSent` protection.

- [ ] **Step 5: Verify and commit**

Run ReconnectPolicy, all client tests, and a release build. Expected: zero failures and zero build errors.

```powershell
git add client/Plugin.cs client/tests/ReconnectPolicy/Program.cs docs/NEXT_RELEASE_BUG_FIXES.md
git commit -m "fix: reconnect after unexpected server loss"
```

