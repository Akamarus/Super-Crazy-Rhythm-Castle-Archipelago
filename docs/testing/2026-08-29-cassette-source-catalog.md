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
the serialized level/variant reward definitions.  The retained evidence names
the 30 Music Lab variants and identifies the six rows above, but it does not
tie the following songs to a physical source event.  For each named row, the
following required fields cannot be completed from metadata, retained logs, or
historical evidence: source type, level, variant, existing AP location or new
source name, region, requirements, native source identity, replay behavior,
and source evidence.

- The Little Things; No Plan B; Jolt City; Quieres Bailar; Gold; Hippo and
  Frog; On the Way; Badass; Heavy Metal; AOK; Rainbow Melodies; Sneaking;
  The Heist; Money; Lets Go; Bounce; Epical; Hollywood Trailer; False Data;
  Gotta Get Up; Fumblin Around; Party Non Stop; Keep On Hustlin; Another Day
  In Paradise.

Because those 24 native source identities are absent, this document is an
evidence audit rather than the required 30-row reviewed catalog.  No new
Archipelago source IDs, item IDs, regions, or source rules are allocated.

## Evidence needed to unblock

Recover the serialized `LevelData.variants[*].songCassettes` definitions (or
equivalent retained native logs) for the 24 listed songs.  For any source that
is not a level award, recover the exact native request, progression flag, or
interaction event.  Then record its player-facing source, reachability, and
replay behavior before allocating a source ID.
