# Full Campaign and Special-Variant Level Mapping Design

**Status:** Approved design; normal campaign mapping is implementation-ready, while Bee/Devil production activation requires diagnostic evidence
**Date:** 2026-09-10

## 1. Purpose and authority

This specification defines the next level-mapping milestone for Super Crazy Rhythm Castle Archipelago. It expands the current partial campaign result map to all 22 numbered levels, separates normal completion from Star-performance locations, defines completion-only checks for the six known Bee/Devil variants, and establishes the access-item chains needed to make those special checks solver-valid.

This document narrows and supersedes the campaign-count wording in sections 5 and 12 of `docs/superpowers/specs/2026-08-20-randomizer-logic-design.md`. The broader Area Access, item lifecycle, Music Lab, Game Garage, AP Star, and victory decisions in that specification remain authoritative.

The historical playthrough in `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md` is evidence, not permission to guess native state. The normal campaign level IDs below are sufficiently established for implementation. Bee/Devil source, switch, inventory, seal, delivery, and final-reward flags must be captured diagnostically before permanent IDs or production mutations are added for those chains.

## 2. Goals

This design has five goals:

1. Give every normal campaign level a distinct completion location and cumulative Star-performance locations.
2. Give each known Bee and Devil variant one completion-only AP location.
3. Model `Bizzle and Clive` and `Demon Key` as real access items instead of treating special variants as area-only checks.
4. Preserve the complete Super Nectar and Demon Seal reward/use chains without forcing player interactions.
5. Replace synthetic Development Cache capacity with real, reportable checks while keeping the 66 AP Stars inactive until genuine Normal-difficulty capacity is sufficient.

## 3. Normal campaign catalog

The canonical campaign order is:

| Level | Player-facing name | Internal level | Area |
| ---: | --- | --- | --- |
| 1 | Light Humor | `Level_05` | Roots |
| 2 | Pop Party | `Level_06` | Roots |
| 3 | The Megafying Ritual | `Level_07` | Roots |
| 4 | DJ Eggplant | `Level_08` | Roots |
| 5 | Lift Quest | `Level_09` | Roots |
| 6 | Boring Room | `Level_02` | Lobby |
| 7 | Demolition Training | `Level_19` | Lobby |
| 8 | Minim Tower | `Level_11` | Lobby |
| 9 | School Trip | `Level_20` | Lobby |
| 10 | The Vault | `Level_01` | Lobby |
| 11 | Act 1: Flavor | `Level_12` | Meat Dimension |
| 12 | Act 2: Sauce and Spice | `Level_15` | Meat Dimension |
| 13 | Act 3: Montage | `Level_22` | Meat Dimension |
| 14 | Act 4: Habanero | `Level_23` | Meat Dimension |
| 15 | Central Mainframe | `Level_16` | Cell Tower |
| 16 | The Thief Prince | `Level_24` | Cell Tower |
| 17 | Cold Storage | `Level_21` | Lobby |
| 18 | The Darkness | `Level_03` | Tower of Fear |
| 19 | Escape | `Level_13` | Tower of Fear |
| 20 | Loneliness | `Level_25` | Tower of Fear |
| 21 | Locker Room | `Level_14` | Royal Corridor |
| 22 | King Ferdinand I | `Level_28` | Royal Corridor |

Cold Storage remains physically classified as Lobby until native routing proves otherwise. School Trip's three rooms are one numbered campaign level and one result identity.

## 4. Normal location model

Every numbered level defines four possible locations:

```text
Level X - Completion
Level X - 1 Star
Level X - 2 Stars
Level X - 3 Stars
```

Completion and 1 Star are distinct AP locations. A successful normal clear necessarily satisfies both, but they remain separate checks and separate item slots.

The AP difficulty option selects cumulative tiers:

