# Local AI Bridge Design

**Status:** Approved for implementation planning; not yet implemented  
**Date:** 2026-08-20

## 1. Purpose

The local AI bridge gives the project owner a controlled PowerShell interface for asking the locally hosted `jacks-assistant` model to investigate or change this repository. The bridge owns all filesystem, Git, build, deployment, and log access. The model receives only the context and actions that the bridge deliberately exposes.

The first release is a development aid, not an autonomous maintainer. It prepares isolated work, records durable task state and handoffs, and stops for human review. It never merges or pushes.

## 2. Approved architecture

```text
Project owner
    |
    v
PowerShell commands and policy layer
    |-- repository reads and controlled writes
    |-- Git worktree lifecycle
    |-- restricted validation/build/deploy
    |-- development-session log watcher
    |-- task state and handoff artifacts
    |
    v
Open WebUI API: http://127.0.0.1:8080/api/chat/completions
    |
    v
model: jacks-assistant
```

There is no Open Terminal dependency. `jacks-assistant` does not receive an unrestricted shell or direct filesystem access. PowerShell is the sole execution boundary and validates every action before performing it.

## 3. Scope

The bridge will support:

- investigation-only tasks that collect repository context and save findings;
- implementation tasks performed in an isolated Git worktree;
- structured task status, event history, and review handoffs;
- repository validation and client builds through approved wrappers;
- client deployment only to `<GameDir>\BepInEx\plugins\RhythmCastleAP`;
- opt-in `BepInEx\LogOutput.log` watching while an explicit development session is active;
- human-controlled review, merge, and push steps.

The initial implementation will not provide a general-purpose command runner, allow arbitrary deployment destinations, launch the game, merge branches, push remotes, or run continuously in the background.

## 4. Repository layout

The planned implementation is organized under `tools/local-ai/`:

| Path | Responsibility |
| --- | --- |
| `tools/local-ai/LocalAiBridge.psd1` | Module manifest and exported command contract. |
| `tools/local-ai/LocalAiBridge.psm1` | Loads focused private scripts and exposes public commands. |
| `tools/local-ai/config.example.psd1` | Checked-in non-secret defaults and documented local overrides. |
| `tools/local-ai/Public/*.ps1` | User-facing task, invocation, build/deploy, log, and review commands. |
| `tools/local-ai/Private/Config.ps1` | Configuration loading and validation. |
| `tools/local-ai/Private/OpenWebUi.ps1` | The only Open WebUI HTTP client. |
| `tools/local-ai/Private/TaskState.ps1` | Task schema, atomic persistence, state transitions, and events. |
| `tools/local-ai/Private/Worktree.ps1` | Safe branch/worktree creation and inspection. |
| `tools/local-ai/Private/Context.ps1` | Bounded repository context assembly. |
| `tools/local-ai/Private/Policy.ps1` | Path, operation, and deployment authorization checks. |
| `tools/local-ai/Private/Build.ps1` | Approved validation, build, and restricted deployment adapters. |
| `tools/local-ai/Private/LogWatcher.ps1` | Development-session-only log tailing and filtered capture. |
| `tools/local-ai/Private/Handoff.ps1` | Review summary and manifest generation. |
| `tools/local-ai/tests/*.Tests.ps1` | Pester coverage for every policy boundary and workflow. |
| `tools/local-ai/README.md` | Setup, command examples, threat model, and recovery guidance. |

Runtime data is not stored in tracked source. The default local state root is `.local-ai/`, ignored by Git, with one directory per task containing `task.json`, an append-only event log, model requests/responses, findings, and handoff files. Worktrees live outside the primary checkout in a configured local worktree root.

## 5. Configuration and credentials

Checked-in configuration contains no secrets. Runtime configuration resolves in this order: explicit command parameters, an ignored local configuration file, then safe defaults.

Required defaults:

```powershell
@{
    OpenWebUiBaseUri = 'http://127.0.0.1:8080'
    ModelId = 'jacks-assistant'
    StateRoot = '.local-ai'
}
```

The API token is read from `OPENWEBUI_API_KEY` at invocation time. It is never written to task state, request captures, logs, handoffs, Git configuration, or console diagnostics. The API base URI must resolve to loopback (`127.0.0.1`, `localhost`, or `::1`) in the first release. HTTP calls use a finite timeout, redact authorization headers, and surface a concise error when Open WebUI is unavailable or returns malformed output.

## 6. Task model and state machine

Every task receives an immutable ID, a filesystem-safe slug, a mode (`investigation` or `implementation`), the baseline commit, and timestamps. State updates are atomic and evented.

```text
created
  -> context_ready
  -> awaiting_model
  -> model_complete
  -> awaiting_review
  -> accepted

Any nonterminal state -> failed
Any pre-acceptance state -> cancelled
```

Implementation tasks additionally record branch name, worktree path, changed files, validation/build results, and deployment results. Investigation tasks cannot enter an edit/build/deploy path. Invalid or skipped transitions fail closed.

The handoff includes the original goal, mode, baseline, worktree and branch when applicable, model findings, changed-file summary, validation evidence, deployment evidence, risks, unresolved questions, and explicit recommended human next actions. `accepted` records review only; it does not merge or push.

