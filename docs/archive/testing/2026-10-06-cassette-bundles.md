> Historical record. Dates, versions and candidate statuses below describe that checkpoint; consult the [current documentation](../../INDEX.md).

# Cassette bundles and exclusion capacity — October 6, 2026

Unreleased candidate APWorld 0.29.0/client 0.76.0. No installation or publication.

- Tests first reproduced missing bundle contracts and 137 non-filler instances exceeding 134 eligible slots.
- Official Archipelago 0.6.8: 54/54 generation cases passed. 36 cover each mode, difficulty and goal 1/40/66;
  18 reuse the original Normal 40 YAML with 19 exclusions, Hypno Pan/Plant Pipes starting items, solo and mixed games.
- Every case passed core and independent iterative-collection beatability, exact cassette coverage,
  66 Stars, 180 points, exclusions and starters. Mixed games exchange items in both directions, including foreign progression.
- Actual Universal Tracker 0.2.32 on AP 0.6.8: 37 cases passed (36 mode/difficulty/goal combinations and one
  retained schema 19 slot). Exact gates/groups, reachability, goal, checked-location removal and individual
  bundle receipt/removal were verified. GUI rendering was not exercised.
- Original individual-mode exclusions consolidate 20 point instances into 11, retaining 180 value.
  Cassette pairs/triples retain the default 20 point instances with those exclusions.

Evidence in the task workspace: work/cassette-bundles/generation/results.json,
work/cassette-bundles/ut-results.json and ut.log. Client and final-suite results will be recorded below.

Native acceptance remains required: receive a pair/triple, read all songs in popup/history,
deposit each, verify medal availability, reconnect without duplicate grants, and verify a
separate legacy seed still behaves normally. Existing movement/cassette-delivery issues
are not declared resolved by these isolated tests.

## Final verification

- 173 APWorld tests passed, including the full repository contract suite.
- Review found that authoritative bundle history could drop individual starting
  cassettes. The fix expands both permanent individual IDs and bundle IDs, with
  regression coverage. All 30 individual ID mappings were independently compared
  against the APWorld catalog with zero mismatches.
- Bundle ownership snapshots are cached until history/configuration changes;
  ordinary Unity ticks do not rescan received-item history or repeatedly request writes.
- Client build and final suite results are recorded in work/cassette-bundles.
- Installed client/APWorld SHA256 hashes still match the pre-work baseline.
  No install, save/seed mutation, game/server restart or publication occurred.

Candidate APWorld package: outputs/cassette-bundles-candidate/scrc.apworld in the
task workspace. This is a local testing artifact, not a published release.

- Final client sweep: all 40 projects pass, including 36 focused bundle assertions.
  Coverage includes mixed individual/bundle histories, invalid/gapped packets,
  callbacks before login, legacy reconnect, notification contents and cached snapshots.
- Release client 0.76.0 build passed with SkipInstall, zero errors. NU1900 warnings
  reflect unavailable NuGet vulnerability metadata, not a compilation error.
- Independent review's individual-starter finding is closed; final ID comparison
  and cached-snapshot review found no further actionable findings.

## Fullscreen follow-up

The user also requested removal of forced fullscreen. Read-only inspection found
no display-mode override in mod code, Steam launch options, game shortcuts, wrapper
configuration or scoped launch scripts. Actual-user Unity preferences both record
Windowed (mode 3), with no running game at inspection. No registry or launch changes
were made; there is no identified override to remove. The historical instruction
to keep fullscreen was retired. If launch still switches modes, its trigger remains
to be reproduced; this report does not claim that the native warning loop is fixed.

## October 11 live bundle acceptance

On the retained local pair-bundle seed, slot AkamarusSCRC/native slot 4, the server sent Cassette Bundle 01 while the player remained in Music Lab. The bundle contains The Little Things and Money Dub. Native readback verified both HAVE_IN_BAG via bounded delayed verification, and persistence verification followed. The user confirmed both names in the notification, successful Cassette Lord deposit, and both songs selectable without restarting. This closes this live pair-bundle receipt/notification/deposit test; it does not establish reconnect/triple-bundle coverage or every item type. Evidence is kept outside Git in work/cassette-bundle-acceptance-20261011/accepted-live-bundle.log.

### Restart and reconnect acceptance

The user restarted the game and confirmed The Little Things and Money Dub remained selectable. The new launch log verifies MONEY_DUB and THE_LITTLE_THINGS as HAVE_DEPOSITED after gameplay readiness. This accepts persistence through this restart/reconnect for the tested pair bundle. Evidence is retained locally in work/cassette-bundle-acceptance-20261011/accepted-after-restart.log. Triple-bundle and other item-type coverage remain separate.
