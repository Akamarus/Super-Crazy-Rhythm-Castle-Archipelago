# Cassette Source Catalog Evidence Audit

**Status:** BLOCKED — do not activate the full cassette catalog.

## Extracted native catalog

`tools/extract-cassette-catalog.ps1` reads the installed game metadata without
writing to the game directory.  It resolves the approved 30 display songs to
unique `ePlayableSong` entries, including the metadata aliases `THE_EPICAL`,
`MONEY_DUB`, and `SNEAKING_LOOP`.  It also verifies the native cassette states
`INVALID`, `HAVE_NOT_EARNED`, `HAVE_IN_BAG`, and `HAVE_DEPOSITED`.

The following runtime surfaces were found in `Assembly-CSharp.dll`:

- `LevelLogic.EvaluatePlayerLevelSongCassettes(bool)`
- `LevelScoringEnquiries.GetCurrentLevelVariantSongCassettes()`
- `CurrentPlayerSaveEnquiries.GetSongCassetteStatus(ePlayableSong)`
- `SongCassetteEnquiries.GetSongCassetteStatus(ePlayableSong)`
- `GameProgressionSaveDataState.SetSongCassetteStatus(ePlayableSong, eSongCassetteStatus)`

## Source rows established by existing evidence

| Display song | Native song | Source type | Level | Variant | Existing AP location | New source name | Region | Requirements | Native source identity | Replay behavior | Evidence | Manual status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| I Got Money | `I_GOT_MONEY` | Level-earned reward | `Level_06` | `LevelVariant_Default` | `Level 2 - Money Cassette` | None | Roots | Roots Access and a successful default Level 2 result | `Level_06_Data.variants[*].songCassettes[0] = 110`; `110 = I_GOT_MONEY` | Sends only for a successful default run while unowned; replays and Bee Mode do not resend | `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md`, `client/Level2MoneyCassettePolicy.cs` | verified |
| Quicksand | `QUICKSAND` | Music Lab point chest | Not applicable | Not applicable | `Music Lab - 32 Point Chest` | None | Music Lab | 32 native Music Lab points | `SONG_CASSETTE_COLLECTED_QUICKSAND = 150001` | Existing chest check is idempotent | `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md`; extracted `eGameProgressionFlag` | verified |
| Flamenco | `FLAMENCO` | Music Lab point chest | Not applicable | Not applicable | `Music Lab - 64 Point Chest` | None | Music Lab | 64 native Music Lab points | `SONG_CASSETTE_COLLECTED_FLAMENCO = 150002` | Existing chest check is idempotent | `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md`; extracted `eGameProgressionFlag` | verified |
| Ten-Four Good Buddy | `TEN_FOUR_GOOD_BUDDY` | Music Lab point chest | Not applicable | Not applicable | `Music Lab - 89 Point Chest` | None | Music Lab | 89 native Music Lab points | `SONG_CASSETTE_COLLECTED_TEN_FOUR_GOOD_BUDDY = 150003` | Existing chest check is idempotent | `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md`; extracted `eGameProgressionFlag` | verified |
| Zen | `ZEN` | Music Lab point chest | Not applicable | Not applicable | `Music Lab - 111 Point Chest` | None | Music Lab | 111 native Music Lab points | `SONG_CASSETTE_COLLECTED_ZEN = 150004` | Existing chest check is idempotent | `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md`; extracted `eGameProgressionFlag` | verified |
| Wiggle | `WIGGLE` | Music Lab point chest | Not applicable | Not applicable | `Music Lab - 140 Point Chest` | None | Music Lab | 140 native Music Lab points | `SONG_CASSETTE_COLLECTED_WIGGLE = 150005` | Existing chest check is idempotent | `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md`; extracted `eGameProgressionFlag` | verified |

## Required source evidence that is absent

The installed interop metadata exposes the status and evaluator APIs but not
the serialized level/variant reward definitions.  The retained full-playthrough
tasks `6a823d3d-f450-83ea-9ad2-890691a86084` and
`6a85fe39-25b8-83ea-b3ad-ba3eec4421ec` confirm the Music Lab display/variant
mapping run and the five chest rewards, but do not tie the remaining songs to
a physical source event.  The retained attached logs confirm the Quicksand
progression event, including `SONG_CASSETTE_COLLECTED_QUICKSAND`, but contain
no missing cassette award event.

For each named row below, the following required fields cannot be completed
from metadata, retained logs, or historical evidence: source type, level,
variant, existing AP location or new source name, region, requirements, native
source identity, replay behavior, and source evidence.

- The Little Things; No Plan B; Jolt City; Quieres Bailar; Gold; Hippo and
  Frog; On the Way; Badass; Heavy Metal; AOK; Rainbow Melodies; Sneaking;
  The Heist; Money; Lets Go; Bounce; Epical; Hollywood Trailer; False Data;
  Gotta Get Up; Fumblin Around; Party Non Stop; Keep On Hustlin; Another Day
  In Paradise.

