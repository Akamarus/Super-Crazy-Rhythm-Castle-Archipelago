# Known issues and coverage limits

Current release: **v0.29.1 / Client 0.76.1 / APWorld 0.29.0**.

## Open reports and modeling limits

| Area | Current status |
| --- | --- |
| Performance | Intermittent note-affecting song/windowed/Combo Bucket hitches remain under investigation. One recent conversion was smooth; that does not close all performance reports. |
| Player movement | The captured short-update stalls are repaired and targeted gameplay accepted in the 0.76.1 client. Extended-session and multiplayer coverage remains incomplete. Older client 0.76.0 does not contain this player repair. |
| Cold Storage / Cell Tower Star Eater | Further native prerequisite investigation was deferred; do not treat this as verification. |
| School Trip / cassette aliases | School Trip Plant Pipes requirements and Gold/On the Way alternatives remain under review. |
| Late King Ferdinand ratings | Depend on late-game Gecko content not fully modeled; these checks remain filler-only and are not guaranteed reachable. |
| Bunker scope | Bunker Keycard, Notepad, Data Stick and Demon Key are not all implemented as separate randomized items/checks; conservative Bunker/Demonic assumptions remain. |
| Hypno Pan crafting | Crafting can be unavailable when already owned; native alternative recipes are not comprehensively modeled. Ability use passed targeted testing. |
| Old-save cassette leaks | Previously leaked vanilla ownership is not automatically removed from existing saves. |
| Display startup | A fullscreen/fallback warning loop was reported historically. The mod does not force fullscreen or alter display preferences; a general native/driver fix is not established. |

## Accepted fixes and remaining coverage

Montage animal movement at normal high frame rates, early Regular/Pro selection, Roots booth visibility, inventory-independent cartridge chests, entrance retry after new Stars, and the tested item delivery paths have user acceptance. Quicksand and Bloody Tears source tests passed without vanilla rewards or duplicate grants after returning. These should not remain labeled wholly unresolved.

Native acceptance does not cover every item/order combination. Triple bundles and large notification batches have automated coverage; pairs and the tested receipts have native acceptance. More seeds/difficulties, cross-player delivery, local co-op, online co-op and long sessions remain useful broader testing.

Tracker/world rule agreement does not establish every native prerequisite. Existing seeds retain their placements when logic is updated.

See [test evidence](TESTING.md), [release history](../CHANGELOG.md), and [historical issue records](archive/checkpoints/docs-KNOWN_ISSUES.md).
