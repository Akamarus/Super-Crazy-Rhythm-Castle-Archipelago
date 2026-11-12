# Project overview

Unofficial Windows Archipelago integration for Super Crazy Rhythm Castle, combining a C# BepInEx/IL2CPP client with a Python APWorld.

Release **v0.30.1 / Client 0.77.1 / APWorld 0.30.0** supports schema 21 Bunker seeds and older seeds using schemas 19 and 20.

## Implemented systems

- Area access, supported quest/progression items and character ownership, cassette/cartridge sources and result checks.
- 66 AP Stars, generated level-entry requirements, non-consuming Star Eaters and a choice of King Ferdinand I or True Ending for Victory.
- Music Lab points totaling 180, nine reward chests, cassette medals and Game Garage medals.
- Individual cassettes or random pairs/triples stored in seed slot data; native deposit flow and notifications naming component songs.
- In-game notifications/F6 history, reconnect delivery, seed/save safeguards and Universal Tracker reconstruction.
- Native display settings retained; Montage timing repair, plus the separately accepted player timing repair released in 0.76.1.

With Bunker progression enabled (the default), the active checks are **Normal 170 / Hard 228 / Expert 286 / Perfection 322**. With it disabled, they are **Normal 164 / Hard 222 / Expert 280 / Perfection 316**. AP performance difficulty is separate from Regular/Pro.

## Current evidence and limits

A Hard/40-Star run completed with server-confirmed Victory without assistance. Recent targeted acceptance includes movement, item delivery, entrances and source suppression; [testing](TESTING.md) distinguishes that from automated checks. See [known issues](KNOWN_ISSUES.md) for remaining modeling/performance limits.

Schemas 19 and 20 retain their original rules and placements.

[Progression](PROGRESSION.md) · [IDs](IDS.md) · [Workflow](WORKFLOW.md) · [Historical overview](archive/checkpoints/docs-PROJECT_OVERVIEW.md)

## Cell Tower escape repair

Client 0.76.2 keeps the Central Mainframe return gate usable for Cell Tower Access owners and redirects unsafe upper-Lobby arrivals to Music Lab. Both reported routes passed user testing; quest flags and APWorld logic are unchanged. See [acceptance record](testing/2026-10-22-hub-escape.md).

## Lobby door access

Lobby Access opens the Demolition Training and School Trip entrances in new seeds. Generated Star requirements still apply. See the [Lobby door rules](PROGRESSION.md#lobby-door-access-in-v0300) and [test record](testing/2026-10-28-lobby-doors.md).

## Bunker and True Ending

Schema 21 adds five Bunker items and six checks. The client delivers received items, suppresses the corresponding vanilla grants and reports the selected ending. End-to-end gameplay acceptance is still open. See the [progression rules](PROGRESSION.md#bunker-and-true-ending) and [Bunker acceptance record](testing/2026-10-28-bunker-client.md).

## v0.30.1 network protection

Remote addresses default to TLS with no automatic plaintext retry. A bounded pre-parse reader limits decompressed messages to 32 MiB and partial-message duration to 60 seconds. The existing dependency binaries are unchanged; a signature-checked Harmony guard must load before any client connection. See [security validation](testing/2026-11-08-network-security.md).
