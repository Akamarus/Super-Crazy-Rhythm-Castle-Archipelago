# SCRC APWorld

Current APWorld: **v0.23.0** (`area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23`) with client **v0.69.0**. This experimental v0.23 candidate adds AP Music Lab Points and requires manual acceptance with a fresh v0.23 seed and fresh native save. It retains all 30 cassette items and sources; many individual routes still require manual verification. Older cassette schemas preserve native cassette behavior; historical APWorld v0.21 paired with historical Client v0.67.95 retains the Money-only pilot.

## Music Lab Points candidate

The pool replaces 20 Stardust with 10 `Music Lab Point` items worth 1 each, 3 `Music Lab Point Bundle` items worth 10 each, and 7 `Music Lab Point Large Bundle` items worth 20 each. Their permanent IDs are `187256153..187256155`; the total is 180 points. The existing nine chests require 5/10/20/32/46/64/89/111/140 points, leaving 40 points of slack after the final threshold. Chests may contain progression when ordinary solver reachability proves a valid chain. No point milestone locations are added; five cassette and two cartridge sources reuse their existing chest checks.

Slot-data schema 14 carries point schema 1 with exact IDs, names, values, counts, totals, cap, and threshold-to-location map. A compatible client returns zero before synchronization, rebuilds from authoritative receipts, caps at 180, and retains the synchronized total during a disconnect. Native medals contribute no AP points. Recognized v0.22 seeds and non-AP play retain native scoring; malformed v0.23 contracts fail closed at zero with an incompatibility error. The managed score getter applies AP points only in `GameRoom_Hub6`, with no native medal/save writes or chest/native detour. Display, all nine thresholds, reconnect/relaunch, and compatibility still require [live acceptance](../docs/testing/2026-09-09-music-lab-points-acceptance.md).

The source package is `apworld/scrc/`. Use `tools/build-apworld.ps1` from the repository root to generate `dist/scrc.apworld`.

For the canonical public build, Launcher installation, YAML generation, hosting, update, and uninstall instructions, see the [installation guide](../docs/INSTALL.md). Install the generated world using Archipelago Launcher's **Install APWorld** action (or double-click/drag it onto the launcher), then restart the Launcher before generating a fresh seed.

## Current live generation behavior

- `starting_area` defaults to `random`, but random currently selects only the validated **Roots Access** starter. Explicit `roots` is supported; `lobby`, `meat_dimension`, and `cell_tower` stop generation with a clear validation error rather than silently substituting Roots.
- Lobby, Meat Dimension, Cell Tower, Tower of Fear, and Royal Corridor Access remain randomized.
- `Weed Killer` is a progression item.
- `Plant Pipes` is a separate progression item.
- `Roots - Gecko's Weed Killer` is reachable with Roots Access alone.
- `Roots - Level 3 - Frog and Hippo` requires Weed Killer / Level 3 reachability, but does not require Plant Pipes.
- `Level 3 - Completion` requires Plant Pipes in addition to the Level 3 entry chain.
- `Hip Glasses` and `Chicken Bucket` are separate progression items.
- `Roots - Level 4 - Hip Glasses` requires the current Level 4 chain: Roots Access, Weed Killer, and Plant Pipes.
- `Roots - Bucket Minion Trade` requires Hip Glasses. The player performs the normal trade; Chicken Bucket must arrive from AP.
- Chicken Bucket is used normally in Lift Quest. Combo Bucket remains a native, non-network event/ability.

This deliberately supports entering Level 3, collecting the Frog/Hippo check, and menu-exiting if Plant Pipes has not yet been received.

## AP performance difficulty

`difficulty` filters only existing campaign performance locations. It does not add campaign checks or allocate IDs, and it does not change the player's native REG/PRO choice.

| AP difficulty | Existing campaign tiers | Existing song medal tiers | Addressed locations |
| --- | --- | --- | ---: |
| Normal | Completion / 1-Star | Bronze | 92 |
| Hard | Add 2-Star | Add Silver | 129 |
| Expert | Add 3-Star | Add Gold | 166 |
| Perfection | Same campaign tiers as Expert | Add Platinum | 202 |

Inactive checks are absent from a generated seed, not replaced with filler. v0.23 retains the v0.22 full cassette source set and these active counts. Active Level-22 2/3-Star checks remain filler-only; Music Lab point chests now use weighted AP-point access rules and may hold solver-reachable progression.

## Generation-foundation previews

The v0.17 YAML retains the v0.16 `required_stars` and `starting_area` previews. Generation deterministically exports provisional Level 1–22 Star requirements. One permanent network ID is registered for `Star`, and the planner represents 66 individual Star items.

Stars are not placed in the live item pool; the client does not enforce generated Star gates; and victory remains the Area Access development milestone. Activating 66 Stars now would exceed the current modeled location capacity once existing required items are included, so activation waits for more validated checks and solver-backed pool construction.

The example generation YAML is [SCRC-AreaRouting-PlantPipes.yaml](examples/SCRC-AreaRouting-PlantPipes.yaml). Pair this APWorld with client **v0.69.0** and generate a fresh v0.23 seed.
