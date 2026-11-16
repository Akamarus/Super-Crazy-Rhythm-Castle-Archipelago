# November 12 source review

This record covers the 63 numbered findings in Muse's review of the source ZIP, its five additional hypotheses and Claude's dead-code and performance review. It describes an unreleased candidate for the current client and APWorld. Finding numbers below refer to that ZIP. Existing seeds keep their item IDs, location IDs and slot-data formats.

## Progression and save safety

| Findings | Change or decision |
| --- | --- |
| 1 | Reproduced the rejection of Plant Pipes, Hip Glasses and Chicken Bucket on generated schemas 19, 20 and 21. The client now consumes the generator's three optional route markers in order. An unknown marker, a duplicate marker or a disabled feature still fails the compatibility check. The generated-slot-data tests exercise the client policies directly. |
| 2, 11, 12, 41, R1 | Native flag grants require an accepted room, a stable selected save and the room's bound slot. Older rooms without quest journals acquire a durable binding on their first verified selection. The client suspends new grants after disconnect. An unrelated save can still perform its own native Bunker and chest actions. A known bound save retains its transaction protections if a journal write fails. |
| 7, 17, 18, 23, 33, 44 | Cassette delivery keeps verification pending across unreadable results, unsuccessful grants and history replays. Deposited songs and a failed earlier batch no longer strand later songs. Disk verification uses a timed retry and cached metadata. Unload checks for outstanding persistence work before tearing down the client. A forced process exit still needs recovery on the next launch. |
| 10, 21, 24 | Removed the legacy keeper that modified shared methods and scanned for nearby players. The opt-in legacy override now uses the same per-instance threshold field as seed-controlled Star Eaters. Switching to a partial legacy map restores the previous threshold even if the player stays in the same room. Scene changes, unload and startup retry field restoration. |
| 14 | The client retains the validated Cell Tower and Lobby capabilities and checks the corresponding capability before changing a path. A historical seed can grant area access while retaining its original story doors. |
| 15, 16 | New Bunker journals contain their room identity and schema. Readers validate the record and still accept valid historical files. Journal writes use a unique staging file, flush it and atomically replace the target, preserving the previous version as a backup. A backup first exists after a successful replacement. |
| 20 | Native hook installers retry a previously failed detach before installing a replacement. A failed cleanup leaves the handle owned and prevents a second copy. |
| 25, 26, 27 | The Lobby approach to Lift Quest was reviewed previously. Completing it without either Hip Glasses or Chicken Bucket still needs direct confirmation. Archived King runs show a rated final clear and both enabled Completion and 1-Star checks being sent. The 2-/3-Star behavior for both endings and native event ordering for Bunker Keycard, Virus and Cheat Codes remain gameplay questions. |
| 61, R2 | Victory uses the Star total at the qualifying clear. Earning the final Star afterward requires another clear. The client retains a qualifying offline clear for delivery when the room reconnects. A native attempt also needs to originate from the room's bound save. |

## Delivery, recovery and Garage

| Findings | Change or decision |
| --- | --- |
| 3–5, 35, 36, R5 | The client records earned checks in a durable room/team/slot outbox. It compares them with the server's checked locations after login and on updates. Socket submission alone does not retire them. Unknown names stay available for later resolution. A rejected replacement room leaves the previous room's checks intact. Checks earned before a first successful connection have no trustworthy room identity, so the client reports that limitation. |
| 8, 22, 37–40, 45 | Permanent contract, journal and dependency failures pause reconnection. Receipt processing uses copied packet contents. Missing or discontinuous named history prompts a full synchronization, and the relevant state waits for authoritative history. A replacement connection invalidates the previous Star generation. Ordinary offline play can still use its last accepted room. |
| 13 | An authoritative correction rebuilds managed named-item ownership. Future native grants use the corrected ledger. Previously saved abilities are left intact because the client cannot distinguish every legitimate native grant from an AP grant after the fact. |
| 6, 19, 31, 42, 43 | A confirmed in-Garage bag transition can establish insertion. Pending server storage retries across reconnects and restarts under its original room/team/slot. Abandoned door transitions expire. Scene lookups reuse the validated Garage root. Garage insertion belongs to the AP player slot and the native save determines where inventory may be changed. |
| R3, R4 | Garage result handling uses the native evaluation when the holder has no song. Contradictory evidence defers that award. All five Garage cartridge names exist in the installed game's playable-song enum. An unknown name on another build skips just that preview row. Back-to-back songs still need gameplay acceptance. |

