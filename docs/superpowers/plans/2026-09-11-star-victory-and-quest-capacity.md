# Star Victory and Quest Capacity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce the first solver-valid single-player playthrough by confirming at least 11 genuine quest/action checks, activating all 66 individual AP Stars, enforcing deterministic campaign Star gates, and requiring a post-threshold Level 22 clear for Victory.

**Architecture:** A shared evidence-backed progression catalog drives APWorld locations, rules, generated Star requirements, and the client’s native-event registry. APWorld owns permanent IDs, item-pool capacity, solver logic, and a strict versioned slot-data contract. The client validates that contract, reconstructs AP Stars from authoritative received-item history, uses one policy for gate display and enforcement, queues confirmed native actions through the existing identity-bound location pipeline, and sends goal status only from a qualifying Level 22 result.

**Tech Stack:** Python 3.13, Archipelago 0.6.7 world APIs, `unittest`, C#/.NET 6 client, .NET 8 pure-policy console tests, Archipelago.MultiClient.Net 6.7.1, Harmony/BepInEx IL2CPP, PowerShell build and packaging scripts.

**Spec:** `docs/superpowers/specs/2026-09-11-star-victory-and-quest-capacity-design.md`

## Global Constraints

- Target APWorld candidate version is exactly `0.25.0`; target client candidate version is exactly `0.71.0`.
- Append `star-victory-quest-capacity-0.25` to the complete v0.24 implementation-version string; preserve every historical prefix.
- Advance top-level slot-data `schema_version` from `15` to `16`; retain campaign-level-mapping schema `1` and add Star/Victory schema `1` plus quest-action schema `1`.
- Preserve every existing item and location ID. The `Star` item remains permanent ID `187256118` and is generated exactly 66 times.
- Do not allocate action-location IDs until the confirmation gate in Task 1 passes. Confirmed actions receive contiguous IDs beginning at `187256292`, in the approved candidate-ledger order, with no gaps and no IDs for Provisional, Rejected, or Deferred rows.
- Freeze that first allocation permanently. A candidate confirmed after this milestone must append at the then-current safe frontier in a later plan; it must never be inserted into the initial sequence or renumber an existing location.
- Require at least 11 confirmed, net-new, progression-safe action locations before enabling Stars. The target is 15; 15 confirmations produce Normal capacity 136 and four Stardust with the current 132 required items.
- Preserve all 20 Music Lab Point instances and their exact `10 x 1`, `3 x 10`, `7 x 20` distribution, 180 total value, and final threshold 140.
- Preserve the forced Roots starter for this milestone.
- Do not use Development Caches, automatic Star/point milestones, inactive performance tiers, or synthetic checks for capacity.
- Keep Bee/Devil production locations inactive. Their future six completion-only checks will join Normal and every higher difficulty only after a separate evidence-backed plan.
- Never infer a native flag, gate, scene object, item prerequisite, or quest order from display text. Historical evidence or focused diagnostics must prove it.
- Native result Stars remain performance records and AP locations; never write the AP Star total into native earned-Star storage.
- Level 22 must remain enterable below `required_stars`. Royal Star Eater completion must remain optional and must not gate the phone-side Level 22 route or Victory.
- Before initial received-item synchronization, only AP Star-gated interactions fail closed. A same-identity disconnect retains the last complete total; a different identity clears it.
- Build without deployment first. Deploy only after explicit approval and only to `D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP`.
- Do not merge, push, publish, remove worktrees, or alter the unrelated `WordFactori/` directory without explicit approval.
- Gameplay acceptance is final. Automated tests and server-sent setup items cannot substitute for the full fresh-seed playthrough.

## File and Responsibility Map

- `docs/testing/2026-09-11-star-victory-and-quest-capacity-acceptance.md` — candidate ledger, evidence citations, build/seed/save identity, targeted results, and full-playthrough verdict.
- `apworld/scrc/quest_actions.py` — ordered candidate catalog, confirmation state, permanent confirmed-location IDs, native identity metadata, area ownership, item prerequisites, and prerequisite event names.
- `apworld/scrc/campaign_levels.py` — normal level identities plus solver-only clear-event names and confirmed mandatory-chain relationships.
- `apworld/scrc/star_requirements.py` — deterministic graph-depth Star requirements, contract canonicalization/digest, and structural capacity validation.
- `apworld/scrc/__init__.py` — action and clear-event region construction, 66-Star pool activation, Star-gated rules, Level 22 Victory event, and schema-16 slot data.
- `apworld/tests/test_quest_actions.py` — candidate ordering, confirmation gate, ID allocation, evidence completeness, uniqueness, and prerequisite validation.
- `apworld/tests/test_star_requirements.py` — deterministic graph generation, mandatory-chain monotonicity, concurrent routes, digest, thresholds, and rejection cases.
- `apworld/tests/test_items.py` — exact Star and preserved item-instance counts.
- `apworld/tests/test_world_integration.py` — instantiated totals, rules, solver spheres, Royal split, Victory, slot data, and representative generation matrices.
- `client/StarProgressionContract.cs` — strict schema-16 Star/Victory and action-contract parsing with field-specific rejection.
- `client/StarRandomization.cs` — identity-bound effective Star snapshot and contract lifecycle.
- `client/StarSessionHistory.cs` — authoritative `ReceivedItemsPacket` reconstruction by item ID and receipt index.
- `client/StarGatePolicy.cs` — one pure decision for displayed requirement and physical interaction admission.
- `client/QuestActionCatalog.cs` — exact confirmed native-event-to-location registry copied from the permanent APWorld contract.
- `client/QuestActionRandomization.cs` — event transition checks, startup/reconnect reconciliation, and bounded diagnostics.
- `client/Level22VictoryPolicy.cs` — pure qualifying-clear decision and non-retroactive offline snapshot rules.
- `client/StarHudPolicy.cs` — campaign/Music Lab/other-room HUD routing using the synchronized Star snapshot.
- `client/CampaignLocationContract.cs` and `client/CampaignLevelRandomization.cs` — v0.24 legacy compatibility plus schema-16 campaign identity binding.
- `client/Plugin.cs` — packet/session wiring, native gate/HUD adapters, progression-event dispatch, result ordering, pending goal delivery, and diagnostics.
- `client/tests/StarProgression/` — contract, real packet history, identity, reconnect, cap, and production-wiring tests.
- `client/tests/StarGate/` — display/enforcement parity, lifecycle states, native-hook wiring, and no-native-Star-write tests.
- `client/tests/QuestActions/` — registry, transition, reconciliation, deduplication, unknown-event, and production-wiring tests.
- `client/tests/Level22Victory/` — early clear, final-Star receipt, replay, offline snapshot, identity, and status-send tests.
- `client/tests/StarHud/` and `client/tests/LevelCompletion/` — retained room routing, ordinary result checks, compatibility, and regressions.
- `tools/validate-repo.py` and `apworld/tests/test_repository_contract.py` — independent version, schema, ID, count, digest, and source-contract enforcement.
- `docs/IDS.md`, `docs/PROJECT_OVERVIEW.md`, `docs/PROGRESSION.md`, `docs/TESTING.md`, `docs/ROADMAP.md`, `README.md`, `apworld/README.md`, embedded APWorld docs, `CHANGELOG.md`, `client/build.ps1`, and `apworld/scrc/archipelago.json` — permanent registry, public behavior, candidate status, install/build truth, and versions.

