# Repository AI Development Rules

- Read `docs/PROJECT_OVERVIEW.md`, `docs/IDS.md`, `docs/PROGRESSION.md`, `docs/WORKFLOW.md`, and `docs/TESTING.md` before changing player-facing behavior.
- Never reuse or guess Archipelago IDs. Do not allocate IDs until native mapping and implementation scope are confirmed.
- Keep AI implementation changes in isolated Git worktrees. Preserve the main checkout as the accepted baseline.
- Never automatically merge, rebase, push, open a merge request, delete a branch, or remove a worktree.
- Build the client without deployment first. Deploy only with explicit confirmation to `<GameDir>\BepInEx\plugins\RhythmCastleAP`.
- Treat `BepInEx\LogOutput.log` as a local test artifact. Never commit logs, local task state, prompts/responses, credentials, game DLLs, generated plugin binaries, or `.apworld` packages.
- Update living project documentation in the same commit as any player-facing randomizer behavior change.
- Automated checks are necessary but gameplay testing is the final acceptance criterion for client behavior.