## Network and distribution

| Findings | Change or decision |
| --- | --- |
| 9, 46, 49 | The exact shipped parser delivered an SDK packet at nesting depth 64 and rejected depth 65 before dispatch. The assembly's version label alone misidentified that behavior. Connection startup checks both reviewed module identities. Locked restore and NuGet advisory checks cover the package dependency. Release hashes remain the separate check on distributed file contents. |
| 28–30 | Peer-controlled log values are sanitized, connection logs omit the configured server and slot, and the support guide asks for a relevant redacted excerpt. The config password remains readable by anyone with file access. Players can instead opt into a process-only password using `UseEnvironmentPassword` and `SCRC_AP_PASSWORD`. |
| 47 | SCRC sessions pace sustained traffic after a burst of 64 MiB or 4,096 messages. Subsequent credit accrues at 16 MiB and 2,048 messages per second. The SDK waits between complete messages. Idle connections, packet order and other plugins' sockets are unaffected by this pacing. Sustained traffic in a very large room can introduce delay and still needs live acceptance. |
| 48, 50, 51 | Deployment validates the game and destination before touching plugin files. Archive processing enforces size limits while reading. Four original 0.25.0/0.27.0 candidate archives repackaged with every original member byte-identical. |
| 52–55 | The install guide distinguishes the shipped retry behavior from the candidate. The shared SDK receive protection remains active across plugin unload while sockets may still close. The area condition hook selects the verified native override. The client retains .NET 6 for the installed BepInEx host. A runtime migration requires a compatible host and game test. |

## Performance, cleanup and remaining measurements

| Findings | Change or decision |
| --- | --- |
| 32, 56 | The phone condition hook looks up a flag only for a matching armed arrival. Quest conditions and Hub6 destination behavior remain independent. Visible reward cards reuse their measured text sizes until the entries, width or scale change. This includes the scrollable oversized card. |
| 34, 57 | Report test totals with their build date. The game acceptance below covers native frame timing. The retired movement recorder stays out of the shipped client. |
| 58, 60 | No predecessor is visited twice in a single walk of the current 22-level graph. During a 66-rule benchmark, adding a per-call cache increased the current-route time from 22.8 to 32.4 microseconds. Retained fresh inventory reads and the explicit Lobby route branch, which records the seed's physical route and skips redundant predecessor checks. |
| 59, 62, 63 | Removed the unused native-song index and campaign lifecycle methods. The cassette medal list and legacy chest map derive from their catalogs. Both exported point totals use one calculation, and Important Letters Pickup uses its catalog ID. Preserved old public slot-data keys and separately validated contract floors. Kept distinct history handlers where gaps, replays and disconnections have different meanings. |

Music Lab Point packets now add only the new IDs to a running total. In an offline .NET 6 Release comparison using the production handler, 1,000 individual packets took 0.204 ms and allocated 567,920 bytes, down from 8.754 ms and 22,783,368 bytes in the audited ZIP. These are medians of 15 timed runs after warmup, excluding network I/O and packet construction. Both implementations accepted all 1,000 items and enforced the 180-point cap. The randomized packet-boundary test compares the result with a full replay.

The four periodic native checks can coincide. Two also run immediately after inventory revisions. Changing their timing without a frame capture could delay the first grant or binding. The APWorld fill hook took 0.026 ms for one SCRC slot, 7.308 ms for 32 and 110.867 ms for 128 in the interleaved test pool. Kept its per-player order because other worlds' hooks can modify that pool between SCRC players.

