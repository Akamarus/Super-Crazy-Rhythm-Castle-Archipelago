# Full Music Lab Cassette Randomization Design

**Status:** Approved for implementation planning
**Date:** 2026-08-29
**Target:** Client v0.68.0 / APWorld v0.22.0

## 1. Purpose and scope

This specification activates all 30 recognized Music Lab song cassettes as individual Archipelago items in one release. It generalizes the live-tested Level 2 Money cassette pilot without including the six Game Garage cartridges. Garage cartridges remain a separate subsystem, and their native-inventory reconciliation defect remains an open release blocker.

The release may label individual cassette routes as manual-testing pending, but it may not guess source identities. Initial mappings come from installed game metadata, the recorded full-playthrough evidence, existing logs, and already verified project mappings. Automated catalog validation and representative live testing replace a requirement for the project owner to replay the entire game before activation.

## 2. Authoritative cassette catalog

One authoritative 30-entry catalog drives APWorld registration, solver rules, client routing, documentation, and tests. Each entry records:

- player-facing song name;
- native `ePlayableSong` identifier;
- AP item name and permanent item ID;
- source event type and player-facing AP location name;
- permanent source location ID when the source is not already an AP location;
- native level ID and variant for level-earned rewards;
- source region, Area Access, quest, ability, performance, and threshold requirements;
- exact native award identity or source flag;
- unowned, bag, and deposited cassette states;
- replay behavior;
- whether the source reuses an existing AP location; and
- manual-testing status and evidence reference.

The catalog must contain exactly the same 30 unique songs used by the Music Lab medal catalog. Builds, packaging, and generation reject missing songs, duplicate songs, duplicate active IDs, unknown regions, unresolved source references, or mismatched client/APWorld identifiers.

`Money Cassette` and `Level 2 - Money Cassette` retain their existing permanent IDs. The other cassette items allocate consecutive permanent IDs beginning at the current next safe item offset, `+124`. New source locations allocate consecutive permanent IDs beginning at the current next safe location offset, `+187`. Existing IDs never move or change meaning.

## 3. Evidence-driven mapping extraction

The initial catalog is assembled in this evidence order:

1. installed game metadata and native level/variant reward definitions;
2. the recorded full-playthrough conversation, especially story, quest, and non-level rewards;
3. retained project logs and existing verified mappings; and
4. targeted read-only diagnostics only where the first three sources disagree or remain incomplete.

The extraction process must record its evidence in a reviewable generated report. Production catalog entries are explicit checked-in data, not runtime guesses. A mapping that cannot be tied to a native identity is an implementation blocker for the catalog, even though its later gameplay acceptance may remain pending.

The source catalog must cover ordinary campaign completion, alternate variants, Music Lab point chests, pickups, quests, and story rewards. One physical reward event is never represented by two AP locations.

## 4. Existing-location reuse

When a cassette is the vanilla reward from an interaction already registered as an AP location, that existing location is its source. The client sends the existing check and suppresses only the cassette reward. It does not create a second simultaneous cassette-source check.

The five known Music Lab chest cassette sources are:

| Threshold | Cassette | Reused AP location |
| ---: | --- | --- |
| 32 | Quicksand | `Music Lab - 32 Point Chest` |
| 64 | Flamenco | `Music Lab - 64 Point Chest` |
| 89 | Ten-Four Good Buddy | `Music Lab - 89 Point Chest` |
| 111 | Zen | `Music Lab - 111 Point Chest` |
| 140 | Wiggle | `Music Lab - 140 Point Chest` |

Any additional quest, pickup, or story reward that already has an AP location follows the same reuse rule. A new source location is allocated only when no existing AP location represents that physical action.

## 5. APWorld item and source model

All 30 cassette items are progression items because each unlocks its song's enabled Music Lab medal locations. The existing Money special case becomes a normal catalog entry.

Each source location uses its real reachability requirements: area, route, prerequisite level, quest stage, ability, performance, or Music Lab threshold. Each Music Lab medal location requires its corresponding cassette item plus the selected AP difficulty tier. Receiving a cassette is sufficient for solver access to the Music Lab insertion action because Hub6 and the cassette machine are always available in compatible AP sessions; insertion itself remains player-controlled.

A cassette item cannot logically reach its own medal locations before it is obtained. Archipelago's reachability solver enforces that dependency directly. Chest-sourced cassette events retain the current non-progression placement restriction on Music Lab point chests until AP Music Lab Point logic is implemented. Cassette medals do not add AP Music Lab Points.

The expanded pool must pass capacity and completion analysis under Normal, Hard, Expert, and Perfection, both supported starting-area option values, retained regression seeds, and deterministic seed matrices. Generation fails with a clear error instead of producing insufficient capacity or unreachable required progression.

## 6. Client source interception

The client replaces the Money-specific source policy with a catalog-driven cassette manager.

For level-earned entries, the verified `LevelLogic.EvaluatePlayerLevelSongCassettes(bool)` prefix reads the current level and variant, selects catalog entries for that source, verifies a successful result, and checks each cassette's native ownership state. Only a catalog-confirmed unowned award is intercepted. The manager queues the mapped AP source location and suppresses the native cassette evaluator for that award while leaving completion, Stars, and other result persistence native.

Chest, quest, pickup, and story sources use their exact verified native request, flag, or event. They do not pass through the level-completion path. Existing-location sources send that existing AP location exactly once.

If required runtime identity or ownership state is unreadable, the client preserves the vanilla reward and emits one focused error containing source, song, room, level, variant, and failed identity. It never silently deletes a reward based on a partial match.

Replays do not resend checked sources. Alternate modes are intercepted only when the catalog explicitly assigns a cassette to that variant. Unrelated level results, cassette statuses, requests, and rewards remain native.

