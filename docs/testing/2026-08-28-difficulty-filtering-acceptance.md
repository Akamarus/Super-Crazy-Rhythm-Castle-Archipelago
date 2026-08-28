# v0.20 Difficulty Filtering Acceptance

**Client:** `0.67.94` (unchanged)

**APWorld:** `0.20.0`
**Implementation:** `area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20`

This record is for real Archipelago generator evidence. Existing v0.19 seeds retain their prior location sets; each row below requires a fresh v0.20 seed. Do not mark a row complete until its generated archive and spoiler audit are recorded.

| Difficulty | Expected addressed locations | Campaign tiers | Medal tiers | Generated |
| --- | ---: | --- | --- | --- |
| Normal | 68 | 1 | Bronze | [x] |
| Hard | 105 | 1–2 | Bronze–Silver | [x] |
| Expert | 142 | 1–3 | Bronze–Gold | [x] |
| Perfection | 178 | 1–3 | Bronze–Platinum | [x] |

## Commands and seed matrix

Package the current APWorld, then create one isolated player directory per row from apworld/examples/SCRC-AreaRouting-PlantPipes.yaml, replacing difficulty with the row's value. The source generator was copied from ProgramData; every real command ran the copied executable from the TEMP runtime:

    $env:PYTHONDONTWRITEBYTECODE = '1'
    & 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest discover apworld/tests -v
    & 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' tools/validate-repo.py
    .\tools\build-apworld.ps1
    $tempRoot = Join-Path $env:TEMP 'scrc-task5-fix-round-1-20260828'
    $runtime = Join-Path $tempRoot 'runtime'
    Copy-Item 'C:\ProgramData\Archipelago\ArchipelagoGenerate.exe' $runtime
    Copy-Item 'dist\scrc.apworld' "$runtime\custom_worlds\scrc.apworld"
    & "$runtime\ArchipelagoGenerate.exe" --player_files_path $players --outputpath $output --seed $seed --spoiler 3 --log_level warning

| Difficulty | Seed | Output archive name | Spoiler file / findings | Progression-placement result |
| --- | ---: | --- | --- | --- |
| Normal | 42001 | AP_02319140527104995104.zip | AP_02319140527104995104_Spoiler.txt: 68 addressed; campaign 1-Star and Bronze only; all nine point chests Stardust; inactive 2/3-Star and Silver/Gold/Platinum absent; Victory in generated sphere 5. | PASS: Bloody Tears Cartridge at Music Lab Cassette - Another Day In Paradise - Bronze is state-reachable in sphere 1, so Garage Bloody Tears - Bronze → Hip Glasses is reachable in sphere 2. Vampire Killer Cartridge at Roots - Bucket Minion Trade is reachable in sphere 3, so Garage Vampire Killer - Bronze → Cell Tower Access is reachable in sphere 4. |
| Hard | 42002 | AP_30644990911972241484.zip | AP_30644990911972241484_Spoiler.txt: 105 addressed; campaign 1/2-Star and Bronze/Silver only; all nine point chests and active Level-22 2-Star are Stardust; inactive 3-Star and Gold/Platinum absent; Victory in generated sphere 3. | PASS: Gradius Remix Cartridge at Music Lab Cassette - Jolt City - Silver, Smooch Cartridge at Music Lab Cassette - Lets Go - Bronze, and Superstar Cartridge at Music Lab Cassette - Hollywood Trailer - Silver are each state-reachable in sphere 1; their Garage rewards (Chicken Bucket, Tower of Fear Access, and Wag the Dog Cartridge) are each reachable in sphere 2. |
| Expert | 42003 | AP_19513945163669142760.zip | AP_19513945163669142760_Spoiler.txt: 142 addressed; campaign 1/2/3-Star and Bronze/Silver/Gold only; all nine point chests and active Level-22 2/3-Star are Stardust; inactive Platinum absent; Victory in generated sphere 2. | PASS: Bloody Tears Cartridge at Music Lab Cassette - Gold - Bronze and Superstar Cartridge at Music Lab Cassette - Keep On Hustlin - Silver are state-reachable in sphere 1; Garage Bloody Tears - Bronze → Weed Killer and Garage Superstar - Gold → Wag the Dog Cartridge are reachable in sphere 2. |
| Perfection | 42004 | AP_54766107558037383335.zip | AP_54766107558037383335_Spoiler.txt: 178 addressed; campaign 1/2/3-Star and all four medal tiers; all nine point chests and active Level-22 2/3-Star are Stardust; Victory in generated sphere 2. | PASS: Superstar Cartridge at Music Lab Cassette - Flamenco - Platinum is state-reachable in sphere 1, so Garage Superstar - Bronze → Vampire Killer Cartridge is reachable in sphere 2. |

## Package and isolation evidence

