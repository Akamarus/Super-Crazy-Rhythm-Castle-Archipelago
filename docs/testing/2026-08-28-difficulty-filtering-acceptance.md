# v0.20 Difficulty Filtering Acceptance

**Client:** `0.67.94` (unchanged)

**APWorld:** `0.20.0`
**Implementation:** `area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20`

This record is for real Archipelago generator evidence. Existing v0.19 seeds retain their prior location sets; each row below requires a fresh v0.20 seed. Do not mark a row complete until its generated archive and spoiler audit are recorded.

| Difficulty | Expected addressed locations | Campaign tiers | Medal tiers | Generated |
| --- | ---: | --- | --- | --- |
| Normal | 68 | 1 | Bronze | [ ] |
| Hard | 105 | 1–2 | Bronze–Silver | [ ] |
| Expert | 142 | 1–3 | Bronze–Gold | [ ] |
| Perfection | 178 | 1–3 | Bronze–Platinum | [ ] |

## Commands and seed matrix

Package the current APWorld, then create one isolated player directory per row from `apworld/examples/SCRC-AreaRouting-PlantPipes.yaml`, replacing `difficulty` with the row's value.

```powershell
$env:PYTHONDONTWRITEBYTECODE = '1'
py -m unittest discover apworld/tests -v
py tools/validate-repo.py
.\tools\build-apworld.ps1
& 'C:\ProgramData\Archipelago\ArchipelagoGenerate.exe' `
  --player_files_path $players `
  --outputpath $output `
  --seed $seed `
  --spoiler 3 `
  --log_level warning
```

| Difficulty | Seed | Output archive name | Spoiler file / findings | Progression-placement result |
| --- | ---: | --- | --- | --- |
| Normal | 42001 | Pending | Pending | Pending |
| Hard | 42002 | Pending | Pending | Pending |
| Expert | 42003 | Pending | Pending | Pending |
| Perfection | 42004 | Pending | Pending | Pending |

## Required spoiler audit for every row

- [ ] Addressed-location count matches the row above.
- [ ] Only the listed campaign and medal tiers appear; inactive checks are absent, not filled with Stardust.
- [ ] Music Lab point chests contain only Stardust.
- [ ] Active Level-22 2-Star and 3-Star checks contain only Stardust.
- [ ] Garage progression appears only on a logically reachable matching-cartridge route.
- [ ] The generated playthrough reaches Victory.

Native REG/PRO remains player-controlled. This release filters only existing campaign performance locations; it adds no campaign checks or IDs.
