# Randomizer Logic and Progression Design

**Status:** Approved architectural baseline; native discovery required before implementation
**Date:** 2026-08-20

## 1. Purpose and authority

This specification defines the approved initial architecture for Super Crazy Rhythm Castle Archipelago progression, items, locations, victory, difficulty, multiplayer, synchronization, and validation.

The full-game playthrough catalog in `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md` is evidence of native behavior. When an older conclusion conflicts with this specification, the later Area Access design and the decisions recorded here take precedence. Historical references to “Weed Killer” after Level 3 mean the permanent **Plant Pipes** ability, not the consumed Weed Killer bag item.

This document authorizes investigation and targeted diagnostic testing. It does not authorize production implementation, permanent AP ID allocation, merge, or push. Native mappings explicitly marked for validation must be proven before their production logic is written.

## 2. Core goal and victory

The item pool contains exactly 66 individual AP `Star` items. There are no Star bundles. `required_stars` is configurable from 1 through 66, with 50 as the recommended default.

Victory requires both conditions in this order:

1. The synchronized AP Star total has reached `required_stars`.
2. The player subsequently completes Level 22 — King Ferdinand I.

Completing Level 22 before reaching the Star goal sends its ordinary cumulative performance locations but not Victory. Receiving the final required Star later does not win automatically. The player must complete Level 22 again. A Star received and synchronized before the completion event qualifies that clear; one received afterward requires another clear.

Level 22 remains playable below the final Star goal. Its generated entrance requirement must always be less than `required_stars`. The client must clearly distinguish an ordinary Level 22 check from a victory-qualifying clear.

Secret Bunker, Devil Mode, Music Lab completion, Game Garage completion, versus content, and every major area not logically needed for the configured goal remain optional.

## 3. Home and Area Access

Hub6 is the permanent AP home. Music Lab and Game Garage are always physically available from Home, while their internal checks still require the relevant items and performance.

The six major Area Access items are:

- Roots Access
- Lobby Access
- Meat Dimension Access
- Cell Tower Access
- Tower of Fear Access
- Royal Corridor Access

One validated Area Access item is precollected. The other five use unrestricted solver-valid placement. `starting_area` defaults to `random` and may expose fixed choices only for starts that have passed clean-save routing tests and provide a healthy opening sphere of roughly three to five checks.

Tower of Fear and Royal Corridor are excluded from the initial starting pool. Their known item-heavy openings and phone-side routing do not presently provide a safe natural start. Other areas enter the pool only after their phone spawns are validated.

Area Access is authoritative for every route into its destination, including Hub6 phones, vanilla entrances, shortcuts, side routes, and story transitions. It grants entry only. It does not complete quests, grant meaningful items, meet generated Star requirements, or set unrelated story flags.

Each phone destination must be modeled as its actual subregion rather than an assumed area beginning. Testing must map forward and reverse routes, one-way blockers, shortcuts, first-arrival state, and a safe path home. During discovery, a transition into a locked area redirects safely to Hub6 without advancing destination state. Release behavior should naturally restrict mapped entrances; Hub6 redirection remains a fail-safe.

All six Area Access items are not independently required for victory. A major area may remain optional when the solver can reach the configured Star goal and Level 22 without it.

## 4. Generated Star gates

Every normal campaign level from 1 through 22 receives a seed-stable generated AP Star requirement. Opening levels may require zero.

Generation must:

- derive structural depth from Area Access, meaningful items, local story prerequisites, and prerequisite levels;
- assign randomized requirements within logical-depth bands;
- keep mandatory chains nondecreasing while allowing equal thresholds and concurrent routes;
- scale against `required_stars` and the selected performance-location difficulty;
- keep every campaign threshold below `required_stars`;
- confirm enough reachable Stars exist before every gate; and
- reject or regenerate invalid threshold sets.