Because those 24 native source identities are absent, this document is an
evidence audit rather than the required 30-row reviewed catalog.  No new
Archipelago source IDs, item IDs, regions, or source rules are allocated.

## Expanded serialized-content audit

The follow-up audit used read-only access only and did not launch or modify the
game.  It examined these installed content formats:

- `D:\SteamLibrary\steamapps\common\Titus\Rhythm Castle_Data\StreamingAssets\aa\catalog.json`:
  an Addressables compact catalog with `m_KeyDataString`, `m_BucketDataString`,
  and `m_EntryDataString`, not decoded key/entry records.
- `D:\SteamLibrary\steamapps\common\Titus\Rhythm Castle_Data\StreamingAssets\aa\StandaloneWindows64\*.bundle`:
  118 UnityFS bundles.  Read-only block-table/decompression inspection found
  hashed internal node names and no literal `LevelData`, `songCassettes`,
  `Level_06_Data`, or `LevelVariant_10` record that could establish a source
  identity.
- `D:\SteamLibrary\steamapps\common\Titus\Rhythm Castle_Data\resources.assets`,
  `globalgamemanagers.assets`, and `sharedassets0.assets`: raw inspection
  confirms type metadata such as `LevelData` and `LevelVariantData`, but not
  decoded serialized instances.  The interop type exposes
  `LevelData.variants` and `LevelVariantData.songCassettes` as native field
  pointers, not their serialized values.

No installed Unity serialized-asset reader was available (AssetRipper,
AssetStudio, UABEA, and UnityPy were absent), and installing software is out
of scope.  The bundled runtime was used only for in-memory, read-only format
inspection.  These results do not establish that the data is absent; they
establish that its values are not recoverable under the task's no-install,
no-launch constraints.

### Portable AssetRipper follow-up

The approved portable reader
`C:\Users\Jack\AppData\Local\Temp\scrc-assetripper-2.0.0\AssetRipper.GUI.Free.exe`
(AssetRipper 2.0.0 x64) was then run headlessly against the same game folder.
Its localhost API was used to load that folder and inspect decoded records;
the only export destinations were temporary directories under
`C:\Users\Jack\AppData\Local\Temp`.

- The reader loaded all 118 installed Addressables UnityFS bundles and
  recovered the IL2CPP application model.  Its `Assets/Yaml` endpoint decoded
  ordinary custom Unity component fields, establishing that this was a
  field-level inspection rather than a raw byte search.
- Its targeted collection scan covered 159 bundle collections and all 94,062
  decoded `MonoBehaviour` records.  No record exposed a `LevelData` script
  reference or a `levelKey`, `variants`, or `songCassettes` field.  The
  reader therefore did not surface a decodable
  `LevelData.variants[*].songCassettes` definition.
- Its `Assets/Text` scan covered all 143 decoded `TextAsset` records and
  found no `songCassettes`, `LevelData`, `I_GOT_MONEY`, `MONEY_DUB`, or
  `QUICKSAND` source definition.
- A broad primary-content export was stopped after it started enumerating
  727,895 unrelated assets.  It wrote only to the temporary partial-export
  directory and never wrote to the game.  The focused scans above saved only
  matching results to temporary directories; each had zero matches.

This reader proves that the installed data is parseable at a general Unity
record level, but it does not recover the missing cassette-source definitions.
It must not be treated as evidence for an inferred level, variant, source, or
replay rule.

The retained attached logs searched in full were:

- `C:\Users\Jack\AppData\Local\Temp\codex-file-preview-rxS4ra\LogOutput(20260819-181239).log`
- `C:\Users\Jack\AppData\Local\Temp\codex-file-preview-SQSNr9\LogOutput(20260819-180443).log`
- `C:\Users\Jack\AppData\Local\Temp\codex-file-preview-PiIE70\LogOutput(20260819-175602).log`
- `C:\Users\Jack\AppData\Local\Temp\codex-file-preview-CSJs7f\LogOutput(20260819-173942).log`
- `C:\Users\Jack\AppData\Local\Temp\codex-file-preview-SgadbJ\LogOutput(20260819-172403).log`
- `C:\Users\Jack\AppData\Local\Temp\codex-file-preview-bHpMjZ\LogOutput(20260819-172144).log`
- `C:\Users\Jack\AppData\Local\Temp\codex-file-preview-CyZ3fU\LogOutput(20260819-171835).log`
- `C:\Users\Jack\AppData\Local\Temp\codex-file-preview-auNFKl\LogOutput(20260819-164603).log`
- `C:\Users\Jack\AppData\Local\Temp\codex-file-preview-cBFNuJ\LogOutput(20260819-162822).log`
- `C:\Users\Jack\AppData\Local\Temp\codex-file-preview-UYqj2G\LogOutput(20260819-162000).log`

## Evidence needed to unblock

Recover the serialized `LevelData.variants[*].songCassettes` definitions (or
equivalent retained native logs) for the 24 listed songs.  For any source that
is not a level award, recover the exact native request, progression flag, or
interaction event.  Then record its player-facing source, reachability, and
replay behavior before allocating a source ID.