---

### Task 1: Confirm the Action Ledger and the Native Gate/HUD Boundaries

**Files:**
- Create: `docs/testing/2026-09-11-star-victory-and-quest-capacity-acceptance.md`
- Modify: `client/Plugin.cs`
- Create: `client/QuestActionDiagnosticPolicy.cs`
- Create: `client/tests/QuestActions/QuestActions.Tests.csproj`
- Create: `client/tests/QuestActions/Program.cs`

**Interfaces:**
- Consumes: `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md`, conversation `6a823d3d-f450-83ea-9ad2-890691a86084`, existing bounded progression-flag logging, and current area-access design.
- Produces: a signed-off ledger with `Confirmed`, `Provisional`, `Rejected`, or `Deferred` for all 17 candidates; exact native identities and prerequisites for every Confirmed row; exact native HUD and gate hook identities; a hard pass/fail count.

- [ ] **Step 1: Create the evidence record before changing production behavior**

Create the acceptance record with these exact candidate keys in this exact order:

```text
roots_combo_bucket_conversion
lobby_important_letters_delivery
lobby_plunger_hand_in
lobby_fish_tears_delivery
meat_hypno_pan_creation
meat_act_1_music_delivery
meat_act_3_music_delivery
meat_act_4_music_delivery
meat_cat_return
meat_scruffy_return
meat_mouse_revolution
cell_super_nectar_delivery
tower_minim_eye_restoration
tower_minim_mind_restoration
tower_minim_heart_restoration
tower_totem_completion
royal_star_eater_feed
```

For each row record: canonical location name, area, physical action, native event/flag, false-to-true semantics, repeat behavior, reload result, reconnect result, required Area Access, required items, required level/quest events, vanilla reward relationship, distinctness from neighboring checks, offline reconciliation strategy, evidence source, and status.

- [ ] **Step 2: Recover existing evidence before requesting gameplay**

Read the historical evidence document and the original gameplay conversation in bounded pages. Cite specific turns or log excerpts in the acceptance record. Reconcile old per-level-unlock discussion through the approved Area Access model; do not copy superseded routing conclusions.

At minimum, explicitly validate the known identities `LEVEL_09_COMBO_ABILITY_EARNED`, `LOBBY_HUB_FISH_TEARS_DEPOSITED`, `MEAT_HUB_ACT_ONE_MUSIC_DONE`, `MEAT_HUB_ACT_THREE_MUSIC_DONE`, `MEAT_HUB_ACT_FOUR_MUSIC_DONE`, `MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED`, `PRISON_HUB_BEES_ESCAPED`, and `KING_CORRIDOR_STAR_EATER_FED`.

- [ ] **Step 3: Prove the native Star gate and HUD hook points**

Record the exact managed types/methods and native object/member identities that:

1. read a campaign level's required Star total;
2. decide whether the interaction is admitted;
3. render the requirement/current total in campaign hubs; and
4. render Music Lab Points in `GameRoom_Hub6`.

The accepted boundary must allow the same pure policy result to drive display and interaction. Reject any proposal that only changes text, only disables a collider, writes native earned-Star storage, or performs unbounded frame logging.

- [ ] **Step 4: Add bounded read-only diagnostics test-first**

Add a failing pure-policy test that requires an allowlisted room/token and bounded one-time logging for the 17-candidate discovery queue. Run:

```powershell
dotnet run --project .\client\tests\QuestActions\QuestActions.Tests.csproj -c Release
```

Expected: RED because the diagnostic policy or production dispatch does not yet exist.

Implement only read-only capture through the existing `GameProgressionFlagUpdatedEvent`, request, result, and scene-object diagnostic paths. Historical evidence may make a live trace unnecessary, but the diagnostic remains available for any unresolved row. It must not call `QueueLocation`, submit a native flag, grant an item, suppress a reward, or alter a gate. Re-run the focused project and expect GREEN.

- [ ] **Step 5: Apply the hard confirmation gate**

Count only Confirmed rows with a distinct one-time action and a complete reconciliation path. Require:

```text
confirmed_action_count >= 11
```

If the count is below 11, stop the implementation. Leave Stars, Star gates, and Victory inactive, record the exact missing evidence, and return to the user for focused gameplay. Do not continue by allocating IDs or substituting synthetic checks.

- [ ] **Step 6: Commit the evidence checkpoint**

```powershell
git add docs/testing/2026-09-11-star-victory-and-quest-capacity-acceptance.md client/Plugin.cs client/QuestActionDiagnosticPolicy.cs client/tests/QuestActions
git commit -m "docs: confirm Star milestone action evidence"
```

This commit contains no production action checks, IDs, Star activation, or gate mutation.

---

### Task 2: Freeze Confirmed Actions and Allocate Permanent Location IDs

**Files:**
- Create: `apworld/scrc/quest_actions.py`
- Create: `apworld/tests/test_quest_actions.py`
- Modify: `docs/IDS.md`
- Modify: `tools/validate-repo.py`
- Modify: `apworld/tests/test_repository_contract.py`

**Interfaces:**
- Produces: `QuestAction`, `ACTION_CANDIDATES`, `CONFIRMED_ACTIONS`, `CONFIRMED_ACTION_LOCATION_NAME_TO_ID`, `CONFIRMED_ACTION_COUNT`, `validate_quest_actions()`, and the new safe location frontier.

