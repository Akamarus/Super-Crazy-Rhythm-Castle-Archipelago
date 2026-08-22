# Local AI Model Evaluation and Decisions Ledger Design

**Status:** Approved design; implementation not started
**Date:** 2026-08-21

## 1. Purpose

The local AI bridge currently permits exactly one Open WebUI model, `jacks-assistant`, and relies on broad repository context plus mandatory human review. Live acceptance testing showed that the model can return valid JSON while still confusing important project concepts: Area Access versus obsolete per-level access, and Archipelago location checks versus randomized items.

This change adds project-specific semantic guardrails and a deterministic comparison workflow for `jacks-assistant` and `jacks-assistant-fast`. It does not grant either model additional filesystem, Git, build, deployment, log, or runtime authority.

## 2. Goals

- Maintain a tracked, machine-readable decisions ledger containing approved project facts that local models must preserve.
- Automatically include the ledger in every investigation context.
- Permit only `jacks-assistant` and `jacks-assistant-fast` through the loopback Open WebUI client.
- Evaluate both models against fixed semantic cases with deterministic structured scoring.
- Produce ignored local reports that support a human model-selection decision.
- Keep the configured default on `jacks-assistant` until a human explicitly changes local configuration.
- Preserve mandatory human review for every investigation and implementation handoff.

## 3. Non-goals

- No automatic model switching.
- No model voting or two-model execution on every task.
- No changes to Open WebUI, LM Studio, model weights, quantization, or sampling configuration.
- No removal of current bridge containment or state-transition rules.
- No live gameplay discovery, client changes, APWorld changes, ID allocation, build, deployment, log watching, merge, or push.
- No claim that a passing local model is equivalent to final human or frontier-model review.

## 4. Model policy

The fixed model allowlist is:

```text
jacks-assistant
jacks-assistant-fast
```

`Get-LocalAiConfiguration` continues to default to `jacks-assistant`. A local ignored configuration file may set `ModelId = 'jacks-assistant-fast'`. Any other value fails before HTTP. `Invoke-OpenWebUiChat` sends the validated configured model ID rather than a hard-coded ID and returns that same ID in its result.

The allowlist remains production code, not local configuration. This prevents a compromised or mistaken local config from selecting an arbitrary Open WebUI tool-enabled model.

## 5. Decisions ledger

The canonical ledger is `tools/local-ai/evaluation/decisions.json`. It contains a schema version and ordered decisions with stable IDs, categories, approved statements, prohibited interpretations, and evidence paths.

The initial ledger covers:

1. `area-access-vs-level-access`: Roots Access is required to enter Roots; individual level-access items are superseded.
2. `location-vs-item`: an AP location check sends the randomized item placed there and does not guarantee its thematically associated item.
3. `hip-glasses-chain`: Level 4's pickup is a Hip Glasses source location; Hip Glasses is separately a randomized item; the normal Bucket Minion trade is the Chicken Bucket source location; Chicken Bucket is separately a randomized item; Combo Bucket remains a vanilla consequence.
4. `historical-vs-current-design`: gameplay observations remain evidence, while pre-pivot per-level design conclusions are not current requirements.
5. `native-flag-confidence`: unknown native identifiers must remain unknown and must never be invented.

Each evidence path must be repository-relative, tracked, and use forward slashes. The loader rejects duplicate IDs, missing fields, empty statements, absolute/outside paths, and untracked evidence files.

## 6. Investigation integration

`Invoke-LocalAiInvestigation` automatically adds the fixed ledger path to the caller's bounded `IncludePath` set. It passes through the existing `New-LocalAiContext` validation, byte accounting, deterministic ordering, tracking checks, and redaction.

The system prompt tells the model that ledger entries are approved constraints, asks it to distinguish observations, AP locations, AP items, vanilla interactions, and unknown mappings, and still requires the existing JSON-only findings contract. Ledger inclusion does not make model output authoritative; handoffs remain `awaiting_review` until a human decides.

## 7. Evaluation cases

`tools/local-ai/evaluation/cases.json` defines stable cases. Each case contains:

- `id`
- `prompt`
- ordered `questions`, each with an ID and allowed answer values
- exact `expected_answers`

Initial cases cover:

- required Roots Access versus forbidden per-level access;
- source-location checks versus randomized Hip Glasses and Chicken Bucket items;
- Combo Bucket as a vanilla ability consequence rather than an AP item;
- historical observation versus superseded design;
- refusal to invent missing native flags.

The evaluation system prompt requires JSON with `case_id`, `answers`, and `explanation`. Scoring uses only exact answer values and required question IDs. Free-form explanation is stored for human review but never affects the score. Missing, duplicate, extra, or invalid answers make the case fail clearly.

## 8. Evaluation command and reports

The exported command is:

```powershell
Invoke-LocalAiModelEvaluation `
    -RepositoryRoot <path> `
    [-ModelId @('jacks-assistant','jacks-assistant-fast')] `
    [-OpenWebUiTimeoutSec 600]
```

The default evaluates both allowlisted models. It calls the same loopback-only authenticated Open WebUI client. Evaluation does not create a development task, worktree, build, deployment, or watcher.

Reports are written atomically beneath ignored `.local-ai/evaluations/<timestamp-and-random-id>/`:

- `report.json`: model totals, per-case answers, pass/fail, errors, duration, and an optional recommendation.
- `report.md`: redacted human-readable comparison.

A recommendation is emitted only when exactly one model has the highest score and that model passes every case. Otherwise the report says that no model change is recommended. The command never edits `config.local.psd1`.

## 9. Error handling and security

- API keys remain environment-only and are never stored or printed.
- Only HTTP loopback endpoints remain valid.
- Only fixed allowlisted model IDs may reach HTTP.
- A failure from one model or case is recorded and evaluation continues with the remaining combinations.
- Raw responses and reports pass through existing redaction helpers before persistence.
- Evaluation paths must remain inside the configured ignored state root.
- Ledger and case definitions are treated as data and validated before use.
- No model output is executed or interpreted as PowerShell.

## 10. Testing strategy

Pester tests will prove:

- both approved model IDs load from configuration and arbitrary IDs fail before HTTP;
- the Open WebUI body and returned metadata use the selected allowlisted model;
- ledger schema and evidence paths validate, and the ledger is automatically included in investigations;
- evaluation definitions reject malformed or ambiguous answer contracts;
- exact scoring catches every known semantic confusion;
- evaluation continues after a per-model error;
- reports are redacted, atomic, ignored, and never change Git state;
- tied, partial, or failing results do not recommend a switch;
- a sole all-cases winner may be recommended but is never selected automatically;
- all existing 81 bridge tests continue to pass.

A live comparison of both installed models is a post-test acceptance step. It is not part of unit tests and requires the owner's local API key and loaded model aliases.

## 11. Documentation and acceptance

The bridge README and example configuration will document the two aliases, the unchanged default, the evaluation command, report location, and manual selection process.

Implementation is accepted when all tests and repository validation pass, a live two-model report is generated without repository mutation, and the project owner explicitly chooses whether to set `jacks-assistant-fast` in ignored local configuration. Human review remains mandatory regardless of the selected model.
