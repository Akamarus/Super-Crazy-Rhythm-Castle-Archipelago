# Public Testing Documentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the GitHub repository usable by public development testers through an immediate README TL;DR, a simplified roadmap, precise source-build/install instructions, clear experimental limitations, and an actionable issue-reporting guide.

**Architecture:** The root README is the concise landing page and routes readers to focused documents. `docs/INSTALL.md`, `docs/ROADMAP.md`, and `docs/TESTING_AND_ISSUES.md` each own one responsibility; component READMEs retain developer-specific details and link back to the public guides. Published wording must distinguish the implemented v0.67.59/v0.15 baseline from the approved future design.

**Tech Stack:** GitHub-flavored Markdown, PowerShell build helpers, BepInEx 6 IL2CPP, .NET 6 SDK, Archipelago 0.6.7 custom APWorld workflow, repository validator.

**Spec:** `docs/superpowers/specs/2026-08-20-randomizer-logic-design.md`

## Global Constraints

- Preserve the existing AI-development disclaimer and state that this is an unofficial, experimental development build.
- Do not claim that the approved 66-Star, Music Lab Point, cassette-item, quest-item, victory, local-co-op, DeathLink, overlay, or online-co-op designs are implemented.
- Current testable versions are client `0.67.59`, APWorld `0.15`, and slot-data implementation `area-routing-plant-pipes-0.15`.
- Current APWorld v0.15 forces Roots as the starter and requires a freshly generated seed after APWorld changes.
- Client installation targets only `<GameDir>\BepInEx\plugins\RhythmCastleAP`.
- Never instruct testers to commit or publicly attach passwords, API keys, full save folders, game binaries, BepInEx binaries, or proprietary IL2CPP assemblies.
- Publish only confirmed public-facing files.
- Do not merge the publishing pull request automatically.

---

### Task 1: GitHub Landing Page and Simplified Roadmap

**Files:**
- Modify: `README.md`
- Create: `docs/ROADMAP.md`
- Modify: `docs/PROJECT_OVERVIEW.md`
- Modify: `docs/PROGRESSION.md`
- Create: `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md` (already prepared in the working tree)

**Interfaces:**
- Consumes: current v0.67.59/v0.15 behavior from component READMEs and the approved architecture from the design spec.
- Produces: stable landing-page links and implemented/planned terminology used by the install and testing guides.

- [ ] **Step 1: Rewrite the top of `README.md` as a tester TL;DR**

Add, before detailed developer content:

```markdown
> [!WARNING]
> This is an experimental development build, not a release. Back up your save and expect incomplete logic.

## TL;DR

- Playable now: Roots-first APWorld v0.15 with Area Access routing, randomized Game Garage cartridges, Weed Killer, Plant Pipes, campaign checks for Levels 1–3, Music Lab cassette medal checks, and nine Music Lab chest checks.
- Not finished: full-game logic, generated AP Stars, randomized cassettes/quest items, victory, balanced item pool, verified local co-op, and release packaging.
- Testers currently build both components from source and must generate a fresh seed with the matching APWorld.
- Start with the installation guide, then use the testing/reporting checklist when something breaks.
```

Link `docs/INSTALL.md`, `docs/ROADMAP.md`, `docs/TESTING_AND_ISSUES.md`, `docs/PROJECT_OVERVIEW.md`, and the randomizer design spec directly beneath it.

- [ ] **Step 2: Add a simplified roadmap to `README.md`**

Use four compact phases with visible status:

```markdown
## Simplified roadmap

1. Current playable prototype — Roots routing, Weed Killer, Plant Pipes, Garage/cassette/chest checks.
2. Native discovery — remaining areas, cassette sources, quest items, characters, multiplayer/versus behavior.
3. Full randomizer logic — Stars, Music Lab Points, level requirements, item pool, Level 22 victory.
4. Player features — verified local co-op, integrated AP log, DeathLink, then low-priority online co-op.
```

Link to `docs/ROADMAP.md` for status tables and acceptance gates.

- [ ] **Step 3: Create the detailed roadmap**

Write `docs/ROADMAP.md` with sections for current implementation, native-discovery gate, generation gate, client/system gate, gameplay acceptance gate, and deferred features. For every row, use one of `Implemented`, `Implemented / needs more testing`, `Design approved / not implemented`, `Discovery required`, or `Deferred`.