- [ ] **Step 1: Write the catalog and ID tests**

Test all 17 keys in Task 1's fixed order. Require every Confirmed entry to have a nonempty canonical name, area, native identity, evidence citation, and reconciliation mode, plus explicit item-prerequisite and event-prerequisite tuples (empty tuples are valid when the evidence proves no prerequisite). Reject duplicate keys, names, native identities, or IDs.

Use this allocation rule and assert it exactly:

```python
FIRST_ACTION_LOCATION_ID = 187256292
CONFIRMED_ACTION_LOCATION_NAME_TO_ID = {
    action.location_name: FIRST_ACTION_LOCATION_ID + index
    for index, action in enumerate(CONFIRMED_ACTIONS)
}
```

Assert `len(CONFIRMED_ACTIONS) >= 11`. Assert all non-Confirmed candidates have no ID. Assert the next safe frontier is `187256292 + len(CONFIRMED_ACTIONS)`.

- [ ] **Step 2: Run the focused test and verify RED**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_quest_actions.py' -v
```

Expected: RED because `quest_actions.py` does not exist.

- [ ] **Step 3: Implement the immutable catalog**

Use frozen records and explicit tuples. Do not derive native identities from location names. Preserve candidate rows that did not pass as documentation-only entries with their non-Confirmed status, but expose only `CONFIRMED_ACTIONS` to production construction.

Validate that each `required_event` names either a solver-only normal-level clear event or another earlier Confirmed action event. Reject cycles with a deterministic topological walk.

- [ ] **Step 4: Update the permanent registry and independent validator**

Add every confirmed name/ID pair to `docs/IDS.md` in ledger order. Advance the next safe location frontier using the tested formula. Extend `tools/validate-repo.py` and its fixture tests so changing a candidate order, confirmation state, name, ID, evidence field, or frontier fails with a precise message.

- [ ] **Step 5: Run focused and validator tests**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_quest_actions.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_repository_contract.py' -v
py .\tools\validate-repo.py
```

Expected: GREEN, with the validator printing the confirmed action count, allocated range, and advanced frontier.

- [ ] **Step 6: Commit**

```powershell
git add apworld/scrc/quest_actions.py apworld/tests/test_quest_actions.py docs/IDS.md tools/validate-repo.py apworld/tests/test_repository_contract.py
git commit -m "feat(apworld): register confirmed quest action locations"
```

---

### Task 3: Model Campaign Clear Events and Action Logic

**Files:**
- Modify: `apworld/scrc/campaign_levels.py`
- Modify: `apworld/scrc/quest_actions.py`
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_campaign_levels.py`
- Modify: `apworld/tests/test_quest_actions.py`
- Modify: `apworld/tests/test_world_integration.py`

**Interfaces:**
- Produces: `campaign_clear_event_name(level_number)`, solver-only `Level N Cleared` event items, solver-only `Quest Event: <key>` items, and evidence-backed access rules for confirmed action locations.

- [ ] **Step 1: Write failing graph tests**

Assert that every normal level has one addressless clear-event location with a locked `Level N Cleared` progression event. Its access rule must exactly mirror that level's completion rule, including Area Access through its parent region, required items, predecessor events, and later its generated Star threshold.

Assert that every Confirmed action creates:

1. one addressed AP location in its verified area; and
2. one addressless locked action event with the same access rule.

Assert action locations exist in all four difficulties, are never filtered as performance tiers, and allow progression placement only when the evidence ledger marks them progression-safe.

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_campaign_levels.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_quest_actions.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_world_integration.py' -v
```

Expected: RED because the clear/action event graph is absent.

- [ ] **Step 3: Add explicit prerequisite edges**

Encode only prerequisites proven in Task 1. Keep Level 22 independent from Level 21 and the Royal Star Eater. Preserve the phone-side Royal route by parenting Level 22 and Victory in Royal Corridor without adding either Royal action as a predecessor.

For every action, build one reusable access-rule closure from its explicit required items and required event items; assign that closure to both the addressed action and its solver event. Do not use display-name matching or implicit table order as logic.

- [ ] **Step 4: Re-run and commit**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_campaign_levels.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_quest_actions.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_world_integration.py' -v
git add apworld/scrc/campaign_levels.py apworld/scrc/quest_actions.py apworld/scrc/__init__.py apworld/tests/test_campaign_levels.py apworld/tests/test_quest_actions.py apworld/tests/test_world_integration.py
git commit -m "feat(apworld): model quest action progression graph"
```

Expected: all focused tests GREEN.

---

### Task 4: Generate Solver-Safe Star Requirements from the Confirmed Graph

**Files:**
- Modify: `apworld/scrc/star_requirements.py`
- Modify: `apworld/scrc/campaign_levels.py`
- Modify: `apworld/tests/test_star_requirements.py`
- Modify: `apworld/tests/test_world_integration.py`

**Interfaces:**
- Produces: `generate_star_requirements(required_stars, rng, graph)`, `validate_star_requirements()`, `validate_star_gate_capacity()`, `canonical_star_contract()`, and `star_contract_digest()`.

- [ ] **Step 1: Replace linear-order assumptions with failing graph tests**

Test representative goals `1`, `25`, `50`, and `66` over at least 100 deterministic RNG seeds. Require:

- exactly Levels 1 through 22;
- integer thresholds in `0..required_stars - 1`;
- deterministic output for the same RNG seed and graph;
- no decrease along an explicitly declared mandatory predecessor edge;
- equal-depth concurrent routes may differ and do not acquire an artificial order;
- Level 22 below `required_stars`;
- Royal Star Eater optional and absent from the Level 22 predecessor closure;
- enough structurally reachable addressed locations to supply each threshold before crossing that gate; and
- rejection of a fixture whose only remaining Stars self-lock behind the tested gate.

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_star_requirements.py' -v
```

Expected: RED because the current generator enforces one global nondecreasing level order and has no structural capacity validator or digest.

- [ ] **Step 3: Implement depth-based deterministic generation**

Compute depth from the explicit prerequisite DAG, not campaign numbering. Scale each node's target to `required_stars - 1`, apply the existing bounded RNG jitter, then clamp against only its mandatory predecessors. Keep opening depth at zero-capable. Reject cycles or an incomplete 22-level mapping.

