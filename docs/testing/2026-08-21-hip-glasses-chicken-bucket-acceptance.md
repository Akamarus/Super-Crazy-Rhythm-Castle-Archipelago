# Hip Glasses and Chicken Bucket Gameplay Acceptance

**Status:** Automated implementation complete; deployment and gameplay acceptance pending

## Test identity

| Field | Value |
| --- | --- |
| APWorld version | `0.17` |
| APWorld SHA-256 | `4F7F10C5132C4EB9A7F9B4774ACED99BC5A8858383F915FC77E7844AF54E8BF8` |
| Client version | `0.67.60` |
| Client SHA-256 | `AC2D9200AEB0D9DA552725ED7984C23AC39C3FF989E383C8DCFF4BEF445E22EE` |
| Seed name | Pending |
| Slot name | Pending |
| Fresh save identity | Pending |
| Native difficulty | Pending |
| Player count | Pending |
| Start time | Pending |
| End time | Pending |
| Focused log path | Pending; do not commit the log |
| Tester | Pending |

## Acceptance checklist

- [ ] Before Level 4, neither new native bag item is held and neither new AP source is checked.
- [ ] Level 4 sets `LEVEL_08_GLASSES_COLLECTED`, sends `Roots - Level 4 - Hip Glasses` exactly once, and does not grant `HIP_GLASSES_BAG_ITEM` locally.
- [ ] AP-delivered Hip Glasses sets `HIP_GLASSES_BAG_ITEM` and permits the normal Bucket Minion interaction without forcing it.
- [ ] The normal trade consumes Hip Glasses, sends `Roots - Bucket Minion Trade` once, preserves dialogue/blockade/King behavior, and does not grant Chicken Bucket locally.
- [ ] AP-delivered Chicken Bucket sets `CHICKEN_BUCKET_BAG_ITEM` and permits normal Lift Quest use without forcing it.
- [ ] Lift Quest consumes Chicken Bucket, grants `COMBO_BUCKET_ABILITY`, sets `LEVEL_09_COMBO_ABILITY_EARNED` and `LEVEL_09_COMPLETED`, and reaches the native Lobby cutscene in `GameRoom_Hub1A`.
- [ ] Save/reload and disconnect/reconnect while each item is held preserve it without duplicate checks.
- [ ] Save/reload and disconnect/reconnect after each item is consumed do not restore it or duplicate checks/abilities.
- [ ] An incompatible old seed leaves both source grants vanilla.
- [ ] A non-AP save leaves the complete vanilla chain unchanged.
- [ ] Existing Weed Killer, Plant Pipes, Area Access, Garage, Music Lab, and starter behavior remain active.

## Evidence table

| Stage | Room | Native flags before/after | AP item/check evidence | Reload/reconnect result | Notes |
| --- | --- | --- | --- | --- | --- |
| Pre-Level 4 | Pending | Pending | Pending | Pending | |
| Level 4 source | `GameRoom_08` | Pending | Pending | Pending | |
| Hip Glasses held | Pending | Pending | Pending | Pending | |
| Bucket Minion trade | `GameRoom_Hub2` | Pending | Pending | Pending | |
| Chicken Bucket held | Pending | Pending | Pending | Pending | |
| Lift Quest conversion | `GameRoom_09` | Pending | Pending | Pending | |
| Lobby arrival | `GameRoom_Hub1A` | Pending | Pending | Pending | |
| Incompatible/vanilla control | Pending | Pending | Pending | Pending | |

## Combo Bucket feasibility report

Use one row per later check attempted without `COMBO_BUCKET_ABILITY`.

| Level/song/objective | Native difficulty | AP tier | Score/stars/medal/objective | Players | Other abilities | Attempts/best result | Log/video | Conclusion |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Pending | Pending | Pending | Pending | Pending | Pending | Pending | Pending | Pending |

A success proves feasibility only under the reported conditions. An unsuccessful attempt is evidence, not proof of impossibility, and does not by itself justify a solver requirement.

## Final result

Pending. Do not merge or push until every required row is resolved and the project owner approves integration.
