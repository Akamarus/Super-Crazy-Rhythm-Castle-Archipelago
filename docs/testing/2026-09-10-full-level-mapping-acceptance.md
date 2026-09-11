# Full level mapping v0.24 / v0.70 acceptance record

## Candidate scope

- Candidate versions: Client v0.70.0 / APWorld v0.24.0.
- A fresh v0.24 seed and fresh native save are required.
- Expected addressed totals: Normal 121 / Hard 179 / Expert 237 / Perfection 273.
- All 22 normal campaign identities are mapped. A successful normal result sends separate Completion and enabled cumulative Star locations; an improved result sends only newly satisfied locations.
- Development Caches are retired from new seeds, while their historical IDs remain reserved.
- Bee and Devil handling is diagnostic-only and read-only. Special-mode locations, all 66 AP Stars, generated Star gates, and final Victory remain inactive.
- Automated coverage does not substitute for gameplay. Broad campaign replay is deferred; unplayed mappings remain manual-testing pending.

## Automated expectations

Run before manual play:

```powershell
py -m unittest discover apworld/tests -v
py .\tools\validate-repo.py
.\client\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus" -SkipInstall
```

Expected validator frontiers are item `187256156` and location `187256292`. Do not deploy, install, publish, or mark a manual scenario passed from automated output alone.

## Automated evidence — 2026-09-10

- Candidate commit verified: `a8b5494` (`fix: isolate special variant diagnostic tests`), full hash `a8b5494726c21fe94ffbc6928a146c40cf069691` at the time of verification.
- Pure client matrix: **20/20** Release projects passed, including `SpecialVariantDiagnostic` (67 assertions).
- Undeployed client build: `client/bin/Release/net6.0/RhythmCastleAP.dll` exists; `-SkipInstall` completed with **0 errors** and **7 existing nullable warnings**. Build output explicitly reported `Build complete; installation skipped.` No game-directory copy or deployment occurred.
- Complete APWorld suite: **102/102** tests passed (`Ran 102 tests in 18.950s`, `OK`).
- Repository validator: passed. It reported Client `v0.70.0`, APWorld `v0.24.0`, campaign locations `88` (new `81`, IDs `187256211`–`187256291`), active totals Normal `121` / Hard `179` / Expert `237` / Perfection `273`, and `special_variant_locations_active: false`; safe frontiers were item `187256156` and location `187256292`.
- Local package: `dist/scrc.apworld` built successfully without installation or publication.
- SHA-256: client DLL `20B76C48FFC5EB2D9C16E84BDAAD6DED485B666C8114513722C0D40C3EAAB262`; APWorld package `7127AB41BA1B1DCF79EE1C1840268CF9872D6703DDCC1605D5F924A521CE3F99`.
- Final pre-evidence inspection: `git diff main...HEAD --check` passed with no output; `git status --short` was clean; `main..HEAD` contained the approved 15 candidate commits through `a8b5494`, with no tracked generated binaries, logs, or packages.

## Manual evidence — leave unchecked until recorded

- [ ] Commit:
- [ ] Client SHA-256:
- [ ] APWorld SHA-256:
- [ ] Seed name:
- [ ] AP slot:
- [ ] Native save slot:
- [ ] AP difficulty:
- [ ] Normal first-clear locations:
- [ ] Improved-result locations:
- [ ] Offline/reconnect result:
- [ ] Bee diagnostic log excerpt:
- [ ] Devil diagnostic log excerpt:
- [ ] Unplayed campaign mappings:
- [ ] Tester verdict:

## Manual-session limits

Record the first clear and improved result against the exact active seed locations, verify an offline result queues once and reconnect sends it once, and attach diagnostic excerpts without inferring any special-mode mapping. Do not treat absent evidence as a pass. The planned broad replay remains deferred for later testers.
