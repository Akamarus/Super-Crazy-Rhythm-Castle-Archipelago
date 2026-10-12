# Universal Tracker

The unreleased APWorld 0.29.0 candidate supports existing schema-19 individual-cassette seeds and schema-20 bundled-cassette seeds. Use the matching APWorld on the computer running Universal Tracker. Bundled seeds also require game client 0.76.0.

## Setup

1. Install Archipelago with its generator component and Universal Tracker.
2. Install the appropriate `scrc.apworld` on the tracker computer, replacing its older copy, then restart Archipelago.
3. Keep your player YAML in the Players folder as required by your Universal Tracker setup. Server-slot reconstruction does not remove that setup requirement.
4. Open Universal Tracker from the Launcher and connect using your room address, slot name and password if needed. Keep the game connected normally.
5. Received items and checked locations drive availability; SCRC restores the generated difficulty, goal, Star gates and cassette bundle membership from slot data.

Existing supported seeds do not need regeneration or a save reset just to update the local tracker. Updating logic cannot change placements in an existing seed. Unsupported or contradictory contracts produce an error instead of silently using guessed values. Schema 19 permits omission of older derived metadata; when present, it must agree with the reconstructed graph. Schema 20 requires its complete metadata.

## Verification and limits

October 11 candidate checks passed with actual AP 0.6.8 and UT 0.2.32: 12 worlds and 1,888 inventory states, a separate 37-case matrix including the retained Hard40 seed, and nine offline received-item protocol cases. These exercise reconstruction and packet processing, not live GUI/socket behavior or every native game route.

Early Lobby story ordering and native Hypno Pan crafting prerequisites remain under investigation. A tracker can match the generator while both use an inaccurate native prerequisite. See [tracker repair evidence](testing/2026-10-11-tracker-repairs.md) for the distinction.

Generic placement exclusions in older seeds are not reconstructed as display filters; excluded checks still exist. No custom map or `/explain` support is claimed. Obtain public builds from the [project releases](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases); this candidate is not yet published.