Canonicalize the contract as ordered UTF-8 JSON containing schema `1`, required goal, Star item ID/count, campaign mapping schema, and all 22 `(number, internal_id, requirement)` rows. Produce a lowercase SHA-256 hex digest. Do not include unordered dictionary serialization or platform-dependent whitespace.

- [ ] **Step 4: Implement structural gate-capacity validation**

For each level threshold, walk the rule graph without crossing that level's Star gate and count unique active addressed locations that can legally hold progression. Require the count to be at least the threshold. Include quest/action and existing location rules; exclude addressless events and the gated location itself. This validator complements Archipelago fill—it does not predict item placements or replace the core solver.

- [ ] **Step 5: Re-run and commit**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_star_requirements.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_world_integration.py' -v
git add apworld/scrc/star_requirements.py apworld/scrc/campaign_levels.py apworld/tests/test_star_requirements.py apworld/tests/test_world_integration.py
git commit -m "feat(apworld): generate solver-safe Star requirements"
```

---

### Task 5: Activate the 66-Star Pool and Enforce APWorld Gates

**Files:**
- Modify: `apworld/scrc/items.py`
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_items.py`
- Modify: `apworld/tests/test_world_integration.py`

**Interfaces:**
- Consumes: permanent Star item ID `187256118`, `STAR_ITEM_COUNT = 66`, confirmed action capacity, and generated requirements.
- Produces: exactly 66 randomized Star instances, Star-count access rules on every normal level/check and clear event, and Stardust for remaining capacity.

- [ ] **Step 1: Write failing pool and rule tests**

Assert every difficulty generates exactly:

```text
5 nonstarter Area Access
5 randomized Garage cartridges
6 existing active quest items/abilities
30 cassettes
20 Music Lab Point instances
66 Stars
```

Assert current non-Star counts are unchanged. Assert Normal capacity is `121 + confirmed_action_count`, required items total 132, and Stardust is `Normal capacity - 132`. Assert generation fails with the existing clear capacity error when a fixture removes enough confirmed actions to fall below 132.

For each campaign location and its clear event, require both the existing item/event prerequisites and `state.count("Star", player) >= generated_requirement[level]`.

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_items.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_world_integration.py' -v
```

Expected: RED because `create_items()` does not add Stars and campaign access rules do not count them.

- [ ] **Step 3: Activate Stars without changing other economies**

Extend the existing `progression_items` list with `planned_star_names()` only after the confirmed-action capacity assertion. Keep `Star` classified progression. Retain the existing Music Lab Point pool verbatim. Compute filler from instantiated addressed capacity as today.

Update the integration-test `State.has()` helper to accept Archipelago's optional count argument and compare it with `State.count()`, so the tests exercise the same `state.has("Star", player, requirement)` interface as production.

Create one helper that returns a level's combined item/event/Star rule and use it for Completion, enabled performance tiers, and the corresponding clear event. Do not duplicate threshold logic across location types.

- [ ] **Step 4: Run the full APWorld suite and commit**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
git add apworld/scrc/items.py apworld/scrc/__init__.py apworld/tests/test_items.py apworld/tests/test_world_integration.py
git commit -m "feat(apworld): activate individual Stars and campaign gates"
```

Expected: all APWorld tests GREEN and every supported pool exactly balanced.

---

### Task 6: Replace Development Victory with the Level 22 Goal

**Files:**
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_world_integration.py`

**Interfaces:**
- Produces: addressless `Victory` event on the logical Level 22 completion route; rule `Level 22 completion prerequisites + required_stars Stars`; unchanged completion condition `state.has("Victory", player)`.

- [ ] **Step 1: Write failing Victory tests**

Assert:

- owning all six Area Access items without the goal Stars is not Victory;
- Royal Corridor Access plus Level 22 prerequisites and `required_stars - 1` Stars is not Victory;
- adding the final Star makes the solver-only Level 22 Victory event reachable;
- Level 22 Completion remains reachable at its generated threshold below the goal;
- neither Level 21 nor Royal Star Eater is required for Level 22 or Victory;
- ordinary Level 22 locations remain addressed checks and Victory remains addressless; and
- the completion condition still depends only on the locked Victory event item.

- [ ] **Step 2: Run the focused test and verify RED**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_world_integration.py' -v
```

Expected: RED because current `set_rules()` grants development Victory for all Area Access items.

- [ ] **Step 3: Implement the new solver rule**

Move the Victory event to the Royal Corridor/Level 22 logical route and reuse the same verified Level 22 completion prerequisites plus `required_stars`. Remove `development_area_access_victory_active` from the new slot-data path; old seeds remain a client compatibility concern and are not regenerated.

- [ ] **Step 4: Run and commit**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_world_integration.py' -v
git add apworld/scrc/__init__.py apworld/tests/test_world_integration.py
git commit -m "feat(apworld): require post-threshold Level 22 victory"
```

---

### Task 7: Publish the Strict Schema-16 Star, Action, and Victory Contract

**Files:**
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/scrc/archipelago.json`
- Modify: `apworld/tests/export_client_slot_data.py`
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `apworld/tests/test_repository_contract.py`
- Modify: `tools/validate-repo.py`

**Interfaces:**
- Produces: schema-16 fields `star_victory_schema`, `quest_action_schema`, `star_item_id`, `star_item_max_effective`, `post_threshold_level_22_victory_active`, `star_requirement_mapping_schema`, `star_requirement_digest`, and exact confirmed action metadata.

- [ ] **Step 1: Write field-by-field slot-data tests**

Require this new-contract truth:

```python
data["schema_version"] == 16
data["star_victory_schema"] == 1
data["quest_action_schema"] == 1
data["star_items_active"] is True
data["client_star_gate_enforcement_active"] is True
data["post_threshold_level_22_victory_active"] is True
data["star_item_name"] == "Star"
data["star_item_id"] == 187256118
data["star_item_count"] == 66
data["star_item_max_effective"] == 66
data["required_stars"] in range(1, 67)
len(data["generated_star_requirements"]) == 22
data["star_requirement_mapping_schema"] == 1
len(data["star_requirement_digest"]) == 64
data["confirmed_quest_action_count"] >= 11
```

