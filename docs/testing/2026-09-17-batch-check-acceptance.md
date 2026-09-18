# Batch check expansion acceptance — v0.74.0 / world0.27

## Candidate boundary

37 new active checks: 31 native source/action flags and six successful special-level completions. Existing quest, character, Garage and notification behavior remains enabled. These checks are implemented candidates, not gameplay accepted. Native rewards remain available; no new item randomization is claimed. The first broad run focuses on check identity, one-time rewards, native quest continuity, and source persistence.

Native flags were audited against installed enum metadata and serialized object paths. All six special variants have binary success evaluation and no star thresholds. Client reads CurrentPlayerSaveEnquiries.HasBinaryResultLevelVariantBeenCompleted with exact level/variant identifiers; failure or missing data cannot award completion. Existing normal result checks remain separate.

## Setup once

1. Package and verify matching candidate client and APWorld. Obtain deployment approval for the concrete build. Back up plugin/config, native saves, and retained server state before any install or test-save reuse.
2. Generate a separate test seed; retain the existing seed and save backup. Use Normal, Roots start and all six area-access items plus Weed Killer, Plant Pipes, Hip Glasses, Chicken Bucket, Hypno Pan, Violance and Plunger in start inventory to reduce unrelated progression waits. Native quest items, character grants and special-mode keys remain acquired through their normal sequences unless an existing AP item legitimately supplies them.
3. Connect to the new seed before recreating/loading slot4. Verify all37 native source reads are available on the fresh save and expanded check binding succeeds. A named completed/unreadable source diagnostic is a stop condition for investigating setup, not a reason to overwrite native flags.
4. Preserve the gameplay log and server log throughout. Check counts and exact sources in logs after each area, not after every interaction. Existing checks may fire normally alongside new ones.

## Area-ordered run

Follow native quest order within each area. The table is an audit inventory, not permission to bypass quest prerequisites. For every row: native reward/consumption and route must still work; one AP source check should arrive; retrying/revisiting must not award twice. The 36 restricted additions award Stardust; inspect the generated spoiler for Combo Bucket's reward before testing. Count base result/cassette checks separately.

| Done | Check | ID |
| --- | --- | --- |
| [ ] | Roots - Combo Bucket Conversion | 187256298 |
| [ ] | Lobby - Important Letters Delivery | 187256299 |
| [ ] | Lobby - Plunger Hand-In | 187256300 |
| [ ] | Lobby - Star Eater Fed | 187256301 |
| [ ] | Lobby - Fish Tears Delivery | 187256302 |
| [ ] | Lobby - Important Letters Pickup | 187256292 |
| [ ] | Lobby - Demolition Certificate Award | 187256315 |
| [ ] | Lobby - Use Bunker Keycard | 187256325 |
| [ ] | Meat Dimension - Act 1 Music Delivery | 187256303 |
| [ ] | Meat Dimension - Act 2 Music Delivery | 187256304 |
| [ ] | Meat Dimension - Act 3 Music Delivery | 187256305 |
| [ ] | Meat Dimension - Act 4 Music Delivery | 187256306 |
| [ ] | Meat Dimension - Return Cat | 187256307 |
| [ ] | Meat Dimension - Return Scruffy | 187256308 |
| [ ] | Meat Dimension - Mouse Revolution | 187256309 |
| [ ] | Meat Dimension - Wooden Spoon Pickup | 187256316 |
| [ ] | Meat Dimension - Saw Disc Pickup | 187256317 |
| [ ] | Meat Dimension - Frying Pan Pickup | 187256318 |
| [ ] | Meat Dimension - Fish Tears Award | 187256319 |
| [ ] | Cell Tower - Deliver Super Nectar | 187256310 |
| [ ] | Cell Tower - Meet the Bees | 187256320 |
| [ ] | Tower of Fear - Restore Eye Statue | 187256311 |
| [ ] | Tower of Fear - Restore Mind Statue | 187256312 |
| [ ] | Tower of Fear - Restore Heart Statue | 187256313 |
| [ ] | Tower of Fear - Eye Pickup | 187256321 |
| [ ] | Tower of Fear - Mind Pickup | 187256322 |
| [ ] | Tower of Fear - Heart Pickup | 187256323 |
| [ ] | Royal Corridor - Star Eater Fed | 187256314 |
| [ ] | Royal Corridor - Bunker Keycard Award | 187256324 |
| [ ] | Secret Bunker - Gecko Interaction | 187256326 |
| [ ] | Secret Bunker - Star Eater Fed | 187256327 |
| [ ] | Nectar Party - Completion | 187256328 |
| [ ] | Act 1B: Nectar - Completion | 187256329 |
| [ ] | Demonic Room - Completion | 187256330 |
| [ ] | Demonic Tower - Completion | 187256331 |
| [ ] | Demonic Escape - Completion | 187256332 |
| [ ] | Demonic Lockers - Completion | 187256333 |

