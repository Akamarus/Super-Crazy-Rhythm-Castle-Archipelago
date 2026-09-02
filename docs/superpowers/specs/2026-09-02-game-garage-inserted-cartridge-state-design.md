# Game Garage Inserted-Cartridge State Design

**Date:** 2026-09-02
**Status:** Approved; not implemented
**Related behavior:** AP-randomized Game Garage cartridges in client v0.68.0

## Problem

Archipelago received-item history proves that the player owns a Game Garage cartridge. The game represents a cartridge currently carried by the player with a native `*_BAG_ITEM` progression flag. Entering the Game Garage consumes that bag item and makes the corresponding song available. The current reconciliation code then sees an AP-owned cartridge missing from the bag and grants it again after the player leaves.

Live testing with Superstar established that `GameProgressionEnquiries.HasGarageCartridgeBeenCollected(eRoom27GameCartridgeType)` does not become a usable terminal insertion signal for an AP-delivered cartridge. The similarly named native `*_COLLECTED` progression flag belongs to the cartridge's physical source. That source is an Archipelago location and must remain collectible until the player actually reaches it. Setting the native collected flag from an AP receipt would remove or pre-complete an unvisited randomized check.

The game therefore has no native flag that can safely serve both requirements. Inserted-cartridge state needs a separate Archipelago-owned source of truth.

## Goals

- Grant each AP-owned randomized cartridge to the native bag exactly once before its first Game Garage use.
- Treat normal Game Garage consumption as terminal for that Archipelago player slot.
- Preserve terminal insertion state across room travel, save reloads, game restarts, and reconnects.
- Keep all physical cartridge-source checks available until the player collects them.
- Keep Vampire Killer on its current physical vanilla entrance path.
- Fail closed when server insertion state is unknown, avoiding speculative regrants.

## Non-goals

- Tying insertion state to an individual local save slot.
- Changing item placement, cartridge source locations, Garage medal checks, or song scoring.
- Automatically inserting a cartridge or forcing a Garage song open.
- Reusing or setting native `*_COLLECTED` source flags from an AP receipt.
- Adding a general-purpose local-AI bridge or unrelated persistence system.

## Approaches Considered

### Slot-scoped Archipelago data storage — selected

Store one boolean per randomized cartridge in Archipelago slot-scoped data. This follows the seed/player across local saves and machines, matches AP ownership scope, and does not consume a native source check.

### Local sidecar file — rejected

A local file could be keyed to server and save slot, but it would not follow the player to another machine, could become stale when seeds are replaced, and would introduce a second local-save identity problem.

### Native collected flag — rejected

The native collected flag is the randomized physical source location. Using it as an insertion marker would hide or award a check the player never visited.

## Architecture

### Server state

The client uses Archipelago `Scope.Slot` data storage. Each of the five randomized cartridges has an independent versioned boolean key:

```text
scrc:garage_inserted:v1:bloody_tears
scrc:garage_inserted:v1:gradius_remix
scrc:garage_inserted:v1:smooch
scrc:garage_inserted:v1:superstar
scrc:garage_inserted:v1:wag_the_dog
```

An absent key means not inserted. `true` means terminally inserted. Independent keys avoid lost updates when two cartridge events occur close together.

The client keeps three distinct states:

- **AP owned:** derived only from received-item history.
- **Inserted:** loaded from slot-scoped server data or recorded during the current process.
- **Native bag held:** read from the exact verified `*_BAG_ITEM` progression flag.

These states are not interchangeable.

### Connection and reconciliation gate

Received-item history may arrive before login slot data and data-storage reads complete. The client may remember AP ownership immediately, but it must not perform native cartridge reconciliation until all of the following are true:

1. compatible cartridge randomization is enabled by slot data;
2. the selected local save is readable and a compatible save processor is available;
3. all five inserted-state keys have completed their initial server read for the current connection;
4. the cartridge is AP owned and is not marked inserted.

If insertion state is unavailable or malformed, reconciliation fails closed and retries after a later connection lifecycle event. It does not assume `false`.

Once the gate is open, an AP-owned, non-inserted cartridge is granted only when its native bag flag is absent. A held bag item is left unchanged. Vampire Killer remains excluded from this reconciliation.

### Insertion detection

The existing Game Garage keeper observes the five randomized cartridges on the Unity thread. A cartridge becomes an insertion candidate only when:

