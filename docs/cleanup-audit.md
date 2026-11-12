# Project cleanup review

November 11, 2026. The comparison baseline is release v0.30.1 at `cc45a56`, with client 0.77.1 and APWorld 0.30.0. This review covers the tracked client, APWorld, contracts, tests, packaging, CI, documentation and archive. It records a local candidate build.

## What was removed

| Area | Change and evidence |
| --- | --- |
| Client startup and game hooks | Removed the unregistered developer hotkeys, manual scene and reward scanners, temporary score override, retired HUD and escort diagnostics, and the ability-request hook that only cached a processor nobody read. Kept the bag-item callback that supplies the processor for real Garage cartridge delivery. Removed eight startup warnings that only described features. |
| Movement | Removed the old capture formatter, timing buffer, profiler and their diagnostic fixtures. The player movement repair and Montage animal timing repair are separate, active hooks with their own regression tests. |
| Native progression requests | Weed Killer and Plant Pipes contained two copies of the same reflection-based flag writer. Both now call one implementation. Each passes its own reentrancy guard so a synchronous callback is recognized by the item that submitted the request. |
| Lobby and difficulty | Removed two prototypes referenced only by their own tests. The Lobby prototype modeled a two-check pickup that was never shipped. Important Letters uses the released Expanded Checks mapping. The live difficulty selector uses the game's assignment-complete flag. |
| Cassette delivery | Removed the superseded disk-write state machine, manual post-load scan and one-shot acceptance mode. The save-identity check used before a write and during readback now reads the selected save's validity and pointer directly. Collecting a snapshot of the HUD bag and song medals for this check was redundant. The live path still checks ownership and native write progress for the batch. |
| APWorld and Tracker | Reused the location filter during generation and Tracker reconstruction. Removed duplicate rule assignments and a second capacity scan that constructed an AP Star for each available location. The placement code still checks exclusions and available capacity. |
| Packaging, tools and docs | Removed four tracked local task reports, an empty distribution placeholder, a duplicate song list and redundant validation work. Corrected the install guide, seed schema descriptions, check counts and historical release entries. Kept the dated playtest evidence and third-party notices. |

## Findings from the second pass

- **Startup and delivery.** The retired Roots startup routine received login configuration but had no registered update or save processor. The Roots intro repair has its own callback. The experimental receipt fallback offered the same item to handlers that had already declined it. Both paths and the unused configuration switch were removed. The client also reuses the cassette contract it validated at login.
- **Checks during play.** A notification history read now reuses its snapshot until deliveries change. Popup entries and cassette timers also reuse their state between changes. AP Stars count incoming items incrementally, while a full replay still replaces the previous count. Fully journaled Bunker checks skip native flag reads. Native progression lookups cache method and enum metadata while reading the flag value from the game each time.
- **Music Lab and Garage.** One catalog now defines the nine chest thresholds, scene paths and existing configuration keys. Each visit verifies chest metadata once and retries chests that have yet to load. The regression test covers a late-loading Bloody Tears chest, a return visit with changed flag IDs and an interrupted scan. Garage cartridge polling also stopped building two temporary lists to identify a single missing or inactive cartridge. [Chest catalog](../client/MusicLabChestMap.cs) and [behavior tests](../client/tests/MusicLabPoints/MusicLabChestTests.cs).
- **Generation and Universal Tracker.** Both paths now use the same difficulty metadata. Across 60 test configurations, setup lookups fell from 36,120 to 1,800, and rule installations fell from 52,068 to 29,688. Across 6,720 inventory states, all 1,686,048 location results and 114,240 entrance results matched the candidate at the start of this pass.
- **Tests and validation.** Cell Tower and Bunker star-total tests now call the method wired to the native hook. Music Lab point tests exercise the separate score callback. Removed an obsolete HUD fixture and four tests of retired Star pool planning helpers. The repository validator reuses each parsed Python module and checks the difficulty flag in generated slot data.

## Follow-up on the external v0.28.2 audit

- **Reconnects.** The client reads Archipelago's typed refusal code. A wrong slot, password, game, protocol version or item-handling setting pauses retries and logs the correction. Timeouts and occupied slots keep retrying, with increasing delays capped at one minute. A socket closing before the refusal arrives cannot restart a paused retry.
- **Quest progress.** Login reads and validates the quest journal before publishing grants or replacing the previous seed's queued checks. Old journals keep their existing filename and load in place. New writes include the schema and identity and use a separate temporary file for each write. If the file needs attention, the log identifies it and automatic reconnect pauses. The connection regression test restores a journal from backup and verifies that two offline checks still reach their original seed.
- **Item notifications.** Before login, the client records positions in the library's item history and keeps up to 512 distinct outgoing notifications. Beyond that limit it logs once and omits further outgoing notices for that login. The library still handles gameplay deliveries. F6 remeasures visible history when its contents, width or scale change.
- **Native teardown.** Unload disables and removes the 13 Unity components the plugin created, leaving their shared BepInEx host available to other plugins. It releases the quest, Cell Tower, cassette chest, Garage and movement hooks and keeps failed handles available for another attempt. The legacy Star Eater patch and AP threshold field restore changes they still own. The separate network receive guard stays active for the lifetime of the process to protect pending reads. In-game unload and relaunch remain acceptance checks.

