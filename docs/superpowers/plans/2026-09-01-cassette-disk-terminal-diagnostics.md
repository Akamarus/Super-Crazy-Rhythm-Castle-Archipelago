# Cassette Disk Terminal Diagnostics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add bounded, behavior-neutral evidence for cassette disk-commit terminal failures, late completion events, and save-state rebuild boundaries.

**Architecture:** Extend the pure disk-commit runtime with diagnostic failure metadata and a last-terminal-attempt tombstone that cannot affect transaction state. Keep all persistence predicates unchanged, derive precise diagnostic causes from the same control observations, and use public-only reflection to capture coherent save-state snapshots. Wire an exact BuildPlayerSaveStateFromFileRequest prefix/postfix pair to emit at most one lifecycle before/after pair for the relevant terminal attempt.

**Tech Stack:** C#/.NET 6, Harmony, IL2CPP public reflection, existing console regression project.

**Spec:** `.superpowers/sdd/2026-08-30-cassette-save-transaction/final-boundary-implementation-report.md`

## Global Constraints

- Diagnostic-only: do not change submission, completion, retry, recovery, timeout, identity, or status predicates.
- Use public reflection contracts only for new native reads.
- Emit bounded records and never install or deploy the client.
- Preserve the exact DEFAULT/URGENT public persistence path.

---

### Task 1: Focused diagnostic contracts

**Files:**
- Modify: `client/tests/CassetteRandomization/Program.cs`
- Modify: `client/CassetteRandomizationPolicy.cs`

**Interfaces:**
- Produces: terminal failure-kind records, one-claim FAILURE diagnostics, a last-attempt tombstone, one late-event claim, and one lifecycle before/after claim pair.

- [x] Add focused assertions for each required failure kind and exact detail.
- [x] Run the cassette suite and confirm compilation/assertion failure for the missing API.
- [x] Add the minimal runtime-only diagnostic state and preserve existing outcomes.
- [x] Run the focused suite and confirm all runtime assertions pass.

### Task 2: Public-only lifecycle snapshot adapter

**Files:**
- Modify: `client/tests/CassetteRandomization/Program.cs`
- Modify: `client/CassetteSaveTransactionAdapter.cs`

**Interfaces:**
- Produces: `TryReadPublicWriteLifecycleDiagnosticState` from one public processor state object, without an expected-pointer rejection.

- [x] Add a failing adapter test for pointer, redundancy state, nullable times, and same-object song statuses.
- [x] Run the focused suite and confirm failure for the missing lifecycle adapter.
- [x] Share the existing public diagnostic-state reader while retaining exact stages and existing pointer validation.
- [x] Run the focused suite and confirm adapter and private-member guards pass.

### Task 3: Production diagnostic wiring

**Files:**
- Modify: `client/tests/CassetteRandomization/Program.cs`
- Modify: `client/Plugin.cs`

**Interfaces:**
- Consumes: runtime tombstone/failure metadata and lifecycle adapter.
- Produces: bounded FAILURE, LATE_EVENT, and SAVE LIFECYCLE BEFORE/AFTER records.

- [x] Add source-wiring assertions that fail before production changes.
- [x] Preserve exact predicate evaluation while capturing identity/status stages.
- [x] Log terminal FAILURE from preserved context, decode late-event headers without invoking the observer, and install the exact Build prefix/postfix pair.
- [x] Run the focused suite and inspect the diff for persistence or recovery changes.

### Task 4: Verification, review, and report

**Files:**
- Modify: `.superpowers/sdd/2026-08-30-cassette-save-transaction/final-boundary-implementation-report.md`

**Interfaces:**
- Produces: review evidence, full verification evidence, and one diagnostic-only commit.

- [x] Request an independent review and resolve every blocking issue.
- [x] Run the focused suite, all 17 client test projects, Release no-install build, repository validator, and `git diff --check`.
- [x] Append exact RED/GREEN and behavior-neutral evidence to the implementation report.
- [x] Review the final diff and commit only intended files.