- it is AP owned and not already marked inserted;
- its native bag flag was authoritatively observed as held;
- the current room is `GameRoom_27`; and
- the corresponding native cartridge object was released for the current Garage visit.

The client records insertion only after that candidate's native bag flag is authoritatively observed changing from held to absent while still in the Game Garage. A missing bag item in the Music Lab, during startup, or before a released Garage candidate exists is not insertion evidence.

This rule matches the live Superstar sequence: the AP grant was present before Garage entry, the Garage consumed it, and the song became available.

### Persistence flow

When insertion is detected, the client:

1. marks the cartridge inserted in process memory immediately;
2. stops native regrant reconciliation for that cartridge;
3. writes `true` to its slot-scoped data-storage key;
4. waits for a server value update or authoritative reread confirming `true`;
5. logs the insertion as durable only after that confirmation.

Repeated consumption observations and repeated server updates are idempotent. Server state is monotonic: this feature never writes `false`.

On reconnect, the client reloads all five keys before allowing native grants. A server-confirmed inserted cartridge remains absent from the bag. The existing Garage routing may make its song available through the normal game path, but the client does not simulate controller input or force the cartridge machine open.

## Failure Handling

- **Initial read unavailable:** keep server state unknown and perform no native grant.
- **Malformed value:** log the key and value type without sensitive connection data, keep it unknown, and perform no native grant.
- **Write failure or disconnect:** retain inserted state in process memory, keep the monotonic write pending, and retry after reconnection.
- **Game closes before server confirmation:** do not claim durable insertion. The next run trusts the server value; if the write never reached the server, the cartridge can be granted again and the test remains failed rather than silently losing access.
- **Duplicate AP receipt:** update ownership idempotently; do not clear insertion state.
- **Native read failure:** keep the cartridge pending and do not infer either holding or consumption.
- **Save switch:** AP ownership and server insertion state remain slot-wide; only native bag observations reset.

## Logging

Logs must distinguish:

- inserted-state synchronization pending, ready, or failed;
- native bag grant submitted and observed held;
- Garage insertion candidate armed;
- native consumption observed;
- server insertion write pending;
- server insertion state confirmed durable;
- already inserted, with no regrant.

No log may call insertion durable merely because a local native bag item disappeared or a network write was submitted.

## Automated Testing

Pure state-machine and storage-adapter tests must prove:

1. AP ownership arriving before server-state synchronization causes no native grant.
2. An absent server key becomes known `false`; an unreadable or malformed key remains unknown and fails closed.
3. AP owned + known not inserted + missing native bag requests one grant.
4. Held native bag is idempotent and does not submit another grant.
5. Missing bag outside the Garage never records insertion.
6. Missing bag inside the Garage without a prior held observation never records insertion.
7. A held-to-missing transition for a released, AP-owned Garage candidate records insertion exactly once.
8. Recording insertion immediately suppresses regrant before server confirmation.
9. A failed write remains pending and retries after reconnect.
10. Server-confirmed `true` survives a simulated restart/reconnect and prevents regrant.
11. Duplicate receipts, transitions, and server callbacks remain idempotent.
12. Vampire Killer never enters AP insertion reconciliation.
13. No AP grant or insertion path writes a native `*_COLLECTED` source flag.

## Manual Acceptance

Use a fresh compatible v0.22 seed and a fresh local save for the first decisive test:

1. Deliver Superstar Cartridge through Archipelago.
2. Confirm the native cartridge appears in the top-right inventory.
3. Enter Game Garage normally and confirm Superstar becomes available.
4. Leave Game Garage and confirm Superstar does not return to inventory.
5. Wait for the log's server-confirmed durable marker.
6. Close and relaunch the game, reconnect, and load the same or another local save for the same AP slot.
7. Confirm Superstar remains absent from inventory and available in Game Garage.
8. Confirm the physical Superstar source remains present until collected and sends its AP location check when collected.

After Superstar passes, repeat the inventory, insertion, relaunch, and source-preservation checks for Bloody Tears, Gradius Remix, Smooch, and Wag the Dog. Vampire Killer receives a separate regression check proving its vanilla pickup and entrance behavior are unchanged.

## Acceptance Boundary

Automated tests are necessary but not sufficient. The bug is not closed until the Superstar flow passes the full live acceptance sequence, including server confirmation, relaunch, and preservation of its randomized physical source. Nothing in this design authorizes merging, pushing, packaging, publishing, or releasing the feature branch.
