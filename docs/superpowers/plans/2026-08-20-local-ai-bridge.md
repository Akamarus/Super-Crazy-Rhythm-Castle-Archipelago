# Local AI Bridge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a controlled PowerShell bridge that invokes the local `jacks-assistant` model through Open WebUI, isolates implementation work, and produces auditable human-review handoffs.

**Architecture:** A PowerShell module is the sole execution and policy layer. It calls the loopback Open WebUI chat-completions API, persists structured local task state, confines implementation edits to Git worktrees, and exposes only named validation, build, deployment, log-watching, and review operations.

**Tech Stack:** PowerShell 7+, Pester 5, Git, Open WebUI HTTP API, existing Python validator, .NET 6 client build.

**Spec:** `docs/superpowers/specs/2026-08-20-local-ai-bridge-design.md`

## Global Constraints

- Use `http://127.0.0.1:8080` and `POST /api/chat/completions`; reject non-loopback API endpoints.
- Use model ID `jacks-assistant` exactly.
- Read the bearer token only from `OPENWEBUI_API_KEY`; never persist or print it.
- PowerShell is the sole execution layer; there is no Open Terminal dependency and no general-purpose command runner.
- All implementation edits occur in isolated AI worktrees; the main checkout is not modified by implementation tasks.
- Log watching exists only during an explicit development session and watches only `<GameDir>\BepInEx\LogOutput.log`.
- Client deployment is restricted to exactly `<GameDir>\BepInEx\plugins\RhythmCastleAP`.
- Never merge, rebase, push, open a merge request, or automatically remove branches/worktrees.
- The first end-to-end acceptance test is investigation-only for Level 4 glasses / Minim progression and must not change source or runtime state.
- Keep credentials, task state, prompts/responses, logs, binaries, and generated artifacts out of Git.

---

## Planned file structure

| File | Responsibility |
| --- | --- |
| `tools/local-ai/LocalAiBridge.psd1` | Module metadata and exports. |
| `tools/local-ai/LocalAiBridge.psm1` | Dot-sources private/public scripts. |
| `tools/local-ai/config.example.psd1` | Safe, non-secret example configuration. |
| `tools/local-ai/Private/Config.ps1` | Load and validate runtime settings. |
| `tools/local-ai/Private/Policy.ps1` | Resolve and enforce repo, worktree, log, and deployment paths. |
| `tools/local-ai/Private/TaskState.ps1` | Task records, transitions, events, and atomic writes. |
| `tools/local-ai/Private/OpenWebUi.ps1` | Authenticated HTTP request and response validation. |
| `tools/local-ai/Private/Context.ps1` | Bounded context selection and redaction. |
| `tools/local-ai/Private/Worktree.ps1` | Create and verify isolated worktrees. |
| `tools/local-ai/Private/Build.ps1` | Named validator/build/package/deploy adapters. |
| `tools/local-ai/Private/LogWatcher.ps1` | Session-bound watcher lifecycle. |
| `tools/local-ai/Private/Handoff.ps1` | Review manifest and Markdown handoff. |
| `tools/local-ai/Public/*.ps1` | Supported user commands. |
| `tools/local-ai/tests/*.Tests.ps1` | Pester tests grouped by responsibility. |
| `tools/local-ai/README.md` | Setup, workflow, security, and recovery guide. |
| `.gitignore` | Ignore `.local-ai/` and local bridge config. |
| `client/build.ps1` | Add an explicitly non-deploying build path without breaking current usage. |
| `docs/WORKFLOW.md` | Document isolated AI task review workflow. |
| `docs/TESTING.md` | Document bridge validation and runtime log workflow. |
| `AGENTS.md` | State repository-specific AI safety and documentation rules. |

### Task 1: Module skeleton and validated configuration

