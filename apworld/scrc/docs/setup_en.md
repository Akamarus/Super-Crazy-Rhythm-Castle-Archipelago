# Super Crazy Rhythm Castle Archipelago setup — v0.21.0

The [public installation guide](../../../docs/INSTALL.md) is the canonical setup flow. It covers trusted custom-world safety, BepInEx first launch, client configuration, APWorld installation, generation, hosting, updates, and uninstalling.

Developer setup summary:

1. Build the world from the repository root with `./tools/build-apworld.ps1`; it creates `dist/scrc.apworld`.
2. Install `scrc.apworld` through Archipelago Launcher's **Install APWorld** action (or double-click/drag it onto the launcher), then restart the Launcher.
3. In Archipelago Launcher, choose **Generate Template Options** to create templates in `<Archipelago>\Players\Templates`, or start with [SCRC-AreaRouting-PlantPipes.yaml](../../examples/SCRC-AreaRouting-PlantPipes.yaml). Copy the chosen YAML to `<Archipelago>\Players` as an uncompressed `.yaml`, edit its top-level `name:` to the intended slot name (the example starts as `name: Jack`), and use that exact name in the client's `Slot` setting.
4. Click **Generate** in the Launcher and host `<Archipelago>\output\AP_XXXXX.zip`. Older generated seeds retain their old slot data and datapackage.
5. Run RhythmCastleAP **v0.67.95** with `DirectStartAtPhoneHub=true`, `EnableAreaAccessPrototype=true`, and `PrototypeStartingArea=AP`; keep the retired `RandomizeEarlyProgression` prototype false.

## AP performance difficulty

Set `difficulty` in the YAML to one of `normal`, `hard`, `expert`, or `perfection`. It filters only existing AP campaign performance locations; native REG/PRO stays player-controlled.

| AP difficulty | Campaign tiers | Song medal tiers | Addressed locations |
| --- | --- | --- | ---: |
| Normal | Completion / 1-Star | Bronze | 67 |
| Hard | Add 2-Star | Add Silver | 104 |
| Expert | Add 3-Star | Add Gold | 141 |
| Perfection | Same campaign tiers as Expert | Add Platinum | 177 |

Inactive checks are absent from the seed, not filler. v0.20 filters only existing campaign performance locations: no campaign checks or IDs are added. Active Level-22 2/3-Star checks and Music Lab point chests remain filler-only.

v0.21.0 adds native Vampire Killer Garage access while retaining five randomized cartridges and active difficulty filtering. Rebuild/replace the APWorld, restart Archipelago, and generate a fresh seed after APWorld changes. Client v0.67.95 remains compatible with v0.20 seeds, which retain six-cartridge behavior.