## 7. Native inventory reconciliation

Received cassette items enter a persistent AP-owned set reconstructed from received-item history on connection and reconnection. Native reconciliation runs only on the Unity thread through the safe lifecycle and bounded-retry pattern proven by the Money pilot.

For each AP-owned cassette:

- native `INVALID` or `HAVE_NOT_EARNED` becomes `HAVE_IN_BAG` through the native cassette-status request;
- `HAVE_IN_BAG` is already reconciled;
- `HAVE_DEPOSITED` is already consumed into the Music Lab and must never be restored to the bag.

Receiving an item never forces its Music Lab song open. The cassette appears in native inventory, and the player inserts it through the normal cassette-machine interaction. The insertion changes native state to deposited and unlocks the song normally.

Ownership must survive room transitions, save reload, reconnect, full restart, and selection of a later compatible save. Duplicate received-item history and repeated lifecycle triggers are idempotent. The client must not restore a deposited cassette or use the `SelectedPlayerSaveSlotChangedEvent.HandleEvent` Harmony hook that caused an IL2CPP startup crash during Money development.

Outside a compatible AP session, all cassette behavior remains vanilla.

## 8. Version and compatibility boundary

Full activation targets Client v0.68.0 and APWorld v0.22.0 with a new implementation-version suffix. Slot data includes:

- full cassette randomization enabled;
- cassette schema version;
- exact catalog count;
- item mapping;
- source mapping; and
- existing-location reuse mapping.

The client validates the schema and catalog before enabling cassette routing. Missing, incompatible, or contradictory slot data disables the full system with a prominent compatibility error. It does not partially interpret a v0.22 seed.

Existing v0.21 seeds retain their Money-only behavior when used with a compatible historical client. Full cassette activation requires the matching v0.22 APWorld, v0.68 client, a newly generated seed, and a fresh in-game save for acceptance testing.

## 9. Automated verification

APWorld tests must prove:

- exactly 30 stable cassette item definitions;
- exact catalog equality with the 30 Music Lab medal songs;
- stable Money IDs and unique new permanent IDs;
- reuse of the five known point-chest locations without duplicate source locations;
- correct source-region and prerequisite rules;
- corresponding cassette requirements on every enabled medal tier;
- no cassette self-lock or progression placement in restricted point chests;
- exact pool/location capacity at all four difficulties;
- deterministic generation and retained regression-seed safety; and
- slot-data/client catalog agreement.

Client tests must prove:

- all 30 native identifiers and source dispatch entries;
- successful interception only for matching unowned source rewards;
- preservation of failed, replayed, unrelated, alternate-mode, and unreadable cases;
- correct existing-location reuse;
- `INVALID` and `HAVE_NOT_EARNED` receipt reconciliation to `HAVE_IN_BAG`;
- preservation of `HAVE_IN_BAG` and `HAVE_DEPOSITED`;
- duplicate receipt idempotence;
- safe unavailable-save and unavailable-processor retries;
- later-save, reload, reconnect, and restart reconciliation; and
- absence of the unsafe selected-save Harmony hook.

Repository validation, APWorld packaging, real Archipelago generation, the deterministic seed matrix, and release client compilation are required before gameplay acceptance.

## 10. Manual acceptance without a full owner replay

The initial release activates all catalog entries but labels unplayed entries **manual verification pending**. The project owner is not required to replay the entire campaign before activation.

The pre-release representative matrix covers:

1. Money as the proven level-completion source;
2. at least one additional ordinary level-completion cassette;
3. at least one Music Lab point-chest cassette;
4. at least one non-level story, pickup, or quest cassette if the catalog contains one;
5. AP receipt and visible native inventory;
6. normal Music Lab insertion and song availability;
7. replay without a duplicate source check;
8. save reload, reconnect, and full restart; and
9. one local-co-op smoke test when practical.

Public testing documentation assigns songs individually and requests the source interaction, AP sent/received messages, inventory result, insertion result, medal access, replay result, reload result, client log, seed, slot, and player count. Each entry's status changes from pending only when its own evidence is recorded.

## 11. Documentation and release reporting

The implementation updates the permanent ID registry, project overview, progression documentation, roadmap, install/testing guide, changelog, APWorld documentation, and next-release checklist.

Player-facing documentation distinguishes:

- cassette source locations;
- cassette items;
- normal Music Lab insertion;
- Music Lab medal locations;
- Music Lab point chests; and
- separate Game Garage cartridges.

Release notes may claim all 30 cassettes are activated only after automated gates and representative live acceptance pass. They must state that remaining individual routes require public manual verification. The separate Game Garage native-inventory blocker must remain visible until fixed and tested.

## 12. Explicit exclusions

This feature does not:

- repair or redesign Game Garage cartridge inventory;
- randomize Vampire Killer differently;
- implement AP Music Lab Point items;
- add cassette or currency milestone checks;
- force Music Lab songs open on receipt;
- count cassette medals toward AP Music Lab Points;
- alter AP Star logic, victory, Area Access, or native Normal/Pro choice; or
- claim every individual cassette route has completed gameplay acceptance.

## 13. Acceptance criteria

The feature is ready for the v0.22 testing release when:

1. the evidence report and checked-in catalog define all 30 unique cassette sources and native identities;
2. all required permanent IDs are allocated without changing existing IDs;
3. every cassette is present once in the item pool and gates its matching medal locations;
4. existing physical AP locations are reused without duplicate checks;
5. received items persist as native bag ownership until normal insertion;
6. catalog, client, APWorld, repository, generation, packaging, and build tests pass;
7. the representative manual matrix passes; and
8. all untested individual routes are clearly labeled manual verification pending.