**Files:**
- Create: `tools/local-ai/LocalAiBridge.psd1`
- Create: `tools/local-ai/LocalAiBridge.psm1`
- Create: `tools/local-ai/config.example.psd1`
- Create: `tools/local-ai/Private/Config.ps1`
- Create: `tools/local-ai/tests/Config.Tests.ps1`
- Modify: `.gitignore`

**Interfaces:**
- Produces: `Get-LocalAiConfiguration -RepositoryRoot <string> [-ConfigPath <string>] -> PSCustomObject` with `RepositoryRoot`, `OpenWebUiBaseUri`, `ModelId`, `StateRoot`, `WorktreeRoot`, and optional `GameDir`.
- Produces: `Assert-LoopbackUri -Uri <uri> -> uri`.

- [ ] **Step 1: Write configuration tests** covering safe defaults, explicit local overrides, missing `OPENWEBUI_API_KEY` only when an API call is requested, rejection of non-loopback HTTP hosts, exact `jacks-assistant` default, absolute normalized paths, and absence of secret values in the returned object.
- [ ] **Step 2: Run `Invoke-Pester .\tools\local-ai\tests\Config.Tests.ps1 -Output Detailed`; expect failures because the module and functions do not exist.**
- [ ] **Step 3: Create the module manifest/loader and minimal configuration implementation.** Use a data-file parser for `.psd1`, accept only known keys, resolve relative state paths beneath the repository, and reject a state root that resolves outside the repository. Add `.local-ai/` and `tools/local-ai/config.local.psd1` to `.gitignore`.
- [ ] **Step 4: Run the configuration tests; expect all tests to pass.**
- [ ] **Step 5: Commit with `git add .gitignore tools/local-ai` and `git commit -m "feat(tools): scaffold local AI bridge configuration"`.**

### Task 2: Path and operation policy

**Files:**
- Create: `tools/local-ai/Private/Policy.ps1`
- Create: `tools/local-ai/tests/Policy.Tests.ps1`

**Interfaces:**
- Consumes: normalized configuration from Task 1.
- Produces: `Resolve-ContainedPath -Root <string> -Path <string> [-MustExist] -> string`.
- Produces: `Resolve-PluginDestination -GameDir <string> -> string`.
- Produces: `Resolve-GameLogPath -GameDir <string> -> string`.
- Produces: `Assert-TaskOperation -Mode <investigation|implementation> -Operation <string>`.

- [ ] **Step 1: Write policy tests** for normal children, `..` traversal, sibling-prefix attacks, wildcard input, missing roots, reparse-point escape, exact plugin destination, rejection of `BepInEx\core` and sibling plugins, exact log path, and investigation rejection of edit/build/deploy operations.
- [ ] **Step 2: Run the policy test file; expect failures for undefined functions.**
- [ ] **Step 3: Implement canonical path comparison with Windows case-insensitive semantics and separator-boundary checks.** Reject wildcard and unresolved inputs before any write; inspect existing ancestors for reparse points; derive deployment and log paths internally rather than accepting destinations from callers.
- [ ] **Step 4: Run `Config.Tests.ps1` and `Policy.Tests.ps1`; expect all tests to pass.**
- [ ] **Step 5: Commit with message `feat(tools): enforce local AI bridge path policy`.**

### Task 3: Atomic structured task state

**Files:**
- Create: `tools/local-ai/Private/TaskState.ps1`
- Create: `tools/local-ai/Public/New-LocalAiTask.ps1`
- Create: `tools/local-ai/Public/Get-LocalAiTask.ps1`
- Create: `tools/local-ai/tests/TaskState.Tests.ps1`

**Interfaces:**
- Consumes: configuration and operation policy.
- Produces: `New-LocalAiTask -Goal <string> -Mode <investigation|implementation> -RepositoryRoot <string> -> PSCustomObject`.
- Produces: `Get-LocalAiTask -TaskId <string> -> PSCustomObject`.
- Produces: `Set-LocalAiTaskState -Task <object> -State <string> [-Data <hashtable>] -> PSCustomObject`.
- Produces: `Add-LocalAiTaskEvent -Task <object> -Operation <string> -Data <hashtable>`.

