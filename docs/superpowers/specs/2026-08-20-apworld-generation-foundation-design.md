# APWorld Generation Foundation Design

**Status:** Approved for implementation
**Date:** 2026-08-20

## Purpose

Build the deterministic Archipelago-side foundation for the approved full randomizer without depending on additional native-game discovery. This phase introduces stable options, Star inventory planning, difficulty filtering, generated campaign requirements, versioned slot data, and automated generation validation. It does not activate client-side Star gates, replace the current development victory condition, add unverified native locations, or guess game mappings.

## Scope

This phase implements:

- `required_stars`, ranging from 1 through 66 with a recommended default of 50;
- `difficulty`, with Normal, Hard, Expert, and Perfection values;
- `starting_area`, with `random`, Roots, Lobby, Meat Dimension, and Cell Tower represented;
- a validated-starter policy in which only Roots is initially generation-valid;
- deterministic requirements for campaign Levels 1 through 22;
- construction of exactly 66 copies of one AP `Star` item type;
- filtering of existing cumulative performance locations by AP difficulty;
- versioned slot-data output for options and generated requirements; and
- automated tests for reproducibility, bounds, validation, item counts, and filtering.

This phase explicitly excludes:

- client enforcement of generated Star gates;
- Level 22 victory changes;
- Tower of Fear or Royal Corridor as starting areas;
- new campaign performance locations beyond names and IDs already present;
- native Normal/Pro behavior changes;
- Music Lab Point items;
- cassette randomization;
- unverified quest items, characters, sources, routes, or native flags; and
- any new native location ID allocation.

## Architecture

The current APWorld keeps definitions, generation, regions, rules, and slot data in `apworld/scrc/__init__.py`. This phase extracts only the new deterministic concerns into focused modules while preserving the existing world behavior:

- `options.py` owns Archipelago option classes, enum values, ranges, and the `SCRCOptions` dataclass.
- `items.py` owns the Star item name, its single network ID, classifications, and pure item-count helpers.
- `difficulty.py` owns cumulative tier ordering and pure filtering helpers for location names that already exist.
- `star_requirements.py` owns deterministic Level 1–22 requirement generation and validation.
- `__init__.py` continues to own the `World` integration, current regions, existing rules, item placement, and slot-data assembly.

The extracted modules must not import the game client, access native save state, or allocate native mappings. Pure functions accept ordinary values and return immutable or newly allocated Python data so they can be tested without launching the game.

## Options

### Required Stars

`required_stars` is a numeric range from 1 through 66 and defaults to 50. It describes the eventual victory target and scales generated campaign requirements. This phase exports it to slot data but does not replace the current Area Access milestone completion condition.

### Difficulty

`difficulty` has four values:

| Value | Campaign locations | Music Lab and Game Garage locations |
| --- | --- | --- |
| Normal | Completion / 1-Star where already defined | Bronze |
| Hard | Normal plus 2-Star where already defined | Bronze and Silver |
| Expert | Hard plus 3-Star where already defined | Bronze, Silver, and Gold |
| Perfection | Same existing campaign tiers as Expert | Bronze, Silver, Gold, and Platinum |

Only existing location names and IDs may be filtered. The current APWorld has campaign completion checks for Levels 1–3 and cumulative medal checks for Music Lab and Game Garage. This phase must not invent Level 4–22 checks or additional campaign tier IDs.

### Starting Area

The option vocabulary represents `random`, Roots, Lobby, Meat Dimension, and Cell Tower. Tower of Fear and Royal Corridor are excluded from the option because their starts are explicitly unsafe.

A separate ordered validated-starter collection initially contains only Roots. Selection follows these rules:

- `roots` selects Roots.
- `random` selects deterministically from the validated-starter collection using the world random-number generator, and therefore resolves to Roots initially.
- Explicit Lobby, Meat Dimension, or Cell Tower requests fail generation with a clear unsupported-start message until their starter validation is promoted in code.
- The selection never silently substitutes Roots for an explicitly requested unsupported fixed start.

Adding a validated starter later changes only the validated collection and its tests; the public option vocabulary remains stable.

## Star Item Model

The APWorld defines one item named `Star` with one new permanent item ID at the next safe item offset recorded by `docs/IDS.md`. A generated multiworld contains exactly 66 instances of that item, each sent individually. There are no Star bundles.

Stars are progression items. This phase adds them to the item-pool construction model and reports their count in slot data. Because client gates and final victory are not activated, the existing v0.15 milestone behavior remains explicitly identified as temporary. Generation must retain enough locations for the entire pool and fail clearly if item capacity is insufficient.

