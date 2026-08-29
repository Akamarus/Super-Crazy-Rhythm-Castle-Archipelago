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
| `+187..+210` | `187256187..187256210` | 24 new full-cassette source checks |

**Next safe location offset:** `+211`
**Next safe location ID:** `187256211`

The v0.18 allocation contains **178** network locations, including four ordinary
Level 22 checks. Victory remains a separate addressless event.
The current allocation contains **203** network locations.

### Full cassette source IDs

| Offset | Absolute ID | Source location |
| ---: | ---: | --- |
| `+187` | `187256187` | Cassette Source - The Little Things |
| `+188` | `187256188` | Cassette Source - No Plan B |
| `+189` | `187256189` | Cassette Source - Jolt City |
| `+190` | `187256190` | Cassette Source - Quieres Bailar |
| `+191` | `187256191` | Cassette Source - Gold |
| `+192` | `187256192` | Cassette Source - Hippo and Frog |
| `+193` | `187256193` | Cassette Source - On the Way |
| `+194` | `187256194` | Cassette Source - Badass |
| `+195` | `187256195` | Cassette Source - Heavy Metal |
| `+196` | `187256196` | Cassette Source - AOK |
| `+197` | `187256197` | Cassette Source - Rainbow Melodies |
| `+198` | `187256198` | Cassette Source - Sneaking |
| `+199` | `187256199` | Cassette Source - The Heist |
| `+200` | `187256200` | Cassette Source - Money |
| `+201` | `187256201` | Cassette Source - Lets Go |
| `+202` | `187256202` | Cassette Source - Bounce |
| `+203` | `187256203` | Cassette Source - Epical |
| `+204` | `187256204` | Cassette Source - Hollywood Trailer |
| `+205` | `187256205` | Cassette Source - False Data |
| `+206` | `187256206` | Cassette Source - Gotta Get Up |
| `+207` | `187256207` | Cassette Source - Fumblin Around |
| `+208` | `187256208` | Cassette Source - Party Non Stop |
| `+209` | `187256209` | Cassette Source - Keep On Hustlin |
| `+210` | `187256210` | Cassette Source - Another Day In Paradise |

### Reused cassette source events

The following Music Lab chest events remain authoritative under their historical
IDs; full cassette randomization creates no duplicate source location for them.

| Cassette | Existing source event | Existing ID |
| --- | --- | ---: |
| Quicksand | Music Lab - 32 Point Chest | `187256172` |
| Flamenco | Music Lab - 64 Point Chest | `187256045` |
| Ten-Four Good Buddy | Music Lab - 89 Point Chest | `187256166` |
| Zen | Music Lab - 111 Point Chest | `187256167` |
| Wiggle | Music Lab - 140 Point Chest | `187256168` |

`I Got Money` separately continues to reuse the existing `Level 2 - Money
Cassette` source at `187256186`.

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
| `+123` | `187256123` | Money Cassette (I Got Money native song) |
| `+124` | `187256124` | The Little Things Cassette |
| `+125` | `187256125` | No Plan B Cassette |
| `+126` | `187256126` | Jolt City Cassette |
| `+127` | `187256127` | Quieres Bailar Cassette |
| `+128` | `187256128` | Quicksand Cassette |
| `+129` | `187256129` | Gold Cassette |
| `+130` | `187256130` | Hippo and Frog Cassette |
| `+131` | `187256131` | On the Way Cassette |
| `+132` | `187256132` | Badass Cassette |
| `+133` | `187256133` | Heavy Metal Cassette |
| `+134` | `187256134` | AOK Cassette |
| `+135` | `187256135` | Rainbow Melodies Cassette |
| `+136` | `187256136` | Sneaking Cassette |
| `+137` | `187256137` | The Heist Cassette |
| `+138` | `187256138` | Money Dub Cassette |
| `+139` | `187256139` | Lets Go Cassette |
| `+140` | `187256140` | Bounce Cassette |
| `+141` | `187256141` | Epical Cassette |
| `+142` | `187256142` | Hollywood Trailer Cassette |
| `+143` | `187256143` | False Data Cassette |
| `+144` | `187256144` | Gotta Get Up Cassette |
| `+145` | `187256145` | Fumblin Around Cassette |
| `+146` | `187256146` | Party Non Stop Cassette |
| `+147` | `187256147` | Keep On Hustlin Cassette |
| `+148` | `187256148` | Another Day In Paradise Cassette |
| `+149` | `187256149` | Flamenco Cassette |
| `+150` | `187256150` | Ten-Four Good Buddy Cassette |
| `+151` | `187256151` | Zen Cassette |
| `+152` | `187256152` | Wiggle Cassette |

**Next safe item offset:** `+153`
**Next safe item ID:** `187256153`

## Rule for changes

Before assigning an ID:

1. Check this document and the current `ITEM_NAME_TO_ID` / `LOCATION_NAME_TO_ID` definitions.
2. Use the next safe unused ID; do not fill historical gaps.
3. Update this document in the same commit.
4. If the APWorld datapackage changes, require a newly generated seed for testing.
