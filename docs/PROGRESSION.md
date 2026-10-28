# Progression reference

Current APWorld: **0.29.0**. Client 0.76.2 does not change these rules.

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

## Cell Tower escape routes (Client 0.76.2)

For authenticated Cell Tower Access owners, the Central Mainframe external gate back to the Cell Tower phone area is kept open during native room initialization. Exiting from that room toward Lobby is redirected to Music Lab until native Fish Tears delivery is confirmed. This does not grant Lobby Access, bypass level-entry requirements, or mark quests complete. Both reported escape routes passed targeted user testing.

## Unreleased Cell Tower route

New seeds explicitly enable the accepted pre-nectar Old Tom route. Thief Prince requires Cell Tower Access and its own generated Star gate, without Mainframe or nectar completion. The Star Eater retains the achievable note route, Violance and its generated gate; Cold Storage keeps its other requirements. Existing seeds retain their original Tracker rules. See [test and compatibility record](testing/2026-10-22-cell-tower-path.md).

## Unreleased Lobby door access

Demolition Training and School Trip entrance barriers are removed for authenticated Lobby Access owners. Boring Room, Important Letters delivery, the story cutscene and Demolition Training completion are no longer door prerequisites. Generated Star requirements, completion rewards, certificate award and Gecko quest objectives remain unchanged. The patch targets only the two exact native doors and their covers; it does not write story flags.

New seeds advertise `lobby_open_doors` and require the corresponding client hooks. Universal Tracker preserves the old prerequisite graph for older seeds. The candidate is not installed or published; native door interaction and School Trip's Gecko variant still require acceptance.

## Unreleased Bunker and True Ending APWorld

Development-only schema 21 adds randomized Bunker Keycard, Demon Key, Virus, Cheat Codes and Game Ender, plus six checks: Virus retrieval, Cheat Codes retrieval, Game Ender creation/use, Demon Key pickup and True Ending completion. Existing IDs are preserved. Enabled-mode check totals are Normal 170, Hard 228, Expert 286 and Perfection 322.

`bunker_progression` defaults to enabled; `goal` defaults to `king_ferdinand`, with `true_ending` available when Bunker progression is enabled. Bunker entry requires the Keycard and the modeled Lobby route. Gecko supplies the native Data Stick and Note Pad. Received Game Ender can bypass its upstream crafting sources; Demon Key gates the separate Devil quest, not True Ending. The Bunker Star Eater retains its randomized Superstar source and requires strictly fewer Stars than Level 22's entrance (minimum Level 22 gate 1; Bunker gate may be 0). Enabled mode requires a Star goal of at least 2.

Legacy schema 19/20 reconstruction preserves its original items, checks and Bunker rules. Schema 21 records the chosen goal, permanent IDs, bundle groups and native True Ending flag for Tracker restoration. **The installed client does not support schema 21 yet.** The unreleased client candidate implements receipt delivery, source suppression and selected-ending reporting; native gameplay acceptance is still required before release.