Perform one ordinary full game restart midway and one at the end. Reconnect before loading; verify completed sources stay completed and consumed native quest items do not reappear. For the special levels, record a failed attempt and a successful clear: failure must not award the supplemental check; success must award it once, without base-level Completion/Star-tier duplicates. Reload after a successful special clear to verify saved binary success recovery.

Roots feeding needs3 native stars; the audited Lobby/Royal/Bunker asset thresholds are15/40/66. Native totals are not consumed. Test the Star Eaters when the run reaches their native requirements; no threshold bypass is part of this candidate. Bunker access follows the native Keycard and Demon Key routes. Do not infer Bunker reachability from its organizational AP region.

Save/seed isolation gameplay testing remains deferred at the user's request. Automated isolation tests still run. This run is not final solver-valid AP Star progression acceptance.

## Entries not implemented separately in this batch

- Hypno Pan creation: AP ability ownership is not an independent recipe-source marker; replacing the native reward can trap the player behind dogs.
- Bean Trumpet independent award/randomized item: Letters pickup is one shared interaction; the native Bean Trumpet remains available. Reserved separate source293 stays inactive.
- Bizzle and Clive independent acquisition checks: one Meet the Bees check represents their shared native conversation.
- Bizzle/Clive switches: no audited persistent switch completion marker; native toggle/cooldown is not a one-time source.
- Super Nectar component awards: no separate durable collected marker verified; the two Bee completions and later delivery are implemented, without duplicate component checks.
- King Ferdinand independent character randomization: native character unlock remains vanilla; existing Level22 check and new Keycard reward source remain distinct.
- Notepad/Data Stick separate checks: both native rewards are part of the single Bunker Gecko interaction check.
- Demon Key acquisition/use: only inventory ownership has been mapped; independent durable source/use completion remains unproven.
- Final demon interaction: exact independent source and relationship to the existing cartridge check remain unresolved.
- Overall Totem completion remains an automatic consequence of the final statue restoration, not an extra check.

## Capacity accounting

Normal contains162 addressed locations and71 non-filler items, with89 locations permitted to hold progression. Existing68 progression items plus66 future Stars need134 such locations, leaving45 progression-safe locations/rule upgrades still required. This is distinct from raw item-slot capacity. Merely adding Stardust-only checks cannot enable AP Stars. Later route modeling can promote existing/new checks when their actual prerequisites are proven.

## Verification record

Automated test/build results and package hashes are recorded in the final candidate manifest. Manual acceptance remains pending deployment and the above run. No game, server or save changes were made for implementation.

## Broad gameplay pass outcome (v0.74.2)

The user completed33 of37 additions; each produced one server check. Demonic Room was confirmed. The user explicitly skipped Demonic Tower, Demonic Escape, Demonic Lockers and the Demolition Certificate level. Keep those four marked implemented but manually unverified; do not require replay for this pass. Royal and Bunker feed events were accepted using the user-approved25-star test thresholds, not their native40/66 requirements. Failed-special-attempt and save/seed isolation acceptance remain unverified. Local evidence is retained outside the repository in the task workspace work/batch074-test. No release or AP Stars activation follows automatically from this outcome.
