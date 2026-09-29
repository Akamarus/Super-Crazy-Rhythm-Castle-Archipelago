# Ionium APTests compatibility repair - September 29, 2026

Release: APWorld v0.28.2; client v0.75.13 unchanged; schema 19 unchanged.

## Reproduced defects

1. Current Ionium Archipelago rejects an explicitly declared `option_random`.
   Unsupported Lobby, Meat Dimension and Cell Tower starts were also exposed to
   random option selection despite the resolver rejecting them.
2. Initial fuzzing reproduced 20 reverse-fill failures. Counted Stars and Music
   Lab points competed with narrow song unlocks and broad access items for the
   remaining reachable locations. Sorting only Stars repaired 19; prioritizing
   points and song unlocks as well repaired all 20. A larger run found one rare
   Old Game Data failure (run 3864, seed 566780926); single-hand-in items now share
   song unlock priority. That exact seed then generated successfully.
3. The first repaired quick suite passed 205 unit tests but found one remaining
   failure (no-restrictive-starts run 422, generator seed 294006608). Stardust had
   been placed in the Empty partner world; the last SCRC filler-only location
   rejected the remaining foreign filler. Restrictions now use classification,
   excluding progression and useful items rather than matching an item name.

## Verification

- Final local regression suite: 149 passed; repository validator and diff
  whitespace checks passed.
- All 20 initial failing generation seeds replayed successfully again with the final fix on the site's
  pinned Ionium Archipelago core, using an isolated Python 3.12 environment.
- Run 422 replayed successfully with the official Empty v0.0.3 world.
- User's exact Normal40 YAML (19 excluded checks, starting Hypno Pan and Plant
  Pipes) generated successfully with an Empty partner, seed 20260929.
- Full browser CI-count suite: **205 unit tests and 14,500 fuzz runs passed**
  in 7 minutes 28 seconds, with zero failures in all 11 variants:
  default 5,000; no-restrictive-starts 5,000; collect-accessibility, determinism,
  gerpocalypse, indirect-conditions, item-location-count, lambda-capture,
  placement-item-location-refs, static-output-placement and UT each 500.
- Report: `waimea-scrc-0.28.2-2026-09-29T20-13-48.zip` (local artifact).
- Tested APWorld SHA-256:
  `41c820f36e29fb342f1403aece3a027c0223453214049d41e04249e13c701d2c`.
  Every packaged source file was compared byte-for-byte with the checkout.

Browser environment: waimea 0.1.0+e8bbccb; Ionium Archipelago 0.6.7 commit
`f04b3a3457efbca6fde29beaf70c7f7f0016cdc8`; Pyodide 0.29.4 / Python 3.13.2.
The index CI remains authoritative; a local browser pass is not index approval.

## Final review and packaging

Independent read-only review found no blocking correctness issues. Three bundled
Markdown descriptions were corrected from Stardust-only to filler/trap-only after
the full suite. A byte comparison confirmed that no executable source or metadata
changed. A focused regression for real combined classification flags also passed
(two fill/flag tests; one additional test beyond the 149-test full local run).

Delivered package SHA-256: `158c95e55dd96c76d07fb0bc4e06669741b7febd11c36f257aee625587050f1e`.

No game DLL or installed seed/save was changed. Published as the v0.28.2 APWorld release; client v0.75.13 is retained.

Before publication, the complete local regression suite passed all 150 tests.
