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
| `+179` | `187256179` | Roots - Level 3 - Plant Pipes Pickup |
| `+180` | `187256180` | Roots - Level 4 - Hip Glasses |
| `+181` | `187256181` | Roots - Bucket Minion Trade |
| `+182..+185` | `187256182..187256185` | Level 22 Completion / 1 Star / 2 Stars / 3 Stars |
| `+186` | `187256186` | Level 2 - Money Cassette |
| `+187..+210` | `187256187..187256210` | 24 new full-cassette source checks |
| `+211..+291` | `187256211..187256291` | Full normal-campaign location catalog (81 newly allocated checks) |
| `+292` | `187256292` | Lobby - Important Letters Pickup (reserved, inactive) |
| `+293` | `187256293` | Lobby - Bean Trumpet Award (reserved, inactive) |
| `+294` | `187256294` | Lobby - Plunger Pickup (Stardust-only) |
| `+295` | `187256295` | Lobby - Car Battery Hand-In |
| `+296` | `187256296` | Game Garage - Old Game Data Hand-In |
| `+297` | `187256297` | Roots - Star Eater Fed (Stardust-only) |

**Next safe location offset:** `+298`
**Next safe location ID:** `187256298`

The v0.18 allocation contains **178** network locations, including four ordinary
Level 22 checks. Victory remains a separate addressless event.
The current registry contains **286** named network locations, including 13 inactive reservations. Active addressed totals are Normal 121 / Hard 179 / Expert 237 / Perfection 273.

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

### Full normal-campaign location IDs

The normal-campaign catalog has four cumulative locations per level: Completion,
1 Star, 2 Stars, and 3 Stars. Existing Level 1–3 Completion and all Level 22
locations retain their historical IDs. The remaining entries are allocated in
ascending level number and tier order.

