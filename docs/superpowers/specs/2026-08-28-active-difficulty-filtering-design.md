# Active Difficulty Filtering Design

**Date:** 2026-08-28  
**Target:** APWorld v0.20 with Client v0.67.94  
**Status:** Approved design; implementation not started

## Purpose

Activate the existing AP Performance Difficulty option so generated seeds contain only the performance locations selected by the player. This milestone changes seed content without forcing the game's native REG/PRO setting and without adding new campaign locations.

## Scope

This milestone:

- activates cumulative difficulty filtering for existing campaign, Music Lab cassette, and Game Garage performance locations;
- completely omits inactive tiers from generated regions, trackers, spoiler logs, and location counts;
- sizes the item pool from the locations actually created;
- advances the APWorld to v0.20 and identifies filtering as active in slot data;
- preserves every permanent item and location ID;
- requires no new client gameplay behavior.

This milestone does not:

- add 1/2/3-Star checks for Levels 1 through 21;
- force or synchronize the native REG/PRO selection;
- implement AP Stars, AP Music Lab Points, cassette-item randomization, or new area logic;
- promote Music Lab point chests or uncertain Level-22 tiers to progression-safe locations.

## Difficulty Behavior

Generated performance locations are cumulative:

| AP Performance Difficulty | Existing campaign locations | Music Lab cassette and Game Garage locations |
| --- | --- | --- |
| Normal | Completion / 1-Star | Bronze |
| Hard | Completion / 1-Star + 2-Star | Bronze + Silver |
| Expert | Completion / 1-Star + 2-Star + 3-Star | Bronze + Silver + Gold |
| Perfection | Same campaign tiers as Expert | Bronze + Silver + Gold + Platinum |

For v0.20, the only implemented campaign performance set affected by this table is Level 22. Permanent IDs for all registered locations remain unchanged even when a location is absent from a particular seed.

Inactive tiers are not created. They must not appear as excluded, disabled, or filler-only locations in the generated world.

## Separation from Native Difficulty

AP Performance Difficulty controls only which Archipelago locations exist. Players retain normal access to both native REG and PRO choices through the game's supported interfaces. The client does not force a native difficulty based on the AP option.

## Region and Item-Pool Construction

The APWorld determines the active location-name set during `generate_early`. Region construction instantiates only names in that set. Non-performance locations remain active regardless of difficulty.

Item-pool size is derived from the count of instantiated, addressed, unfilled locations rather than the complete permanent registry. Required progression items are added first; the remaining active capacity is filled with Stardust. Generation stops with a clear error if the active location set cannot hold all required items.

The complete name-to-ID registry remains available to preserve the permanent network contract and allow clients to report known IDs safely.

The registry-derived addressed-location totals are 68 for Normal, 105 for Hard,
142 for Expert, and 178 for Perfection. Earlier draft totals were eight too high:
`BASE_ID + 0` and the documented reserved `BASE_ID + 4..+10` range are not
registered locations and must not be instantiated.

## Placement Safety

Existing access and placement rules remain authoritative:

- Music Lab point chests remain filler-only because their native medal-score thresholds have not yet been replaced by AP Music Lab Point logic.
- Game Garage locations continue to require their matching AP-owned cartridge.
- Inaccurately modeled cartridge-source locations remain filler-only.
- Royal Corridor Access continues to expose only the confirmed phone-side Level-22 route.
- Level 22 Completion and 1-Star are progression-safe.
- Level 22 2-Star and 3-Star may exist according to AP difficulty but remain filler-only. The original vanilla playthrough and project-owner knowledge establish that higher ratings depend on a broader late-game loadout, including items and abilities. That complete prerequisite set has not been isolated or modeled, so these tiers cannot safely hold required progression.

## Slot Data and Versioning

The APWorld metadata version advances from `0.19.0` to `0.20.0`. The implementation string gains a `difficulty-filtering-0.20` suffix. The slot-data schema advances only if required by the final field layout.

Slot data reports:

- the selected numeric difficulty and display name;
- `difficulty_filtering_active: true`;
- the active addressed-location count;
- the cumulative active campaign and medal tiers.

The v0.67.94 client already accepts compatible extensions to the established area-routing implementation prefix. Existing v0.19 seeds remain usable; newly filtered generation requires APWorld v0.20.

## Failure Handling

Generation fails before filling when:

- the difficulty value is not one of Normal, Hard, Expert, or Perfection;
- an active location name is unregistered;
- required items exceed active addressed-location capacity;
- item-pool size does not exactly match active unfilled location capacity.

Error messages identify the selected difficulty, required-item count, and active capacity where applicable.

## Verification

Automated verification must prove:

1. Exact cumulative tier selection for all four difficulty values.
2. Inactive locations are absent from instantiated regions.
3. Active non-performance locations remain unchanged.
4. Item-pool size exactly matches active unfilled addressed locations for every difficulty.
5. Required progression fits every supported difficulty/start combination.
6. Permanent IDs remain unchanged.
7. Music Lab point chests, active but unvalidated Level-22 higher tiers, and other unsafe checks cannot hold progression; inactive tiers are absent.
8. Garage cartridge access rules and Royal split routing remain intact.
9. Slot data reports active filtering, selected tiers, and accurate counts.
10. The full APWorld suite and repository validator pass.

Real Archipelago generation must also run once for each of Normal, Hard, Expert, and Perfection. Spoiler inspection must confirm that only expected tiers exist and that progression placement remains valid.

No manual gameplay is required for this milestone because the client already reports every affected permanent location ID. A later full gameplay suite will exercise representative generated difficulties alongside other content changes.
