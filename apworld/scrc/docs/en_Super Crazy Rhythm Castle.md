# Super Crazy Rhythm Castle

The experimental v0.23.0 candidate uses the native Hub6 phone bank as the Archipelago navigation hub, and **Roots Access is forced as the precollected starter** during the current Roots development phase. It retains experimental routing for all 30 Music Lab cassettes, with many individual routes still awaiting manual verification. Their source check sends once; the received cassette enters the native bag and must still be inserted normally. Five cassette rewards reuse the 32/64/89/111/140-point chest locations. A fresh v0.23 seed, fresh native save, and Client v0.69.0 are required for manual acceptance; this is not a completed release.

Five Game Garage cartridges remain randomized items and require their matching AP item. Vampire Killer is native and its Bronze/Silver/Gold/Platinum checks have no AP-item gate.

## Music Lab Points candidate

The pool contains 10 one-point items, 3 ten-point bundles, and 7 twenty-point large bundles: 180 points in 20 progression items replacing 20 Stardust. Nine existing chests require 5/10/20/32/46/64/89/111/140 AP points. The final threshold leaves 40 points of slack. No point milestone checks are added; existing cassette and Gradius Remix/Bloody Tears chest sources send only their existing location.

The exact schema-14/point-schema-1 contract is required. Before authoritative history sync the Music Lab total is zero; afterwards it is the weighted AP total capped at 180, retained during a temporary disconnect and rebuilt on reconnect/relaunch. Native medal results never add AP points or receive AP score writes. Recognized v0.22 seeds and non-AP play keep native scoring; malformed v0.23 contracts report incompatibility and stay at zero.

The existing managed getter substitutes AP points only in `GameRoom_Hub6`. No chest/native detour is installed and no chest is forced open. Live display, all nine below/at thresholds, exactly-once checks, persistence, compatibility, and native medal invariance remain required acceptance tests.

## AP performance difficulty

`difficulty` selects existing AP performance locations only; it does not change the player's native REG/PRO choice and does not create campaign checks or IDs.

| AP difficulty | Campaign tiers | Song medal tiers | Addressed locations |
| --- | --- | --- | ---: |
| Normal | Completion / 1-Star | Bronze | 92 |
| Hard | Completion / 1-Star + 2-Star | Bronze + Silver | 129 |
| Expert | Completion / 1-Star + 2-Star + 3-Star | Bronze + Silver + Gold | 166 |
| Perfection | Same campaign tiers as Expert | Bronze + Silver + Gold + Platinum | 202 |

Inactive checks are absent, not filler. This candidate retains the existing performance filters and location counts. Active Level-22 2/3-Star checks remain filler-only; Music Lab point chests may hold progression when weighted AP-point reachability proves a valid chain.

## Roots progression

Gecko's Roots reward is the AP location `Roots - Gecko's Weed Killer`, with randomized item `Weed Killer`.

Inside Level 3, Frog and Hippo's reward is now the AP location `Roots - Level 3 - Frog and Hippo`, with randomized item `Plant Pipes`.

Logical split:

- Weed Killer is required to enter Level 3 and reach Frog/Hippo.
- Plant Pipes is **not** required for the Frog/Hippo check.
- Plant Pipes **is** required for `Level 3 - Completion`.
- If Plant Pipes is not owned after reaching Frog/Hippo, using the normal menu exit is intentional progression behavior.

Randomized AP-Star costs and the remaining vanilla-world item prerequisites are still under development.