Assert `active_quest_action_locations` exactly matches Confirmed action names in ID order. Assert the contract contains the exact ordered level number/internal ID/requirement rows used by the digest. Assert old inactive flags cannot coexist with schema 16.

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_world_integration.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_repository_contract.py' -v
```

Expected: RED on schema/version/feature fields.

- [ ] **Step 3: Publish the contract and version**

Set APWorld `world_version` to `0.25.0`, append the implementation suffix, and emit the exact contract above. Retain the current campaign mapping schema and the exact Music Lab Point sub-contract. Export a deterministic client fixture from a real built world, not a hand-maintained approximation.

- [ ] **Step 4: Extend independent validation**

Make the validator independently recompute the Star digest, item count, action count, ID range, active totals, version, and next frontiers. Add mutation tests for every new scalar and for reordering one level or action row.

- [ ] **Step 5: Run and commit**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
py .\tools\validate-repo.py
git add apworld/scrc/__init__.py apworld/scrc/archipelago.json apworld/tests/export_client_slot_data.py apworld/tests/test_world_integration.py apworld/tests/test_repository_contract.py tools/validate-repo.py
git commit -m "feat(apworld): publish Star victory contract"
```

---

### Task 8: Validate the Client Contract and Rebuild Authoritative Star History

**Files:**
- Create: `client/StarProgressionContract.cs`
- Create: `client/StarRandomization.cs`
- Create: `client/StarSessionHistory.cs`
- Create: `client/tests/StarProgression/StarProgression.Tests.csproj`
- Create: `client/tests/StarProgression/Program.cs`
- Create: `client/tests/StarProgression/PacketHistoryTests.cs`
- Modify: `client/CampaignLocationContract.cs`
- Modify: `client/CampaignLevelRandomization.cs`
- Modify: `client/ReceivedItemDispatch.cs`
- Modify: `client/Plugin.cs`

**Interfaces:**
- Produces: `StarProgressionCompatibilityResult`, `StarProgressionSnapshot`, `StarReceipt`, `StarSessionHistory.ApplySlotData()`, `StarSessionHistory.HandlePacket()`, `StarRandomization.SynchronizeHistory()`, and lifecycle clear/retain methods.

- [ ] **Step 1: Write strict contract tests**

Use the exported schema-16 fixture and the real `ConnectedPacket`/`LoginSuccessful` parser. Accept only the exact implementation suffix, schema values, Star name/ID/count/max, goal range, ordered 22-level mapping, matching digest, enabled flags, campaign mapping identity, and confirmed action list.

Mutate each field independently and assert `IncompatibleClaim`, zero effective Stars, disabled gates/actions/Victory, and a detail naming the first failing field. Keep the exact v0.24 implementation as `Legacy`; unknown future claims fail closed.

- [ ] **Step 2: Write real-packet history tests**

Mirror `MusicLabPointSessionHistory`'s proven packet boundary. Test:

- login before index-zero history remains awaiting;
- an index-zero empty history publishes zero;
- 66 unique Star receipts by permanent ID publish 66;
- duplicate callbacks and repeated index-zero replays do not double count;
- unrelated known SCRC item IDs are ignored quietly;
- unknown IDs produce one bounded diagnostic per identity;
- gaps retain the last complete total until a new index-zero replay;
- same identity reconnect retains then corrects from the authoritative replay;
- changed seed/team/slot/game/schema clears immediately; and
- receipts beyond 66 clamp to 66 and log one cap anomaly.

- [ ] **Step 3: Run the new project and verify RED**

```powershell
dotnet run --project .\client\tests\StarProgression\StarProgression.Tests.csproj -c Release
```

Expected: RED because the contract/history classes do not exist.

- [ ] **Step 4: Implement the isolated Star state**

Copy the synchronization semantics, not mutable state, from Music Lab Points. Give each AP session its own `StarSessionHistory` under the existing generation lease. Count by numeric item ID and receipt index. Do not call `ReceivedItemDispatch` to increment Stars from item-name callbacks; history is the sole authority.

Store game, seed, team, slot, schema, generation, readiness, total, goal, and digest in the snapshot. `OnDisconnected(generation)` retains a same-identity synchronized snapshot. `OnIdentityReplaced()` and `Shutdown()` clear it. Add a `ReceivedItemDispatch` Star handler that recognizes the exact Star item name and prevents the experimental native fallback, but never increments the total; packet history remains the sole counter.

- [ ] **Step 5: Wire login and packets under the existing lease**

Construct both Music Lab Point and Star history objects for the session. Feed completed `ReceivedItemsPacket` packets to both while holding the same generation lease. Apply schema-16 slot data only after login succeeds and before marking the connection usable. A Star incompatibility makes the schema-16 connection non-playable and logs one field-specific error.

- [ ] **Step 6: Run regressions and commit**

```powershell
dotnet run --project .\client\tests\StarProgression\StarProgression.Tests.csproj -c Release
dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release
dotnet run --project .\client\tests\ReconnectPolicy\ReconnectPolicy.Tests.csproj -c Release
dotnet run --project .\client\tests\LevelCompletion\LevelCompletion.Tests.csproj -c Release
git add client/StarProgressionContract.cs client/StarRandomization.cs client/StarSessionHistory.cs client/tests/StarProgression client/CampaignLocationContract.cs client/CampaignLevelRandomization.cs client/ReceivedItemDispatch.cs client/Plugin.cs
git commit -m "feat(client): synchronize authoritative AP Stars"
```

Expected: all four projects GREEN.

---

### Task 9: Drive Campaign HUD and Physical Gates from One Policy

**Files:**
- Create: `client/StarGatePolicy.cs`
- Create: `client/tests/StarGate/StarGate.Tests.csproj`
- Create: `client/tests/StarGate/Program.cs`
- Modify: `client/StarHudPolicy.cs`
- Modify: `client/tests/StarHud/Program.cs`
- Modify: `client/Plugin.cs`

**Interfaces:**
- Produces: `StarGateDecision`, `StarGatePolicy.Decide(snapshot, internalLevel)`, and native adapters for the exact Task 1 gate/HUD boundaries.

- [ ] **Step 1: Write pure policy tests**

For every mapped level, require one decision containing current total, required total, display value, admission result, and reason. Cover totals immediately below/at requirement and lifecycle modes Non-AP, Legacy, Awaiting, Synchronized, RetainedDisconnected, and Incompatible.

Require:

- Non-AP and Legacy return native behavior;
- Awaiting and Incompatible schema-16 states deny only mapped AP Star gates;
- Synchronized and retained same-identity states compare AP total to the generated requirement;
- unknown levels fail closed for schema 16;
- Music Lab retains `PreserveMusicLabPoints`; and
- other rooms do not receive the campaign Star HUD.

- [ ] **Step 2: Write production-wiring source tests**

Assert the exact native display and interaction hooks proven in Task 1 both call `StarGatePolicy.Decide`. Assert neither branch contains an independent numeric comparison. Assert the relevant production slice contains none of: native earned-Star setters, save writes for Stars, unconditional collider disabling, developer override totals, or per-frame logging.

- [ ] **Step 3: Run focused tests and verify RED**

```powershell
dotnet run --project .\client\tests\StarGate\StarGate.Tests.csproj -c Release
dotnet run --project .\client\tests\StarHud\StarHud.Tests.csproj -c Release
```

Expected: RED because the central gate decision and native wiring do not exist.

- [ ] **Step 4: Implement the adapters**

Patch only the exact managed methods/objects proven in Task 1. At evaluation time, resolve the internal level identity, read one immutable `StarProgressionSnapshot`, call the policy once, and use that decision for both displayed requirement and interaction admission. Preserve every native non-Star prerequisite beneath the AP decision.

Substitute current AP Stars only in campaign HUD contexts. Keep `GameRoom_Hub6` on the Music Lab Point path and preserve native behavior in Game Garage, levels, title/menu, and recognized old/non-AP sessions.

- [ ] **Step 5: Run and commit**

```powershell
dotnet run --project .\client\tests\StarGate\StarGate.Tests.csproj -c Release
dotnet run --project .\client\tests\StarHud\StarHud.Tests.csproj -c Release
dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release
dotnet run --project .\client\tests\BottomHudDiagnostic\BottomHudDiagnostic.Tests.csproj -c Release
git add client/StarGatePolicy.cs client/tests/StarGate client/StarHudPolicy.cs client/tests/StarHud/Program.cs client/Plugin.cs
git commit -m "feat(client): enforce synchronized campaign Star gates"
```

---

### Task 10: Send Confirmed Quest Actions with Reconciliation

**Files:**
- Create: `client/QuestActionCatalog.cs`
- Create: `client/QuestActionRandomization.cs`
- Modify: `client/tests/QuestActions/QuestActions.Tests.csproj`
- Modify: `client/tests/QuestActions/Program.cs`
- Modify: `client/Plugin.cs`

**Interfaces:**
- Produces: `QuestActionDefinition`, `QuestActionCatalog.All`, `TryResolveTransition()`, `ReconcileNativeState()`, and schema-bound lifecycle methods.

- [ ] **Step 1: Write registry parity tests**

Assert the client catalog exactly matches the exported confirmed action rows: canonical name, native identity, area/room scope, transition semantics, and reconciliation mode. Unknown flags, wrong rooms, false values, and already-true-to-true notifications must not resolve.

Assert each valid false-to-true event queues exactly one canonical location. Repeated events, reload reconciliation, reconnect replay, and server checked-location duplication remain harmless through the existing identity-bound queue.

- [ ] **Step 2: Write fail-closed and source-wiring tests**

Assert schema-16 incompatibility disables all action sends. Assert v0.24/old seeds do not enable this registry. Assert `ProgressionPatches.ProgressionFlagEventPostfix` passes the real event, normalized flag identity, boolean transition members, and room identity into `QuestActionRandomization` before generic diagnostics.

Assert reconciliation reads only the exact durable flags marked Confirmed and never submits or clears them. No action code may call native grant methods or suppress vanilla rewards.

- [ ] **Step 3: Run focused tests and verify RED**

```powershell
dotnet run --project .\client\tests\QuestActions\QuestActions.Tests.csproj -c Release
```

Expected: RED because the production registry is absent.

- [ ] **Step 4: Implement event and reconciliation paths**

On compatible login, bind the confirmed action registry to the authenticated identity. On each native event, require the exact expected transition and room/area scope, then call the existing `QueueLocation(canonicalName)`. On save readiness and reconnect, read Confirmed durable flags and queue any already-completed actions; rely on server checked-location state and `_queuedOrSent` for idempotency.

Bound logs to one contract message, one first observation per action/identity, one reconciliation result per action/identity, and queue/retry transitions. Never log credentials or poll every frame.

- [ ] **Step 5: Run and commit**

```powershell
dotnet run --project .\client\tests\QuestActions\QuestActions.Tests.csproj -c Release
dotnet run --project .\client\tests\LevelCompletion\LevelCompletion.Tests.csproj -c Release
dotnet run --project .\client\tests\RootsBucketRandomization\RootsBucketRandomization.Tests.csproj -c Release
git add client/QuestActionCatalog.cs client/QuestActionRandomization.cs client/tests/QuestActions client/Plugin.cs
git commit -m "feat(client): send confirmed quest action checks"
```

---

### Task 11: Send Victory Only from a Qualifying Level 22 Clear

**Files:**
- Create: `client/Level22VictoryPolicy.cs`
- Create: `client/tests/Level22Victory/Level22Victory.Tests.csproj`
- Create: `client/tests/Level22Victory/Program.cs`
- Modify: `client/LevelCompletionPolicy.cs`
- Modify: `client/tests/LevelCompletion/Program.cs`
- Modify: `client/Plugin.cs`

**Interfaces:**
- Produces: `Level22VictoryDecision`, identity-bound `QueueGoalStatus()`, pending offline goal state, and `StatusUpdatePacket { Status = ClientStatus.ClientGoal }` delivery.

- [ ] **Step 1: Write the non-retroactive policy matrix**

Test these exact cases:

| Event | Star snapshot | Expected |
| --- | ---: | --- |
| Failed/non-default/non-Level-22 result | any | no goal |
| Successful normal Level 22 | goal minus one | ordinary locations only |
| Later receipt reaches goal | goal | no goal |
| Successful normal Level 22 replay | goal | goal status |
| Offline clear with last synchronized total below goal | below | no pending goal |
| Offline clear with last synchronized total at goal | at goal | pending goal |
| Identity replacement before retry | any | discard pending goal |
| Same-identity reconnect | qualifying snapshot | send once |

Assert `LevelCompletionPolicy` continues to return only ordinary addressed locations and never returns `Victory`.