| AP difficulty | Enabled locations per normal level | Campaign total |
| --- | --- | ---: |
| Normal | Completion, 1 Star | 44 |
| Hard | Normal tiers plus 2 Stars | 66 |
| Expert | Hard tiers plus 3 Stars | 88 |
| Perfection | Same campaign tiers as Expert | 88 |

The game's native Normal/Pro choice remains independent. A Pro clear is not a different AP location set.

For a successful normal result, the client sends Completion and every newly satisfied enabled cumulative Star tier. A first 2-Star result therefore satisfies Completion, 1 Star, and 2 Stars. Improving later from 2 Stars to 3 Stars sends only the still-unchecked 3-Star location. Failed, abandoned, missing-result, and zero-Star attempts send nothing.

## 5. Special-variant catalog

The six approved special-mode locations are:

| Location | Internal identity | Area | Known requirements |
| --- | --- | --- | --- |
| Bee Mode - Nectar Party - Completion | `Level_06` / `LevelVariant_BeeMode` | Roots | Roots Access, Bizzle and Clive, Hypno Pan |
| Bee Mode - Act 1B: Nectar - Completion | `Level_12` / `LevelVariant_BeeMode` | Meat Dimension | Meat Dimension Access, Bizzle and Clive |
| Devil Mode - Demonic Room - Completion | `Level_02` / `LevelVariant_DevilMode` | Lobby / Secret Bunker chain | Lobby Access, used Bunker Keycard, Demon Key |
| Devil Mode - Demonic Tower - Completion | `Level_11` / `LevelVariant_DevilMode` | Lobby / Secret Bunker chain | Lobby Access, used Bunker Keycard, Demon Key, Plant Pipes |
| Devil Mode - Demonic Escape - Completion | `Level_13` / `LevelVariant_DevilMode` | Tower of Fear / Secret Bunker chain | Tower of Fear Access, used Bunker Keycard, Demon Key |
| Devil Mode - Demonic Lockers - Completion | `Level_14` / `LevelVariant_DevilMode` | Royal Corridor / Secret Bunker chain | Royal Corridor Access, used Bunker Keycard, Demon Key, Plant Pipes |

These requirements are the approved minimum model. Additional requirements may be added only after diagnostic proof. A special-mode result sends exactly its completion location. Bee and Devil variants never send normal campaign Completion or Star locations and never create their own Star tiers.

Special checks exist on every AP difficulty once their production chains are activated, adding six locations to every difficulty.

## 6. Bizzle and Clive / Super Nectar chain

`Bizzle and Clive` is one combined AP progression item, not two items and not a character unlock. Its verified native source will become one AP source location. Receiving the item must enable the normal Bee Mode switch interactions beside eligible level doors; it must not force a variant open or launch a level.

The Bee variants are the physical sources for two separate AP progression items:

- Super Nectar Bubbles from Nectar Party.
- Super Nectar Recipes from Act 1B: Nectar.

Their direct native grants are suppressed while their durable completion/source markers send the corresponding Bee completion location. The AP items may be placed anywhere solver-valid. Receiving either item grants its real native bag state. Returning both remains a player-controlled vanilla interaction that consumes them normally, sends one distinct Super Nectar delivery location, and allows the native bee-escape consequence.

Source, held, consumed, delivery, reload, reconnect, and local-save-switch behavior must be proven before this chain is activated.

## 7. Demon Key / Demon Seal chain

`Demon Key` is one AP progression item obtained from the verified demon source location. Receiving it enables the normal Devil Mode switch interactions. It does not complete a seal, force a variant, or bypass the Bunker Keycard route.

Each of the four Devil variants sets or retains its normal native Demon Seal state and sends one completion location. Seal state is story/progression state, not four additional portable AP items. After all four seals, the normal final demon escape/reward event becomes one AP source location. If that event is the already-registered source for an existing cartridge, its existing permanent location is reused instead of creating a duplicate.

The exact demon source, switch, seal, final-event, and cartridge identities must be captured before production activation. Completed seals must survive reload and reconnect without duplicate sends.