No other item or location IDs are allocated in this phase. `docs/IDS.md` is updated in the same commit that introduces the Star ID.

## Generated Campaign Requirements

The generator produces a requirement for every campaign level from 1 through 22. The output is an ordered mapping keyed by the stable player-facing names `Level 1` through `Level 22`.

Requirements are deterministic for a seed because they use the Archipelago world's seeded random-number generator. They obey these invariants:

- every requirement is an integer from 0 through `required_stars - 1`;
- opening requirements may be zero;
- requirements are nondecreasing across the provisional linear Level 1–22 order;
- equal thresholds are allowed;
- Level 22 is always below `required_stars`;
- the default 50-Star configuration follows the approved approximate depth bands; and
- values scale down monotonically for smaller goals without exceeding available Stars.

The current APWorld does not yet model the final branching campaign graph. Therefore, this phase uses the stable linear level order as a provisional structural sequence and labels it as such in slot data. Later native-routing work may supply a graph/depth input without changing the validation contract.

The pure API is:

```python
def generate_star_requirements(required_stars: int, rng: random.Random) -> dict[str, int]:
    ...

def validate_star_requirements(requirements: Mapping[str, int], required_stars: int) -> None:
    ...
```

Validation raises a descriptive `ValueError` for an out-of-range goal, missing or extra level, non-integer value, negative value, threshold at or above the goal, decreasing sequence, or invalid Level 22 threshold.

## Difficulty Filtering

Difficulty filtering is based on explicit cumulative tier tables rather than string-order assumptions. A helper receives existing location names and returns the subset enabled by the selected difficulty while preserving input order.

The filter recognizes:

- `Game Garage - <song> - <tier>`;
- `Music Lab Cassette - <song> - <tier>`; and
- existing campaign completion/tier names when those names are present.

Unrecognized locations, source checks, reward chests, development caches, and the current internal Victory event are retained. Filtering never creates a location or ID.

The pure API is:

```python
def filter_locations_for_difficulty(
    location_names: Iterable[str],
    difficulty: int,
) -> tuple[str, ...]:
    ...
```

## Slot Data and Compatibility

The implementation version advances from `area-routing-plant-pipes-0.15` to a new generation-foundation schema tag. Slot data adds:

- `schema_version`;
- `required_stars`;
- canonical `difficulty` name and numeric value;
- requested and resolved starting-area values;
- the ordered validated-starter list;
- `star_item_name` and `star_item_count`;
- the Level 1–22 generated requirement mapping;
- a flag stating that requirements use provisional linear campaign depth;
- a flag stating that client Star-gate enforcement is not active; and
- a flag stating that the existing completion condition remains the development Area Access milestone.

Existing v0.15 fields remain available unless their meaning would become false. The client is not changed in this phase, so the new slot data must be additive and must not claim support the client lacks.

## Generation Validation

Generation fails with a clear error when:

- an explicitly selected starter is not validated;
- `required_stars` is outside 1–66;
- generated requirements violate an invariant;
- the active location count cannot contain the complete item pool; or
- a difficulty value cannot be normalized.

`random` never fails merely because represented-but-unvalidated starters exist; it samples only validated starters. Deterministic tests use fixed RNG seeds rather than global randomness.

## Testing Strategy

The implementation follows test-driven development. Pure-module tests run without the game and cover:

- option defaults, ranges, and enum values;
- deterministic random-start resolution and explicit unsupported starts;
- exactly 66 individual Star items and one Star network ID;
- reproducible requirement tables for equal seeds;
- variation across different seeds where the allowed range permits it;
- all requirement validation failures;
- monotonic scaling and Level 22 remaining below the goal for goals 1, 2, 10, 50, and 66;
- cumulative difficulty filtering for every difficulty;
- preservation of unrecognized/source/chest locations;
- item-capacity rejection; and
- complete, accurately labeled slot data.

Repository validation and APWorld packaging run after the unit suite. No gameplay claim is made from these tests.

## Acceptance Criteria

This foundation is complete when:

1. the new option classes load through the Archipelago world interface;
2. random starting-area selection resolves only among validated starters and fixed unsupported starts fail clearly;
3. exactly 66 individual Stars are constructed with one registered item ID;
4. generated Level 1–22 requirements are deterministic and satisfy every invariant;
5. existing performance locations are filtered cumulatively without allocating new locations;
6. slot data accurately distinguishes generated planning from inactive client enforcement;
7. all new unit tests, repository validation, and APWorld packaging pass; and
8. current native mappings, client behavior, and development victory logic remain unchanged.
