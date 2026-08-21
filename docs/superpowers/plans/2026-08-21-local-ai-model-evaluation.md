# Local AI Model Evaluation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a guarded two-model local-AI comparison workflow, a canonical project decisions ledger, and deterministic semantic scoring without automatic model switching or reduced human oversight.

**Architecture:** Extend the existing PowerShell bridge through focused private loaders/scorers and one public evaluation command. The fixed decisions ledger enters every investigation through the existing bounded context path; model evaluation uses the existing loopback Open WebUI client, exact answer contracts, atomic redacted reports, and an explicit human selection step.

**Tech Stack:** PowerShell 7, Pester 6, JSON data files, Open WebUI loopback API, existing local bridge helpers, Git.

**Spec:** `docs/superpowers/specs/2026-08-21-local-ai-model-evaluation-design.md`

## Global Constraints

- The only allowed model IDs are `jacks-assistant` and `jacks-assistant-fast`.
- Default `ModelId` remains `jacks-assistant` until the owner explicitly changes ignored local configuration.
- Open WebUI remains HTTP loopback-only and bearer tokens remain environment-only.
- Models receive no filesystem, Git, build, deployment, log, terminal, merge, or push authority.
- Every investigation still ends at `awaiting_review`; evaluation never accepts a handoff or changes configuration.
- Evaluation reports remain beneath the configured ignored state root and never enter Git.
- Model explanations never affect scores; only exact structured answers do.
- Use TDD: observe each focused test fail for the missing behavior before writing production code.

---

### Task 1: Fixed Model Allowlist and Configurable Selection

**Files:**
- Modify: `tools/local-ai/Private/Config.ps1`
- Modify: `tools/local-ai/Private/OpenWebUi.ps1`
- Modify: `tools/local-ai/config.example.psd1`
- Modify: `tools/local-ai/tests/Config.Tests.ps1`
- Modify: `tools/local-ai/tests/OpenWebUi.Tests.ps1`

**Interfaces:**
- Produces: `Get-AllowedLocalAiModelId -> string[]`.
- Changes: `Get-LocalAiConfiguration(...).ModelId` may be either fixed allowlisted value.
- Changes: `Invoke-OpenWebUiChat -Configuration <object>` sends and returns `Configuration.ModelId`.

- [ ] **Step 1: Add failing configuration tests**

Add cases proving the default remains `jacks-assistant`, a config file containing `@{ ModelId = 'jacks-assistant-fast' }` succeeds, and `other-model` fails with a message naming the fixed allowlist. Assert `Get-AllowedLocalAiModelId` returns exactly:

```powershell
@('jacks-assistant', 'jacks-assistant-fast')
```

- [ ] **Step 2: Add failing Open WebUI request tests**

Run the existing mocked request once per allowlisted model and assert the JSON body and returned `ModelId` use the selected value. Keep the existing arbitrary-model rejection assertion, but update its expected message to mention `allowlisted`.

- [ ] **Step 3: Run focused tests and verify RED**

Run:

```powershell
Invoke-Pester .\tools\local-ai\tests\Config.Tests.ps1,.\tools\local-ai\tests\OpenWebUi.Tests.ps1 -Output Detailed
```

Expected: failures because the fast alias is rejected and requests remain hard-coded to `jacks-assistant`.

- [ ] **Step 4: Implement the minimal allowlist**

Add to `Config.ps1`:

```powershell
function Get-AllowedLocalAiModelId {
    return @('jacks-assistant', 'jacks-assistant-fast')
}
```

Validate `$values.ModelId` with ordinal membership in that array. Return the validated string in the configuration object. In `OpenWebUi.ps1`, repeat allowlist validation before reading the API key, set `model = [string] $Configuration.ModelId`, and return the same value. Do not make the allowlist configurable.

- [ ] **Step 5: Document the optional local alias**

Keep `ModelId = 'jacks-assistant'` in `config.example.psd1` and add a comment showing `jacks-assistant-fast` is the only alternative and selection remains manual.

- [ ] **Step 6: Run focused and full bridge tests**

Expected: all Task 1 tests and the pre-existing 81 tests pass.

- [ ] **Step 7: Commit Task 1**

```powershell
git add tools/local-ai/Private/Config.ps1 tools/local-ai/Private/OpenWebUi.ps1 tools/local-ai/config.example.psd1 tools/local-ai/tests/Config.Tests.ps1 tools/local-ai/tests/OpenWebUi.Tests.ps1
git commit -m "feat(tools): allowlist local AI model profiles"
```

---

### Task 2: Canonical Decisions Ledger and Investigation Context