- [ ] **Step 2: Write ordering and production-wiring tests**

Extract the persisted-result production slice and assert it:

1. queues ordinary campaign locations first;
2. snapshots `StarRandomization` only for a verified successful normal `Level_28` result;
3. calls `Level22VictoryPolicy` after ordinary checks;
4. never calls Victory evaluation from received-item dispatch/history; and
5. sends Archipelago `ClientGoal` status under the current authenticated generation lease.

- [ ] **Step 3: Run focused tests and verify RED**

```powershell
dotnet run --project .\client\tests\Level22Victory\Level22Victory.Tests.csproj -c Release
dotnet run --project .\client\tests\LevelCompletion\LevelCompletion.Tests.csproj -c Release
```

Expected: RED because no goal policy or status queue exists.

- [ ] **Step 4: Implement goal delivery**

Represent a pending goal with the complete authenticated identity and the Star total captured at the result event. Never recompute qualification when flushing. Send `StatusUpdatePacket` only after acquiring the same current session generation lease; clear it after success. Keep a per-identity sent flag so repeated winning clears or reconnects do not spam status.

Log one bounded line for early non-winning clear, qualifying clear, queued offline goal, delivered goal, and discarded stale identity. Star receipt callbacks must contain no path to `QueueGoalStatus()`.

- [ ] **Step 5: Run and commit**

```powershell
dotnet run --project .\client\tests\Level22Victory\Level22Victory.Tests.csproj -c Release
dotnet run --project .\client\tests\LevelCompletion\LevelCompletion.Tests.csproj -c Release
dotnet run --project .\client\tests\StarProgression\StarProgression.Tests.csproj -c Release
dotnet run --project .\client\tests\ReconnectPolicy\ReconnectPolicy.Tests.csproj -c Release
git add client/Level22VictoryPolicy.cs client/tests/Level22Victory client/LevelCompletionPolicy.cs client/tests/LevelCompletion/Program.cs client/Plugin.cs
git commit -m "feat(client): require qualifying Level 22 clear for Victory"
```

---

### Task 12: Run Broad Solver, Compatibility, and Client Regression Matrices

**Files:**
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `apworld/tests/test_repository_contract.py`

- [ ] **Step 1: Add the generation matrix**

For every AP difficulty, goals `1`, `25`, `50`, and `66`, and at least 100 deterministic world seeds, build regions/items/rules and collect solver spheres. Assert exact pool balance, nonempty opening sphere, all required progression collectible, every Star gate structurally satisfiable, Level 22 reachable below the goal, Royal Star Eater optional, and Victory reachable only with the goal count.

Add explicit impossible fixtures: ten confirmed actions, a cyclic action graph, a Star threshold equal to the goal, a gate with fewer prior progression-safe locations than required, duplicate action IDs, and a mismatched digest. Each must fail generation with a precise error.

- [ ] **Step 2: Run all APWorld tests**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
```

Expected: GREEN. Do not weaken the matrix, lower the Star count, or mark unsafe locations safe to resolve a failure.

- [ ] **Step 3: Run every client test project**

```powershell
$projects = Get-ChildItem .\client\tests -Recurse -Filter *.csproj | Sort-Object FullName
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { throw "Client test failed: $($project.FullName)" }
}
```

Expected: every discovered project GREEN. Fix the first root cause and restart the loop from the beginning.

- [ ] **Step 4: Run independent validation**

```powershell
py .\tools\validate-repo.py
```

Expected: repository validation passes with APWorld 0.25.0, client still at the pre-version-bump value, schema 16, 66 active Stars, at least 11 confirmed actions, the exact allocated action range, recomputed digest, balanced totals, and unchanged historical IDs/economies.

- [ ] **Step 5: Commit matrix hardening**

```powershell
git add apworld/tests/test_world_integration.py apworld/tests/test_repository_contract.py
git commit -m "test: verify Star milestone generation and lifecycle matrices"
```

Review the staged diff before committing so generated `bin/`, `obj/`, logs, packages, game files, and unrelated changes are absent.

---

### Task 13: Version, Document, Build, and Package the Candidate

**Files:**
- Modify: `client/Plugin.cs`
- Modify: `client/build.ps1`
- Modify: `apworld/scrc/archipelago.json`
- Modify: `tools/validate-repo.py`
- Modify: `apworld/tests/test_repository_contract.py`
- Modify: `docs/PROJECT_OVERVIEW.md`
- Modify: `docs/PROGRESSION.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/IDS.md`
- Modify: `README.md`
- Modify: `apworld/README.md`
- Modify: `apworld/scrc/docs/setup_en.md`
- Modify: `apworld/scrc/docs/en_Super Crazy Rhythm Castle.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/testing/2026-09-11-star-victory-and-quest-capacity-acceptance.md`

- [ ] **Step 1: Advance candidate versions last**

Set client version strings to `0.71.0`, APWorld world version to `0.25.0`, schema to 16, and the full implementation suffix ending `star-victory-quest-capacity-0.25`. Update independent validator expectations only after production strings are changed.

- [ ] **Step 2: Update living and public documentation truthfully**

Document:

- exact confirmed action names and IDs;
- exact location totals by difficulty and Stardust count;
- 66 individual Stars and recommended `required_stars: 50`;
- deterministic per-seed gates and AP-only Star counting;
- Music Lab Point economy unchanged;
- Level 22 playable early and Victory requiring a later qualifying clear;
- forced Roots starter retained;
- Bee/Devil checks deferred but intended for Normal later;
- manual acceptance still pending; and
- fresh v0.25 seed and fresh save required.

Do not claim a functional full playthrough until Task 15 passes.

- [ ] **Step 3: Run complete automated verification**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
$projects = Get-ChildItem .\client\tests -Recurse -Filter *.csproj | Sort-Object FullName
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { throw "Client test failed: $($project.FullName)" }
}
py .\tools\validate-repo.py
```

Expected: all APWorld and client tests GREEN and repository validation passes.

- [ ] **Step 4: Build without deployment**

```powershell
Set-Location .\client
.\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall
Set-Location ..
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\build-apworld.ps1 -OutputDir .\dist
```

Expected: client Release build has zero errors, no files are copied into the game, and `dist\scrc.apworld` is created. Record artifact SHA-256 values in the acceptance record. Do not commit built artifacts unless current repository release policy explicitly tracks them.