There is no separate Star-gate difficulty option initially. Approximate bands for the default 50-Star goal are 0 for openings, 1–10 early, 8–20 early-middle, 15–30 middle, 25–40 late, and roughly 35–45 for the final route. These are generation targets, not fixed level values.

During an AP game, generated gates and the player-facing Star counter use synchronized AP Stars. Native earned-Star ratings remain saved for performance records but never satisfy AP gates. Before initial item synchronization, Star gates fail closed. After synchronization, temporary disconnection retains the last valid total.

## 5. Performance difficulty and native play difficulty

The seed option `difficulty` controls cumulative performance locations:

| AP difficulty | Campaign levels | Music Lab and Game Garage |
| --- | --- | --- |
| Normal | Completion / 1-Star | Bronze |
| Hard | Completion / 1-Star + 2-Star | Bronze + Silver |
| Expert | Completion / 1-Star + 2-Star + 3-Star | Bronze + Silver + Gold |
| Perfection | Same campaign tiers as Expert | Bronze + Silver + Gold + Platinum |

Disabled tiers do not exist in that seed. Higher results send all enabled lower tiers that remain unchecked. Failed, abandoned, and non-default-variant attempts do not send normal campaign checks.

The game’s native Normal/Pro selection is independent from AP difficulty and must be available immediately from an AP start. Starting in Hub6 must not force Normal or require displaced campaign progression before Pro becomes selectable. Native choice persists normally and does not alter the generated AP location set.

## 6. Music Lab Point economy

Music Lab Points are AP inventory, separate from AP Stars and native medal score. The provisional item pool totals 180 points in 20 item instances:

| Item | Count | Value each | Total value |
| --- | ---: | ---: | ---: |
| Music Lab Point | 10 | 1 | 10 |
| Music Lab Point Bundle | 3 | 10 | 30 |
| Music Lab Point Large Bundle | 7 | 20 | 140 |
| **Total** | **20** |  | **180** |

The nine existing Music Lab chests remain the only initial point-threshold locations: 5, 10, 20, 32, 46, 64, 89, 111, and 140. The final chest leaves 40 points of slack. Point items may appear behind chest locations when the solver proves the chain reachable.

Cassette and Game Garage medals save their native results and send AP checks but do not add AP Music Lab Points. During an AP game, the existing point display and chest eligibility use the synchronized AP total without writing it into the native medal-score save. Before initial synchronization chests remain locked; after synchronization a disconnect retains the last valid total and queues newly opened checks.

Automatic Star or Music Lab Point milestone locations are not included initially. The 20-instance distribution remains provisional until the final Normal location count is proven.

## 7. Source, item, and story model

Meaningful portable items and permanent abilities follow one consistent flow:

```text
reach native source
-> send AP source location
-> suppress only the randomized native reward
-> receive the AP item
-> grant the real native inventory/ability state
-> perform normal use, trade, insertion, consumption, or transformation
```

Trades and deliveries remain player-driven. Story conversations, witnessed cutscenes, opened gates, blocker state, and quest-stage bookkeeping remain vanilla unless a narrow normalization is required to support displaced area order. Source state and AP ownership are separate.

Reusable environmental mechanisms remain vanilla. In particular, the Lobby Manual Button is not randomized or permanently bypassed. Starting in Hub6 normally triggers its return sequence; the client should not pre-set that state unless testing proves a narrow fallback is necessary.

No production mapping may be guessed. Every randomized item requires a verified native source event, owned state, use/consumption behavior, downstream dependency list, reload behavior, and reconnection behavior.

## 8. Campaign progression decisions

### Roots

- Weed Killer and Plant Pipes are separate AP progression items.
- Gecko is the Weed Killer source. Vanilla consumes the delivered bag item to open Level 3.
- Frog and Hippo inside Level 3 are the Plant Pipes source. Their check is reachable without Plant Pipes.
- Plant Pipes are required to complete Level 3, enter Level 4, and complete Level 4.
- Hip Glasses are randomized at their Level 4 source.
- The Bucket Minion normally consumes delivered Hip Glasses; that trade is the Chicken Bucket source.
- Chicken Bucket is randomized and used normally during Lift Quest.
- Combo Bucket is the vanilla consequence of that use, not a separate AP item initially.
- Later references to “Weed Killer” in the discovery record are Plant Pipes. Plant Pipes are globally relevant, including later normal and Devil Mode content.