- [ ] **Step 4: Reconcile living project documentation**

Keep `docs/PROJECT_OVERVIEW.md`, `docs/PROGRESSION.md`, and `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md` consistent with the public roadmap. Preserve historical facts while clearly marking the Area Access pivot and future approved design.

- [ ] **Step 5: Verify landing-page claims**

Run:

```powershell
rg -n "TL;DR|Simplified roadmap|experimental development build|0\.67\.59|0\.15|not implemented|INSTALL\.md|TESTING_AND_ISSUES\.md" README.md docs/ROADMAP.md
```

Expected: every required phrase/link appears and no future feature is described as currently playable.

### Task 2: Precise Client and APWorld Installation

**Files:**
- Create: `docs/INSTALL.md`
- Modify: `client/README.md`
- Modify: `apworld/README.md`
- Modify: `apworld/scrc/docs/setup_en.md`

**Interfaces:**
- Consumes: `client/build.ps1`, `tools/build-apworld.ps1`, `client/Plugin.cs` configuration keys, `apworld/examples/SCRC-AreaRouting-PlantPipes.yaml`, and official BepInEx/Archipelago setup workflows.
- Produces: one canonical public install flow linked by component-specific developer instructions.

- [ ] **Step 1: Document prerequisites and safety**

List Windows, a legally installed PC copy of Super Crazy Rhythm Castle, BepInEx 6 IL2CPP `win-x64`, first-launch interop generation, .NET 6 SDK, PowerShell, Git, and Archipelago 0.6.7 or a compatible newer local install. Explain that custom APWorlds execute code and should be built from or obtained from a trusted source.

- [ ] **Step 2: Document the client build and install exactly**

Use:

```powershell
git clone https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago.git
cd .\Super-Crazy-Rhythm-Castle-Archipelago\client
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\build.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Titus"
```

Explain alternate Steam library paths, that the current baseline build always installs, the exact plugin destination, expected `BepInEx\LogOutput.log` version line, and that the script removes stale plugin-local DLLs but not BepInEx core files.

- [ ] **Step 3: Document first-run client configuration**

Point testers to `BepInEx\config\jack.rhythmcastle.archipelago.cfg` after one launch and document these exact current-development values:

```ini
[Archipelago]
Enabled = true
Server = HOST:PORT
Slot = SLOT_NAME
Password =
ApplyReceivedProgression = true

[QualityOfLife]
DirectStartAtPhoneHub = true
DirectStartAtLevelOne = false

[Developer]
EnableAreaAccessPrototype = true
PrototypeStartingArea = AP
```

State that `RandomizeEarlyProgression` is a retired per-level prototype and should remain false with Area Access enabled. Explain that configuration field names are case-sensitive only as represented by BepInEx and should not be renamed.

- [ ] **Step 4: Document APWorld build, installation, generation, and hosting**

Use:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\tools\build-apworld.ps1
```

Document `dist\scrc.apworld`, Archipelago Launcher’s `Install APWorld` action (or double-click/drag onto the launcher), restart, template generation, the included example YAML, local generation, `AP_XXXXX.zip`, local or website hosting, and the room’s host/port/slot/password values. State that custom worlds generate locally and hosted seeds may be uploaded afterward.

- [ ] **Step 5: Document update and uninstall procedures**

Require replacing both components together, restarting Archipelago after APWorld replacement, and generating a fresh seed. For uninstall, remove only `BepInEx\plugins\RhythmCastleAP` and the installed `scrc.apworld`; preserve saves and BepInEx.

- [ ] **Step 6: Update component READMEs and AP setup page**

Make `docs/INSTALL.md` canonical. Keep build-developer details in component READMEs, correct stale `v0.67.58` to `v0.67.59`, and link back to the public guide.

- [ ] **Step 7: Verify documented commands against repository files**

Run:

```powershell
Test-Path .\client\build.ps1
Test-Path .\tools\build-apworld.ps1
Test-Path .\apworld\examples\SCRC-AreaRouting-PlantPipes.yaml
rg -n 'Config\.Bind\("Archipelago"|Config\.Bind\("QualityOfLife"|Config\.Bind\("Developer"' .\client\Plugin.cs
```

Expected: all paths exist and every documented key matches the source.

### Task 3: Public Testing and Issue Reports

**Files:**
- Create: `docs/TESTING_AND_ISSUES.md`
- Modify: `docs/TESTING.md`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: README’s implemented-feature boundary, install guide paths, current version constants, and existing developer tests.
- Produces: a tester-facing smoke test and a copyable issue-report template.

- [ ] **Step 1: Write the public smoke test**

Cover matched versions, a fresh seed/save, Hub6 start, Roots phone, Gecko/Weed Killer, Level 3 Frog/Hippo partial-level exit, Plant Pipes receipt, Level 3 completion, one Garage cartridge test, one Music Lab cassette medal, and one Music Lab chest reconciliation check.

- [ ] **Step 2: Document known limitations**

State that Roots is forced, full-game logic and victory are incomplete, AP Stars and randomized Music Lab Points are design-only, native difficulty toggle changes are not implemented, local co-op is unverified, online co-op/DeathLink/overlay are deferred, and developer hotkeys may exist.

- [ ] **Step 3: Add a copyable issue-report template**

Require:

```markdown
### Versions
- Client:
- APWorld:
- Archipelago:
- Game platform/store:

### Seed and configuration
- Slot-data implementation tag:
- Fresh seed after APWorld update: yes/no
- Starting area:
- Relevant non-secret config values:

### Reproduction
1.
2.
3.

### Expected behavior

### Actual behavior

### Attachments
- Relevant excerpt or zipped copy of BepInEx/LogOutput.log
- Screenshot/video
- YAML used to generate the slot
- Seed/room link or generated seed name, if safe to share
```

Tell reporters to include the first `[SCRC-AP]` version line, connection/login lines, the action and approximately 30 seconds before/after it, and exact level/song names. Tell them to redact passwords, API keys, private server addresses when necessary, personal paths, and unrelated player information. Never request game DLLs or proprietary files.

- [ ] **Step 4: Link developer testing without overwhelming public testers**

Keep `docs/TESTING.md` as the detailed developer checklist and add a prominent link to the public guide. Link back from the public guide for advanced diagnostics.

- [ ] **Step 5: Update the changelog**

Add an `Unreleased — Public testing documentation` section describing documentation only; do not imply a client/APWorld version bump or release artifact.

- [ ] **Step 6: Verify issue guidance and disclaimer**

Run:

```powershell
rg -n "LogOutput\.log|Password|redact|YAML|fresh seed|experimental|AI-generated|local co-op|online co-op|DeathLink" README.md docs/INSTALL.md docs/ROADMAP.md docs/TESTING_AND_ISSUES.md CHANGELOG.md
```

Expected: tester evidence, secret-redaction guidance, limitations, and the disclaimer are present.

### Task 4: Repository Verification and GitHub Publication

**Files:**
- Verify: all confirmed documentation files from Tasks 1–3

**Interfaces:**
- Consumes: completed documentation and approved GitHub publication scope.
- Produces: one pushed feature branch and one draft pull request; no merge.

- [ ] **Step 1: Run repository validation**

Run with the available Python runtime:

```powershell
python .\tools\validate-repo.py
git diff --check
```

Expected: repository validation passes and `git diff --check` produces no output.

- [ ] **Step 2: Inspect final scope**

Run:

```powershell
git status --short
git diff -- README.md CHANGELOG.md client/README.md apworld/README.md apworld/scrc/docs/setup_en.md docs/INSTALL.md docs/ROADMAP.md docs/TESTING.md docs/TESTING_AND_ISSUES.md docs/PROJECT_OVERVIEW.md docs/PROGRESSION.md docs/HISTORICAL_GAMEPLAY_EVIDENCE.md docs/superpowers/plans/2026-08-20-public-testing-documentation.md
```

Expected: only confirmed public/testing documentation is selected for publication.

- [ ] **Step 3: Commit only confirmed paths**

Stage every confirmed path explicitly and commit:

```powershell
git commit -m "docs: add public testing and install guides"
```

- [ ] **Step 4: Push the feature branch**

Push `docs/public-testing-guide` to `origin` and verify the remote branch exists.

- [ ] **Step 5: Open one draft pull request**

Create a draft pull request from `docs/public-testing-guide` to `main` titled `docs: add public testing and install guides`. Summarize the README TL;DR, install guide, roadmap, issue template, disclaimer, verification evidence, and experimental-status boundary. Do not merge it.
