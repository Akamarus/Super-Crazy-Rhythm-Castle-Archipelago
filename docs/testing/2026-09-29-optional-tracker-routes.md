# Optional tracker routes - September 29, 2026

APWorld v0.28.3 candidate; no client, wire ID or schema changes. Existing schema19
slots regenerate the corrected access predicates with their original Star gates.

## Root cause

Twelve optional locations had only a parent-region rule (or lived in the always
reachable Phone Hub). Restricting their placements to filler protected progression
but did not make their displayed tracker availability accurate. Compatibility and
fuzz tests did not compare those predicates to the native gameplay requirements.

## Native evidence and conservative routes

- Smooch: Hub4 pickup11379/item2273/source127013 is in Fountain Plaza. All six
  plaza walkable sectors require condition12662 -> flag430 (gate opened).
  Gate11295 -> condition12471 -> LevelScoreCondition12308 requires native
  Level12 main-variant one-star clear (Act 1). The client suppresses the physical
  FirstAreaGate object, but does not set430. Use achievable Act 1 completion.
- Superstar: Bunker pickup4708/item2274/source127022 activates only after fed
  flag1261. The Bunker Star Eater retains66 stars independently of the goal.
- Wag the Dog: pickup4110/item2275/source127024 requires demon released1251.
  Demon interaction4360 requires all four Devil result conditions in4693;
  sequence4570 sets1251 via4202 and activates the pickup via4277.
- Bunker keycard slot12412 requires native bag800; entrance13556 requires603.
  Reuse the existing conservative Lobby/keycard route (achievable levels6+22).
  Exact physical traversal to the slot remains conservatively overestimated.
- Demon key is obtained freely from the Bunker demon's initial sequence4417,
  step4481, item4100. Gecko interaction4737 needs no external item; Note Pad and
  Data Stick are rewards. Bunker walkable sectors have no extra access condition.
- Devil entrances are in their original hubs, not the Bunker. Native Level02 is
  Lobby,11 is Lobby upper route,13 is Tower Complexity,14 is Royal Corridor.
  All require4100. No normal-level clear is required by those doors. Use the
  established conservative hub traversal and required abilities (Weed Killer
  for Tower/Lockers, Violance for Escape, Tower branches for Royal bridge side).
- Bees: Hub5B first meeting grants3100+3103 and sets812 and130010. Roots Bee door
  disable logic7346 checks130010; standalone child flag903 is not that trigger.
  Both Bee checks require the achievable Central Mainframe route plus their
  original hub route and Hypno Pan. Hypno Pan is explicitly observed for Nectar
  Party; applying it to Act1B is conservative. Exact minimum Act1B ability and
  ordinary-door bypass requirements remain live-verification limits.
- Demolition Certificate uses the existing achievable Demolition Training route.

All twelve locations retain filler/trap-only item rules. No new progression
capacity is inferred. These are conservative achievable routes, not a claim of
minimal routes or live gameplay acceptance for all optional challenges.

## Validation

- Four new route regressions reproduced the incorrect opening recommendations
  before the change and pass afterward; full local suite:155 tests passed.
- Real UT v0.2.32:13 cases (four difficulties x goals1/40/66, plus retained old
  schema19 Hard40 seed). All original Star gates/check IDs retained; the four
  reported checks are absent with Roots-only receipts and present with all inputs.
- Ionium browser quick suite:205 unit tests and1,450 fuzz runs passed, including
  50 UT checks, with no failures. This is not the full14,500 CI-count suite.
- Repository validation/diff checks passed. Independent review found no blockers.
- Local installed APWorld upgraded0.28.1 ->0.28.3 after a backup; hash verified.
  Tracker restart required. No game-client, server, seed or native-save mutation.
- v0.28.3 is not published to GitHub yet. Live availability review still needed;
  conservative-route limits above remain explicit.
