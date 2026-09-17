# SCRC APWorld

Current testing release: **Client v0.73.9 / APWorld v0.26.0** (`area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23-full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26`). Top-level slot-data schema 17 / campaign-mapping schema 1 maps all 22 normal campaign checks. Bee/Devil special variants are diagnostic-only, and AP Stars remain inactive. A fresh v0.26 seed and fresh native save are mandatory. It retains all 30 cassette items and sources; many individual routes still require manual verification. Older cassette schemas preserve native cassette behavior; historical APWorld v0.21 paired with historical Client v0.67.95 retains the Money-only pilot.

## Music Lab Points candidate

The pool replaces 20 Stardust with 10 `Music Lab Point` items worth 1 each, 3 `Music Lab Point Bundle` items worth 10 each, and 7 `Music Lab Point Large Bundle` items worth 20 each. Their permanent IDs are `187256153..187256155`; the total is 180 points. The existing nine chests require 5/10/20/32/46/64/89/111/140 points, leaving 40 points of slack after the final threshold. Chests may contain progression when ordinary solver reachability proves a valid chain. No point milestone locations are added; five cassette and two cartridge sources reuse their existing chest checks.

Top-level slot-data schema 17 / campaign-mapping schema 1 carries the full normal map. Its retained Music Lab Point sub-contract uses point schema 1 with exact IDs, names, values, counts, totals, cap, and threshold-to-location map. A compatible client returns zero before synchronization, rebuilds from authoritative receipts, caps at 180, and retains the synchronized total during a disconnect. Native medals contribute no AP points. Recognized v0.22 seeds and non-AP play retain native scoring; malformed point contracts fail closed at zero with an incompatibility error. The managed score getter applies AP points only in `GameRoom_Hub6`, with no native medal/save writes or chest/native detour. Display, all nine thresholds, reconnect/relaunch, and compatibility still require [live acceptance](../docs/testing/2026-09-09-music-lab-points-acceptance.md).

The source package is `apworld/scrc/`. Use `tools/build-apworld.ps1` from the repository root to generate `dist/scrc.apworld`.

For the canonical public build, Launcher installation, YAML generation, hosting, update, and uninstall instructions, see the [installation guide](../docs/INSTALL.md). Install the generated world using Archipelago Launcher's **Install APWorld** action (or double-click/drag it onto the launcher), then restart the Launcher before generating a fresh seed.

## Current live generation behavior

- `starting_area` defaults to `random`, but random currently selects only the validated **Roots Access** starter. Explicit `roots` is supported; `lobby`, `meat_dimension`, and `cell_tower` stop generation with a clear validation error rather than silently substituting Roots.
- Lobby, Meat Dimension, Cell Tower, Tower of Fear, and Royal Corridor Access remain randomized.
- `Weed Killer` is a progression item.
- `Plant Pipes` is a separate progression item.
- `Roots - Gecko's Weed Killer` is reachable with Roots Access alone.
- `Roots - Level 3 - Plant Pipes Pickup` requires Weed Killer / Level 3 reachability, but does not require Plant Pipes.
- `Level 3 - Completion` requires Plant Pipes in addition to the Level 3 entry chain.
- `Hip Glasses` and `Chicken Bucket` are separate progression items.
- `Roots - Level 4 - Hip Glasses` requires the current Level 4 chain: Roots Access, Weed Killer, and Plant Pipes.
- `Roots - Bucket Minion Trade` requires Hip Glasses. The player performs the normal trade; Chicken Bucket must arrive from AP.
- Chicken Bucket is used normally in Lift Quest. Combo Bucket remains a native, non-network event/ability.

This deliberately supports entering Level 3, collecting the Frog/Hippo check, and menu-exiting if Plant Pipes has not yet been received.

## AP performance difficulty

`difficulty` filters only existing campaign performance locations. It does not add campaign checks or allocate IDs, and it does not change the player's native REG/PRO choice.

| AP difficulty | Campaign tiers | Existing song medal tiers | Addressed locations |
| --- | --- | --- | ---: |
| Normal | Completion / 1-Star | Bronze | 125 |
| Hard | Add 2-Star | Add Silver | 183 |
| Expert | Add 3-Star | Add Gold | 241 |
| Perfection | Same campaign tiers as Expert | Add Platinum | 277 |

Inactive checks are absent from a generated seed, not replaced with filler. v0.26 maps all 22 normal campaign checks, retains the v0.22 full cassette source set, and keeps Bee/Devil special variants diagnostic-only. AP Stars remain inactive. Active Level-22 2/3-Star checks remain filler-only; Music Lab point chests now use weighted AP-point access rules and may hold solver-reachable progression.

## Generation-foundation previews

The v0.17 YAML retains the v0.16 `required_stars` and `starting_area` previews. Generation deterministically exports provisional Level 1–22 Star requirements. One permanent network ID is registered for `Star`, and the planner represents 66 individual Star items.

Stars are not placed in the live item pool; the client does not enforce generated Star gates; and victory remains the Area Access development milestone. Activating 66 Stars now would exceed the current modeled location capacity once existing required items are included, so activation waits for more validated checks and solver-backed pool construction.

The example generation YAML is [SCRC-AreaRouting-PlantPipes.yaml](examples/SCRC-AreaRouting-PlantPipes.yaml). Pair this APWorld with Client v0.71.0 and generate a fresh v0.26 seed.

### Character quest items (v0.26)

Old Game Data and Car Battery are each randomized once as progression items. Their sources reuse the existing Music Lab 5-point and 20-point chest checks. Receiving them supplies the corresponding native bag item; their hand-ins send AP checks, and Meoo and Maniac are separate AP unlock items. The exact `character_quest_item_schema: 1` contract is opt-in under top-level schema 17. Generate a fresh v0.26 seed to use these items; retained older seeds cannot gain the new pool entries. Lobby item staging, AP Stars, Star gates, and Victory remain inactive.


## Quest checks candidate (v0.26)

A fresh v0.26 seed and fresh native save are required. Slot-data schema 17 keeps
character quest item schema 1 and adds quest checks schema 1. Plunger, Meoo, and
Maniac each appear once as useful AP items. Old Game Data and Car Battery are now
progression: their ordinary hand-ins send `Game Garage - Old Game Data Hand-In`
and `Lobby - Car Battery Hand-In`, which may contain progression. Those checks
require the input item, never the character reward. Existing Music Lab 5-point
and 20-point source checks retain their IDs and do not gain duplicate locations.

`Lobby - Plunger Pickup` is a Lobby check restricted to Stardust until its native
phone/button route is fully modeled. Plunger has no inferred Vault or cassette
gate. `Roots - Star Eater Fed` is also Stardust-only: the native 3-star threshold
is unchanged, but native earned stars are not modeled by AP logic. Its logical
Roots reachability is therefore an approximation, never a progression route.
AP Stars, generated Star gates, and final Star Victory remain inactive.

Addressed totals are Normal 125 / Hard 183 / Expert 241 / Perfection 277; the pool
contains 71 non-Stardust items at every difficulty. Lobby letter/trumpet and
Demolition Certificate IDs remain reserved and inactive. This is a source/build
candidate pending targeted gameplay acceptance; no installation is implied.