## 7. Context and model invocation

The bridge assembles bounded context from explicitly selected tracked text files, repository status, current branch/commit, relevant documentation, and the task request. It excludes `.git`, `.local-ai`, build outputs, binaries, archives, logs unless explicitly requested for investigation, secret/local files, and paths outside the approved repository/worktree.

The Open WebUI request uses `POST /api/chat/completions`, bearer authentication from `OPENWEBUI_API_KEY`, model ID `jacks-assistant`, and a messages array. The system message explains the task mode, allowed repository root, output contract, and that the model has no tools. Responses must be captured before downstream actions and parsed into a structured result. An unparseable response becomes `failed`; it is never interpreted as permission to execute text as PowerShell.

For investigation mode, the response contract contains findings, evidence with repository-relative paths, uncertainties, and recommended next steps. For implementation mode, later plan tasks may define a constrained change-manifest contract; no model-generated command text is executed.

## 8. Worktree isolation

Implementation work starts only from a clean, identified baseline commit. The bridge creates a task-specific branch and linked worktree beneath the configured worktree root, verifies both with Git, and stores their resolved absolute paths. It rejects a worktree path inside the main checkout, an existing nonempty destination, an unresolvable baseline, or a repository with unsafe ambiguity.

All AI-authored source edits occur inside that worktree. The primary checkout remains untouched. Investigation-only tasks do not create a worktree unless a read-only checkout is later shown to be necessary.

Cleanup is always an explicit, separate human action. The bridge does not delete a worktree or branch automatically, including after failure.

## 9. Build, validation, and deployment policy

The bridge exposes named operations, not arbitrary commands:

- repository validation: `python .\tools\validate-repo.py`;
- APWorld packaging: `.\tools\build-apworld.ps1` when relevant;
- client build through the repository's approved client build path;
- client deployment through a bridge wrapper that enforces the destination boundary.

Before client deployment, the wrapper resolves `<GameDir>` and the final destination, then requires the destination to be exactly:

```text
<GameDir>\BepInEx\plugins\RhythmCastleAP
```

It rejects traversal, wildcards, unresolved paths, symlink/reparse escapes, the `BepInEx\core` directory, sibling plugin directories, and any arbitrary destination. Only expected client artifacts from the task worktree's Release output may be copied. BepInEx, Harmony, and Il2CppInterop assemblies remain excluded. Deployment records source hashes, destination paths, and time in task state.

Because the existing `client/build.ps1` both builds and installs, implementation must either add a non-deploy build mode to that script or invoke `dotnet build` separately and keep deployment in the restricted wrapper. The bridge must not weaken the current manual build workflow.

## 10. Development-session log watcher

Log watching is opt-in and tied to an explicit task development session. Starting it records the task ID, resolved log path, initial file position, filters, and process identity. It watches only `<GameDir>\BepInEx\LogOutput.log`, begins at the end by default, and writes filtered captures beneath that task's ignored state directory.

The watcher stops when the user stops the session, its owning PowerShell process exits, the configured timeout expires, or the task becomes terminal. It is not installed as a service, scheduled task, startup item, or persistent background daemon. It does not launch or control the game. Logs remain test artifacts and are never committed.

## 11. Review, merge, and push boundary

The bridge may produce read-only review information: status, diff summary, changed files, test results, commit suggestions, and handoff documentation. The human owner decides whether to accept changes.

The bridge never automatically:

- stages or commits changes unless a future command explicitly adds that separately;
- merges the task branch;
- rebases the task branch;
- pushes any branch or tag;
- opens or updates a merge request;
- deletes branches or worktrees.

Any future Git mutation beyond isolated branch/worktree creation requires a new design decision and explicit command boundary.

## 12. Failure handling and auditability

Operations fail closed. A failure records a redacted error, operation name, timestamp, and last valid state. Interrupted writes use a temporary file in the same task directory followed by an atomic replace. Existing task artifacts are never silently overwritten.

Each consequential operation emits an event. Sensitive values and raw authorization headers are prohibited from events. Model prompts and responses may be retained locally for reproducibility, but must pass secret redaction and remain under ignored local state.

## 13. First end-to-end acceptance test

The first acceptance task is investigation-only:

```text
Investigate Level 4 glasses / Minim progression.
```

It must prove that the bridge can:

1. create and transition an investigation task;
2. capture the clean repository baseline;
3. assemble bounded context covering the Level 4 glasses, Minim trade, and Chicken Bucket roadmap;
4. call `http://127.0.0.1:8080/api/chat/completions` using model `jacks-assistant`;
5. save structured findings with evidence and uncertainties;
6. generate a reviewable handoff;
7. leave tracked source, Git branches/worktrees, build outputs, the game install, and plugin deployment unchanged.

Only after this passes should the project owner authorize an implementation acceptance task that exercises edit, validate, build, restricted deployment, and log watching.

## 14. Acceptance criteria

The bridge design is satisfied when tests demonstrate loopback-only authenticated API access, exact model selection, secret redaction, valid state transitions, investigation/implementation separation, worktree containment, strict deployment containment, session-bound log watching, atomic artifacts, and the absence of automatic merge or push behavior.

