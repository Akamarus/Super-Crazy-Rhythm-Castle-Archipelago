# Progression reference

Current APWorld: **0.29.0**. The Client 0.76.1 movement repair does not change these rules.

## Stars, results and Victory

The pool contains 66 AP Stars. Each seed stores its generated entrance and Star Eater thresholds, difficulty and goal. Stars are requirements, not currency spent at doors or Star Eaters. Native earned result ratings remain separate from received AP Stars.

Collect the configured goal (1–66, default 50), then successfully clear Level 22: King Ferdinand I. A pre-goal clear does not qualify retroactively. AP Normal/Hard/Expert/Perfection controls check requirements, not Regular/Pro chart selection.

## Access and items

Area Access items open their supported routes. Roots is the validated starting area. Lobby Access independently opens Boring Room, Minim Tower and The Vault, subject to each level's Star gate; Lift Quest completion is not required for these routes.

Plunger use requires Lobby Access and Plunger. Lobby Star Eater additionally requires the seed's Star threshold. Roots Level 3's extra door and Level 4's plants require Weed Killer. Plant Pipes is required for Level 15 completion, Level 16's higher ratings and Level 22; Loneliness and Party Non Stop require Hypno Pan.

These highlights are not the complete rule table. The [reviewed prerequisite audit](testing/2026-10-12-user-rule-audit.md), [APWorld rule source](../apworld/scrc/__init__.py) and [known limitations](KNOWN_ISSUES.md) provide the full implementation context. Native uncertainty must not be inferred away from tracker agreement.

## Cassettes, cartridges and Music Lab

Individual mode retains schema 19. Pair/triple bundles use schema 20, with all 30 cassettes grouped once per seed into 15 pairs or 10 triples. Source and medal checks remain separate; reconnects and Tracker restore the saved groups. Bundled seeds require Client 0.76.0 or newer.

AP Music Lab point items total 180; chest thresholds are 5, 10, 20, 32, 46, 64, 89, 111 and 140. Generation may consolidate point denominations to fit excluded checks without reducing the total or placing progression in excluded locations. Game Garage cartridges keep their native insertion flow.

Updating a client or local APWorld does not regenerate a running seed or change its placements. Use a fresh associated save for a new seed. Permanent IDs remain in [IDS.md](IDS.md).

[Historical progression details](archive/checkpoints/docs-PROGRESSION.md) preserve earlier contracts and development decisions.
