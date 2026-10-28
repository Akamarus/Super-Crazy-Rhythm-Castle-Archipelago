# Bunker client candidate — 2026-10-28

Unreleased schema21 client implements authenticated Bunker Keycard, Demon Key, Virus, Cheat Codes and Game Ender receipts; durable consumption/source journals bound to AP identity and selected native slot; six Bunker checks; and selected-ending victory. Source conversion suppresses native grants. Demon pickup is observed only at its Hub8 award boundary, never from AP receipts or bag polling. Native Gecko Data Stick/Note Pad remain unchanged.

True Ending qualification freezes the received Star count at a successful persisted final-level result, requires Game Ender installed, and waits for the observed BEAT_KING_TWO flag. Normal ending journals cannot satisfy True Ending. Missing progression/result hooks reject schema21. Legacy schemas retain existing behavior.

Validation: all 46 client regression projects pass; Release build has zero warnings/errors; diagnostic-cleanup smoke, repository validation and 11 packaging/tool tests pass. The previous APWorld validation covers 204 tests and two six-player AP0.6.8 generations. Review findings about applied-result hook readiness and remaining Music Lab reflection dumps were corrected and retested. No native gameplay acceptance of this candidate yet.

## Required gameplay acceptance

Use a fresh schema21 seed and associated save, not the existing async save or staged native quest save. Verify early Keycard entry; original Keycard and Demon sources send randomized checks without vanilla duplicate grants; all received quest items appear without restart; consumption survives reload; downstream Game Ender received before upstream objectives is usable; Gecko sources and six checks work; Superstar source remains randomized; normal and True Ending goals report only their selected completion after enough Stars. Retest legacy connection and a short movement/song transition with recorders removed.

The installed client, configuration, saves and SCRC.BunkerTest helper were not modified. At approved deployment, back up the installed client and disable the temporary Bunker helper while the game is closed. Existing recordings are preserved. Development assembly version remains baseline 0.76.2 pending release versioning; identify this candidate by SHA256, not version alone.

Historical candidate snapshot above. Published as v0.30.0 / client0.77.0 with the corrected APWorld; randomized schema21 gameplay acceptance remains pending.
