# October 12 user prerequisite audit

Candidate only; not installed or published. Source: the user's edited 316-check workbook (233 confirmed, 68 needs-change, 7 questions, 8 unreviewed), followed by their explicit correction that Hypno Pan IS needed to access Loneliness. Do not remove that requirement from Level 20 or Party Non Stop.

## Applied

- Plunger pickup requires Lobby Access without Boring Room.
- Meet the Bees requires Cell Tower Access without Central Mainframe.
- Frying Pan and Smooch pickups require Meat Dimension Access without Act 1.
- Bucket Minion trade and its internal event require Roots Access and Hip Glasses without earlier level completion or Stars.
- Demolition Certificate source uses Boring Room rather than Demolition Training; retains its filler-only policy.
- Bee variants require Cell Tower Access, their ordinary base route and Hypno Pan, without Central Mainframe.
- Fish Tears delivery requires the native Act 4 award route and Lobby Access, plus either Cell Tower Access or the previous Minim Tower/School Trip/Vault route. Fish Tears is not an AP inventory item.
- Lift Quest has a Lobby return entrance: Roots Access + Lobby Access + its own generated Star gate. The forward route remains unchanged. Its normal cassette sources share this alternative; native trigger identities are unchanged. Combo Bucket conversion and its internal event still require Hip Glasses and Chicken Bucket.
- Cold Storage explicitly requires Plant Pipes; its existing story and Hypno Pan requirements remain pending further targeted review.
- Royal Corridor Star Eater requires Royal Corridor Access and its configured Star threshold, without Locker Room.

## Already enforced / retained

Plant Pipes for Levels 3, 4, 15, 18, 21 and 22, and Hypno Pan for Acts 2–4 are inherited through the level rules. The revised workbook expands them instead of requiring the reader to recursively follow route references. Level 3's partial Plant Pipes pickup retains the Level 3 entrance Star gate and does not require Plant Pipes. Roots Star Eater already uses its seed-derived threshold.

Do not remove result/entry Star gates from shorthand edits listing area access alone. Preserve verified native cassette aliases; they do not add checks. The normal award route remains available independently when its prerequisites are met.

## Targeted questions still open

- School Trip: whether Plant Pipes is needed for completion or only particular ratings.
- Cold Storage and its three cassette sources: confirm exact story access and ability requirements.
- Cell Tower Star Eater: user reports Level 17 story dependency; earlier native inspection found an ability ownership condition named like a completion condition. Resolve with native evidence/testing before changing the rule.
- Bunker Keycard is currently native, not an AP item. The user's conditional AP-keycard model is not implemented in this patch; no new IDs are allocated. Bunker Star Eater remains 66 Stars.
- Higher King Ferdinand ratings require late-game Gecko investigation. Existing checks remain filler-only and are not guaranteed reachable; filler-only placement does not mean their access model is correct.
- Gold and On the Way aliases remain explicitly under review.
- Preserve the eight unreviewed rows and prior missing-check audit items (Notepad, Data Stick, Demon Key).

## Validation

Nine new regression tests first reproduced the incorrect restrictions; all 190 APWorld tests now pass. Updated old assertions only where they encoded the superseded route, preserving negative tests for missing areas/items and below-threshold Stars. Hypno Pan remains covered for both Loneliness and Party Non Stop.

Official AP 0.6.8 generation: 54/54 cases pass, across all four difficulties, goals 1/40/66, individual/pair/triple cassettes, original exclusions and mixed-game sessions. Checks include independent collection to Victory, bidirectional foreign progression, retained currencies and exclusions.

Actual Universal Tracker 0.2.32 replay: 12 worlds / 1,888 inventory states, zero reconstruction differences, including item removal, checked-location filtering and currency boundaries. This demonstrates tracker/world consistency, not native proof of the unresolved routes.

No client deployment, seed rewrite, save reset, server restart or release publication was performed.