- [ ] **Step 5: Inspect repository state and commit**

```powershell
git status --short
git diff --check
git add client/Plugin.cs client/build.ps1 apworld/scrc/archipelago.json tools/validate-repo.py apworld/tests/test_repository_contract.py docs/PROJECT_OVERVIEW.md docs/PROGRESSION.md docs/TESTING.md docs/ROADMAP.md docs/IDS.md README.md apworld/README.md apworld/scrc/docs CHANGELOG.md docs/testing/2026-09-11-star-victory-and-quest-capacity-acceptance.md
git commit -m "docs: prepare Star victory testing candidate"
```

Expected: only source/docs changes are staged; worktree is clean after commit. Stop before deployment, merge, push, or release.

---

### Task 14: Perform Targeted Live Acceptance

**Files:**
- Modify: `docs/testing/2026-09-11-star-victory-and-quest-capacity-acceptance.md`

- [ ] **Step 1: Request explicit deployment approval**

Present the exact client DLL and APWorld package hashes and the only deployment targets. Do not install or launch until the user approves. After approval, install only the client DLL/config under `BepInEx\plugins\RhythmCastleAP` and the APWorld in the user's selected Archipelago custom-world location.

- [ ] **Step 2: Start the server before entering the fresh save**

Generate a fresh v0.25 seed at recommended goal 50, start the Archipelago server, verify it is listening, then launch the game once. Verify `BepInEx\LogOutput.log` reports client v0.71.0, accepted schema 16, the expected seed/team/slot identity, and initial Star synchronization. Manually select the agreed fresh save slot before Continue.

- [ ] **Step 3: Test synchronization and UI boundaries**

Confirm campaign hubs show AP Stars, Music Lab shows Music Lab Points, and Game Garage/levels preserve their expected HUD. Use server-sent Stars to test representative gates immediately below and at their requirements. Confirm displayed requirement and physical admission always agree, including reconnect while below and at a threshold.

- [ ] **Step 4: Test representative confirmed actions**

Exercise at least one Confirmed action in Roots, one in a middle area, and one in a late area. Confirm each queues its exact AP location once, preserves its vanilla reward/quest transition, survives reload, and reconciles after one action performed during a temporary disconnect.

- [ ] **Step 5: Test the complete Victory sequence**

Using server-sent setup items only for this targeted test:

1. enter and complete normal Level 22 below 50 AP Stars;
2. confirm ordinary Level 22 checks send and no goal status sends;
3. receive enough Stars to reach 50;
4. confirm the receipt alone does not win;
5. complete normal Level 22 again; and
6. confirm goal status sends exactly once.

Repeat the qualifying clear once while disconnected with a last synchronized total at goal, then reconnect and confirm one queued goal delivery. Also test a disconnected clear whose retained total is below goal and confirm later receipts do not retroactively qualify it.

- [ ] **Step 6: Record the targeted verdict**

Record exact seed, save slot, client/APWorld hashes, thresholds tested, action locations, Level 22 results, log excerpts, failures, and user verdict. If any gate, action, identity, or Victory behavior fails, return to the relevant test-first task and do not proceed to a full playthrough.

- [ ] **Step 7: Commit evidence only after review**

```powershell
git add docs/testing/2026-09-11-star-victory-and-quest-capacity-acceptance.md
git commit -m "test: record targeted Star victory acceptance"
```

---

### Task 15: Complete the Fresh Normal-Seed Playthrough

**Files:**
- Modify: `docs/testing/2026-09-11-star-victory-and-quest-capacity-acceptance.md`
- Modify after acceptance: `docs/PROJECT_OVERVIEW.md`
- Modify after acceptance: `docs/TESTING.md`
- Modify after acceptance: `README.md`
- Modify after acceptance: `CHANGELOG.md`

- [ ] **Step 1: Generate the acceptance run**

Generate a new Normal v0.25 seed with `required_stars: 50`, use a fresh native save, start the server before loading the save, and use no manually sent progression. Record the generation seed, AP slot identity, save slot, options, hashes, and start time.

- [ ] **Step 2: Play the seed naturally**

Exercise Area Access, meaningful items, confirmed quest actions, cassettes/cartridges, Music Lab Points, performance checks, and all generated Star gates. Record each progression sphere and any time the game offers no reachable unchecked location. A progression block is a release blocker even if a console command can bypass it.

- [ ] **Step 3: Prove the approved ending**

Complete Level 22 below the goal and confirm no win. Continue the seed until the 50th Star is received naturally, confirm no automatic win, then replay and complete Level 22 to send goal status. Verify save, quit, reconnect, and resume behavior during the run.

- [ ] **Step 4: Record the final verdict**

Mark the milestone accepted only if the entire run completes without manual progression and every required identity/reconnect/Victory behavior passes. Update public docs from “candidate/manual acceptance pending” to the exact accepted status. Keep any unplayed Confirmed action or unsupported co-op case explicitly pending.

- [ ] **Step 5: Run final verification and commit acceptance**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
$projects = Get-ChildItem .\client\tests -Recurse -Filter *.csproj | Sort-Object FullName
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { throw "Client test failed: $($project.FullName)" }
}
py .\tools\validate-repo.py
git diff --check
git add docs/testing/2026-09-11-star-victory-and-quest-capacity-acceptance.md docs/PROJECT_OVERVIEW.md docs/TESTING.md README.md CHANGELOG.md
git commit -m "test: accept first Star-gated playthrough"
```

Expected: all verification GREEN and the evidence record contains the user's explicit gameplay verdict. Stop before merge, push, deployment refresh, or release publication; those require separate approval and the `superpowers:finishing-a-development-branch` workflow.

---

## Execution Checkpoints

1. **Evidence gate:** stop if fewer than 11 action checks are Confirmed.
2. **ID gate:** allocate only after evidence, contiguously from `187256292` in fixed ledger order.
3. **Automated gate:** all APWorld/client tests, validator, package build, and non-deploy client build pass.
4. **Deployment gate:** exact hashes and targets receive explicit user approval.
5. **Targeted gameplay gate:** Star sync, display/enforcement parity, action reconciliation, and replay-only Victory pass.
6. **Full-playthrough gate:** a fresh Normal seed completes without manually sent progression.
7. **Integration gate:** merge, push, release, and worktree removal remain separate explicit decisions.
