# SCRC APWorld

Current APWorld: **v0.21.0** (`area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21`) with client **v0.67.95**. Vampire Killer remains a physical vanilla pickup for the normal Garage entrance; the other five cartridges remain randomized. Existing v0.20 seeds retain six-cartridge behavior.

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
| Normal | Completion / 1-Star | Bronze | 67 |
| Hard | Add 2-Star | Add Silver | 104 |
| Expert | Add 3-Star | Add Gold | 141 |
| Perfection | Same campaign tiers as Expert | Add Platinum | 177 |

Inactive checks are absent from a generated seed, not replaced with filler. This v0.20 release filters only existing campaign performance locations. Active Level-22 2/3-Star checks and Music Lab point chests remain filler-only.

## Generation-foundation previews

The v0.17 YAML retains the v0.16 `required_stars` and `starting_area` previews. Generation deterministically exports provisional Level 1–22 Star requirements. One permanent network ID is registered for `Star`, and the planner represents 66 individual Star items.

Stars are not placed in the live item pool; the client does not enforce generated Star gates; and victory remains the Area Access development milestone. Activating 66 Stars now would exceed the current modeled location capacity once existing required items are included, so activation waits for more validated checks and solver-backed pool construction.

The example generation YAML is [SCRC-AreaRouting-PlantPipes.yaml](examples/SCRC-AreaRouting-PlantPipes.yaml). Pair this APWorld with client **v0.67.95** and generate a fresh v0.21 seed.