## 8. Client result contract

The client will use a focused campaign-level catalog instead of extending the ad hoc `LocationMap.InternalToLocationName` dictionary. Each descriptor owns:

- internal level ID;
- player-facing number and name;
- logical area;
- the four normal location names.

Result evaluation accepts the internal level ID, exact variant ID, persisted successful-result data, and the active campaign tiers supplied by compatible slot data.

```text
Default variant + at least 1 Star
  -> Completion + enabled cumulative Star locations

Known Bee/Devil variant + verified successful completion
  -> exact special completion location only

Unknown or ambiguous variant
  -> diagnostic warning, no AP location
```

The client must not infer a default variant from an unknown non-empty value. It must not send a location absent from the connected datapackage. Ordinary Level 22 locations and Victory remain separate.

## 9. Slot data, compatibility, and persistence

The next campaign-mapping APWorld will publish a strict level-mapping sub-contract containing:

- a level-mapping schema version;
- active normal location tiers;
- the complete active normal location-name set or an equivalently strict catalog digest;
- whether special-mode locations are active;
- special-mode location names when active.

Older recognized seeds retain their existing partial level behavior. A seed claiming the new implementation with malformed level-mapping data fails closed for the new locations and reports an explicit compatibility error.

Location delivery remains idempotent through the existing AP checked-location set and pending queue. Results earned while disconnected queue and flush after reconnect. Any durable local journal used for result recovery is bound to game, seed, and AP slot. Switching native saves cannot cause cross-seed or cross-slot check leakage.

Native best-result reconciliation may restore missed normal tiers only when the saved level identity and Star count are readable unambiguously. Special-mode reconciliation requires verified native completion flags. Unsupported reconciliation logs a bounded diagnostic rather than guessing.

## 10. Level 22 and Victory

Level 22 follows the normal location policy. A clear can send Completion and cumulative Star-performance locations regardless of whether it wins the seed.

Victory requires the configured AP Star total to have been synchronized before the qualifying Level 22 completion event. A Level 22 clear below the goal does not arm a deferred win. Receiving the final required Star afterward does not complete the seed; the player must clear Level 22 again. The ordinary Completion location remains checked and is not duplicated by that replay.

The 66 individual AP Stars, generated Star gates, and production Victory behavior remain inactive during the initial full-level-mapping milestone. They activate only after genuine location capacity and generation validation pass.

## 11. APWorld regions and rules

Every normal level location belongs to its physical area region and inherits that area's access requirement. Verified item prerequisites apply equally to Completion and enabled Star tiers unless a source is explicitly reachable before level completion.

Known examples remain:

- Level 3 Completion requires Weed Killer and Plant Pipes, while Frog/Hippo requires only Weed Killer.
- Levels 4 and 5 follow the verified Roots chain.
- Darkness requires Plant Pipes for completion.
- Escape has no known in-level ability requirement.
- Loneliness requires Hypno Pan for completion.
- Locker Room requires Plant Pipes; Violance is helpful but not a hard requirement.
- Level 22 is completable for one Star without Plant Pipes, Hypno Pan, or Violance. Higher tiers remain conservative until Note Pad/Data Stick and other performance dependencies are proven.

Unknown prerequisites are not filled with area-only guesses. Until their dependency rules are verified, their locations may exist but accept filler only. This preserves visibility and tester coverage without allowing required progression to self-lock behind an inaccurately modeled level.

## 12. IDs and catalog validation

Existing IDs are immutable. Historical Development Cache IDs remain reserved even after the locations stop appearing in new seeds. The gaps at offsets `+4..+10` remain unused.

All newly approved normal campaign location IDs begin at the current safe location frontier `187256211`. The allocation order is deterministic: ascending level number, then Completion, 1 Star, 2 Stars, 3 Stars, skipping names with existing permanent IDs. The resulting explicit mapping is recorded in `docs/IDS.md` and guarded by tests and the repository validator.

