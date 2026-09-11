# Super Crazy Rhythm Castle Archipelago setup — v0.24.0 candidate

Client v0.70.0 / APWorld v0.24.0 is an experimental candidate requiring manual acceptance, not a release. The exact top-level contract is slot-data schema 15 / campaign-mapping schema 1. Use a fresh v0.24 seed and a fresh native save. Broad campaign replay remains pending.

The [public installation guide](../../../docs/INSTALL.md) is the canonical setup flow. It covers trusted custom-world safety, BepInEx first launch, client configuration, APWorld installation, generation, hosting, updates, and uninstalling.

Developer setup summary:

1. Build the world from the repository root with `./tools/build-apworld.ps1`; it creates `dist/scrc.apworld`.
2. Install `scrc.apworld` through Archipelago Launcher's **Install APWorld** action (or double-click/drag it onto the launcher), then restart the Launcher.
3. In Archipelago Launcher, choose **Generate Template Options** to create templates in `<Archipelago>\Players\Templates`, or start with [SCRC-AreaRouting-PlantPipes.yaml](../../examples/SCRC-AreaRouting-PlantPipes.yaml). Copy the chosen YAML to `<Archipelago>\Players` as an uncompressed `.yaml`, edit its top-level `name:` to the intended slot name (the example starts as `name: Jack`), and use that exact name in the client's `Slot` setting.
4. Click **Generate** in the Launcher and host `<Archipelago>\output\AP_XXXXX.zip`. Older generated seeds retain their old slot data and datapackage.
5. After candidate build/review and approval for live testing, run RhythmCastleAP **v0.70.0** with `DirectStartAtPhoneHub=true`, `EnableAreaAccessPrototype=true`, and `PrototypeStartingArea=AP`; keep the retired `RandomizeEarlyProgression` prototype false.

## Music Lab Points

Ten 1-point items, three 10-point bundles, and seven 20-point large bundles replace 20 Stardust: 180 total points, with a 140-point final chest and 40 points of slack. The nine existing thresholds are 5/10/20/32/46/64/89/111/140. No point milestones or new chest locations are created; five cassette and two cartridge sources reuse those same chest checks.

Top-level schema 15 / campaign-mapping schema 1 is required for the full normal map. The retained Music Lab Point sub-contract uses point schema 1: exact names, IDs, values, counts, totals, cap, and threshold-to-location map must match. Compatible sessions show zero before history sync, then the weighted AP total (capped at 180), retaining it through temporary disconnects and rebuilding it on reconnect. Native medals never contribute. Recognized v0.22 seeds and non-AP play retain the native medal economy; malformed point contracts report incompatibility and keep the effective total at zero.

Only the existing managed score getter in the Music Lab hub (`GameRoom_Hub6`) substitutes AP totals; other rooms retain native/developer behavior. The diagnostic-first investigation rejected the generated chest hook and native detour. The candidate installs neither, writes no native medal/save state, and never forces a chest interaction. Test the display, every threshold below/at, each existing check exactly once, disconnect/reconnect, relaunch, native medal invariance, and both compatibility regressions before acceptance. Do not use Shift+F4 to supply points; compatible AP state takes precedence over that developer override.

## AP performance difficulty

Set `difficulty` in the YAML to one of `normal`, `hard`, `expert`, or `perfection`. It filters only existing AP campaign performance locations; native REG/PRO stays player-controlled.

| AP difficulty | Campaign tiers | Song medal tiers | Addressed locations |
| --- | --- | --- | ---: |
| Normal | Completion / 1-Star | Bronze | 121 |
| Hard | Add 2-Star | Add Silver | 179 |
| Expert | Add 3-Star | Add Gold | 237 |
| Perfection | Same campaign tiers as Expert | Add Platinum | 273 |

Inactive checks are absent from the seed, not filler. v0.24 maps all 22 normal identities, retains performance filtering and the full cassette source set, and has these location totals. Bee/Devil diagnostics are observation-only; special locations, AP Stars, Star gates, and Victory remain inactive. Level-22 2/3-Star checks remain filler-only; point chests may hold progression when their weighted AP-point rules give the solver a reachable chain.

v0.24.0 retains the experimental implementation for all 30 Music Lab cassette items and sources; many individual routes remain manual verification pending. Rebuild/replace the APWorld, restart Archipelago, and generate a fresh v0.24 seed. Client v0.70.0 enables cassette routing only when schema, count, item mappings, source mappings, and reused chest mappings match exactly; otherwise it logs the mismatch and leaves native cassette behavior enabled. This cassette fallback is separate from malformed point contracts, which fail closed at zero.