### Lobby

- Lobby Access provides a playable local baseline without completing Level 5.
- Important Letters and Bean Trumpet are separate AP items in a source, trade, and mail-delivery chain.
- Demolition Certificate is a randomized AP item from Demolition Training.
- Boring Room, the three-hand objectives, and their blocker state remain branching vanilla progression.
- Plunger is an optional randomized source/use chain. Its old route does not replace Meat Dimension Access.
- Fish Tears are an optional randomized source/delivery chain. Their old route does not replace Cell Tower Access.
- Cold Storage is tentatively classified as Lobby because its entrance is physically there. Its exact visibility and heist prerequisites must be proven.

### Meat Dimension

- Wooden Spoon, Frying Pan, Hypno Pan, and Saw Disc are randomized AP items.
- Frog and Hippo require both utensils for the Hypno Pan source; AP delivery grants `PIED_PIPER_ABILITY`.
- Music selection and delivery remain player-driven.
- Distinct Act music deliveries, cat return, Scruffy return, and mouse revolution are AP action locations.
- Hypno Pan remains a global dependency wherever dogs, crowds, pets, or mice require it.
- Fish Tears are sourced inside Level 14 under the optional chain above.

### Cell Tower and Super Nectar

- Cell Tower Access provides an independent local baseline without Fish Tears delivery.
- Central Mainframe opens the native forward route; Area Access does not force it.
- `Bizzle and Clive` is one combined AP inventory item sourced from the bees.
- AP receipt enables the normal player-controlled Bee Mode switch interactions near eligible doors.
- Nectar Party and Act 1B: Nectar are the separate sources for Super Nectar Bubbles and Super Nectar Recipes.
- Returning both AP-delivered native items consumes them normally, sends one delivery check, and produces the native bee-escape state.
- Bee Mode has no Star-performance locations.
- Cell Tower’s central phone spawn requires explicit two-direction routing tests.

### Cold Storage and Violance

- Cold Storage is tentatively a Lobby level with Lobby Access as its physical area requirement.
- Its hidden door remains controlled by verified Old Tom, Thief Prince, heist, and related native state. The client does not force it visible.
- Hypno Pan is required where used.
- Violance is randomized at its distinct source; AP receipt grants `VIOLIN_ABILITY`.
- Cell Tower Access is included only if the verified native prerequisite chain actually requires Cell Tower progression.

### Tower of Fear

- Minim’s Eye, Brain/Mind, and Heart are separate AP items with separate native-source checks.
- Each delivered part is used normally at its corresponding statue; each statue restoration is a separate AP action location.
- Tentative statue requirements are Eye + Plant Pipes for Darkness, Brain/Mind + Violance for Complexity/Escape, and Heart + Hypno Pan for Loneliness.
- The Darkness requires Plant Pipes for level completion.
- Escape requires no ability inside the level itself.
- Loneliness requires Hypno Pan for level completion.
- Branch shields remain native consequences of level completion, not AP items or duplicate checks.
- The separate Totem completion sequence is one AP location.
- Tower of Fear is excluded from the initial starting pool.

### Royal Corridor and Level 22

- Locker Room requires Plant Pipes for completion. Violance is helpful but not a hard completion requirement.
- The Royal Star Eater is an optional AP interaction location with a generated threshold around Level 21’s depth band.
- The Royal Star Eater is not required for Level 22 or Victory.
- Level 22 has its own higher generated threshold below the final Star goal.
- Fridge/ice, King cutscene, bridge, and phone-side routing remain native and require validation.
- Royal Corridor is excluded from the initial starting pool because the phone lands opposite the Star Eater interaction.
- Bunker Keycard is randomized at Level 22 and is the Secret Bunker’s access item; no separate Bunker Access item exists.
- The Keycard may open the Bunker before Level 22 when placed early. It is used normally at the Lobby door and is not regranted after consumption.

