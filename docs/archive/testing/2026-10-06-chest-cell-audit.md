> Historical record. Dates, versions and candidate statuses below describe that checkpoint; consult the [current documentation](../../INDEX.md).

# October 6 native chest and Cell Tower audit

## Music Lab cartridge chests

Read-only extraction of installed Unity bundle `11e1657dcb37907aef75e48af324519f.bundle` confirms:

| Source | Interaction | Block compound | Ownership | Source flag |
| --- | --- | --- | --- | --- |
| Bloody Tears / 46 points | 10196 | 10511, mode 1 | HasBagItemCondition 10665, bag 2271 | 10659, flag 127011 |
| Gradius / 10 points | 10154 | 9886, mode 1 | HasBagItemCondition 9689, bag 2277 | 9770, flag 127018 |

Both compounds OR the bag-ownership condition with the collected-source condition. Received cartridges therefore block unopened checks while still in inventory. Point totals do not repair that independent block.

Candidate repair uses the existing native CompoundCondition dispatch. Only these exact compound paths return their native collected-source flag, omitting the bag-ownership branch:

- `Root/GameRoom_Hub6_Logic/Objects/RewardChests/GarageCartridgeChest_BloodyTears/Interaction/Condition`
- `Root/GameRoom_Hub6_Logic/Objects/RewardChests/GarageCartridgeChest_Gradius/Interaction/Condition.01`

Native flags are `LEVEL_27_CARTRIDGE_BLOODYTEARS_COLLECTED` and `LEVEL_27_CARTRIDGE_GRADIUS_COLLECTED`. Unknown flag reads return no override. Existing enabled-session, Hub6 and stable-save guards remain; native point threshold logic and collected flag writes remain unchanged. Gradius's `Condition.01` is admitted by the component-name fast filter. No new native hook or Plugin.cs registration is needed.

Verification: new regression fails against old policy with the expected unopened-chest assertion. After repair: QuestChecks 67 assertions, native dispatch 8, native sequence admission 3 and shared ownership 8 pass; QuestRuntime 55 assertions pass; MusicLabPoints suite passes, including native read bypass, room scope and point thresholds. Regression covers both exact paths, collected/uncollected/unknown state, and unrelated/leaf paths. No deployment or live acceptance performed. Same-save live retest should hold each received cartridge, open its unchecked chest at the native point threshold, verify one AP check and preserved inventory, then re-enter/restart and verify no duplicate check. Also verify below-threshold refusal and disabled-session vanilla behavior.

## Cell Tower Star Eater

Read-only extraction of bundle `492a3f1dba2594f7a6e05aa5cb82b4b6.bundle`:

- `Objects/StarEater/StarEater`: component3383 `SetActiveAfterRegistered`, activeCondition3697.
- 3697 `CompoundCondition`, named `StarEaterActiveCondition`, mode0 (all), references3914 and3744.
- 3914 `IsGameProgressionFlagNotSetCondition`, flag823 (`PRISON_HUB_STAR_EATER_FED`).
- 3744 `HasAbilityItemCondition`, named `ViolinHeistCompletedCondition`, abilityItem30.
- Native interop metadata confirms `eAbilityItemType.VIOLIN=30`. Client `ViolanceReconciler` grants `VIOLIN_ABILITY`.
- StarEaterInteraction3824 itself has no required/block condition, sets interacted822/full823, and uses the generated threshold adapter.

The condition name sounds like a Lobby story check, but its actual input is randomized Violance ownership. Proposed minimal world rule is existing `can_complete(world,state,16)` plus `state.has('Violance',world.player)` plus existing Cell Star count. Do not additionally force Cold Storage/Lobby completion just to reproduce the vanilla item acquisition route. World code was not edited by this audit. Tests should reject without Violance despite all existing prerequisites; accept with Violance and the current route; preserve the existing missing Plant Pipes/area/Stars rejections. Live confirmation remains necessary for any additional physical route constraints.

## Roots phone booth portal visual

Native bundle `908ebcb5fb23373ad7ddecfee53b70ff.bundle`, exact hierarchy:

`Root/GameRoom_Hub2_Logic/Objects/PhoneBox/CleanRoomsDoorway-tofb` (GO456)

- `Doorway` GO1649: MeshRenderer4944 and MeshFilter5943.
- `Doorway/BlockingCollision` GO1434: BoxCollider6298.
- sibling `CleanRoomsInteraction` GO939: Switch7642, with child `OnTrigger` GO1268 carrying LeaveRoomTriggerReactor6781.
- root has NetworkSyncRoomObjectView7360, synchronizeActivity enabled.

To hide only the purple portal while Gecko source remains incomplete, suppress only the exact Doorway MeshRenderer, with restoration on source completion/room change/disabled session. Do not deactivate Doorway or its root: that would also alter collision or interaction. User confirmed the purple portal covers Gecko/Weed Killer. Candidate helper `RootsPortalPresentation` now binds only this exact MeshRenderer and polls source state once per second, with at most30 failed binding attempts per active room/session. It restores only renderer state it changed on collection, disable, room exit or reset. Parent integration into the existing Roots keeper is required. Ten controller assertions passed after a red regression, including initially disabled renderers, unknown source state, reset restoration and bounded binding. Gameplay acceptance remains pending.

Native Gecko visibility uses flag250: SpawnCondition7333 and NotVanished6872 require it false. WeedKillerBagItem/InteractCondition7707 requires250 true. Therefore the portal must remain hidden across Gecko disappearance until source flag260 is collected: restoring at Gecko disappearance would obscure the dropped Weed Killer. The helper uses ROOTS_HUB_WEED_KILLER_COLLECTED, independent of received Weed Killer ownership.

## October 11 Bloody Tears chest gameplay acceptance

On the retained local test seed, the user had an unopened 46-point chest and 40 Music Lab points. Server test grants supplied Bloody Tears Cartridge and six single Music Lab points. Native readback confirmed bagHeld=True/NotInserted and synchronized points=46. The user confirmed the chest opened successfully. Log evidence confirms source flag 127011, one SENT CHECK for Music Lab - 46 Point Chest (187256173), duplicate callbacks ignored, and the vanilla bag reward suppressed. This accepts the previously blocked chest while already carrying its vanilla cartridge; no reset or reload was requested. Evidence remains outside Git in work/chest-acceptance-20261011/accepted-46-point-chest.log.
