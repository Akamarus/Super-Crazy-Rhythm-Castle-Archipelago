# Project overview

Unofficial Windows Archipelago integration for Super Crazy Rhythm Castle, combining a C# BepInEx/IL2CPP client with a Python APWorld.

Current release: **v0.29.1 / Client 0.76.1 / APWorld 0.29.0**, retaining the v0.29.0 APWorld and seed contracts.

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

The maintenance patch changes no item/location IDs, APWorld logic, save formats, seed placements or bundle groups. Schemas 19 (individual) and 20 (bundled) remain supported.

[Progression](PROGRESSION.md) · [IDs](IDS.md) · [Workflow](WORKFLOW.md) · [AI disclosure](AI_DISCLOSURE.md) · [Historical overview](archive/checkpoints/docs-PROJECT_OVERVIEW.md)
