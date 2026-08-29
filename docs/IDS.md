# Permanent Archipelago ID Registry

**Base ID:** `187256000`

Once an item or location ID has existed in a published/tested datapackage, it is never reused for a different thing. Gaps remain reserved.

## Location IDs

| Offset | Absolute ID(s) | Allocation |
| ---: | ---: | --- |
| `+1..+3` | `187256001..187256003` | Level 1–3 Completion |
| `+4..+10` | `187256004..187256010` | **Historical/reserved — never reuse** |
| `+11..+20` | `187256011..187256020` | Development Cache 01–10 |
| `+21..+44` | `187256021..187256044` | Game Garage sticker checks |
| `+45` | `187256045` | Music Lab 64 Point Chest |
| `+46..+165` | `187256046..187256165` | 30 Music Lab cassette songs × 4 medal tiers |
| `+166..+168` | `187256166..187256168` | Music Lab 89 / 111 / 140 Point Chests |
| `+169..+173` | `187256169..187256173` | Music Lab 5 / 10 / 20 / 32 / 46 Point Chests |
| `+174..+177` | `187256174..187256177` | Smooch / Superstar / Vampire Killer / Wag the Dog cartridge source checks |
| `+178` | `187256178` | Roots - Gecko's Weed Killer |
| `+179` | `187256179` | Roots - Level 3 - Frog and Hippo |
| `+180` | `187256180` | Roots - Level 4 - Hip Glasses |
| `+181` | `187256181` | Roots - Bucket Minion Trade |
| `+182..+185` | `187256182..187256185` | Level 22 Completion / 1 Star / 2 Stars / 3 Stars |
| `+186` | `187256186` | Level 2 - Money Cassette |

**Next safe location offset:** `+187`
**Next safe location ID:** `187256187`

The v0.18 allocation contains **178** network locations, including four ordinary
Level 22 checks. Victory remains a separate addressless event.
The current allocation contains **179** network locations.

## Item IDs

| Offset | Absolute ID | Item |
| ---: | ---: | --- |
| `+101` | `187256101` | Level 2 Access — historical, preserved, no longer generated |
| `+102` | `187256102` | Level 3 Access — historical, preserved, no longer generated |
| `+103` | `187256103` | Stardust |
| `+104` | `187256104` | Roots Access |
| `+105` | `187256105` | Lobby Access |
| `+106` | `187256106` | Meat Dimension Access |
| `+107` | `187256107` | Cell Tower Access |
| `+108` | `187256108` | Tower of Fear Access |
| `+109` | `187256109` | Royal Corridor Access |
| `+110` | `187256110` | Bloody Tears Cartridge |
| `+111` | `187256111` | Gradius Remix Cartridge |
| `+112` | `187256112` | Smooch Cartridge |
| `+113` | `187256113` | Superstar Cartridge |
| `+114` | `187256114` | Vampire Killer Cartridge |
| `+115` | `187256115` | Wag the Dog Cartridge |
| `+116` | `187256116` | Weed Killer |
| `+117` | `187256117` | Plant Pipes |
| `+118` | `187256118` | Star — registered for the generation foundation; not yet active in the live item pool |
| `+119` | `187256119` | Hip Glasses |
| `+120` | `187256120` | Chicken Bucket |
| `+121` | `187256121` | Hypno Pan — registered preview; excluded from generated pools |
| `+122` | `187256122` | Violance — registered preview; excluded from generated pools |
| `+123` | `187256123` | Money Cassette |

**Next safe item offset:** `+124`
**Next safe item ID:** `187256124`

## Rule for changes

Before assigning an ID:

1. Check this document and the current `ITEM_NAME_TO_ID` / `LOCATION_NAME_TO_ID` definitions.
2. Use the next safe unused ID; do not fill historical gaps.
3. Update this document in the same commit.
4. If the APWorld datapackage changes, require a newly generated seed for testing.