- dist/scrc.apworld is 14,775 bytes with SHA-256
  E49DA69BFDEE41B439A9C1EFBCA0ED869D7A49E95029DB014CE742F875B19854.
- Its 10 archive members exactly match the 10 files under apworld/scrc
  after excluding bytecode caches. It has one scrc/ root, no
  __pycache__/.pyc, and scrc/archipelago.json reports
  world_version: 0.20.0, version: 7, and compatible_version: 7.
- Exact archive members:
  - scrc/__init__.py
  - scrc/archipelago.json
  - scrc/difficulty.py
  - scrc/docs/en_Super Crazy Rhythm Castle.md
  - scrc/docs/setup_en.md
  - scrc/items.py
  - scrc/options.py
  - scrc/placement.py
  - scrc/star_requirements.py
  - scrc/starting_areas.py
- The four seeds were generated under
  C:\Users\Jack\AppData\Local\Temp\scrc-task5-fix-round-1-20260828.
  The source C:\ProgramData\Archipelago\ArchipelagoGenerate.exe was
  copied to that TEMP runtime, and each command ran
  $runtime\ArchipelagoGenerate.exe. Player, output, runtime, YAML, log,
  archive, and audit paths were all under this TEMP root. The copied
  runtime loaded the newly built package from its own custom_worlds
  directory.
- Exact TEMP paths:
  - runtime:
    C:\Users\Jack\AppData\Local\Temp\scrc-task5-fix-round-1-20260828\runtime
  - Normal players/output:
    C:\Users\Jack\AppData\Local\Temp\scrc-task5-fix-round-1-20260828\Normal\players
    and
    C:\Users\Jack\AppData\Local\Temp\scrc-task5-fix-round-1-20260828\Normal\output
  - Hard players/output:
    C:\Users\Jack\AppData\Local\Temp\scrc-task5-fix-round-1-20260828\Hard\players
    and
    C:\Users\Jack\AppData\Local\Temp\scrc-task5-fix-round-1-20260828\Hard\output
  - Expert players/output:
    C:\Users\Jack\AppData\Local\Temp\scrc-task5-fix-round-1-20260828\Expert\players
    and
    C:\Users\Jack\AppData\Local\Temp\scrc-task5-fix-round-1-20260828\Expert\output
  - Perfection players/output:
    C:\Users\Jack\AppData\Local\Temp\scrc-task5-fix-round-1-20260828\Perfection\players
    and
    C:\Users\Jack\AppData\Local\Temp\scrc-task5-fix-round-1-20260828\Perfection\output
  - state audit:
    C:\Users\Jack\AppData\Local\Temp\scrc-task5-fix-round-1-20260828\state-reachability-audit.txt
- The installed C:\ProgramData\Archipelago\custom_worlds\scrc.apworld
  remained v0.19.0 and retained SHA-256
  CA6F456F67DF597C360ED11A890362CC5E6991C716CAFC4BF976C3D334E1AF62
  before and after the matrix. It was not replaced.

## Garage state-reachability method

The audit started with only the generated Roots Access starting item and
replayed the APWorld's current access rules one state sphere at a time:
Music Lab and Phone Hub checks are always reachable; every Garage check
requires its matching cartridge; Roots quest checks require their exact
Weed Killer, Plant Pipes, Hip Glasses, Chicken Bucket, and trade-event
states; Level 22 requires Royal Corridor Access; Victory requires all six
area accesses. Items from a sphere were added only after every location in
that sphere was evaluated. Every spoiler location reached the fixed point.
For every non-Stardust Garage reward, the matching cartridge source has a
strictly earlier sphere than the Garage check, proving independent logical
reachability and ruling out direct and indirect cartridge cycles.

## Required spoiler audit for every row

- [x] Addressed-location count matches the row above.
- [x] Only the listed campaign and medal tiers appear; inactive checks are absent, not filled with Stardust.
- [x] Music Lab point chests contain only Stardust.
- [x] Active Level-22 2-Star and 3-Star checks contain only Stardust.
- [x] Garage progression appears only on a logically reachable matching-cartridge route.
- [x] The generated playthrough reaches Victory.

## Verification result

On 2026-08-28, the complete APWorld suite passed 69 tests. Repository
validation reported Client v0.67.94 and APWorld v0.20.0. The package
contents check, four real-generator runs, spoiler audit, final quiet test
suite, repository validator, and whitespace/error diff check all passed.
The built package remains git-ignored. Real YAMLs, logs, archives, and the
state-reachability audit remain under the TEMP root above and were not
staged. The superseded dist/task-5-acceptance directory was removed after
its exact resolved worktree-local path and lib junction were verified.

Native REG/PRO remains player-controlled. This release filters only existing campaign performance locations; it adds no campaign checks or IDs.