## 9. Music Lab cassettes and Game Garage

All 30 recognized Music Lab cassettes and all six Game Garage cartridges are individual randomized AP items.

For a post-level cassette reward, campaign performance, cassette acquisition, and Music Lab performance are distinct actions and locations. The direct cassette grant is suppressed; AP delivery grants native inventory; the player inserts it through the normal Music Lab machine; only then is the song unlocked. The same inventory-first, player-activated rule applies to cartridges in Game Garage.

Each song requires its activated cassette or cartridge before its enabled cumulative medal locations are reachable. Medal results do not add AP Music Lab Points.

All 30 cassette sources require a discovery table containing level ID, variant, player-facing song, award timing, source flag, inventory ID, song-unlock ID, replay behavior, performance dependency, and existing-save reconciliation. Campaign, Bee Mode, Devil Mode, chest, pickup, and quest sources must not be omitted or double-counted.

Known cartridge sources include the Gradius and Bloody Tears chests plus Smooch, Superstar, Vampire Killer, and Wag the Dog sources. Exact Bunker reward identities must be reconciled with those six before implementation.

Old Game Data follows this chain:

```text
5-point chest source
-> receive Old Game Data
-> insert normally in Game Garage
-> send Maniac character source
-> receive Maniac character item
-> unlock Maniac in the native roster
```

The complete interaction and flags require logged gameplay validation.

## 10. Characters

Every nonstarting playable character becomes an individual AP useful/cosmetic item unless testing proves a required mechanic. Native character sources become AP locations when they expose a distinct event. Receiving the character item adds it to the native roster.

Known chains include Car Battery from the 20-point chest, normal Car Battery hand-in as Meoo’s source, Old Game Data insertion as Maniac’s source, and King Ferdinand’s Level 22 source. King’s source becomes separate only if it exposes a distinct event from Level 22 completion and Bunker Keycard award.

Bizzle and Clive are explicitly not characters; they are one combined quest inventory item. The full playable-character roster and native flags require discovery.

## 11. Secret Bunker and special modes

Secret Bunker is optional postgame content reached with Lobby Access plus a used Bunker Keycard.

- Gecko’s interaction is one source location for separate Note Pad and Data Stick AP items.
- Those two items tentatively gate Level 22 results above one Star. Testing must compare neither, either individually, and both across all Level 22 results.
- Demon Key is randomized at the demon source and enables normal Devil Mode switching.
- Demonic Room, Demonic Tower, Demonic Escape, and Demonic Lockers each provide one Demon Seal completion location and no Star tiers.
- Demonic Tower and Demonic Lockers require Plant Pipes. Other special-mode requirements must be proven.
- After all seals, the final demon escape/cartridge event is one source location.
- The Bunker Star Eater provisionally requires a fixed 50 AP Stars. Its normal feed/portal/cartridge sequence is one source location and cannot contain a Star required to reach itself.

The fixed Bunker threshold must be revisited after early-Keycard seeds are tested. Seal flags, portal state, and both cartridge identities require discovery.

The Lobby-accessible versus area is excluded from initial logic and item counts until testing determines its room, player-count requirements, solo/bot support, modes, persistent results, rewards, and Lobby side effects. Multiplayer-only checks, if viable, require a later option and remain disabled by default.

## 12. Location and item-pool accounting

Known performance and chest locations are:

| Category | Normal | Hard | Expert | Perfection |
| --- | ---: | ---: | ---: | ---: |
| 22 campaign levels | 22 | 44 | 66 | 66 |
| 30 Music Lab songs | 30 | 60 | 90 | 120 |
| 6 Game Garage songs | 6 | 12 | 18 | 24 |
| Music Lab chests | 9 | 9 | 9 | 9 |
| **Subtotal** | **67** | **125** | **183** | **219** |

