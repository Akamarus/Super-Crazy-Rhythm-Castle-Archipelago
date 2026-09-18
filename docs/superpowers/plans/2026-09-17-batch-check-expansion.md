# Batch Check Expansion Implementation Plan

> Execution: subagent-driven-development with independent native audit, APWorld task, client runtime task, integration and final review. Existing isolated worktree retained; no commit, deployment or publishing in this plan.

Goal: implement the largest evidence-backed subset of the 50-entry backlog as one client/APWorld candidate, followed by automated verification and one coordinated gameplay acceptance run.

Spec: docs/testing/2026-09-17-check-expansion-backlog.md and the user's instruction to batch implementation before large-scale testing.

Architecture: append a strictly validated schema-18 supplemental check catalog, with immutable permanent IDs from 187256298 onward (reserved prior IDs unchanged). Client0.74.0 / world0.27.0. Preserve schema17 quest contract and native character/Garage repairs. Existing native event observation supplies one-time checks; a separate identity/native-slot-bound atomic journal recovers offline and reload completions. No new unsafe native detours. Native quest rewards remain available for passive action checks unless a separately verified randomization path can replace them safely. Item randomization is not inferred from adding an action check.

Ruling: user has authorized the broader implementation and batching; no per-check approval or gameplay pause. Native mapping and solver uncertainty remain reasons to defer particular entries or restrict placement, not reasons to stop all work. Unverified reachability cannot hold required progression. Do not activate AP Stars merely because raw location count grows.

## Task 1 — Freeze evidence-backed catalog
- Review all50 entries against native metadata/assets and recorded gameplay.
- Resolve exact room, durable source flag, known prerequisites and source/reward aliasing.
- Produce a catalog and explicit deferred reasons. No guessed flag/ID reuse.

## Task 2 — APWorld catalog and integration
- Tests first: catalog unique names/IDs/flags, all difficulty location totals, preserved71-item pool, exact schema18 supplemental map and filler restrictions.
- Add expanded_checks.py and integrate region locations and slot data. Safe conversion check uses native required items and existing route events; uncertain sources remain filler-only.
- Test generation and reachability across all difficulties; report actual progression-safe capacity separately from raw count.

## Task 3 — Native client pipeline
- Tests first: wrongroom/duplicate events, oldseed compatibility, malformed claims, native-slot isolation, firstbind completed markers, atomic journal failures, restart/reconnect.
- Add ExpandedCheckCatalog.cs, ExpandedChecksPolicy.cs, ExpandedChecksJournal.cs and ExpandedChecks.cs.
- Persist completion before queueing; no replay until selected save is bound; no mutation of native source flags/rewards. Reconcile durable markers at a bounded interval, skipping completed entries.

## Task 4 — Compatibility and lifecycle integration
- Extend CampaignLocationContract, CharacterQuestItemsPolicy, MusicLabPointPolicy and QuestChecksPolicy to accept exact schema18 suffix while preserving old contracts and rejecting wrong schema numbers.
- Plugin validates supplemental map before enabling features, configures with authenticated seed/team/slot and current generation, resets on explicit disconnect, and calls event/prewrite/tick observation paths.
- If six special completions have reliable markers or persisted result observations, register them independently of normal campaign/star-tier checks. Otherwise document concrete blockers.

## Task 5 — Verification, review, package and grouped test
- Run all client test projects, APWorld tests and repository validation; build without installation.
- Review runtime safety, native mapping, aliases, schema compatibility and progression capacity. Repair concrete findings.
- Update IDs, progression, overview and backlog statuses, preserving historical release statements.
- Package one candidate plus one area-ordered gameplay checklist with received-item setup and expected source/reward outcomes. Deployment requires approval of this concrete candidate per AGENTS.md.

## Coordination ledger
- Native audit writes only task-workspace evidence.
- Client worker owns new ExpandedChecks* engine/tests, root owns catalog and Plugin/compatibility adapters.
- World worker owns apworld source/tests after catalog frozen; root owns shared version/docs/validator integration.
- Shared interface: ExpandedCheckEntry(long Id,string Name,string Room,string Flag), ExpandedCheckCatalog.Entries.
- Tasks2/3 share only frozen catalog; Task4 consumes runtime API; Task5 consumes final candidate. No overlapping writers.
- Initial review: passive checks cannot be claimed as randomized quest items; broad candidate list does not override missing durable source identity; 14-location arithmetic is only raw capacity and excludes further progression-placement limitations.
