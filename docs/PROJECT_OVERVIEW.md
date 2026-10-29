# Project overview

Unofficial Windows Archipelago integration for Super Crazy Rhythm Castle, combining a C# BepInEx/IL2CPP client with a Python APWorld.

Current release: **v0.30.0 / Client 0.77.0 / APWorld 0.30.0**; supports legacy schemas19/20 and new schema21 Bunker seeds.

## Implemented systems

- Area access, supported quest/progression items and character ownership, cassette/cartridge sources and result checks.
- 66 AP Stars, generated level-entry requirements, non-consuming Star Eaters and configurable Victory after Level 22: King Ferdinand I.
- Music Lab points totaling 180, nine reward chests, cassette medals and Game Garage medals.
- Individual cassettes or random pairs/triples stored in seed slot data; native deposit flow and notifications naming component songs.
- In-game notifications/F6 history, reconnect delivery, seed/save safeguards and Universal Tracker reconstruction.
- Native display settings retained; Montage timing repair, plus the separately accepted player timing repair released in 0.76.1.

Active checks: **Normal 164 / Hard 222 / Expert 280 / Perfection 316**. AP performance difficulty is separate from Regular/Pro.

## Current evidence and limits

A Hard/40-Star run completed with server-confirmed Victory without assistance. Recent targeted acceptance includes movement, item delivery, entrances and source suppression; [testing](TESTING.md) distinguishes that from automated checks. See [known issues](KNOWN_ISSUES.md) for remaining modeling/performance limits.

Existing IDs remain stable; schema21 adds five items and six checks. Schemas19/20 retain their original rules and placements.

[Progression](PROGRESSION.md) · [IDs](IDS.md) · [Workflow](WORKFLOW.md) · [Historical overview](archive/checkpoints/docs-PROJECT_OVERVIEW.md)

## Cell Tower escape repair

Client 0.76.2 keeps the Central Mainframe return gate usable for Cell Tower Access owners and redirects unsafe upper-Lobby arrivals to Music Lab. Both reported routes passed user testing; quest flags and APWorld logic are unchanged. See [acceptance record](testing/2026-10-22-hub-escape.md).

## Lobby door access

Demolition Training and School Trip entrance barriers are removed for authenticated Lobby Access owners. Boring Room, Important Letters delivery, the story cutscene and Demolition Training completion are no longer door prerequisites. Generated Star requirements, completion rewards, certificate award and Gecko quest objectives remain unchanged. The patch targets only the two exact native doors and their covers; it does not write story flags.

New seeds advertise `lobby_open_doors` and require the corresponding client hooks. Universal Tracker preserves the old prerequisite graph for older seeds. Broader native door and story-order coverage remains useful.

## Bunker and True Ending

Schema21 adds randomized Bunker Keycard, Demon Key, Virus, Cheat Codes and Game Ender, plus six checks: Virus retrieval, Cheat Codes retrieval, Game Ender creation/use, Demon Key pickup and True Ending completion. Existing IDs are preserved. Enabled-mode check totals are Normal 170, Hard 228, Expert 286 and Perfection 322.

`bunker_progression` defaults to enabled; `goal` defaults to `king_ferdinand`, with `true_ending` available when Bunker progression is enabled. Bunker entry requires the Keycard and the modeled Lobby route. Gecko supplies the native Data Stick and Note Pad. Received Game Ender can bypass its upstream crafting sources; Demon Key gates the separate Devil quest, not True Ending. The Bunker Star Eater retains its randomized Superstar source and requires strictly fewer Stars than Level 22's entrance (minimum Level 22 gate 1; Bunker gate may be 0). Enabled mode requires a Star goal of at least 2.

Legacy schema 19/20 reconstruction preserves its original items, checks and Bunker rules. Schema 21 records the chosen goal, permanent IDs, bundle groups and native True Ending flag for Tracker restoration. Client0.77.0 implements schema21 receipt delivery, source suppression and selected-ending reporting; complete randomized gameplay acceptance remains pending.
