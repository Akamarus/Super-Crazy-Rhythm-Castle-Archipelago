# Local AI Bridge

This PowerShell module provides a controlled development boundary between this repository and the locally hosted `jacks-assistant` and `jacks-assistant-fast` model profiles. PowerShell owns all filesystem, Git, build, deployment, log, and task-state operations. Model text is always treated as data and is never executed.

## Requirements

- PowerShell 7 or newer
- Pester 5 or newer for tests
- Git
- Python available through `py -3`
- Open WebUI at `http://127.0.0.1:8080`
- Open WebUI model IDs `jacks-assistant` and `jacks-assistant-fast`

Set the API token only in the current process environment:

```powershell
$env:OPENWEBUI_API_KEY = '<your-local-token>'
```

Never put the token in a configuration file. The bridge does not persist or print it.

## Load the module

```powershell
Import-Module .\tools\local-ai\LocalAiBridge.psd1 -Force
```

Copy `config.example.psd1` to the ignored `tools/local-ai/config.local.psd1` only when local overrides are needed. The default model remains `jacks-assistant`. To select the only alternative manually, set `ModelId = 'jacks-assistant-fast'` in that ignored file. Configuration may also set the state root, external worktree root, and game directory, but cannot change the loopback security boundary or select a model outside the fixed allowlist.

## Compare the local model profiles

From the repository root, load the module and evaluate both allowlisted profiles:

```powershell
Import-Module .\tools\local-ai\LocalAiBridge.psd1 -Force
Invoke-LocalAiModelEvaluation `
    -RepositoryRoot $PWD `
    -ModelId @('jacks-assistant','jacks-assistant-fast') `
    -OpenWebUiTimeoutSec 600
```

The command writes redacted `report.json` and `report.md` files beneath the ignored `.local-ai/evaluations/<evaluation-id>/` directory. Its exact-answer scoring is deterministic for a given set of returned answers, but the case set is intentionally narrow and does not measure general coding quality or gameplay correctness.

The report and its recommendation are advisory. Evaluation never edits `config.local.psd1` or selects a model automatically; a human must review the per-case answers, errors, and timing before deciding whether to keep `jacks-assistant` or manually select `jacks-assistant-fast`. Human review of model-assisted work remains mandatory, and this evaluation is not a substitute for APWorld generation, client runtime, or gameplay validation.

## Investigation workflow

```powershell
$task = New-LocalAiTask `
    -Goal 'Investigate Level 4 glasses / Minim progression.' `
    -Mode investigation `
    -RepositoryRoot $PWD

$findings = Invoke-LocalAiInvestigation `
    -TaskId $task.TaskId `
    -RepositoryRoot $PWD `
    -IncludePath @(
        'README.md',
        'docs/PROJECT_OVERVIEW.md',
        'docs/PROGRESSION.md',
        'client/Plugin.cs'
    ) `
    -OpenWebUiTimeoutSec 600

New-LocalAiHandoff -TaskId $task.TaskId -RepositoryRoot $PWD
```

Only explicitly selected tracked text files enter model context. Logs, binaries, generated output, local state, secrets, and outside paths are rejected. Investigation tasks cannot create worktrees, edit, validate, build, deploy, or watch logs.

## Implementation workflow

```powershell
$task = New-LocalAiTask -Goal 'Implement an approved change' -Mode implementation -RepositoryRoot $PWD
$worktree = New-LocalAiWorktree -TaskId $task.TaskId -RepositoryRoot $PWD

Invoke-LocalAiValidation -TaskId $task.TaskId -RepositoryRoot $PWD
Invoke-LocalAiBuild -TaskId $task.TaskId -RepositoryRoot $PWD -Component apworld
Invoke-LocalAiBuild -TaskId $task.TaskId -RepositoryRoot $PWD -Component client -GameDir 'D:\SteamLibrary\steamapps\common\Titus'
```

Implementation worktrees are created outside the main checkout at the task's recorded baseline. Cleanup is deliberately manual. Do not remove a task worktree until its handoff has been reviewed and any desired changes have been preserved.

## Restricted deployment

```powershell
Publish-LocalAiClient `
    -TaskId $task.TaskId `
    -RepositoryRoot $PWD `
    -GameDir 'D:\SteamLibrary\steamapps\common\Titus' `
    -ConfirmDeployment
```

Deployment requires a successful recorded client build and explicit confirmation. The only destination is:

```text
<GameDir>\BepInEx\plugins\RhythmCastleAP
```

The wrapper excludes BepInEx, Harmony, and Il2CppInterop assemblies and records SHA-256 hashes. It rejects traversal, sibling plugin paths, `BepInEx\core`, wildcards, and reparse-point escapes.

## Development-session log watcher

```powershell
$session = Start-LocalAiDevelopmentSession `
    -TaskId $task.TaskId `
    -RepositoryRoot $PWD `
    -GameDir 'D:\SteamLibrary\steamapps\common\Titus' `
    -Pattern @('SCRC-AP', 'ERROR') `
    -TimeoutMinutes 30

Stop-LocalAiDevelopmentSession -TaskId $task.TaskId -RepositoryRoot $PWD
```

The watcher tails only `<GameDir>\BepInEx\LogOutput.log`, starts at the current end, and writes filtered lines to the ignored task directory. It is a job owned by the invoking PowerShell process—not a service, scheduled task, startup item, or game launcher. It stops explicitly, on timeout, or when the task becomes terminal.

## Review

```powershell
New-LocalAiHandoff -TaskId $task.TaskId -RepositoryRoot $PWD
Complete-LocalAiReview -TaskId $task.TaskId -RepositoryRoot $PWD -Decision accept -Notes 'Reviewed locally'
```

`accept` records human review only. The bridge never stages, commits, merges, rebases, pushes, opens a merge request, removes a worktree, or deletes a branch. Those remain explicit human Git actions.

## Local artifacts and recovery

Task artifacts live beneath ignored `.local-ai/<task-id>/` directories. They include `task.json`, `events.jsonl`, model responses, findings, captures, and handoffs. Do not commit them.

If a command fails:

1. Read the task's latest state and event record.
2. Preserve the task worktree and local artifacts.
3. Correct configuration or environment problems without editing task history.
4. Create a new task when a failed task must be retried; terminal states are immutable.

Run bridge tests with:

```powershell
Invoke-Pester .\tools\local-ai\tests -Output Detailed
```

Passing automated tests do not validate gameplay. Client changes still require the runtime workflow and gameplay acceptance checks in `docs/TESTING.md`.

## Exported command reference

User workflow commands:

- `New-LocalAiTask`, `Get-LocalAiTask`
- `Invoke-LocalAiModelEvaluation`
- `Invoke-LocalAiInvestigation`, `New-LocalAiHandoff`, `Complete-LocalAiReview`
- `New-LocalAiWorktree`, `Get-LocalAiWorktreeStatus`
- `Invoke-LocalAiValidation`, `Invoke-LocalAiBuild`, `Publish-LocalAiClient`
- `Start-LocalAiDevelopmentSession`, `Stop-LocalAiDevelopmentSession`

Policy and integration commands used by the tested workflow:

- `Get-LocalAiConfiguration`, `Assert-LoopbackUri`, `Assert-TaskOperation`
- `Resolve-ContainedPath`, `Resolve-GameLogPath`, `Resolve-PluginDestination`
- `Invoke-OpenWebUiChat`, `New-LocalAiContext`
- `Add-LocalAiTaskEvent`, `Set-LocalAiTaskState`

The lower-level commands remain constrained by the same loopback, model, path, mode, state-transition, and redaction checks. They are not general-purpose execution interfaces.