**Files:**
- Create: `tools/local-ai/evaluation/decisions.json`
- Create: `tools/local-ai/Private/EvaluationData.ps1`
- Create: `tools/local-ai/tests/DecisionLedger.Tests.ps1`
- Modify: `tools/local-ai/Public/Invoke-LocalAiInvestigation.ps1`
- Modify: `tools/local-ai/tests/Investigation.Tests.ps1`
- Modify: `tools/local-ai/tests/Acceptance.Tests.ps1`

**Interfaces:**
- Produces: `Get-LocalAiDecisionLedger -RepositoryRoot <string> -> PSCustomObject`.
- Produces: `Get-LocalAiDecisionLedgerPath -> 'tools/local-ai/evaluation/decisions.json'`.
- Changes: every investigation context includes the validated ledger path exactly once.

- [ ] **Step 1: Add the ledger fixture expected by tests**

Create schema version 1 with stable entries for:

```json
{
  "id": "area-access-vs-level-access",
  "category": "access",
  "approved_statement": "Roots Access is required to enter Roots; individual Level Access items are superseded.",
  "prohibited_interpretations": [
    "Roots content requires no Area Access item.",
    "Level 5 Access should be restored."
  ],
  "evidence_paths": ["docs/PROGRESSION.md"]
}
```

Add equivalent exact entries for `location-vs-item`, `hip-glasses-chain`, `historical-vs-current-design`, and `native-flag-confidence`, using `docs/PROGRESSION.md`, `docs/PROJECT_OVERVIEW.md`, and `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md` only where each file directly supports the statement.

- [ ] **Step 2: Write failing ledger-validation tests**

Tests must copy the ledger and evidence files into a temporary Git repository, then prove:

- valid ledger loads with five unique IDs in file order;
- duplicate IDs fail;
- missing/empty approved statements fail;
- missing or empty prohibited interpretation arrays fail;
- absolute, traversal, untracked, and missing evidence paths fail;
- schema versions other than 1 fail.

Tests assert returned data contains no API key value.

- [ ] **Step 3: Write failing investigation-integration tests**

Update temporary investigation repositories to contain and track a minimal ledger. Mock `Invoke-OpenWebUiChat`, capture its messages, and assert the user context contains `--- FILE: tools/local-ai/evaluation/decisions.json ---` exactly once even when the caller also supplies that path. Assert the ledger counts toward `MaxBytes` and an undersized limit fails before HTTP.

- [ ] **Step 4: Run focused tests and verify RED**

Expected: loader functions do not exist and investigations omit the ledger.

- [ ] **Step 5: Implement strict ledger loading**

In `EvaluationData.ps1`, resolve the fixed ledger through `Resolve-ContainedPath`, require it to be tracked with `git ls-files --error-unmatch`, parse JSON with `ConvertFrom-Json -ErrorAction Stop`, validate exact required properties, and validate every evidence path with the same containment/tracking rules used by context assembly. Return a deep redacted copy.

- [ ] **Step 6: Integrate the ledger through bounded context**

In `Invoke-LocalAiInvestigation`, validate the ledger first, construct:

```powershell
$contextPaths = @($IncludePath) + @(Get-LocalAiDecisionLedgerPath)
$context = New-LocalAiContext -Task $task -IncludePath $contextPaths -MaxBytes $MaxBytes
```

Rely on `New-LocalAiContext`'s sorted set for deduplication. Extend the system prompt to state that ledger entries are approved constraints and that results must distinguish observations, AP locations, AP items, vanilla interactions, and unknown mappings. Do not relax the existing output schema.

- [ ] **Step 7: Run focused and full tests**

Expected: ledger and investigation tests pass; acceptance still proves no tracked, branch, worktree, build, deployment, or watcher mutation.

- [ ] **Step 8: Commit Task 2**

```powershell
git add tools/local-ai/evaluation/decisions.json tools/local-ai/Private/EvaluationData.ps1 tools/local-ai/tests/DecisionLedger.Tests.ps1 tools/local-ai/Public/Invoke-LocalAiInvestigation.ps1 tools/local-ai/tests/Investigation.Tests.ps1 tools/local-ai/tests/Acceptance.Tests.ps1
git commit -m "feat(tools): anchor investigations to approved decisions"
```

---

### Task 3: Deterministic Evaluation Definitions and Scoring

**Files:**
- Create: `tools/local-ai/evaluation/cases.json`
- Modify: `tools/local-ai/Private/EvaluationData.ps1`
- Create: `tools/local-ai/Private/EvaluationScoring.ps1`
- Create: `tools/local-ai/tests/EvaluationScoring.Tests.ps1`

**Interfaces:**
- Produces: `Get-LocalAiEvaluationCases -RepositoryRoot <string> -> PSCustomObject[]`.
- Produces: `ConvertFrom-LocalAiEvaluationResponse -Content <string> -> PSCustomObject`.
- Produces: `Measure-LocalAiEvaluationCase -Case <object> -Response <object> -> PSCustomObject` with `Passed`, `Score`, `MaximumScore`, `Answers`, and `Errors`.

