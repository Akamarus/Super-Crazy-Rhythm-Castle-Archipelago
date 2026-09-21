# Universal Tracker

SCRC APWorld v0.28.1 supports Universal Tracker's YAML-less logic tracking for
existing and new **v0.28/schema-19** seeds. No new game-client build is required.

## Setup

1. Install Archipelago with its generator component.
2. Install the updated `scrc.apworld` from the [SCRC release](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/tag/v0.28.1) on the tracker computer, replacing the older SCRC APWorld. Restart Archipelago afterward so it loads the new version.
3. Install [Universal Tracker](https://github.com/FarisTheAncient/Archipelago/releases). Versions v0.3.3 and v0.2.32 were tested with Archipelago 0.6.7.
4. Open **Universal Tracker** from the Archipelago Launcher and connect using your server address, slot name and password if required. Keep the game connected normally as well. No player YAML is needed for this SCRC integration.
5. Received items and checked locations drive the tracker. The exact server-generated AP Star gates, goal and difficulty are restored automatically.

Already-generated v0.28 seeds work: install the updated APWorld locally; do not
regenerate the seed or erase the save. Older slot schemas are rejected rather than
silently tracked with the wrong logic. Generic placement exclusions from older
seeds are not reconstructed as tracker display filters; they do not remove those
checks or change their reachability. No custom map or `/explain` support is claimed.

## Verification

Automated tests exercise the production restoration path, JSON key reordering,
different tracker random seeds/default options, and malformed data rejection.
The actual UT v0.3.3 and v0.2.32 initialization, regeneration and update code passed 13 headless
cases: Normal/Hard/Expert/Perfection at goals 1/40/66 and an existing Hard40 seed.
Reachable check sets were compared against the original APWorld at multiple item
and Star states, including below-goal and at-goal Victory logic. Checked locations
disappeared from the available list. These checks did not exercise the GUI or a
live network connection and do not establish compatibility with every UT version.

Integration follows the [upstream UT API documentation](https://github.com/FarisTheAncient/Archipelago/blob/tracker/worlds/tracker/docs/apworld-integration.md).

Release validation: all 146 APWorld tests and repository validation passed. Independent review found no blocking issue. The installed APWorld hash matches the release package.
