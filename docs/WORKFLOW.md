# Repository Workflow

## Suggested branch model

Keep `main` as the last accepted/tested baseline. Create a short-lived branch for each milestone, for example:

```text
feature/roots-level4-glasses
fix/roots-phone-visual-overlap
feature/star-requirements
```

After local compilation and gameplay testing, merge the branch into `main`.

## Commit style

Prefer small commits that describe the actual behavior change, for example:

```text
feat(apworld): add Plant Pipes progression item
feat(client): randomize Frog/Hippo Plant Pipes reward
fix(client): bypass displaced Roots intro cutscene
 docs: record permanent AP IDs
```

## Working with ChatGPT

For future development, provide the GitLab repository URL and the branch/commit that should be treated as the baseline. A public repository can be read directly. If the repository is private and no authenticated GitLab connector is available, provide a source archive or the relevant files/diff in the conversation.

For runtime bugs, upload `BepInEx\LogOutput.log` from the exact build/seed being tested.

Generated game DLLs, interop assemblies, credentials, and private tokens must not be added to the repository or uploaded as source artifacts.

## Controlled local AI workflow

The local AI bridge under `tools/local-ai/` uses PowerShell as its only execution and policy layer. Open WebUI is restricted to `http://127.0.0.1:8080` with model ID `jacks-assistant`; the model has no terminal or direct filesystem access.

Investigation tasks operate read-only and produce local findings/handoffs. Implementation tasks use a task-specific branch and worktree outside the main checkout. Review the generated handoff and diff before accepting any result.

Bridge acceptance never performs Git integration. Merging, rebasing, pushing, merge-request creation, branch deletion, and worktree removal remain manual owner decisions. See `tools/local-ai/README.md` for command examples and recovery guidance.

## Living project documentation

Any milestone that changes player-facing randomizer behavior must update `docs/PROJECT_OVERVIEW.md` in the same commit. This includes new/renamed items or checks, progression requirements, area routing, difficulty rules, Star-gating behavior, and implemented-system status.

`docs/IDS.md` remains authoritative for permanent network IDs; `docs/PROGRESSION.md` remains the concise logic reference.
