# Super Crazy Rhythm Castle Archipelago setup — v0.15

The [public installation guide](../../../docs/INSTALL.md) is the canonical setup flow. It covers trusted custom-world safety, BepInEx first launch, client configuration, APWorld installation, generation, hosting, updates, and uninstalling.

Developer setup summary:

1. Build the world from the repository root with `./tools/build-apworld.ps1`; it creates `dist/scrc.apworld`.
2. Install `scrc.apworld` through Archipelago Launcher's **Install APWorld** action (or double-click/drag it onto the launcher), then restart the Launcher.
3. In Archipelago Launcher, choose **Generate Template Options** to create templates in `<Archipelago>\Players\Templates`, or start with [SCRC-AreaRouting-PlantPipes.yaml](../../examples/SCRC-AreaRouting-PlantPipes.yaml). Copy the chosen YAML to `<Archipelago>\Players` as an uncompressed `.yaml`, edit its top-level `name:` to the intended slot name (the example starts as `name: Jack`), and use that exact name in the client's `Slot` setting.
4. Click **Generate** in the Launcher and host `<Archipelago>\output\AP_XXXXX.zip`. Older generated seeds retain their old slot data and datapackage.
5. Run RhythmCastleAP **v0.67.59** with `DirectStartAtPhoneHub=true`, `EnableAreaAccessPrototype=true`, and `PrototypeStartingArea=AP`; keep the retired `RandomizeEarlyProgression` prototype false.

v0.15 (`area-routing-plant-pipes-0.15`) forces Roots as the starter area, retains Weed Killer and cartridge randomization, and adds Plant Pipes plus the Level 3 Frog/Hippo source/completion split. Rebuild/replace both the client and APWorld together, restart Archipelago, and generate a fresh seed after APWorld changes.