| Offset | Absolute ID | Campaign location |
| ---: | ---: | --- |
| `+211` | `187256211` | Level 1 - 1 Star |
| `+212` | `187256212` | Level 1 - 2 Stars |
| `+213` | `187256213` | Level 1 - 3 Stars |
| `+214` | `187256214` | Level 2 - 1 Star |
| `+215` | `187256215` | Level 2 - 2 Stars |
| `+216` | `187256216` | Level 2 - 3 Stars |
| `+217` | `187256217` | Level 3 - 1 Star |
| `+218` | `187256218` | Level 3 - 2 Stars |
| `+219` | `187256219` | Level 3 - 3 Stars |
| `+220` | `187256220` | Level 4 - Completion |
| `+221` | `187256221` | Level 4 - 1 Star |
| `+222` | `187256222` | Level 4 - 2 Stars |
| `+223` | `187256223` | Level 4 - 3 Stars |
| `+224` | `187256224` | Level 5 - Completion |
| `+225` | `187256225` | Level 5 - 1 Star |
| `+226` | `187256226` | Level 5 - 2 Stars |
| `+227` | `187256227` | Level 5 - 3 Stars |
| `+228` | `187256228` | Level 6 - Completion |
| `+229` | `187256229` | Level 6 - 1 Star |
| `+230` | `187256230` | Level 6 - 2 Stars |
| `+231` | `187256231` | Level 6 - 3 Stars |
| `+232` | `187256232` | Level 7 - Completion |
| `+233` | `187256233` | Level 7 - 1 Star |
| `+234` | `187256234` | Level 7 - 2 Stars |
| `+235` | `187256235` | Level 7 - 3 Stars |
| `+236` | `187256236` | Level 8 - Completion |
| `+237` | `187256237` | Level 8 - 1 Star |
| `+238` | `187256238` | Level 8 - 2 Stars |
| `+239` | `187256239` | Level 8 - 3 Stars |
| `+240` | `187256240` | Level 9 - Completion |
| `+241` | `187256241` | Level 9 - 1 Star |
| `+242` | `187256242` | Level 9 - 2 Stars |
| `+243` | `187256243` | Level 9 - 3 Stars |
| `+244` | `187256244` | Level 10 - Completion |
| `+245` | `187256245` | Level 10 - 1 Star |
| `+246` | `187256246` | Level 10 - 2 Stars |
| `+247` | `187256247` | Level 10 - 3 Stars |
| `+248` | `187256248` | Level 11 - Completion |
| `+249` | `187256249` | Level 11 - 1 Star |
| `+250` | `187256250` | Level 11 - 2 Stars |
| `+251` | `187256251` | Level 11 - 3 Stars |
| `+252` | `187256252` | Level 12 - Completion |
| `+253` | `187256253` | Level 12 - 1 Star |
| `+254` | `187256254` | Level 12 - 2 Stars |
| `+255` | `187256255` | Level 12 - 3 Stars |
| `+256` | `187256256` | Level 13 - Completion |
| `+257` | `187256257` | Level 13 - 1 Star |
| `+258` | `187256258` | Level 13 - 2 Stars |
| `+259` | `187256259` | Level 13 - 3 Stars |
| `+260` | `187256260` | Level 14 - Completion |
| `+261` | `187256261` | Level 14 - 1 Star |
| `+262` | `187256262` | Level 14 - 2 Stars |
| `+263` | `187256263` | Level 14 - 3 Stars |
| `+264` | `187256264` | Level 15 - Completion |
| `+265` | `187256265` | Level 15 - 1 Star |
| `+266` | `187256266` | Level 15 - 2 Stars |
| `+267` | `187256267` | Level 15 - 3 Stars |
| `+268` | `187256268` | Level 16 - Completion |
| `+269` | `187256269` | Level 16 - 1 Star |
| `+270` | `187256270` | Level 16 - 2 Stars |
| `+271` | `187256271` | Level 16 - 3 Stars |
| `+272` | `187256272` | Level 17 - Completion |
| `+273` | `187256273` | Level 17 - 1 Star |
| `+274` | `187256274` | Level 17 - 2 Stars |
| `+275` | `187256275` | Level 17 - 3 Stars |
| `+276` | `187256276` | Level 18 - Completion |
| `+277` | `187256277` | Level 18 - 1 Star |
| `+278` | `187256278` | Level 18 - 2 Stars |
| `+279` | `187256279` | Level 18 - 3 Stars |
| `+280` | `187256280` | Level 19 - Completion |
| `+281` | `187256281` | Level 19 - 1 Star |
| `+282` | `187256282` | Level 19 - 2 Stars |
| `+283` | `187256283` | Level 19 - 3 Stars |
| `+284` | `187256284` | Level 20 - Completion |
| `+285` | `187256285` | Level 20 - 1 Star |
| `+286` | `187256286` | Level 20 - 2 Stars |
| `+287` | `187256287` | Level 20 - 3 Stars |
| `+288` | `187256288` | Level 21 - Completion |
| `+289` | `187256289` | Level 21 - 1 Star |
| `+290` | `187256290` | Level 21 - 2 Stars |
| `+291` | `187256291` | Level 21 - 3 Stars |

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
| `+153` | `187256153` | Music Lab Point |
| `+154` | `187256154` | Music Lab Point Bundle |
| `+155` | `187256155` | Music Lab Point Large Bundle |
| `+156` | `187256156` | Important Letters (reserved, inactive) |
| `+157` | `187256157` | Bean Trumpet (reserved, inactive) |
| `+158` | `187256158` | Demolition Certificate (reserved, inactive) |
| `+159` | `187256159` | Old Game Data (progression in v0.26; useful in v0.25) |
| `+160` | `187256160` | Car Battery (progression in v0.26; useful in v0.25) |
| `+161` | `187256161` | Plunger (useful) |
| `+162` | `187256162` | Meoo (useful character unlock) |
| `+163` | `187256163` | Maniac (useful character unlock) |

**Next safe item offset:** `+164`
**Next safe item ID:** `187256164`

## Rule for changes

Before assigning an ID:

1. Check this document and the current `ITEM_NAME_TO_ID` / `LOCATION_NAME_TO_ID` definitions.
2. Use the next safe unused ID; do not fill historical gaps.
3. Update this document in the same commit.
4. If the APWorld datapackage changes, require a newly generated seed for testing.

### v0.25 character quest items

Old Game Data and Car Battery each appear once as useful items, replacing two filler items. They reuse the existing Music Lab 5-point (`187256169`) and 20-point (`187256171`) checks; no new locations or character rewards are randomized. Native bag flags are `LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM` and `CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM`. Slot data uses schema 16 and character quest item schema 1. A newly generated v0.25 seed is required; old seeds are not upgraded. Lobby item IDs `187256156`–`187256158` remain reserved and inactive.


### v0.26 quest checks candidate

The new IDs above follow native metadata inspection; existing reservations are
unchanged. Schema 17 exports `quest_checks_schema: 1`, `quest_items` as the exact
three name-to-ID entries, and `quest_locations` as the exact four name-to-ID
entries above. Original `character_quest_items` and
`character_quest_item_locations` maps remain unchanged. New quests require a
fresh v0.26 seed; schema-16 seeds are not upgraded. The candidate is uninstalled
and gameplay acceptance is pending. Native mapping and scope limitations are
recorded in [the acceptance record](testing/2026-09-16-quest-checks-candidate.md).