## Where to start a human code review

1. Follow the [connection lease](../client/ConnectionGenerationLease.cs) into [received-item dispatch](../client/ReceivedItemDispatch.cs). The comments explain how a delayed callback from an old connection gets rejected and why a handler can claim an item before a save is ready.
2. Follow [the native flag request](../client/NativeProgressionFlagRequest.cs) into [Plant Pipes reconciliation](../client/PlantPipesReconciler.cs) for the difference between submitting a native write and reading back ownership. Read [cassette persistence](../client/CassetteRandomizationPolicy.cs) to see how a batch stays tied to one save and what counts as successful write verification.
3. Read [Bunker session history](../client/BunkerSessionHistory.cs) for reconnect behavior. An index-zero packet replaces the entire item history, including when the replacement is empty. A gap suspends the Bunker state until the client receives a complete history again.
4. Compare [world generation](../apworld/scrc/__init__.py) and [Tracker restoration](../apworld/scrc/universal_tracker.py). Comments explain the shared location filter, legacy source IDs and why some cartridge pickups have different placement rules in schema 21.

## Compatibility decisions

- **Seeds and saves.** Schemas 19 and 20 still describe playable rooms. Their mappings, reserved IDs, replay handling and older save records are compatibility code. The shared cassette contract also supplies an independent cross-language check on catalog drift.
- **Player settings.** `DirectStartAtLevelOne` and some progression settings still have live callers. Removing them would change the behavior of existing configurations. Deciding which settings belong to the seed remains a separate design decision.
- **Flag request verification.** The shared flag writer still tries the property's setter and the backing field after construction. A normal return from the game confirms submission. It does not establish that those setters succeeded or that the change reached disk. The Plant Pipes reconciler reads native ownership separately. Changing what happens on a setter failure needs testing against the game's actual request wrappers.
- **Reviewability.** The large plugin entry point and several cassette tests still locate methods by their source text. Extracting live hooks into smaller files would be easier to verify after replacing those tests with ones that exercise the same callbacks. Native game objects, save pointers and callback order still need coverage inside the game.

The [cassette verification test](../client/tests/CassetteRandomization/CassetteVerificationTests.cs) drives the client’s receipt and frame callbacks against a simulated save. It records song reads and grants, then checks which songs reached the save queue when verification fell due.

## Verification

### Repeated client work

The cassette retry check now compares the number of owned and satisfied songs. It creates a sorted snapshot only when reconciling the individual songs. The verification timer returns immediately when there are no timers. The Star Eater code looks up the room descriptor on a room change and still checks the live component and required stars each update. Bundle matching and notification text use one table of the 15 permanent bundle names.

| Operation repeated 10,000 times | Managed bytes before | Managed bytes after |
| --- | ---: | ---: |
| Check for pending cassettes and due verification with 30 owned songs already deposited | 2,480,000 | 0 |
| Update Star Eater thresholds while staying in an ordinary song room | 1,200,000 | 0 |
| Update Star Eater thresholds while staying in Roots with its threshold already applied | 1,200,000 | 0 |
| Pass an unrelated Plant Pipes receipt through the cassette bundle handler | 10,880,000 | 0 |
| Read an unchanged 100-entry notification history | 8,720,000 | 0 |
| Advance and read three visible popups before expiry | 1,280,000 | 0 |
| Advance 30 pending cassette timers before verification is due | 2,960,000 | 0 |
| Read a general progression flag, including reading its current value | 1,440,000 | 240,000 |

Measurements used the client test fixtures in Release mode, with 1,000 warm-up calls and per-thread managed allocation counters. Native callbacks and frame times need measurement in the running game.

### Regression checks

| Check | Result |
| --- | --- |
| Candidate build and packaging | Release build against the installed game and BepInEx assemblies. Zero build warnings and errors. Both the client archive and APWorld package passed manifest validation. |
| Client regression projects | All 38 client projects passed, including the loopback network suite. |
| Python and repository checks | All 200 APWorld tests and 11 tooling tests passed, along with repository validation and the diagnostic cleanup smoke check. |
| Release comparison | Two two-player seeds generated on Archipelago 0.6.8 matched release `cc45a56` for all 994 addressed placements, 12 locked events, progression spheres, starting items, player slot data and datapackages. Both passed full accessibility and beatability. Together they covered both endings, all four difficulties and schemas 19 through 21. The baseline and candidate produced the same three nonfatal balancing warnings. |

**Pending native acceptance.** On the candidate client, receive Plant Pipes or Weed Killer and use it, insert a Garage cartridge and earn a sticker, compare Music Lab points inside and outside the hub, receive a cassette and reload the same save, and revisit the Meat Dimension mouse escort. These exercise native game callbacks that automated fixtures can only approximate.
