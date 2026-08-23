# Client v0.67.63 native repair identities

**Date:** 2026-08-23  
**Build:** Client v0.67.62 temporary read-only diagnostic  
**Seed:** retained APWorld v0.18 test seed on `localhost:38281`  
**Save:** retained fresh-seed test save

The temporary diagnostic enumerated candidates and native state without changing the save, object activation, progression, or AP ownership.

## Difficulty

| Room/state | Exact type or request | Member/method | Vanilla state | Desired AP state | Evidence |
| --- | --- | --- | --- | --- | --- |
| AP startup / first Roots bookkeeping | `AssignAndCommentOnMusicDifficultySequenceStep` | `Begin()` / `MarkAsComplete()` | Sequence is normally responsible for the early difficulty presentation. | Complete only the approved bookkeeping after both choices remain available. | Existing Harmony hook logs `EARLY SEQUENCE BLOCKER HOOKED AssignAndCommentOnMusicDifficultySequenceStep.Begin`. |
| Player choice | `SetPlayerTrackingDifficultyRequest` processed by `PlayerModificationRequestProcessor` | `DifficultyLevel` | Native request records the player's actual Normal/Pro choice. | Never synthesize or block this request during compatible AP startup. | Existing exact hook logs `RUNTIME DIFFICULTY BLOCKER HOOKED PlayerModificationRequestProcessor.ProcessRequest(SetPlayerTrackingDifficultyRequest)`. |

Rejected: no managed availability enquiry was emitted by the temporary reflection scan (`difficultyTypes=0`). Availability must therefore be repaired through the verified startup bookkeeping/request flow, not a guessed `IsProAvailable` member.

## Star HUD

| Room/state | Exact path or flag | Member/method | Vanilla state | Desired AP state | Evidence |
| --- | --- | --- | --- | --- | --- |
| Music Lab / `GameRoom_Hub6` | Native Music Lab Points HUD | Native room HUD lifecycle | Star count is intentionally absent and replaced by Music Lab Points. | Preserve vanilla Music Lab HUD exactly. | Project owner confirmed this is normal native behavior on 2026-08-23. |
| Roots / `GameRoom_Hub2` after direct-start save | `ROOTS_HUB_GATE_OPENED` | `RecordGameProgressionInSaveDataRequest` | Missing bootstrap bookkeeping; no Star HUD object was instantiated. | Set and verify during compatible AP save bootstrap. | Project owner confirmed Star HUD absent in Roots; diagnostic returned `hubCandidates=0`. |
| Roots / `GameRoom_Hub2` after direct-start save | `ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE` | `RecordGameProgressionInSaveDataRequest` | Missing bootstrap bookkeeping; difficulty presentation remains pending. | Set and verify during compatible AP save bootstrap. | Flag is already approved by `2026-08-21-next-release-repair-design.md`; zero live HUD candidates rules out an activation-only repair. |

Rejected: no `StarCounter`/`StarHud` transform or component existed in the broken Roots state. Do not force a guessed UI object active. Let native campaign HUD initialization occur after the two exact bookkeeping flags are reconciled.

## Music Lab barriers

| Room | Exact path | Member/method | Vanilla state | Desired AP state | Evidence |
| --- | --- | --- | --- | --- | --- |
| `GameRoom_Hub6` | `Root/GameRoom_Hub6_Logic/Objects/Barriers-tofb/Barriers_Lobby` | root activation | Active; owns Lobby barrier visuals, collision, and cassette-owned conditions. | Inactive only during compatible AP sessions. | Diagnostic enumerated active Lobby `Collision/Box`, `Barriers/Barrier.01`, and cassette conditions including Lets Go, The Heist, Money Dub, Rainbow, Jolt City, and No Plan B. |
| `GameRoom_Hub6` | `Root/GameRoom_Hub6_Logic/Objects/Barriers-tofb/Barriers_Final` | root activation | Active; owns Final barrier collision and cassette-owned conditions. | Inactive only during compatible AP sessions. | Diagnostic enumerated active Final `Collision/Box` and conditions including Hollywood Trailer, Quicksand, Fumblin Around, Another Day in Paradise, and Zen. |

Rejected: individual `Condition/CassetteOwned_*`, `Collision/Box`, and visual `Barrier*` children are not separate repair targets. Disabling the two exact parent roots is narrower and prevents partial visual/collision state.

## Game Garage

| Room | Exact path or type | Member/method | Vanilla state | Desired AP state | Evidence |
| --- | --- | --- | --- | --- | --- |
| Music Lab entrance | Physical cartridge immediately in front of Game Garage | native pickup/entry interaction | Must be picked up before the player can enter normally. | Preserve the pickup and native entry requirement. | Project owner confirmed normal entry is unavailable until this cartridge is picked up; after pickup the room loaded correctly. |
| `GameRoom_27` | `Root/GameRoom_27_Logic/Objects/Cartridges/CartridgeHolder_*` (six exact holders) | holder root activation | All six holders active. | Keep all holders active regardless of AP cartridge ownership so room initialization completes. | Snapshot #01 logged every holder `activeSelf=True activeInHierarchy=True`. |
| `GameRoom_27` | each holder's `GR27_GameCartridge_*` child | child activation and normal insertion interaction | All six inactive when AP ownership is empty. | Activate only the matching AP-owned song cartridge. | Runtime state logged `receivedSoFar='<none>'`; baseline and all six song states were inactive while the room remained visible and usable. |
| `GameRoom_27` | `GameRoom_27_Script/MiscSequences/Play*/SetScoredSong` | normal play sequence | Six play sequences exist and remain initialized. | Preserve; AP ownership gates the matching cartridge child, not these sequences. | F5 resolved all six exact `SetScoredSong` objects and play sequences. |

Rejected: deactivating `Objects/Cartridges`, a `CartridgeHolder_*`, or `Play*` sequence. The observed working zero-AP-ownership state requires those initialization roots to remain alive.

## Resulting implementation constraints

- Preserve Music Lab Points HUD; repair campaign Star HUD bootstrap only.
- Submit only `ROOTS_HUB_GATE_OPENED` and `ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE` for startup bookkeeping, plus the separately scoped `ROOTS_HUB_INTRO_WITNESSED` arrival bypass.
- Do not block or synthesize `SetPlayerTrackingDifficultyRequest`; preserve the player's Normal/Pro selection.
- Disable exactly the two `Barriers-tofb` parent roots during compatible AP Hub6 sessions.
- Preserve the physical Garage entry cartridge, six holder roots, and six play sequences; ownership controls only each `GR27_GameCartridge_*` child.
