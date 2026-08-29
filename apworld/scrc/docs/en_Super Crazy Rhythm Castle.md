# Super Crazy Rhythm Castle

v0.22.0 uses the native Hub6 phone bank as the Archipelago navigation hub, and **Roots Access is forced as the precollected starter** during the current Roots development phase. All 30 Music Lab cassettes are randomized. Their source check sends once; the received cassette enters the native bag and must still be inserted normally. Five cassette rewards reuse the 32/64/89/111/140-point chest locations. A fresh v0.22 seed and Client v0.68.0 are required.

Five Game Garage cartridges remain randomized items and require their matching AP item. Vampire Killer is native and its Bronze/Silver/Gold/Platinum checks have no AP-item gate.

## AP performance difficulty

`difficulty` selects existing AP performance locations only; it does not change the player's native REG/PRO choice and does not create campaign checks or IDs.

| AP difficulty | Campaign tiers | Song medal tiers | Addressed locations |
| --- | --- | --- | ---: |
| Normal | Completion / 1-Star | Bronze | 92 |
| Hard | Completion / 1-Star + 2-Star | Bronze + Silver | 129 |
| Expert | Completion / 1-Star + 2-Star + 3-Star | Bronze + Silver + Gold | 166 |
| Perfection | Same campaign tiers as Expert | Bronze + Silver + Gold + Platinum | 202 |

Inactive checks are absent, not filler. This release filters only existing campaign performance locations. Active Level-22 2/3-Star checks and Music Lab point chests remain filler-only.

## Roots progression

Gecko's Roots reward is the AP location `Roots - Gecko's Weed Killer`, with randomized item `Weed Killer`.

Inside Level 3, Frog and Hippo's reward is now the AP location `Roots - Level 3 - Frog and Hippo`, with randomized item `Plant Pipes`.

Logical split:

- Weed Killer is required to enter Level 3 and reach Frog/Hippo.
- Plant Pipes is **not** required for the Frog/Hippo check.
- Plant Pipes **is** required for `Level 3 - Completion`.
- If Plant Pipes is not owned after reaching Frog/Hippo, using the normal menu exit is intentional progression behavior.

Randomized AP-Star costs and the remaining vanilla-world item prerequisites are still under development.