- [ ] **Step 1: Write tests** asserting ID/slug creation, baseline commit capture, the exact state graph from the spec, rejection of skipped/terminal transitions, investigation mode restrictions, JSON schema fields, append-only JSON Lines events, secret redaction, and atomic replacement behavior under a simulated write failure.
- [ ] **Step 2: Run the task-state tests; expect failures for missing commands.**
- [ ] **Step 3: Implement task directories and atomic JSON persistence.** Write a temporary file beside `task.json`, flush it, replace the destination, retain the last valid file on failure, and serialize timestamps in UTC ISO 8601 form.
- [ ] **Step 4: Run Config, Policy, and TaskState tests; expect all tests to pass.**
- [ ] **Step 5: Commit with message `feat(tools): add auditable local AI task state`.**

### Task 4: Open WebUI client with redaction

**Files:**
- Create: `tools/local-ai/Private/OpenWebUi.ps1`
- Create: `tools/local-ai/tests/OpenWebUi.Tests.ps1`

**Interfaces:**
- Consumes: validated loopback URI, `OPENWEBUI_API_KEY`, task event writer.
- Produces: `Invoke-OpenWebUiChat -Configuration <object> -Messages <object[]> [-TimeoutSec <int>] -> PSCustomObject` containing validated assistant content and non-sensitive response metadata.

- [ ] **Step 1: Write mocked HTTP tests** asserting `POST` to `/api/chat/completions`, bearer header use, `model = 'jacks-assistant'`, JSON messages, timeout behavior, missing-key failure, non-2xx failure, malformed/missing `choices[0].message.content` failure, and no token leakage in errors or task events.
- [ ] **Step 2: Run the OpenWebUi tests; expect failures for the missing client.**
- [ ] **Step 3: Implement the single HTTP adapter.** Build headers only immediately before the request, never return them, validate response shape, and normalize error messages without echoing request headers or secret-bearing exception details.
- [ ] **Step 4: Run all existing Pester tests; expect all tests to pass.**
- [ ] **Step 5: Commit with message `feat(tools): add secured Open WebUI client`.**

### Task 5: Bounded context and investigation contract

**Files:**
- Create: `tools/local-ai/Private/Context.ps1`
- Create: `tools/local-ai/Public/Invoke-LocalAiInvestigation.ps1`
- Create: `tools/local-ai/tests/Context.Tests.ps1`
- Create: `tools/local-ai/tests/Investigation.Tests.ps1`

**Interfaces:**
- Consumes: task state and `Invoke-OpenWebUiChat`.
- Produces: `New-LocalAiContext -Task <object> -IncludePath <string[]> [-MaxBytes <int>] -> PSCustomObject`.
- Produces: `Invoke-LocalAiInvestigation -TaskId <string> -IncludePath <string[]> -> PSCustomObject` with `summary`, `findings[]`, `evidence[]`, `uncertainties[]`, and `recommended_next_steps[]`.

- [ ] **Step 1: Write context tests** for tracked UTF-8 text inclusion; deterministic ordering; byte limit; exclusion of `.git`, `.local-ai`, logs, binaries, archives, build outputs, local config/secrets, and outside-root paths; and redaction of values matching known secret inputs.
- [ ] **Step 2: Write investigation tests** with a mocked valid response and malformed JSON, verifying transitions `created -> context_ready -> awaiting_model -> model_complete -> awaiting_review` and `failed` on invalid output, with no worktree/build/deploy fields or side effects.
- [ ] **Step 3: Run both test files; expect failures for missing functions.**
- [ ] **Step 4: Implement deterministic context assembly and a JSON-only investigation prompt contract.** Treat model output as data, parse it with `ConvertFrom-Json`, validate every required collection, save the raw redacted response and parsed findings, and never evaluate response text.
- [ ] **Step 5: Run the full Pester suite; expect all tests to pass, then commit with `git commit -m "feat(tools): add investigation workflow"`.**