- [ ] **Step 1: Define five exact-answer cases**

Use answer enums rather than phrase matching. Example question contracts:

```json
{
  "id": "roots_access_required",
  "allowed_values": ["yes", "no"],
  "expected": "yes"
},
{
  "id": "level_5_access_required",
  "allowed_values": ["yes", "no"],
  "expected": "no"
},
{
  "id": "hip_glasses_source_kind",
  "allowed_values": ["location", "item", "vanilla_ability", "unknown"],
  "expected": "location"
}
```

Across five cases, cover all initial ledger decisions and explicitly classify Hip Glasses source, Hip Glasses item, Bucket Minion trade source, Chicken Bucket item, Combo Bucket vanilla ability, historical facts, superseded designs, and unknown flags.

- [ ] **Step 2: Write failing case-definition tests**

Reject duplicate case IDs, duplicate question IDs, missing prompts, empty allowed values, expected values outside their enum, and cases without questions.

- [ ] **Step 3: Write failing response-parser tests**

Accept plain JSON and one surrounding `json` Markdown fence, matching investigation behavior. Reject malformed JSON, wrong case ID type, missing answers, duplicate answer IDs, and non-string answer values.

- [ ] **Step 4: Write failing exact-scoring tests**

Prove a perfect response earns one point per question and passes. Wrong, missing, extra, duplicate, or out-of-enum answers produce descriptive errors and fail. Free-form explanation changes must never change the score.

- [ ] **Step 5: Run scoring tests and verify RED**

Expected: missing loader/parser/scorer functions.

- [ ] **Step 6: Implement strict definitions, parsing, and scoring**

Load fixed tracked `cases.json` through the same containment pattern as the ledger. Normalize response answer IDs with ordinal comparison only; do not use fuzzy matching, substring matching, embeddings, or a second model to score. Set `Passed = ($score -eq $maximum -and $errors.Count -eq 0)`.

- [ ] **Step 7: Run focused and full tests**

Expected: all scoring cases pass deterministically and existing bridge tests remain green.

- [ ] **Step 8: Commit Task 3**

```powershell
git add tools/local-ai/evaluation/cases.json tools/local-ai/Private/EvaluationData.ps1 tools/local-ai/Private/EvaluationScoring.ps1 tools/local-ai/tests/EvaluationScoring.Tests.ps1
git commit -m "feat(tools): score local models on project semantics"
```

---

### Task 4: Public Two-Model Evaluation and Atomic Reports

**Files:**
- Create: `tools/local-ai/Public/Invoke-LocalAiModelEvaluation.ps1`
- Create: `tools/local-ai/Private/EvaluationReport.ps1`
- Create: `tools/local-ai/tests/ModelEvaluation.Tests.ps1`
- Modify: `tools/local-ai/LocalAiBridge.psd1`

**Interfaces:**
- Produces: `Invoke-LocalAiModelEvaluation -RepositoryRoot <string> [-ModelId <string[]>] [-ConfigPath <string>] [-OpenWebUiTimeoutSec <int>] -> PSCustomObject`.
- Produces ignored `report.json` and `report.md` beneath `<StateRoot>/evaluations/<UTC timestamp>-<12 hex chars>/`.

- [ ] **Step 1: Write failing orchestration tests with mocked HTTP boundary**

Use real case loading, parsing, scoring, path creation, redaction, and report writing. Mock only `Invoke-OpenWebUiChat`. Prove:

- default order is `jacks-assistant`, then `jacks-assistant-fast`;
- an explicit subset is allowed but arbitrary/duplicate IDs fail before HTTP;
- each model receives every case with a system prompt requiring exact JSON;
- a failure for one model/case is recorded and remaining cases continue;
- no task, worktree, branch, build, deployment, or watcher is created.

- [ ] **Step 2: Write failing recommendation tests**

Cover:

- fast is sole perfect winner -> recommend `jacks-assistant-fast`;
- legacy is sole perfect winner -> recommend `jacks-assistant`;
- tie -> recommendation is null with reason `tie`;
- highest score is not perfect -> recommendation is null with reason `no-perfect-winner`;
- errors prevent perfection and cannot be ignored.

- [ ] **Step 3: Write failing report-security tests**

Set a recognizable API key and mocked response containing it. Assert neither report contains the key. Assert paths remain under `StateRoot/evaluations`, JSON is parseable, Markdown includes scores/errors/recommendation, no temporary files remain, and `git status`, HEAD, branches, and worktrees are unchanged.

- [ ] **Step 4: Run evaluation tests and verify RED**

Expected: public command and report helpers do not exist.

- [ ] **Step 5: Implement evaluation orchestration**

For each model and case, construct an in-memory configuration copy with the selected allowlisted model. Prompt for exactly:

