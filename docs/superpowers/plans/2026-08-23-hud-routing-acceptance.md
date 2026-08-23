# HUD, Routing, and Acceptance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve provable HUD behavior, strengthen Royal/generation verification, and produce one consolidated manual candidate and checklist.

**Architecture:** Pure policies decide HUD context and routing reachability; native code applies only proven decisions. Unknown bottom-control initialization remains read-only instrumentation rather than guessed state mutation.

**Tech Stack:** C#/.NET client, Python APWorld tests, PowerShell packaging/build, Markdown acceptance ledger.

**Spec:** `docs/superpowers/specs/2026-08-23-consolidated-stability-preview-abilities-design.md`

## Global Constraints

- Hub6 shows Music Lab Points; campaign hubs show synchronized AP Stars.
- Bottom HUD controls character and REG/PRO difficulty and is unrelated to Bizzle and Clive.
- Royal Access reaches only the phone-side Level 22 subregion.
- Do not request replay of verified Level 3 or Plant Pipes behavior unless overlapping code changes.
- No release, merge, push, or tester publication without explicit approval.

---

### Task 1: Make Star HUD context deterministic

**Files:**
- Modify: `client/StarHudPolicy.cs`
- Modify: `client/tests/StarHud/Program.cs`
- Modify: `client/Plugin.cs`

**Interfaces:**
- Produces: `StarHudPolicy.Decide(bool enabled, bool compatible, bool synchronized, string? roomId) -> StarHudDecision`.
- Decisions: `Vanilla`, `PreserveMusicLabPoints`, `ShowCampaignStars`, `WaitForSynchronization`.

- [ ] **Step 1: Write failing context tests**

Assert Hub6 always preserves Music Lab Points; Hub2/Hub1/Hub3/Hub4/Hub5/Hub7 show campaign Stars only after synchronization; unknown rooms preserve vanilla; incompatible sessions preserve vanilla.

- [ ] **Step 2: Run the test and observe the policy mismatch**

Run: `dotnet run --project client/tests/StarHud/StarHud.Tests.csproj`  
Expected: FAIL because current decisions depend on campaign bootstrap flags rather than synchronized display context.

- [ ] **Step 3: Implement and integrate the minimal context policy**

Apply synchronized AP totals only to the campaign Star display in known campaign hubs. Do not activate the campaign display in Hub6 and do not write native medal totals.

- [ ] **Step 4: Verify and commit**

Run StarHud, all client tests, and release build.

```powershell
git add client/StarHudPolicy.cs client/tests/StarHud/Program.cs client/Plugin.cs
git commit -m "fix: separate campaign Star and Music Lab HUDs"
```

### Task 2: Investigate bottom-control initialization safely

**Files:**
- Create: `client/BottomHudDiagnosticPolicy.cs`
- Create: `client/tests/BottomHudDiagnostic/BottomHudDiagnostic.Tests.csproj`
- Create: `client/tests/BottomHudDiagnostic/Program.cs`
- Modify: `client/Plugin.cs`
- Modify: `docs/NEXT_RELEASE_BUG_FIXES.md`

**Interfaces:**
- Produces: one-shot snapshots keyed by room and save lifecycle containing native control type, path, interaction-enabled value, required condition type/result, block condition type/result, and relevant initialization flags.
- Produces no native state mutation until a specific prerequisite is proven.

- [ ] **Step 1: Write failing deduplication tests**

Assert the policy emits once for `direct-start`, `after-level-1`, `after-reload`, and `after-restart`, suppresses frame duplicates, and never requests mutation.

- [ ] **Step 2: Implement the read-only policy and focused native capture**

Bind only the exact combined control discovered in the active hub. Log one structured `BOTTOM HUD SNAPSHOT` per lifecycle key. Remove all phase writes and global `Resources.FindObjectsOfTypeAll` polling.

- [ ] **Step 3: Decide from evidence**

If repository/native evidence identifies a single prerequisite, add a new failing policy test for that prerequisite and implement only its narrow normalization. Otherwise retain instrumentation and leave the release bug open.

- [ ] **Step 4: Verify and commit**

Run BottomHudDiagnostic, DifficultyAvailability, all client tests, and release build.

```powershell
git add client/BottomHudDiagnosticPolicy.cs client/tests/BottomHudDiagnostic client/Plugin.cs docs/NEXT_RELEASE_BUG_FIXES.md
git commit -m "test: capture bottom hud initialization state"
```

### Task 3: Strengthen Royal routing and generation matrices

**Files:**
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `apworld/tests/fixtures/bk_seed_28223804408101432968.json` only if the fixture schema requires explicit Royal facts
- Modify: `docs/PROGRESSION.md`

**Interfaces:**
- Consumes: existing `Royal Corridor Access` region connection.
- Proves: Level 22 ordinary locations reachable with Royal Access; Level 21 and Royal Star Eater absent or unreachable without their future native events.

- [ ] **Step 1: Add failing split-routing assertions**

Create a state with only `Royal Corridor Access`. Assert Level 22 ordinary locations are reachable and explicitly assert no Level 21 or Royal Star Eater location is exposed through that state.

- [ ] **Step 2: Add deterministic generation coverage**

For seeds `0..99` across every supported difficulty and start, build regions/items/rules and run sphere expansion. Assert every required progression item is reachable and unsafe performance locations reject progression placement.

- [ ] **Step 3: Run focused and full APWorld suites**

Expected: focused routing/matrix tests pass; full APWorld suite and repository validator pass.

- [ ] **Step 4: Commit**

```powershell
git add apworld/tests/test_world_integration.py apworld/tests/fixtures/bk_seed_28223804408101432968.json docs/PROGRESSION.md
git commit -m "test: enforce Royal split routing and seed reachability"
```

### Task 4: Build consolidated candidate and manual handoff

**Files:**
- Modify: `client/Plugin.cs` and `client/build.ps1` for the next candidate version
- Modify: `apworld/scrc/__init__.py` and manifest metadata for the next preview world version
- Modify: `CHANGELOG.md`
- Modify: `docs/NEXT_RELEASE_BUG_FIXES.md`
- Create: `docs/testing/2026-08-23-consolidated-preview-acceptance.md`

**Interfaces:**
- Produces: one matching client/APWorld candidate and one ordered manual checklist.

- [ ] **Step 1: Run complete automated verification**

Run every client test project, every APWorld test, `tools/validate-repo.py`, and the release build. Expected: zero test failures, repository validation passes, and build has zero errors.

- [ ] **Step 2: Update versions and changelog accurately**

List only automated repairs as candidates. Keep bottom HUD, preview native sources, and any unplayed behavior under known issues until manual evidence passes.

- [ ] **Step 3: Write the evidence-led manual checklist**

Include direct start, difficulty choices, both HUD contexts, bottom control lifecycle, server restart recovery, AP delivery/persistence of all three abilities, representative Hypno/Violance interactions, overlapping Garage/Music Lab checks, Royal split routing, and controlled Level 22 failure/success. Mark Level 3 and existing Plant Pipes persistence as no-retest unless overlapping code changed.

- [ ] **Step 4: Stop for installation approval**

Report exact test counts, build result, artifact paths, known issues, and manual steps. Do not install, merge, push, or publish until the owner explicitly approves.