### Task 6: Isolated implementation worktrees

**Files:**
- Create: `tools/local-ai/Private/Worktree.ps1`
- Create: `tools/local-ai/Public/New-LocalAiWorktree.ps1`
- Create: `tools/local-ai/tests/Worktree.Tests.ps1`

**Interfaces:**
- Consumes: implementation task with recorded baseline and configured worktree root.
- Produces: `New-LocalAiWorktree -TaskId <string> -> PSCustomObject` with verified `Branch`, `Path`, and `BaselineCommit`.
- Produces: `Get-LocalAiWorktreeStatus -TaskId <string> -> PSCustomObject`.

- [ ] **Step 1: Write tests using a temporary Git repository** for branch naming, exact baseline, worktree outside the main checkout, rejection for investigation tasks, existing/nonempty destination, unresolved baseline, mismatched Git metadata, and preservation after later failure.
- [ ] **Step 2: Run the worktree tests; expect failures for missing functions.**
- [ ] **Step 3: Implement Git calls as fixed argument arrays.** Resolve and verify `git rev-parse`, create `ai/<task-slug>-<short-id>`, run `git worktree add -b`, then independently verify the worktree common directory, branch, and HEAD before persisting them.
- [ ] **Step 4: Run the full Pester suite; expect all tests to pass.**
- [ ] **Step 5: Commit with message `feat(tools): isolate AI implementation worktrees`.**

### Task 7: Named validation and non-deploying build operations

**Files:**
- Modify: `client/build.ps1`
- Create: `tools/local-ai/Private/Build.ps1`
- Create: `tools/local-ai/Public/Invoke-LocalAiValidation.ps1`
- Create: `tools/local-ai/Public/Invoke-LocalAiBuild.ps1`
- Create: `tools/local-ai/tests/Build.Tests.ps1`

**Interfaces:**
- Consumes: implementation task worktree and policy functions.
- Produces: `Invoke-LocalAiValidation -TaskId <string> -> result`.
- Produces: `Invoke-LocalAiBuild -TaskId <string> -Component <client|apworld> [-GameDir <string>] -> result`.
- Changes existing interface: `client/build.ps1 -GameDir <string> [-SkipInstall]`, preserving current install behavior when `-SkipInstall` is absent.

- [ ] **Step 1: Write tests** that mock process invocation and assert only fixed repository scripts/arguments run, commands execute from the task worktree, investigation tasks are rejected, nonzero exits are persisted, client `-SkipInstall` produces no copy/removal, and existing no-switch behavior still targets the normal plugin directory.
- [ ] **Step 2: Run Build tests; expect failures and confirm the legacy build script lacks `-SkipInstall`.**
- [ ] **Step 3: Add `-SkipInstall` to `client/build.ps1`.** Return after verifying the Release DLL when set; retain the current install path and dependency exclusions otherwise.
- [ ] **Step 4: Implement adapters for `python .\tools\validate-repo.py`, `.\tools\build-apworld.ps1`, and `.\client\build.ps1 -GameDir ... -SkipInstall`; capture exit code and bounded output in task state.**
- [ ] **Step 5: Run all Pester tests plus `python .\tools\validate-repo.py`; expect passes, then commit with `git commit -m "feat(tools): add controlled bridge build operations"`.**

### Task 8: Restricted client deployment

**Files:**
- Create: `tools/local-ai/Public/Publish-LocalAiClient.ps1`
- Create: `tools/local-ai/tests/Deployment.Tests.ps1`

**Interfaces:**
- Consumes: successful client build record, task worktree Release output, and `Resolve-PluginDestination`.
- Produces: `Publish-LocalAiClient -TaskId <string> -GameDir <string> -ConfirmDeployment -> result`.

