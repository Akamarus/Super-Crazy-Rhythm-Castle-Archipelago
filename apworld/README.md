# SCRC APWorld

Current consolidated preview: **v0.19** (`area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19`) with client **v0.67.64**. The retained prefix preserves existing live systems and the v0.18 fail-closed repair contract.

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

## Generation-foundation previews

The v0.17 YAML retains the v0.16 `required_stars`, `difficulty`, and `starting_area` previews. Generation deterministically exports provisional Level 1–22 Star requirements and a cumulative difficulty-location preview. One permanent network ID is registered for `Star`, and the planner represents 66 individual Star items.

These are foundations only. Stars are not placed in the live item pool; difficulty does not remove live locations; the client does not enforce generated Star gates; and victory remains the Area Access development milestone. Activating 66 Stars now would exceed the current modeled location capacity once existing required items are included, so activation waits for more validated checks and solver-backed pool construction.

The example generation YAML is [SCRC-AreaRouting-PlantPipes.yaml](examples/SCRC-AreaRouting-PlantPipes.yaml). Pair this APWorld with client **v0.67.64** and generate a fresh v0.19 seed.