## Verified on November 12

- The Release client built against the installed game and BepInEx assemblies with zero warnings and errors. The locked NuGet restore completed with live advisory checking.
- All 38 client test projects passed. The networking project ran its offline reader and parser cases. A separate localhost test delivered a normal fragmented 1 MiB packet through the SDK, observed close, checked TLS refusal and repeated delivery after reinstalling the guard.
- All 200 APWorld tests and 19 tooling tests passed. The repository validator and diagnostic-cleanup smoke test passed.
- Both candidate archives passed package validation. The client archive contained the plugin and its two approved dependency DLLs. Archipelago 0.6.8 loaded the packaged APWorld and generated a schema 21 True Ending seed.

## Gameplay acceptance

On November 13 the client received Zen once after an offline restart and reconnect, but the Cassette Lord had nothing to deposit. On this launch the game resumed its most recent save and entered the Music Lab without identifying the active slot to the client. In the earlier successful slot-identification trace, entering Roots triggered that step. The cassette runtime regression now exercises a cold start without that callback, an initially unreadable save, ambiguous slot evidence and another room-bound slot. It verifies that the retained receipt ultimately grants Zen once and queues one write for the selected slot.

The running-room regression keeps the same live player in slot 2 throughout. AP connects and delivers Zen after gameplay has started. Both cases passed: the client already knows the slot, or the slot becomes identifiable after receipt. Each grants Zen once without another selection, room transition or AP delivery.

In the slot 2 gameplay retest, Zen from the original delivery and Bounce sent while the player was already in the Music Lab both reached the bag. The player deposited and selected both songs without leaving the room or restarting. The client logged a verified save write for each bag grant. After quitting and relaunching on slot 2, the player could still select both songs without depositing them again. The client read both native cassette statuses as `HAVE_DEPOSITED`.

1. On disposable saves, receive Plant Pipes and a Roots item on a marked seed, switch slots during delivery and confirm only the bound save gains the items. In the marked slot 2 seed, the Lobby booth blocked travel before Lobby Access and opened after delivery. With two AP Stars and the mail quest unfinished, the player reached Demolition Training and confirmed its three-Star gate worked. The player could not reach School Trip on the upper path before Boring Room and Important Letters delivery. After the server recorded both actions, School Trip blocked entry at four of the required five AP Stars. With the fifth Star and Demolition Training still unfinished, the player entered School Trip. The client logged the transition from `GameRoom_Hub1A` to `GameRoom_20A`. Both sides of School Trip's Star gate passed.

   On November 15, with Cell Tower Access and before Super Nectar delivery, the player crossed the cable bridge to Old Tom's platform on slot 2. The client logged the raised route in `GameRoom_Hub5B`. The server recorded Meet the Bees and still listed Deliver Super Nectar as unchecked. On slot 1, using the v0.29.2 comparison seed, the client received Cell Tower Access and entered the same room. The player confirmed the bridge stayed down and the platform was unreachable before nectar delivery. The Cell Tower comparison passed. The older seed's Lobby door was untested when the comparison ended.
2. Disconnect after earning an ordinary check, restart, and reconnect to its room. Confirm the server receives it once. Deposit a cassette, reload its save and confirm it remains selectable.
3. Garage medal attribution and cartridge persistence passed on slot 2. Vampire Killer Bronze awarded a Star to AkamarusPipesQA. Bloody Tears arrived while the player was inside the Garage and appeared on the rack after re-entry from the Music Lab. The client logged server confirmation of insertion. Playing Bloody Tears then cleared its own Bronze check and awarded a Star to PipesControl. After a full game restart on slot 2, the player could still select Bloody Tears without inserting it again.
4. Record the Bunker Keycard, Virus and Cheat Codes source checks in their native award order. When those routes are next played, capture Lift Quest without either trade item and Level 22's higher-tier checks for each ending.
5. During a busy sync, compare normal song timing and receipt delays with the previous client. If a periodic hitch returns, measure the four fallback checks over the same song before changing their schedules.