- [ ] **Step 1: Write deployment tests** for required confirmation, exact destination, permitted DLL allowlist, BepInEx/Harmony/Il2CppInterop exclusions, source containment, missing build rejection, path traversal and reparse escape rejection, no sibling/core writes, hash recording, and failure without partial unrecorded success.
- [ ] **Step 2: Run Deployment tests; expect failures for the missing command.**
- [ ] **Step 3: Implement deployment with a manifest assembled from the verified Release directory.** Stage expected DLLs in a task-local temporary directory, hash them, revalidate the destination immediately before copying, replace only plugin-local allowlisted DLLs, and persist source/destination/hash records.
- [ ] **Step 4: Run the full Pester suite; expect all tests to pass.**
- [ ] **Step 5: Commit with message `feat(tools): restrict client plugin deployment`.**

### Task 9: Development-session-only log watcher

**Files:**
- Create: `tools/local-ai/Private/LogWatcher.ps1`
- Create: `tools/local-ai/Public/Start-LocalAiDevelopmentSession.ps1`
- Create: `tools/local-ai/Public/Stop-LocalAiDevelopmentSession.ps1`
- Create: `tools/local-ai/tests/LogWatcher.Tests.ps1`

**Interfaces:**
- Produces: `Start-LocalAiDevelopmentSession -TaskId <string> -GameDir <string> [-Pattern <string[]>] [-TimeoutMinutes <int>] -> session`.
- Produces: `Stop-LocalAiDevelopmentSession -TaskId <string> -> result`.

- [ ] **Step 1: Write tests** using a temporary append-only log for exact log-path enforcement, tail-from-end default, filtering, task-local capture, stop behavior, timeout behavior, terminal-task shutdown, duplicate-session rejection, and absence of service/scheduled-task/startup registration.
- [ ] **Step 2: Run LogWatcher tests; expect failures for missing commands.**
- [ ] **Step 3: Implement a PowerShell job owned by the invoking process.** Store its process/job identity in task state, poll with a short cancellation-aware interval, stop on timeout/terminal marker, redact captures, and make explicit stop idempotent.
- [ ] **Step 4: Run the full Pester suite; expect all tests to pass.**
- [ ] **Step 5: Commit with message `feat(tools): add session-bound game log watcher`.**

### Task 10: Handoff and human review commands

**Files:**
- Create: `tools/local-ai/Private/Handoff.ps1`
- Create: `tools/local-ai/Public/New-LocalAiHandoff.ps1`
- Create: `tools/local-ai/Public/Complete-LocalAiReview.ps1`
- Create: `tools/local-ai/tests/Handoff.Tests.ps1`

**Interfaces:**
- Produces: `New-LocalAiHandoff -TaskId <string> -> PSCustomObject` and task-local `handoff.json`/`handoff.md`.
- Produces: `Complete-LocalAiReview -TaskId <string> -Decision <accept|reject> -Notes <string> -> PSCustomObject`.

- [ ] **Step 1: Write tests** for goal/mode/baseline, findings, branch/worktree, changed files, validation/build/deploy evidence, risks/questions, next actions, secret redaction, deterministic Git diff summaries, accept/reject records, and absence of stage/commit/merge/rebase/push/worktree-remove calls.
- [ ] **Step 2: Run Handoff tests; expect failures for missing commands.**
- [ ] **Step 3: Implement handoff generation from task state plus fixed read-only Git queries.** `accept` transitions to `accepted`; `reject` records review and leaves artifacts available without mutating Git.
- [ ] **Step 4: Run the full Pester suite; expect all tests to pass.**
- [ ] **Step 5: Commit with message `feat(tools): generate human review handoffs`.**

### Task 11: Repository guidance and operator documentation

**Files:**
- Create: `tools/local-ai/README.md`
- Create: `AGENTS.md`
- Modify: `docs/WORKFLOW.md`
- Modify: `docs/TESTING.md`