The approved physical-source/action catalog currently adds approximately 75 unique locations, producing about 142 identified Normal locations before remaining discoveries. The provisional item pool is approximately 156 instances before the complete character roster. The gap must be closed with verified gameplay locations or a further reduction in Music Lab Point item instances. Higher performance tiers will not be generated merely as storage, and automatic currency milestones will not be used as structural padding.

The exact source/action ledger must avoid counting one physical event twice. A source event and a level result may be separate when native flags prove separate actions; simultaneous indistinguishable rewards are not split artificially.

Stardust is the only initial filler. There are no traps initially. Every difficulty must have enough locations for all mandatory item instances before generation is enabled.

## 13. Solver policy

The solver must model actual source reachability, level completion, item use, story prerequisites, Area Access, generated Star gates, point thresholds, activated cassettes/cartridges, and selected performance difficulty.

It may place Area Access, Stars, points, cassettes, cartridges, and quest items behind their own systems only when earlier reachable resources prevent self-lock. A required Star may be placed at the first Level 22 completion because the boss is replayable after the goal count is reached.

Random starts require a healthy opening sphere. Unsupported fixed starts and invalid threshold sets fail generation clearly. Optional content does not become goal-required merely because it has locations. Standard Archipelago accessibility settings remain supported, but no setting may permit an unbeatable goal.

## 14. Save, synchronization, and offline behavior

An AP session binds game, seed, slot, schema version, and native save. A save bound to a different seed or slot cannot silently mix progression.

On connection, the client validates slot data, rebuilds AP inventory from received-item history, applies permanent state idempotently, reconciles native source flags with server locations, and only then marks synchronization ready. Incompatible or missing slot data fails closed.

Consumed and transformed items require lifecycle tracking so replay does not restore them after native use. This includes Weed Killer, Chicken Bucket, Bunker Keycard, trade items, Super Nectar, Minim parts, and inserted cassettes/cartridges. Native consumed/converted flags take precedence over reapplication.

Before first synchronization, randomized gates remain locked. After synchronization, a temporary disconnect retains last-known items and currencies. Completed checks queue for reconnection. Every one-time source requires a persistent native flag or durable seed/slot-bound journal. Duplicate items and checks are harmless.

Outside an AP-bound save, vanilla behavior is unchanged.

## 15. Player feedback and future text client

The initial release uses existing Star/point displays where practical and concise notifications for items, checks, missing requirements, synchronization, offline queues, incompatibility, and victory state. There is no permanent custom HUD initially.

Normal logs record meaningful events without credentials. Frame-level polling and blockers must not spam. Detailed native diagnostics require an explicit development setting.

A later toggleable in-game text client will show received items, sent checks, other-player activity, connection changes, warnings, and errors in a scrollable history. Its event stream should be designed now, but the first release panel is deferred. Later chat/command support is separate.

## 16. Multiplayer and deferred features

Local co-op is supported by initial architecture: one game process, native save, AP connection, and AP slot shared by all local players. Shared results send each check once. Implementation may begin with single-player paths, but local co-op remains explicitly supported-by-design and labeled unverified until smoke-tested.

Online co-op is a future low-priority feature. Its intended model is one host-authoritative AP connection, with remote players as cooperative participants. Host-only versus passive-client plugin requirements must be discovered. Multiple active AP connections for one shared session are prohibited.

DeathLink is deferred but planned as an off-by-default feature after safe failure behavior is tested in levels, hubs, results, Music Lab, Game Garage, and local multiplayer. Traps are also deferred.

## 17. AP IDs and compatibility

`docs/IDS.md` remains authoritative. Existing and historical IDs are never reused. Development Cache IDs remain reserved. New location IDs begin at the documented next safe location offset and new item IDs at the documented next safe item offset, but allocation occurs only during approved implementation after the final catalogs are reviewed.

