# Progression reference

**APWorld 0.30.2**, included in release v0.30.3.

## Stars, results and Victory

The pool contains 66 AP Stars. Each seed stores its generated entrance and Star Eater thresholds, difficulty and goal. Stars are requirements, not currency spent at doors or Star Eaters. Native earned result ratings remain separate from received AP Stars.

Collect the configured number of Stars (default 50, maximum 66), then complete the selected ending. The default goal requires a successful Level 22 King Ferdinand I clear. The True Ending goal also requires installing the Game Ender and reaching Gecko's ending. A clear before reaching the Star goal must be replayed. Bunker progression requires at least two Stars for the goal. With Bunker progression disabled, the minimum is one. AP Normal/Hard/Expert/Perfection controls which performance checks are available. Players choose Regular/Pro charts in the game.

## Access and items

Area Access items open their supported routes. Roots is the validated starting area. Lobby Access independently opens Boring Room, Minim Tower and The Vault, subject to each level's Star gate; Lift Quest completion is not required for these routes.

Plunger use requires Lobby Access and Plunger. Lobby Star Eater additionally requires the seed's Star threshold. Roots Level 3's extra door and Level 4's plants require Weed Killer. Plant Pipes is required for Level 15 completion, Level 16's higher ratings and Level 22; Loneliness and Party Non Stop require Hypno Pan.

`Cell Tower - Deliver Super Nectar` requires Hypno Pan for the Bee Mode nectar routes, in addition to its existing campaign routes, area access and Star gates. APWorld 0.30.2 corrects the missing dependency in generation and Universal Tracker. Existing seed placements remain unchanged.

These highlights are not the complete rule table. The [reviewed prerequisite audit](testing/2026-10-12-user-rule-audit.md), [APWorld rule source](../apworld/scrc/__init__.py) and [known limitations](KNOWN_ISSUES.md) provide the full implementation context. Native uncertainty must not be inferred away from tracker agreement.

## Cassettes, cartridges and Music Lab

Each seed has 30 cassettes, available individually or grouped into 15 pairs or 10 triples. Reconnects and Tracker restore the groups from slot data. Bunker seeds use schema 21 for every cassette mode. With Bunker progression disabled, individual cassettes use schema 19 and bundles use schema 20. Source and medal checks are separate.

AP Music Lab point items total 180; chest thresholds are 5, 10, 20, 32, 46, 64, 89, 111 and 140. Generation may consolidate point denominations to fit excluded checks without reducing the total or placing progression in excluded locations. Game Garage cartridges keep their native insertion flow.

Updating a client or local APWorld does not regenerate a running seed or change its placements. Use a fresh associated save for a new seed. Permanent IDs remain in [IDS.md](IDS.md).

[Historical progression details](archive/checkpoints/docs-PROGRESSION.md) preserve earlier contracts and development decisions.

## Cell Tower escape routes (Client 0.76.2)

For authenticated Cell Tower Access owners, the Central Mainframe external gate back to the Cell Tower phone area is kept open during native room initialization. Exiting from that room toward Lobby is redirected to Music Lab until native Fish Tears delivery is confirmed. This does not grant Lobby Access, bypass level-entry requirements, or mark quests complete. Both reported escape routes passed targeted user testing.

## Cell Tower route in v0.30.0

New seeds explicitly enable the accepted pre-nectar Old Tom route. Thief Prince requires Cell Tower Access and its own generated Star gate, without Mainframe or nectar completion. The Star Eater retains the achievable note route, Violance and its generated gate; Cold Storage keeps its other requirements. Existing seeds retain their original Tracker rules. See [test and compatibility record](testing/2026-10-22-cell-tower-path.md).

## Lobby door access in v0.30.0

For authenticated Lobby Access owners on a marked seed, the client clears the story condition on the Demolition Training door and the Training-completion condition on the School Trip door. These changes apply to the doors and their covers. The upper path to School Trip still has a separate mail blocker.

On seeds with `lobby_open_doors`, generation requires a route through Boring Room and the Important Letters delivery for School Trip. Lobby Access and School Trip's own Star gate still apply. Demolition Training is independent. The player tested the ordinary entrance on slot 2 after delivering the mail. The door denied entry at four Stars and opened at five while Demolition Training was unfinished.

With the updated game client and APWorld, Universal Tracker waits for the server to confirm both Level 6 completion and Important Letters delivery. The client publishes that team's and slot's progress on login and when checks arrive. With deferred entrances turned off, Tracker shows when the mail route is possible from inventory. Older seeds retain their original door rules.

## Bunker and True Ending

Schema 21 adds five randomized items (Bunker Keycard, Demon Key, Virus, Cheat Codes and Game Ender) and six checks covering Virus retrieval, Cheat Codes retrieval, Game Ender creation and use, Demon Key pickup and True Ending completion.

`bunker_progression` defaults to enabled; `goal` defaults to `king_ferdinand`, with `true_ending` available when Bunker progression is enabled. Bunker entry requires the Keycard and the modeled Lobby route. Gecko supplies the native Data Stick and Note Pad. Received Game Ender can bypass its upstream crafting sources; Demon Key gates the separate Devil quest, not True Ending. The Bunker Star Eater retains its randomized Superstar source and requires strictly fewer Stars than Level 22's entrance (minimum Level 22 gate 1; Bunker gate may be 0). Enabled mode requires a Star goal of at least 2.

Tracker preserves the original items, checks and Bunker rules for schemas 19 and 20. Schema 21 records the chosen goal, permanent IDs, bundle groups and native True Ending flag. Clients from 0.77.0 onward support schema 21. Full randomized gameplay acceptance remains pending. [Bunker test record](testing/2026-10-28-bunker-client.md)
