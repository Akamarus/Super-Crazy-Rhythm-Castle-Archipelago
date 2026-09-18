# Missing native check sources — schema 19 candidate

These two additions are statically mapped and covered by automated runtime tests. They are **not gameplay accepted**. The user's prior 33/37 acceptance remains valid; the three skipped Demonic checks and Certificate remain unverified and are not required to be replayed during this implementation pass.

| AP location | Permanent ID | Native evidence |
| --- | --- | --- |
| Cell Tower - Star Eater Fed | 187256334 | `eGameProgressionFlag.PRISON_HUB_STAR_EATER_FED = 823`, `GameRoom_Hub5B`, native threshold 30 |
| Royal Corridor - King Ferdinand Unlocked | 187256335 | `ePlayableCharacter.KING = 6`, `GameRoom_28B`, native victory sequence below |

Cell Tower's exact source is `Root/GameRoom_Hub5B_Logic/Objects/StarEater/StarEater/HR05B_StarEater/Interaction`, serialized `StarEaterInteraction.fullFlag = 823`. Introduction flag 822 is not a source. Read-only audit artifacts are `work/batch-native-objects.json` and `work/batch-native-audit.md`; enum values were checked against the installed interop metadata.

King's source is `UnlockPlayableCharacterSequenceStep.Trigger` at `Root/GameRoom_28B_Logic/GameRoom_28B_Script/MiscSequences/End_Win/EarnKingSequence/AwardKing`. Bundle `ea6ae8b732309a1675e9f831a7006686.bundle`, component 24495, serializes `characterToUnlock = 6` and `saveChangeBundleToUse = 2`. Read-only extraction is `work/king-native-objects.json`. The existing direct native step detour observes this exact path, preserves the original action once, and arms a source only when King ownership was false before it ran. This check accompanies the boss victory; it is not a new independent interaction or randomized King ownership item.

Native save bundle persistence can follow the sequence step. Therefore the expanded journal stores a pending native character source before waiting for saved King ownership. A later saved-state read confirms an already observed action; polling ownership alone cannot create one. AP character receipts submit save requests directly and do not pass through the audited source sequence. Journal schema 2 retains support for reading schema 1 files and atomically stores pending sources, completed sources, and the native slot under the AP identity. Legacy schema 1 journal files remain readable by this client.

The slot contract keeps schema 18's exact 37-check map and version suffix unchanged. Schema 19 requires the exact 39-check map and appended `-ap-stars-0.28` suffix; `expanded_checks_schema` remains 1. Old contracts do not bind or emit the new checks. A first bind rejects completed or unreadable Cell/King state, even if the AP server already checked that location.

Automated coverage exercises the actual runtime adapter and actual King reflection reader: legacy gating, fresh/old/unreadable binding, source-versus-ownership separation, unrelated native steps, deferred save confirmation across restart, deduplication, journal failures, queue retries, native exceptions, and AP/native save changes during a callback. No game save, server state or installed client was modified by these tests.

Pending gameplay acceptance:

- On a fresh schema 19 seed/save, perform Cell Tower's real feed interaction and observe exactly one check; verify introduction alone does not check it.
- Complete the native King victory/unlock sequence and observe one King source check while retaining the native character reward.
- Confirm reconnect/restart replays a pending check, and a different native save or AP identity does not inherit it.
- Confirm the AP Star feeding requirement and original victory sequence remain playable with the combined AP Stars implementation.