**Interfaces:**
- Consumes: all public commands and safety guarantees implemented above.
- Produces: complete setup, investigation, implementation, build/deploy, log, review, failure-recovery, and manual cleanup instructions.

- [ ] **Step 1: Write a documentation checklist test** that verifies every exported command appears in `tools/local-ai/README.md`, both fixed API/model values are documented, the deployment destination is exact, and merge/push prohibition and investigation-first acceptance are present.
- [ ] **Step 2: Run the documentation test; expect failures because the documents are missing or incomplete.**
- [ ] **Step 3: Write the operator README and repository guidance.** Include environment setup without a literal token, config example, command transcripts, artifact locations, threat model, troubleshooting, explicit manual worktree cleanup, and a statement that runtime gameplay remains the final client acceptance criterion.
- [ ] **Step 4: Update `docs/WORKFLOW.md` and `docs/TESTING.md` without changing existing project behavior guidance; run all Pester tests and `python .\tools\validate-repo.py`; expect passes.**
- [ ] **Step 5: Commit with message `docs: document controlled local AI workflow`.**

### Task 12: Investigation-only end-to-end acceptance

**Files:**
- Create locally, ignored: `.local-ai/<task-id>/task.json`
- Create locally, ignored: `.local-ai/<task-id>/events.jsonl`
- Create locally, ignored: `.local-ai/<task-id>/findings.json`
- Create locally, ignored: `.local-ai/<task-id>/handoff.json`
- Create locally, ignored: `.local-ai/<task-id>/handoff.md`
- Test: `tools/local-ai/tests/Acceptance.Tests.ps1`

**Interfaces:**
- Consumes: completed investigation and handoff workflow.
- Produces: a reviewable Level 4 glasses / Minim progression investigation with no repository or runtime mutation.

- [ ] **Step 1: Add an automated acceptance test with a mocked Open WebUI response** that snapshots tracked status/HEAD/worktree list and asserts they remain unchanged, while the task reaches `awaiting_review` and produces complete findings/handoff artifacts.
- [ ] **Step 2: Run the mocked acceptance test; expect it to pass before contacting the live service.**
- [ ] **Step 3: Record a clean baseline with `git status --short`, `git rev-parse HEAD`, and `git worktree list --porcelain`, then create an investigation task whose exact goal is `Investigate Level 4 glasses / Minim progression.` Include `README.md`, `docs/PROJECT_OVERVIEW.md`, `docs/PROGRESSION.md`, relevant tracked client source, and no runtime logs unless the owner explicitly supplies them.**
- [ ] **Step 4: Invoke the live loopback Open WebUI endpoint and generate the handoff.** Verify it identifies evidence separately from unknown native flags and does not allocate IDs or claim gameplay facts absent evidence.
- [ ] **Step 5: Re-run the baseline Git queries and verify no tracked changes, branches, or worktrees were created; verify no build, deployment, game launch, or log watcher event exists; retain only ignored task artifacts.**
- [ ] **Step 6: Run `Invoke-Pester .\tools\local-ai\tests -Output Detailed` and `python .\tools\validate-repo.py`; expect all tests and repository validation to pass.**
- [ ] **Step 7: Create a final documentation-only commit for any acceptance wording corrections, if needed, with `git commit -m "test(tools): verify investigation-only AI bridge workflow"`; do not commit local task artifacts.**

## Final self-review gate

- [ ] Map every design-spec requirement to at least one passing test and task above.
- [ ] Search this plan and implementation for placeholder language and unresolved function/type naming inconsistencies.
- [ ] Confirm `git status --short` contains only intended tracked implementation/documentation changes.
- [ ] Confirm no credential, prompt/response artifact, log, binary, `.apworld`, local config, or `.local-ai` file is tracked.
- [ ] Confirm the implementation contains no merge, rebase, push, merge-request, scheduled-task, service-install, startup-registration, game-launch, or arbitrary-command capability.