No Bee/Devil, Bizzle/Clive, Super Nectar, Demon Key, delivery, seal, or final-event ID is allocated until its native identity and unique source event are confirmed. Once confirmed, those IDs append after the normal campaign block; none may reuse Development Cache or other historical IDs.

## 13. Capacity and staged rollout

The current v0.23 seed exposes 92/129/166/202 locations. Expanding only the normal campaign map adds the missing normal tiers and retiring the ten Development Caches produces expected addressed totals of:

| AP difficulty | Initial full-normal-map total |
| --- | ---: |
| Normal | 121 |
| Hard | 179 |
| Expert | 237 |
| Perfection | 273 |

These totals exclude the six inactive special checks and their still-unverified sources/actions. They must be asserted from instantiated locations rather than treated as hand-maintained constants.

The first production slice therefore:

1. implements all normal campaign descriptors and enabled tiers;
2. removes Development Caches from active seeds while reserving their IDs;
3. adds bounded diagnostics for special-mode discovery;
4. leaves special chains and all 66 Stars inactive.

After diagnostic evidence, the second slice activates the six special checks and full access/reward chains together. Even with those additions, 66 Stars require further real progression-safe locations under the conservative placement model. The project must map at least enough verified source/action checks to fit every required item plus filler-only locations; a target of roughly ten additional safe locations provides necessary room under current accounting.

Synthetic caches, automatic currency milestones, and higher difficulty tiers must not be used as structural padding.

## 14. Diagnostic-first implementation

Special-mode diagnostics must capture, in bounded snapshots:

- internal level and exact variant at launch and successful result;
- Bizzle/Clive source, held, used, and switch-state candidates;
- both Super Nectar source, bag, consumed, delivery, and bee-escape candidates;
- Demon Key source, held/used, and switch-state candidates;
- all four individual seal candidates;
- all-seals-complete, final escape, and reward/cartridge candidates;
- before/after differences across interaction, result, hub return, save reload, and reconnect.

Diagnostics may observe and log. They must not set flags, grant items, suppress rewards, send new locations, or allocate permanent IDs. Repeated frame polling must be deduplicated.

## 15. Verification and acceptance

Automated tests must prove:

- all 22 campaign descriptors and internal IDs are unique;
- all normal location names and IDs are unique and permanent;
- difficulty totals are exactly 121/179/237/273 for the first slice;
- normal cumulative result evaluation is correct;
- Bee/Devil variants cannot send normal or Star locations;
- inactive tiers and absent datapackage locations are never queued;
- Development Caches are not instantiated but their IDs remain reserved;
- region and known-item rules match verified evidence;
- disconnect, retry, reload, and duplicate results remain idempotent;
- an early Level 22 clear cannot later auto-trigger Victory.

Targeted manual acceptance avoids an immediate full-game replay:

1. Verify one normal first clear and one improved Star result.
2. Verify an offline result queues and reconnect sends it once.
3. Capture the special-mode diagnostic ledger using appropriate existing saves and server-granted test items.
4. After special activation, verify one Bee and one Devil completion, reload, and reconnect.
5. Generate matrices across every supported AP difficulty and starting area.
6. Mark unplayed individual mappings as manual-testing pending for broader testers.

The 66 Stars and generated gates require a later clean-seed generation matrix and full post-threshold Level 22 victory acceptance before they can be described as active.

## 16. Non-goals

This milestone does not:

- activate AP Stars, generated Star gates, or final Victory;
- invent unverified level prerequisites;
- add Star tiers to Bee or Devil variants;
- randomize Bee/Devil chains before their native identities are proven;
- force a Bee/Devil switch, level entry, Super Nectar return, Keycard use, seal, or final reward;
- add automatic Star/Music Lab Point milestone locations;
- merge, push, deploy, or publish a release without explicit approval.
