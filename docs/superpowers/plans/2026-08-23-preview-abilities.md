# Preview Abilities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Register sendable preview Hypno Pan and Violance items and reconcile their permanent native abilities without adding them to generated seeds.

**Architecture:** APWorld registers two permanent item IDs but excludes both names from `create_items`. Two independent client reconcilers consume received-item counts and idempotently grant one verified native flag each.

**Tech Stack:** Python Archipelago world, C#/.NET 6 client, .NET 8 console tests.

**Spec:** `docs/superpowers/specs/2026-08-23-consolidated-stability-preview-abilities-design.md`

## Global Constraints

- Hypno Pan and Violance are preview-only and never enter the generated item pool or solver rules.
- Allocate item IDs `187256121` and `187256122`; allocate no source-location IDs.
- Vanilla and incompatible saves remain unchanged.
- Do not refactor the manually verified Plant Pipes reconciler.
- Do not merge, push, publish, or install without explicit approval.

---

### Task 1: Register preview datapackage items

**Files:**
- Modify: `apworld/scrc/items.py`
- Modify: `apworld/tests/test_items.py`
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `docs/IDS.md`

**Interfaces:**
- Produces: `HYPNO_PAN_ITEM_NAME`, `VIOLANCE_ITEM_NAME`, IDs `187256121` and `187256122`.
- Guarantees: `SCRCWorld.create_item(name)` succeeds while `SCRCWorld.create_items()` excludes both names.

- [ ] **Step 1: Write failing registry tests**

```python
self.assertEqual(self.items.NEW_ITEM_NAME_TO_ID["Hypno Pan"], 187256121)
self.assertEqual(self.items.NEW_ITEM_NAME_TO_ID["Violance"], 187256122)
```

In the integration test, call `world.create_items()` and assert both names are absent, then call `world.create_item` for each and assert the permanent ID and progression classification.

- [ ] **Step 2: Run tests to verify failure**

Run: `python -m unittest apworld.tests.test_items apworld.tests.test_world_integration -v`  
Expected: FAIL because the preview names are not registered.

- [ ] **Step 3: Add the registry entries**

```python
HYPNO_PAN_ITEM_NAME = "Hypno Pan"
VIOLANCE_ITEM_NAME = "Violance"
NEW_ITEM_NAME_TO_ID = {
    STAR_ITEM_NAME: BASE_ID + 118,
    HYPNO_PAN_ITEM_NAME: BASE_ID + 121,
    VIOLANCE_ITEM_NAME: BASE_ID + 122,
}
```

Classify both as `ItemClassification.progression`. Do not append them in `create_items`.

- [ ] **Step 4: Update permanent IDs and verify**

Document both IDs and advance the next safe item ID to `187256123`. Run the focused tests and `python tools/validate-repo.py`; expect both to pass.

- [ ] **Step 5: Commit**

```powershell
git add apworld/scrc/items.py apworld/tests/test_items.py apworld/tests/test_world_integration.py docs/IDS.md
git commit -m "feat: register preview Hypno Pan and Violance items"
```

### Task 2: Add isolated native ability reconcilers

**Files:**
- Create: `client/PreviewAbilityReconcilePolicy.cs`
- Create: `client/HypnoPanReconciler.cs`
- Create: `client/ViolanceReconciler.cs`
- Create: `client/tests/PreviewAbilities/PreviewAbilities.Tests.csproj`
- Create: `client/tests/PreviewAbilities/Program.cs`
- Modify: `client/Plugin.cs`

**Interfaces:**
- Produces: pure `PreviewAbilityReconcilePolicy.Decide(int receivedCount, bool compatible, bool saveAvailable, bool? nativeFlag) -> PreviewAbilityReconcileDecision`.
- Produces: separate runtime classes `HypnoPanReconciler` for `PIED_PIPER_ABILITY` and `ViolanceReconciler` for `VIOLIN_ABILITY`.
- Each runtime class exposes `NoteReceivedCount(int count)`, `OnLifecyclePoint(string reason)`, and `TickPending(TimeSpan elapsed)`.

- [ ] **Step 1: Write failing decision tests**

Test literal outcomes for: no ownership, incompatible session, save unavailable, owned flag already true, and owned flag false requiring one grant. Test duplicate `NoteReceivedCount(1)` calls do not submit duplicate grants after verification.

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet run --project client/tests/PreviewAbilities/PreviewAbilities.Tests.csproj`  
Expected: FAIL because the reconciler does not exist.

- [ ] **Step 3: Implement the minimal state machine**

```csharp
internal enum PreviewAbilityReconcileDecision
{
    NoOwnership,
    SaveUnavailable,
    AlreadyGranted,
    SubmitGrant,
}
```

`Decide` returns `SubmitGrant` only when received count is positive, the session is compatible, the save is readable, and the native flag is false. Each separate runtime reconciler submits its one named flag and reads that same flag back before reporting success.

- [ ] **Step 4: Wire AP receipt and lifecycle points**

When item history reports `Hypno Pan` or `Violance`, update its reconciler count on the network thread and queue native reconciliation to the Unity thread. Invoke reconciliation after initial sync, room transition, reconnect, save selection, and restart initialization. Log one concise `PREVIEW ABILITY RECEIVED` and `PREVIEW ABILITY VERIFIED` line per item lifecycle.

- [ ] **Step 5: Run focused and complete client tests**

Expected: PreviewAbilities passes and every existing client test project still passes.

- [ ] **Step 6: Commit**

```powershell
git add client/PreviewAbilityReconcilePolicy.cs client/HypnoPanReconciler.cs client/ViolanceReconciler.cs client/tests/PreviewAbilities client/Plugin.cs
git commit -m "feat: reconcile preview native abilities"
```