```json
{
  "case_id": "<case id>",
  "answers": [{"id": "<question id>", "value": "<allowed value>"}],
  "explanation": "brief human-review rationale"
}
```

Measure duration with `Diagnostics.Stopwatch`. Catch and redact per-case errors. Never terminate the whole comparison because one model fails.

- [ ] **Step 6: Implement recommendation and reports**

Recommend only a unique highest-scoring model that passes every case. Use `Write-AtomicJson` for JSON and a temporary-file-plus-atomic-move pattern for redacted Markdown. Include schema version 1, timestamps, evaluated model IDs, case-definition hash, totals, per-case results, recommendation, and reason.

- [ ] **Step 7: Export the command and run focused tests**

Add `Invoke-LocalAiModelEvaluation` to `FunctionsToExport`. Expected: orchestration, recommendation, security, and Git-invariance tests pass.

- [ ] **Step 8: Run the full bridge suite**

Expected: all existing and new Pester tests pass with zero failures.

- [ ] **Step 9: Commit Task 4**

```powershell
git add tools/local-ai/Public/Invoke-LocalAiModelEvaluation.ps1 tools/local-ai/Private/EvaluationReport.ps1 tools/local-ai/tests/ModelEvaluation.Tests.ps1 tools/local-ai/LocalAiBridge.psd1
git commit -m "feat(tools): compare allowlisted local AI models"
```

---

### Task 5: Documentation, Repository Contract, and Live Acceptance

**Files:**
- Modify: `tools/local-ai/README.md`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `tools/validate-repo.py`
- Modify: `apworld/tests/test_repository_contract.py`

**Interfaces:**
- Documents the manual evaluation and selection workflow.
- Repository validator proves the fixed ledger/case files and exported command exist.

- [ ] **Step 1: Add failing repository-contract assertions**

Extend the subprocess validator test to require output facts:

```json
{
  "local_ai_allowed_models": ["jacks-assistant", "jacks-assistant-fast"],
  "local_ai_decision_schema": 1,
  "local_ai_evaluation_schema": 1
}
```

Add temporary fixture mutations for a removed ledger entry, an invalid case expected value, and an absent public evaluation script; each must fail with the relevant relative path.

- [ ] **Step 2: Run the contract test and verify RED**

Expected: validator does not yet inspect local-AI evaluation assets.

- [ ] **Step 3: Extend repository validation**

Parse both JSON files, validate schema/version/stable required IDs, verify both public model aliases in bridge policy, and require `Invoke-LocalAiModelEvaluation.ps1` plus its manifest export. Retain every existing APWorld/client/ID check.

- [ ] **Step 4: Update documentation accurately**

Document:

- current default remains `jacks-assistant`;
- optional ignored config selects `jacks-assistant-fast`;
- exact evaluation command and ignored report location;
- scoring is deterministic but narrow;
- reports never change configuration automatically;
- human review remains mandatory;
- local model evaluation is not gameplay validation.

- [ ] **Step 5: Run the complete automated gate**

Run:

```powershell
Invoke-Pester .\tools\local-ai\tests -Output Detailed
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' .\tools\validate-repo.py
git diff --check
git status --short
```

Expected: all tests and validation pass; only intended tracked changes remain.

- [ ] **Step 6: Commit documentation and contract checks**

```powershell
git add tools/local-ai/README.md README.md CHANGELOG.md tools/validate-repo.py apworld/tests/test_repository_contract.py
git commit -m "docs: document local AI model evaluation"
```

- [ ] **Step 7: Run live two-model acceptance without automatic selection**

Record `git status --short`, HEAD, branches, and worktrees. Load the API key from the user environment into the process without printing it, then run:

```powershell
Import-Module .\tools\local-ai\LocalAiBridge.psd1 -Force
Invoke-LocalAiModelEvaluation `
    -RepositoryRoot $PWD `
    -ModelId @('jacks-assistant','jacks-assistant-fast') `
    -OpenWebUiTimeoutSec 600
```

Inspect the ignored report, verify no secret or unsupported claim is present, and repeat the Git snapshots. Do not edit `config.local.psd1` yet.

- [ ] **Step 8: Present the report for owner selection**

Summarize scores, failures, response time, and recommendation. Ask the owner whether to keep `jacks-assistant` or manually set `jacks-assistant-fast`. Do not infer approval from the score.

---

## Final Review and Handoff

- [ ] Run a fresh full Pester suite, APWorld unit suite, repository validator, and `git diff --check`.
- [ ] Compare the branch against its base and confirm no client, APWorld gameplay, ID, build/deployment containment, task review, merge, or push behavior changed.
- [ ] Request an independent whole-branch review against the approved spec and this plan.
- [ ] Fix every Critical or Important issue with a new failing test first.
- [ ] Present the verified branch and live evaluation report; do not merge, push, or select a model without explicit owner approval.
