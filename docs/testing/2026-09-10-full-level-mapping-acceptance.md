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
