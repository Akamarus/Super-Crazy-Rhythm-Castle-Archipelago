# Development workflow

Keep the accepted baseline separate from candidate work in an isolated Git worktree. Read AGENTS.md and the current overview, IDs, progression and testing documents before changing player-facing behavior. Preserve permanent network IDs and do not commit game assemblies, logs, credentials or generated distributions.

## Build and validate

From the repository root, with the .NET SDK and Python installed:

```powershell
./client/build.ps1 -GameDir "D:/SteamLibrary/steamapps/common/Titus" -SkipInstall
python tools/validate-repo.py
python -m unittest discover -s tools/tests
./tools/build-apworld.ps1
```

Replace the example game path with your installation. The client references your separately installed BepInEx/IL2CPP runtime. Download users do not need the SDK or Git.

Run regression projects relevant to the change and record native acceptance separately. Never deploy a candidate over the user's game without explicit confirmation. Prepare distributions with the allowlisted [packager](PACKAGING.md), including licenses, source provenance and checksums.

## Documentation and publication

Update the overview, changelog and current issue/testing pages with player-facing changes. Keep dated evidence under testing; move superseded development records to the indexed archive and repair links. Release notes describe what shipped; later acceptance belongs in the current test record.

Review the diff and package contents before requesting publication approval. Merging, pushing, changing public releases and removing worktrees require explicit authorization. Keep the AI disclosure accessible and include it with public release communications.

For collaboration, provide the GitHub URL and exact branch/commit; for runtime reports, provide the relevant log privately if it contains sensitive room details. Historical GitLab setup remains in the [archive](archive/checkpoints/GITLAB_SETUP.md).
