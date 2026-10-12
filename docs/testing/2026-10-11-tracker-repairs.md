# Universal Tracker candidate repairs - October 11, 2026

Unpublished APWorld 0.29.0 / client 0.76.0. No installation, game/save changes,
server connection, commit, push or publication. Existing candidate work preserved.

## Changes

Cassette result aliases dispatch both Bee and Devil variants through the existing
optional completion predicates, matching native identities in the expanded-check
catalog. No new IDs or checks. Covers Gold, On the Way, AOK, Rainbow Melodies,
Fumblin Around and Keep On Hustlin Devil aliases. Bee aliases share their check's
ability prerequisites.

The parser validates authoritative metadata against the reconstructed graph:
Star Eater thresholds, active location count, campaign names and tiers, campaign
Star tiers, medal tiers, and difficulty filtering. Contradictions, wrong types,
duplicate list entries, boolean integers and floating-point schema versions fail.
List ordering is immaterial. Exact generated gates and bundle mappings survive.

Legacy policy: schema19 may omit these seven derived metadata fields; canonical
values are supplied. Present fields must agree. Schema20 requires all seven.
Normalized metadata survives interpretation/regeneration and is independent of
input dictionaries. Seeds without a native-logic revision marker still use the
current predicates; historical rule-version equivalence is not established.

## Verification

Before implementation, 12 malformed-metadata subcases failed because invalid
contracts were accepted. Both Devil alias assertions failed despite available
optional completions. After repairs:

- Full APWorld unit suite: 177 tests passed.
- Actual official AP0.6.8 and UT0.2.32: 12 worlds (four difficulties, three bundle
  modes), 1,888 inventory states, zero reconstruction defects. Includes missing
  items, currency boundaries, mixed inventory, checked filtering and empty reset.
- Separate actual UT matrix: 37 cases passed, including retained schema19 Hard40,
  all difficulties/goals and bundle modes.
- Actual AP CommonClient packet processing plus UT: nine offline protocol cases
  passed (initial/append/duplicate/gap/resync/checked-only/reset). Transport shell
  and outbound recording are mocked; socket authentication, actual reconnect,
  production watcher timing and GUI are not exercised.

Scratch evidence: `work/tracker-repairs/` in the local task workspace. AP loads
copied candidate source from scratch `work/ap068/source/.../worlds/scrc`.
The older scratch archive is explicitly skipped. Runtime warnings report the
optional C++ speedup fallback and skipped duplicate world.

The alias regression deliberately uses a synthetic nonmonotonic Star map to
isolate dispatch. Valid generated gates are nondecreasing and Bunker entry
requires Level22; ordinary routes currently shadow these aliases. This repairs
missing alternative dispatch, not a demonstrated valid-seed suggestion failure.

## Native findings deliberately unresolved

Early Lobby: E33 records AP Lobby arrival after Roots Level1; E36 records Boring
Room and Letters later in that session. This disproves a blanket physical-arrival
dependency on Lift Quest but does not isolate minimum button/King/story conditions
at the later completion. Historical vanilla notes begin after Lift Quest. The
Level6-to-Level5 closure remains conservative and may hide early checks. Required
acceptance: fresh early Lobby arrival without Lift Quest/Roots quest items, then
trace button, Boring Room, Letters and upper branches individually. Do not erase
all inherited conditions from Plunger, Fish Tears or Bunker without proof.

Hypno/Nectar: delivery and Level16 use ordinary Level2/11/15 surrogates, while Bee
completion checks require Hypno Pan. This is inconsistent. However E18 in
`2026-09-11-star-victory-and-quest-capacity-acceptance.md` proves the native recipe
grants Hypno Pan and consumes Spoon/Frying Pan. It is deliberately preserved
because dogs enclose the post-recipe route. AP receipt is not the only ability
source. Requiring AP Hypno solely for Nectar would not model that route and may
hide valid actions. Required acceptance: prove the recipe's minimum route and
represent attainable native Hypno consistently across Bee, Nectar, Cell and all
other Hypno-dependent checks. No guessed recipe event or traversal is introduced.

Native fresh-save playthrough and live tracker acceptance remain required.

## Retained current-seed replay — October 11

Actual UT v0.2.32 on AP 0.6.8 reconstructed the retained pair-bundle seed from its slot data. Receipts include precollected items and the saved complete self-item stream, with duplicates preserved and completed checks removed. Snapshot: 21 AP Stars, 46 Music Lab points, 27/164 checked, 25 unchecked in logic, goal false. UT output matched direct current-world rule evaluation. Seven targeted assertions passed: checked filtering, Weed Killer gate, Cell Tower Star Eater hidden, Normal medal tiers, unowned Smooch hidden, above-46 point chests hidden, and both new cassette medal checks visible or checked. Zen Bronze was already checked. Installed SCRC Python files matched the tested source.

Evidence: local work/current-tracker-audit/report.json and report.md. This is offline engine replay, not a live tracker GUI or socket test, and cannot establish native reachability where early Lobby/Hypno route evidence remains unresolved. No items, checks, saves or running tracker configuration were changed.

## October 11 corrected independent Lobby routes

User-confirmed native behavior: Lobby Access independently opens Boring Room, Minim Tower and The Vault; none requires Lift Quest or another Lobby level. Each retains its own generated AP Star entry gate. Plunger hand-in requires Lobby Access and Plunger only. Lobby Star Eater requires those plus its seed-authored Star threshold (4 in the current test seed). Other Lobby story dependencies remain explicit and are not silently removed.

Regression tests reproduce the old failures and cover independent level gates, absent area access, Plunger and below-threshold Stars. All 181 APWorld tests pass. Actual UT replay covers 12 worlds / 1,888 inventory states with zero reconstruction defects; current test inventory now exposes 48 unchecked locations rather than 25. Existing placements, IDs and saved Star requirements are preserved. This supersedes the historical conservative Lift Quest prerequisite and the earlier claim that matching tracker/world predicates alone validated native access.

