# Known issues and coverage limits

Current release: **v0.30.0 / Client 0.77.0 / APWorld 0.30.0**.

## Open reports and modeling limits

| Area | Current status |
| --- | --- |
| Performance | Intermittent note-affecting song/windowed/Combo Bucket hitches remain under investigation. One recent conversion was smooth; that does not close all performance reports. |
| Player movement | The captured short-update stalls are repaired and targeted gameplay accepted in the 0.76.1 client. Extended-session and multiplayer coverage remains incomplete. Older client 0.76.0 does not contain this player repair. |
| Cold Storage / Cell Tower Star Eater | Early Violance did not block Old Tom's note in the tested post-nectar state; feeding the Cell Tower Star Eater worked without Cold Storage completion after leaving and returning. Full prerequisite isolation remains incomplete; new seeds include the accepted early Cell Tower route. |
| School Trip / cassette aliases | School Trip Plant Pipes requirements and Gold/On the Way alternatives remain under review. |
| Late King Ferdinand ratings | Depend on late-game Gecko content not fully modeled; these checks remain filler-only and are not guaranteed reachable. |
| Bunker scope | Keycard, Demon Key, Virus, Cheat Codes and Game Ender are randomized; Data Stick and Note Pad remain native. Full randomized flow acceptance is pending. |
| Hypno Pan crafting | Crafting can be unavailable when already owned; native alternative recipes are not comprehensively modeled. Ability use passed targeted testing. |
| Old-save cassette leaks | Previously leaked vanilla ownership is not automatically removed from existing saves. |
| Display startup | A fullscreen/fallback warning loop was reported historically. The mod does not force fullscreen or alter display preferences; a general native/driver fix is not established. |

## Accepted fixes and remaining coverage

Client 0.76.2 passed return-gate interaction/traversal and unsafe upper-Lobby exit fallback tests. Other story orders and multiplayer remain broader coverage.

Montage animal movement at normal high frame rates, early Regular/Pro selection, Roots booth visibility, inventory-independent cartridge chests, entrance retry after new Stars, and the tested item delivery paths have user acceptance. Quicksand and Bloody Tears source tests passed without vanilla rewards or duplicate grants after returning. These should not remain labeled wholly unresolved.

Native acceptance does not cover every item/order combination. Triple bundles and large notification batches have automated coverage; pairs and the tested receipts have native acceptance. More seeds/difficulties, cross-player delivery, local co-op, online co-op and long sessions remain useful broader testing.

Tracker/world rule agreement does not establish every native prerequisite. Existing seeds retain their placements when logic is updated.

See [test evidence](TESTING.md), [release history](../CHANGELOG.md), and [historical issue records](archive/checkpoints/docs-KNOWN_ISSUES.md).

## Cell Tower route

The user accepted the pre-nectar raised route, note interaction, area re-entry, Star Eater feeding and onward door. Versioned generation and Tracker rules are implemented and automated validation passes. Normal nectar delivery after taking the early route remains the targeted native regression; this route ships in v0.30.0. See [candidate evidence](testing/2026-10-22-cell-tower-path.md).

## Bunker development compatibility

Schema21 Bunker/True Ending generation and the matching client ship in v0.30.0, with full randomized gameplay acceptance pending. Use a new seed/save for new options; preserve active rooms. Native item receipt/source separation (especially Demon Key), early Bunker entry, received downstream Game Ender usage, Gecko setup replay and goal reporting remain acceptance work. Legacy schema19/20 support remains.