Every allocation updates the registry in the same change. Slot data carries an implementation/schema version. Major datapackage changes require a newly generated seed; the client explains incompatibility rather than interpreting old slot data as the new design.

## 18. Mandatory native discovery ledger

Before production implementation or permanent IDs, investigation must resolve:

1. all six phone destinations, paths, first-arrival states, and viable starter areas;
2. all 30 cassette source events and native identifiers;
3. the complete meaningful-item dependency matrix across normal and special levels;
4. Important Letters, Bean Trumpet, Demolition Certificate, Plunger, and Fish Tears flags;
5. every Meat quest milestone and safe out-of-order behavior;
6. Bizzle and Clive’s combined inventory representation, Bee Mode switches, Super Nectar, and bee escape;
7. Cold Storage visibility, physical routing, heist prerequisites, and Violance source;
8. all Minim-part sources, statue mappings, shield flags, and Totem completion;
9. Royal phone-side routing, optional Star Eater, bridge, fridge/ice, and Level 22 path;
10. Note Pad and Data Stick effects on Level 22 performance;
11. Bunker seal flags, demon/Star Eater reward events, and cartridge identities;
12. Old Game Data insertion and Maniac unlock;
13. the full playable-character roster and source flags;
14. Lobby versus content and multiplayer requirements;
15. immediate native Normal/Pro selection from a clean Hub6 start;
16. exact location counts for every AP difficulty; and
17. local-co-op shared results and reward behavior.

Approved-but-unverified chains must retain that label until gameplay logs and visible behavior agree.

## 19. Verification gates

### Native discovery gate

Complete the ledger above using clean saves, out-of-order starts, before/after native state, logs, reloads, and targeted reconnection tests.

### Generation gate

Generate broad seed matrices across every supported start, Star goal, and difficulty. Prove opening spheres, threshold validity, item capacity, self-lock prevention, optional-area behavior, and Level 22 replay logic.

### Client/system gate

Test initial sync, reconnect, offline checks, duplicate suppression, consumed items, save binding, schema rejection, displays, notifications, and every source-to-use chain.

### Gameplay acceptance gate

Complete a clean single-player seed through a post-threshold Level 22 victory; exercise an early non-winning clear and replay; test all Music Lab chests; representative difficulty tiers; optional Tower and Bunker chains; and a local-co-op smoke test before claiming local multiplayer is verified.

Online co-op and DeathLink have separate future validation gates.

## 20. Decision ledger

| Decision | Status |
| --- | --- |
| 50-of-66 recommended Star goal followed by a new Level 22 clear | Approved |
| Individual AP Stars; no Star bundles | Approved |
| Area Access authoritative across all major-area routes | Approved |
| Random validated starting area; Tower and Royal initially excluded | Approved |
| Generated per-level Star requirements below final goal | Approved |
| Normal/Hard/Expert/Perfection cumulative performance table | Approved |
| Native Normal/Pro selectable immediately | Approved |
| 180 Music Lab Points in provisional 20-item distribution | Approved provisionally pending final count |
| Nine native Music Lab chest thresholds; no automatic currency milestones | Approved |
| Thirty cassette and six cartridge AP items activated normally | Approved; mappings unverified |
| Source/item/use model for meaningful quest items | Approved |
| Area-by-area progression described in this specification | Approved; named native mappings require validation |
| Characters randomized as useful/cosmetic | Approved; roster incomplete |
| Secret Bunker optional; Keycard is its access item | Approved |
| Bunker Star Eater fixed at 50 | Provisional; revisit after early-access tests |
| Local co-op supported by design | Approved; unverified |
| Online co-op, DeathLink, integrated text client, and traps deferred | Approved |
| Stardust-only initial filler | Approved |
| Preserve IDs and require new seeds for incompatible schemas | Approved |
| Four verification gates before completion claims | Approved |
