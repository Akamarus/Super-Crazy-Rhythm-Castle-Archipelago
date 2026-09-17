# Complete location and check-name audit

Audited 2026-09-16 against the installed game and current source. **286 registered addressed checks**, including **13 inactive reservations**. Active totals: Normal **121**, Hard **179**, Expert **237**, Perfection **273**. Three addressless solver events are listed separately below.

## Results and limits

- All 22 normal campaign numbers/internal IDs agree with the native LevelData assets. Eight incorrect or incomplete campaign titles were corrected in source (including the earlier Light Humor correction). No network IDs changed. After review, the Plant Pipes source check was renamed to make the pickup explicit.
- All 30 cassette identities and all 37 native cassette-award routes agree with the catalogs. All six Garage song identities and nine chest threshold/source-flag pairs were checked against game data.
- Nine cassette song spellings and one Garage song spelling differ from the exact in-game titles. They remain explicit legacy AP network aliases below. Renaming those live protocol strings is a separate compatibility change; they are not represented as exact game spellings.
- AP Platinum corresponds to the native PERFECT result (the game also has a PLATINUM quick-play override enum). Completion, numbered Stars, point chest and quest-source strings are AP descriptions of native events, not verbatim interface labels.
- Quest-source descriptions retain the native flags and recorded gameplay evidence. Reserved Lobby sources and retired caches are identified as inactive, not claimed as verified gameplay checks.
- This is a static identity/name audit. It does not certify every location as reachable or every runtime check as tested end to end. Gameplay testing remains paused.

## All 22 campaign locations

| Number | Exact English game title | Internal ID | Native cassette award(s) |
| ---: | --- | --- | --- |
| 1 | Light Humor | Level_05 | Gold |
| 2 | Pop Party | Level_06 | I Got Money |
| 3 | The Megafying Ritual | Level_07 | Hippo and Frog |
| 4 | DJ Eggplant | Level_08 | On The Way |
| 5 | Lift Quest | Level_09 | Badass, Heavy Metal |
| 6 | Boring Room | Level_02 | AOK |
| 7 | Demolition Training | Level_19 | Sneaking, Rainbow Melodies |
| 8 | Minim Tower | Level_11 | Rainbow Melodies, On The Way, Gold |
| 9 | School Trip | Level_20 | Sneaking, The Heist |
| 10 | The Vault | Level_01 | Money |
| 11 | Act 1: Flavor | Level_12 | Let's Go |
| 12 | Act 2: Sauce and Spice | Level_15 | Bounce |
| 13 | Act 3: Montage | Level_22 | No Plan B |
| 14 | Act 4: Habanero | Level_23 | Jolt City |
| 15 | Central Mainframe | Level_16 | Quieres Bailar |
| 16 | The Thief Prince | Level_24 | The Little Things |
| 17 | Cold Storage | Level_21 | The Epical, Hollywood Trailer, False Data |
| 18 | The Darkness | Level_03 | Gotta Get Up |
| 19 | Escape | Level_13 | Fumblin' Around |
| 20 | Loneliness | Level_25 | Party Non-Stop |
| 21 | Locker Room | Level_14 | Keep on Hustlin' |
| 22 | King Ferdinand I | Level_28 | Another Day in Paradise |

## Corrected campaign names

| Level | Previous source label | Exact game title |
| ---: | --- | --- |
| 1 | The Little Things | Light Humor |
| 3 | Jolt City | The Megafying Ritual |
| 4 | Quieres Bailar | DJ Eggplant |
| 12 | Act 2 | Act 2: Sauce and Spice |
| 13 | Act 3 | Act 3: Montage |
| 14 | Act 4 | Act 4: Habanero |
| 16 | Thief Prince | The Thief Prince |
| 18 | Darkness | The Darkness |

## Retained network song aliases

| Exact AP song label | Exact game title |
| --- | --- |
| On the Way | On The Way |
| Lets Go | Let's Go |
| Epical | The Epical |
| Fumblin Around | Fumblin' Around |
| Party Non Stop | Party Non-Stop |
| Keep On Hustlin | Keep on Hustlin' |
| Another Day In Paradise | Another Day in Paradise |
| Ten-Four Good Buddy | 10-4 Good Buddy |
| Wiggle | Time to Wiggle |
| Smooch | Smooooch |

The legacy check **Level 2 - Money Cassette** is the **I Got Money** source. It is distinct from **Cassette Source - Money**, which belongs to The Vault. The Little Things cassette belongs to The Thief Prince, not Light Humor.

## Entire registered check list

Every row below is one exact registered AP name. No tier groups are collapsed. Active difficulties refer to generated locations, not proof of in-game reachability.

| ID | Exact AP check name | In-game name / action | Active difficulties | Evidence |
| ---: | --- | --- | --- | --- |
| 187256001 | Level 1 - Completion | Light Humor — Completion | Normal/Hard/Expert/Perfection | Level_05 / #LOC_GR05_LevelDisplayName |
| 187256002 | Level 2 - Completion | Pop Party — Completion | Normal/Hard/Expert/Perfection | Level_06 / #LOC_GR06_LevelDisplayName |
| 187256003 | Level 3 - Completion | The Megafying Ritual — Completion | Normal/Hard/Expert/Perfection | Level_07 / #LOC_GR07_LevelDisplayName |
| 187256011 | Development Cache 01 | Retired synthetic check; no native location | Inactive/reserved | Permanent ID reservation |
| 187256012 | Development Cache 02 | Retired synthetic check; no native location | Inactive/reserved | Permanent ID reservation |
| 187256013 | Development Cache 03 | Retired synthetic check; no native location | Inactive/reserved | Permanent ID reservation |
| 187256014 | Development Cache 04 | Retired synthetic check; no native location | Inactive/reserved | Permanent ID reservation |
| 187256015 | Development Cache 05 | Retired synthetic check; no native location | Inactive/reserved | Permanent ID reservation |
| 187256016 | Development Cache 06 | Retired synthetic check; no native location | Inactive/reserved | Permanent ID reservation |
| 187256017 | Development Cache 07 | Retired synthetic check; no native location | Inactive/reserved | Permanent ID reservation |
| 187256018 | Development Cache 08 | Retired synthetic check; no native location | Inactive/reserved | Permanent ID reservation |
| 187256019 | Development Cache 09 | Retired synthetic check; no native location | Inactive/reserved | Permanent ID reservation |
| 187256020 | Development Cache 10 | Retired synthetic check; no native location | Inactive/reserved | Permanent ID reservation |
| 187256021 | Game Garage - Bloody Tears - Bronze | Bloody Tears — Bronze | Normal/Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256022 | Game Garage - Bloody Tears - Silver | Bloody Tears — Silver | Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256023 | Game Garage - Bloody Tears - Gold | Bloody Tears — Gold | Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256024 | Game Garage - Bloody Tears - Platinum | Bloody Tears — Perfect (AP Platinum) | Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256025 | Game Garage - Gradius Remix - Bronze | Gradius Remix — Bronze | Normal/Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256026 | Game Garage - Gradius Remix - Silver | Gradius Remix — Silver | Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256027 | Game Garage - Gradius Remix - Gold | Gradius Remix — Gold | Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256028 | Game Garage - Gradius Remix - Platinum | Gradius Remix — Perfect (AP Platinum) | Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256029 | Game Garage - Smooch - Bronze | Smooooch — Bronze | Normal/Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256030 | Game Garage - Smooch - Silver | Smooooch — Silver | Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256031 | Game Garage - Smooch - Gold | Smooooch — Gold | Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256032 | Game Garage - Smooch - Platinum | Smooooch — Perfect (AP Platinum) | Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256033 | Game Garage - Superstar - Bronze | Superstar — Bronze | Normal/Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256034 | Game Garage - Superstar - Silver | Superstar — Silver | Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256035 | Game Garage - Superstar - Gold | Superstar — Gold | Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256036 | Game Garage - Superstar - Platinum | Superstar — Perfect (AP Platinum) | Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256037 | Game Garage - Vampire Killer - Bronze | Vampire Killer — Bronze | Normal/Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256038 | Game Garage - Vampire Killer - Silver | Vampire Killer — Silver | Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256039 | Game Garage - Vampire Killer - Gold | Vampire Killer — Gold | Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256040 | Game Garage - Vampire Killer - Platinum | Vampire Killer — Perfect (AP Platinum) | Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256041 | Game Garage - Wag the Dog - Bronze | Wag the Dog — Bronze | Normal/Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256042 | Game Garage - Wag the Dog - Silver | Wag the Dog — Silver | Hard/Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256043 | Game Garage - Wag the Dog - Gold | Wag the Dog — Gold | Expert/Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256044 | Game Garage - Wag the Dog - Platinum | Wag the Dog — Perfect (AP Platinum) | Perfection | SongInformationProvider; eRoom27StickerQuality |
| 187256045 | Music Lab - 64 Point Chest | 64-point Music Lab chest | Normal/Hard/Expert/Perfection | SONG_CASSETTE_COLLECTED_FLAMENCO |
| 187256046 | Music Lab Cassette - The Little Things - Bronze | The Little Things — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_LittleThings; native THE_LITTLE_THINGS |
| 187256047 | Music Lab Cassette - The Little Things - Silver | The Little Things — Silver | Hard/Expert/Perfection | #LOC_SongTitle_LittleThings; native THE_LITTLE_THINGS |
| 187256048 | Music Lab Cassette - The Little Things - Gold | The Little Things — Gold | Expert/Perfection | #LOC_SongTitle_LittleThings; native THE_LITTLE_THINGS |
| 187256049 | Music Lab Cassette - The Little Things - Platinum | The Little Things — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_LittleThings; native THE_LITTLE_THINGS |
| 187256050 | Music Lab Cassette - No Plan B - Bronze | No Plan B — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_NoPlanB; native NO_PLAN_B |
| 187256051 | Music Lab Cassette - No Plan B - Silver | No Plan B — Silver | Hard/Expert/Perfection | #LOC_SongTitle_NoPlanB; native NO_PLAN_B |
| 187256052 | Music Lab Cassette - No Plan B - Gold | No Plan B — Gold | Expert/Perfection | #LOC_SongTitle_NoPlanB; native NO_PLAN_B |
| 187256053 | Music Lab Cassette - No Plan B - Platinum | No Plan B — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_NoPlanB; native NO_PLAN_B |
| 187256054 | Music Lab Cassette - Jolt City - Bronze | Jolt City — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_JoltCity; native JOLT_CITY |
| 187256055 | Music Lab Cassette - Jolt City - Silver | Jolt City — Silver | Hard/Expert/Perfection | #LOC_SongTitle_JoltCity; native JOLT_CITY |
| 187256056 | Music Lab Cassette - Jolt City - Gold | Jolt City — Gold | Expert/Perfection | #LOC_SongTitle_JoltCity; native JOLT_CITY |
| 187256057 | Music Lab Cassette - Jolt City - Platinum | Jolt City — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_JoltCity; native JOLT_CITY |
| 187256058 | Music Lab Cassette - Quieres Bailar - Bronze | Quieres Bailar — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_QuieresBailar; native QUIERES_BAILAR |
| 187256059 | Music Lab Cassette - Quieres Bailar - Silver | Quieres Bailar — Silver | Hard/Expert/Perfection | #LOC_SongTitle_QuieresBailar; native QUIERES_BAILAR |
| 187256060 | Music Lab Cassette - Quieres Bailar - Gold | Quieres Bailar — Gold | Expert/Perfection | #LOC_SongTitle_QuieresBailar; native QUIERES_BAILAR |
| 187256061 | Music Lab Cassette - Quieres Bailar - Platinum | Quieres Bailar — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_QuieresBailar; native QUIERES_BAILAR |
| 187256062 | Music Lab Cassette - Quicksand - Bronze | Quicksand — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_Quicksand; native QUICKSAND |
| 187256063 | Music Lab Cassette - Quicksand - Silver | Quicksand — Silver | Hard/Expert/Perfection | #LOC_SongTitle_Quicksand; native QUICKSAND |
| 187256064 | Music Lab Cassette - Quicksand - Gold | Quicksand — Gold | Expert/Perfection | #LOC_SongTitle_Quicksand; native QUICKSAND |
| 187256065 | Music Lab Cassette - Quicksand - Platinum | Quicksand — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_Quicksand; native QUICKSAND |
| 187256066 | Music Lab Cassette - Gold - Bronze | Gold — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_Gold; native GOLD |
| 187256067 | Music Lab Cassette - Gold - Silver | Gold — Silver | Hard/Expert/Perfection | #LOC_SongTitle_Gold; native GOLD |
| 187256068 | Music Lab Cassette - Gold - Gold | Gold — Gold | Expert/Perfection | #LOC_SongTitle_Gold; native GOLD |
| 187256069 | Music Lab Cassette - Gold - Platinum | Gold — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_Gold; native GOLD |
| 187256070 | Music Lab Cassette - I Got Money - Bronze | I Got Money — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_IGotMoney; native I_GOT_MONEY |
| 187256071 | Music Lab Cassette - I Got Money - Silver | I Got Money — Silver | Hard/Expert/Perfection | #LOC_SongTitle_IGotMoney; native I_GOT_MONEY |
| 187256072 | Music Lab Cassette - I Got Money - Gold | I Got Money — Gold | Expert/Perfection | #LOC_SongTitle_IGotMoney; native I_GOT_MONEY |
| 187256073 | Music Lab Cassette - I Got Money - Platinum | I Got Money — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_IGotMoney; native I_GOT_MONEY |
| 187256074 | Music Lab Cassette - Hippo and Frog - Bronze | Hippo and Frog — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_HippoAndFrog; native HIPPO_AND_FROG |
| 187256075 | Music Lab Cassette - Hippo and Frog - Silver | Hippo and Frog — Silver | Hard/Expert/Perfection | #LOC_SongTitle_HippoAndFrog; native HIPPO_AND_FROG |
| 187256076 | Music Lab Cassette - Hippo and Frog - Gold | Hippo and Frog — Gold | Expert/Perfection | #LOC_SongTitle_HippoAndFrog; native HIPPO_AND_FROG |
| 187256077 | Music Lab Cassette - Hippo and Frog - Platinum | Hippo and Frog — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_HippoAndFrog; native HIPPO_AND_FROG |
| 187256078 | Music Lab Cassette - On the Way - Bronze | On The Way — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_OnTheWay; native ON_THE_WAY |
| 187256079 | Music Lab Cassette - On the Way - Silver | On The Way — Silver | Hard/Expert/Perfection | #LOC_SongTitle_OnTheWay; native ON_THE_WAY |
| 187256080 | Music Lab Cassette - On the Way - Gold | On The Way — Gold | Expert/Perfection | #LOC_SongTitle_OnTheWay; native ON_THE_WAY |
| 187256081 | Music Lab Cassette - On the Way - Platinum | On The Way — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_OnTheWay; native ON_THE_WAY |
| 187256082 | Music Lab Cassette - Badass - Bronze | Badass — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_Badass; native BADASS |
| 187256083 | Music Lab Cassette - Badass - Silver | Badass — Silver | Hard/Expert/Perfection | #LOC_SongTitle_Badass; native BADASS |
| 187256084 | Music Lab Cassette - Badass - Gold | Badass — Gold | Expert/Perfection | #LOC_SongTitle_Badass; native BADASS |
| 187256085 | Music Lab Cassette - Badass - Platinum | Badass — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_Badass; native BADASS |
| 187256086 | Music Lab Cassette - Heavy Metal - Bronze | Heavy Metal — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_HeavyMetal; native HEAVY_METAL |
| 187256087 | Music Lab Cassette - Heavy Metal - Silver | Heavy Metal — Silver | Hard/Expert/Perfection | #LOC_SongTitle_HeavyMetal; native HEAVY_METAL |
| 187256088 | Music Lab Cassette - Heavy Metal - Gold | Heavy Metal — Gold | Expert/Perfection | #LOC_SongTitle_HeavyMetal; native HEAVY_METAL |
| 187256089 | Music Lab Cassette - Heavy Metal - Platinum | Heavy Metal — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_HeavyMetal; native HEAVY_METAL |
| 187256090 | Music Lab Cassette - AOK - Bronze | AOK — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_AOK; native AOK |
| 187256091 | Music Lab Cassette - AOK - Silver | AOK — Silver | Hard/Expert/Perfection | #LOC_SongTitle_AOK; native AOK |
| 187256092 | Music Lab Cassette - AOK - Gold | AOK — Gold | Expert/Perfection | #LOC_SongTitle_AOK; native AOK |
| 187256093 | Music Lab Cassette - AOK - Platinum | AOK — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_AOK; native AOK |
| 187256094 | Music Lab Cassette - Rainbow Melodies - Bronze | Rainbow Melodies — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_RainbowMelodies; native RAINBOW_MELODIES |
| 187256095 | Music Lab Cassette - Rainbow Melodies - Silver | Rainbow Melodies — Silver | Hard/Expert/Perfection | #LOC_SongTitle_RainbowMelodies; native RAINBOW_MELODIES |
| 187256096 | Music Lab Cassette - Rainbow Melodies - Gold | Rainbow Melodies — Gold | Expert/Perfection | #LOC_SongTitle_RainbowMelodies; native RAINBOW_MELODIES |
| 187256097 | Music Lab Cassette - Rainbow Melodies - Platinum | Rainbow Melodies — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_RainbowMelodies; native RAINBOW_MELODIES |
| 187256098 | Music Lab Cassette - Sneaking - Bronze | Sneaking — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_Sneaking; native SNEAKING_LOOP |
| 187256099 | Music Lab Cassette - Sneaking - Silver | Sneaking — Silver | Hard/Expert/Perfection | #LOC_SongTitle_Sneaking; native SNEAKING_LOOP |
| 187256100 | Music Lab Cassette - Sneaking - Gold | Sneaking — Gold | Expert/Perfection | #LOC_SongTitle_Sneaking; native SNEAKING_LOOP |
| 187256101 | Music Lab Cassette - Sneaking - Platinum | Sneaking — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_Sneaking; native SNEAKING_LOOP |
| 187256102 | Music Lab Cassette - The Heist - Bronze | The Heist — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_Heist; native THE_HEIST |
| 187256103 | Music Lab Cassette - The Heist - Silver | The Heist — Silver | Hard/Expert/Perfection | #LOC_SongTitle_Heist; native THE_HEIST |
| 187256104 | Music Lab Cassette - The Heist - Gold | The Heist — Gold | Expert/Perfection | #LOC_SongTitle_Heist; native THE_HEIST |
| 187256105 | Music Lab Cassette - The Heist - Platinum | The Heist — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_Heist; native THE_HEIST |
| 187256106 | Music Lab Cassette - Money - Bronze | Money — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_MoneyDub; native MONEY_DUB |
| 187256107 | Music Lab Cassette - Money - Silver | Money — Silver | Hard/Expert/Perfection | #LOC_SongTitle_MoneyDub; native MONEY_DUB |
| 187256108 | Music Lab Cassette - Money - Gold | Money — Gold | Expert/Perfection | #LOC_SongTitle_MoneyDub; native MONEY_DUB |
| 187256109 | Music Lab Cassette - Money - Platinum | Money — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_MoneyDub; native MONEY_DUB |
| 187256110 | Music Lab Cassette - Lets Go - Bronze | Let's Go — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_LetsGo; native LETS_GO |
| 187256111 | Music Lab Cassette - Lets Go - Silver | Let's Go — Silver | Hard/Expert/Perfection | #LOC_SongTitle_LetsGo; native LETS_GO |
| 187256112 | Music Lab Cassette - Lets Go - Gold | Let's Go — Gold | Expert/Perfection | #LOC_SongTitle_LetsGo; native LETS_GO |
| 187256113 | Music Lab Cassette - Lets Go - Platinum | Let's Go — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_LetsGo; native LETS_GO |
| 187256114 | Music Lab Cassette - Bounce - Bronze | Bounce — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_Bounce; native BOUNCE |
| 187256115 | Music Lab Cassette - Bounce - Silver | Bounce — Silver | Hard/Expert/Perfection | #LOC_SongTitle_Bounce; native BOUNCE |
| 187256116 | Music Lab Cassette - Bounce - Gold | Bounce — Gold | Expert/Perfection | #LOC_SongTitle_Bounce; native BOUNCE |
| 187256117 | Music Lab Cassette - Bounce - Platinum | Bounce — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_Bounce; native BOUNCE |
| 187256118 | Music Lab Cassette - Epical - Bronze | The Epical — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_Epical; native THE_EPICAL |
| 187256119 | Music Lab Cassette - Epical - Silver | The Epical — Silver | Hard/Expert/Perfection | #LOC_SongTitle_Epical; native THE_EPICAL |
| 187256120 | Music Lab Cassette - Epical - Gold | The Epical — Gold | Expert/Perfection | #LOC_SongTitle_Epical; native THE_EPICAL |
| 187256121 | Music Lab Cassette - Epical - Platinum | The Epical — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_Epical; native THE_EPICAL |
| 187256122 | Music Lab Cassette - Hollywood Trailer - Bronze | Hollywood Trailer — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_HollywoodTrailer; native HOLLYWOOD_TRAILER |
| 187256123 | Music Lab Cassette - Hollywood Trailer - Silver | Hollywood Trailer — Silver | Hard/Expert/Perfection | #LOC_SongTitle_HollywoodTrailer; native HOLLYWOOD_TRAILER |
| 187256124 | Music Lab Cassette - Hollywood Trailer - Gold | Hollywood Trailer — Gold | Expert/Perfection | #LOC_SongTitle_HollywoodTrailer; native HOLLYWOOD_TRAILER |
| 187256125 | Music Lab Cassette - Hollywood Trailer - Platinum | Hollywood Trailer — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_HollywoodTrailer; native HOLLYWOOD_TRAILER |
| 187256126 | Music Lab Cassette - False Data - Bronze | False Data — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_FalseData; native FALSE_DATA |
| 187256127 | Music Lab Cassette - False Data - Silver | False Data — Silver | Hard/Expert/Perfection | #LOC_SongTitle_FalseData; native FALSE_DATA |
| 187256128 | Music Lab Cassette - False Data - Gold | False Data — Gold | Expert/Perfection | #LOC_SongTitle_FalseData; native FALSE_DATA |
| 187256129 | Music Lab Cassette - False Data - Platinum | False Data — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_FalseData; native FALSE_DATA |
| 187256130 | Music Lab Cassette - Gotta Get Up - Bronze | Gotta Get Up — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_GottaGetUp; native GOTTA_GET_UP |
| 187256131 | Music Lab Cassette - Gotta Get Up - Silver | Gotta Get Up — Silver | Hard/Expert/Perfection | #LOC_SongTitle_GottaGetUp; native GOTTA_GET_UP |
| 187256132 | Music Lab Cassette - Gotta Get Up - Gold | Gotta Get Up — Gold | Expert/Perfection | #LOC_SongTitle_GottaGetUp; native GOTTA_GET_UP |
| 187256133 | Music Lab Cassette - Gotta Get Up - Platinum | Gotta Get Up — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_GottaGetUp; native GOTTA_GET_UP |
| 187256134 | Music Lab Cassette - Fumblin Around - Bronze | Fumblin' Around — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_FumblinAround; native FUMBLIN_AROUND |
| 187256135 | Music Lab Cassette - Fumblin Around - Silver | Fumblin' Around — Silver | Hard/Expert/Perfection | #LOC_SongTitle_FumblinAround; native FUMBLIN_AROUND |
| 187256136 | Music Lab Cassette - Fumblin Around - Gold | Fumblin' Around — Gold | Expert/Perfection | #LOC_SongTitle_FumblinAround; native FUMBLIN_AROUND |
| 187256137 | Music Lab Cassette - Fumblin Around - Platinum | Fumblin' Around — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_FumblinAround; native FUMBLIN_AROUND |
| 187256138 | Music Lab Cassette - Party Non Stop - Bronze | Party Non-Stop — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_PartyNonStop; native PARTY_NON_STOP |
| 187256139 | Music Lab Cassette - Party Non Stop - Silver | Party Non-Stop — Silver | Hard/Expert/Perfection | #LOC_SongTitle_PartyNonStop; native PARTY_NON_STOP |
| 187256140 | Music Lab Cassette - Party Non Stop - Gold | Party Non-Stop — Gold | Expert/Perfection | #LOC_SongTitle_PartyNonStop; native PARTY_NON_STOP |
| 187256141 | Music Lab Cassette - Party Non Stop - Platinum | Party Non-Stop — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_PartyNonStop; native PARTY_NON_STOP |
| 187256142 | Music Lab Cassette - Keep On Hustlin - Bronze | Keep on Hustlin' — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_KeepOnHustlin; native KEEP_ON_HUSTLIN |
| 187256143 | Music Lab Cassette - Keep On Hustlin - Silver | Keep on Hustlin' — Silver | Hard/Expert/Perfection | #LOC_SongTitle_KeepOnHustlin; native KEEP_ON_HUSTLIN |
| 187256144 | Music Lab Cassette - Keep On Hustlin - Gold | Keep on Hustlin' — Gold | Expert/Perfection | #LOC_SongTitle_KeepOnHustlin; native KEEP_ON_HUSTLIN |
| 187256145 | Music Lab Cassette - Keep On Hustlin - Platinum | Keep on Hustlin' — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_KeepOnHustlin; native KEEP_ON_HUSTLIN |
| 187256146 | Music Lab Cassette - Another Day In Paradise - Bronze | Another Day in Paradise — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_AnotherDayInParadise; native ANOTHER_DAY_IN_PARADISE |
| 187256147 | Music Lab Cassette - Another Day In Paradise - Silver | Another Day in Paradise — Silver | Hard/Expert/Perfection | #LOC_SongTitle_AnotherDayInParadise; native ANOTHER_DAY_IN_PARADISE |
| 187256148 | Music Lab Cassette - Another Day In Paradise - Gold | Another Day in Paradise — Gold | Expert/Perfection | #LOC_SongTitle_AnotherDayInParadise; native ANOTHER_DAY_IN_PARADISE |
| 187256149 | Music Lab Cassette - Another Day In Paradise - Platinum | Another Day in Paradise — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_AnotherDayInParadise; native ANOTHER_DAY_IN_PARADISE |
| 187256150 | Music Lab Cassette - Flamenco - Bronze | Flamenco — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_Flamenco; native FLAMENCO |
| 187256151 | Music Lab Cassette - Flamenco - Silver | Flamenco — Silver | Hard/Expert/Perfection | #LOC_SongTitle_Flamenco; native FLAMENCO |
| 187256152 | Music Lab Cassette - Flamenco - Gold | Flamenco — Gold | Expert/Perfection | #LOC_SongTitle_Flamenco; native FLAMENCO |
| 187256153 | Music Lab Cassette - Flamenco - Platinum | Flamenco — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_Flamenco; native FLAMENCO |
| 187256154 | Music Lab Cassette - Ten-Four Good Buddy - Bronze | 10-4 Good Buddy — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_TenFourGoodBuddy; native TEN_FOUR_GOOD_BUDDY |
| 187256155 | Music Lab Cassette - Ten-Four Good Buddy - Silver | 10-4 Good Buddy — Silver | Hard/Expert/Perfection | #LOC_SongTitle_TenFourGoodBuddy; native TEN_FOUR_GOOD_BUDDY |
| 187256156 | Music Lab Cassette - Ten-Four Good Buddy - Gold | 10-4 Good Buddy — Gold | Expert/Perfection | #LOC_SongTitle_TenFourGoodBuddy; native TEN_FOUR_GOOD_BUDDY |
| 187256157 | Music Lab Cassette - Ten-Four Good Buddy - Platinum | 10-4 Good Buddy — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_TenFourGoodBuddy; native TEN_FOUR_GOOD_BUDDY |
| 187256158 | Music Lab Cassette - Zen - Bronze | Zen — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_Zen; native ZEN |
| 187256159 | Music Lab Cassette - Zen - Silver | Zen — Silver | Hard/Expert/Perfection | #LOC_SongTitle_Zen; native ZEN |
| 187256160 | Music Lab Cassette - Zen - Gold | Zen — Gold | Expert/Perfection | #LOC_SongTitle_Zen; native ZEN |
| 187256161 | Music Lab Cassette - Zen - Platinum | Zen — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_Zen; native ZEN |
| 187256162 | Music Lab Cassette - Wiggle - Bronze | Time to Wiggle — Bronze | Normal/Hard/Expert/Perfection | #LOC_SongTitle_Wiggle; native WIGGLE |
| 187256163 | Music Lab Cassette - Wiggle - Silver | Time to Wiggle — Silver | Hard/Expert/Perfection | #LOC_SongTitle_Wiggle; native WIGGLE |
| 187256164 | Music Lab Cassette - Wiggle - Gold | Time to Wiggle — Gold | Expert/Perfection | #LOC_SongTitle_Wiggle; native WIGGLE |
| 187256165 | Music Lab Cassette - Wiggle - Platinum | Time to Wiggle — Perfect (AP Platinum) | Perfection | #LOC_SongTitle_Wiggle; native WIGGLE |
| 187256166 | Music Lab - 89 Point Chest | 89-point Music Lab chest | Normal/Hard/Expert/Perfection | SONG_CASSETTE_COLLECTED_TEN_FOUR_GOOD_BUDDY |
| 187256167 | Music Lab - 111 Point Chest | 111-point Music Lab chest | Normal/Hard/Expert/Perfection | SONG_CASSETTE_COLLECTED_ZEN |
| 187256168 | Music Lab - 140 Point Chest | 140-point Music Lab chest | Normal/Hard/Expert/Perfection | SONG_CASSETTE_COLLECTED_WIGGLE |
| 187256169 | Music Lab - 5 Point Chest | 5-point Music Lab chest | Normal/Hard/Expert/Perfection | CLEAN_HUB_MEMORY_CARD_TAKEN |
| 187256170 | Music Lab - 10 Point Chest | 10-point Music Lab chest | Normal/Hard/Expert/Perfection | LEVEL_27_CARTRIDGE_GRADIUS_COLLECTED |
| 187256171 | Music Lab - 20 Point Chest | 20-point Music Lab chest | Normal/Hard/Expert/Perfection | CLEAN_HUB_GHOST_CAT_BATTERY_REWARDED |
| 187256172 | Music Lab - 32 Point Chest | 32-point Music Lab chest | Normal/Hard/Expert/Perfection | SONG_CASSETTE_COLLECTED_QUICKSAND |
| 187256173 | Music Lab - 46 Point Chest | 46-point Music Lab chest | Normal/Hard/Expert/Perfection | LEVEL_27_CARTRIDGE_BLOODYTEARS_COLLECTED |
| 187256174 | Cartridge Pickup - Smooch | Smooooch cartridge pickup | Normal/Hard/Expert/Perfection | LEVEL_27_CARTRIDGE_SMOOCH_COLLECTED; retained source gameplay evidence |
| 187256175 | Cartridge Pickup - Superstar | Superstar cartridge pickup | Normal/Hard/Expert/Perfection | LEVEL_27_CARTRIDGE_STAR_EATER_COLLECTED; retained source gameplay evidence |
| 187256176 | Cartridge Pickup - Vampire Killer | Vampire Killer cartridge pickup | Inactive/reserved | LEVEL_27_CARTRIDGE_VAMPIREKILLER_COLLECTED; retained source gameplay evidence |
| 187256177 | Cartridge Pickup - Wag the Dog | Wag the Dog cartridge pickup | Normal/Hard/Expert/Perfection | LEVEL_27_CARTRIDGE_SUPER_CRAZY_RHYTHM_CASTLE_COLLECTED; retained source gameplay evidence |
| 187256178 | Roots - Gecko's Weed Killer | Gecko: Weed Killer source | Normal/Hard/Expert/Perfection | ROOTS_HUB_WEED_KILLER_COLLECTED |
| 187256179 | Roots - Level 3 - Plant Pipes Pickup | The Megafying Ritual: Plant Pipes pickup from Frog and Hippo | Normal/Hard/Expert/Perfection | LEVEL_07_WK_ABILITY_EARNED |
| 187256180 | Roots - Level 4 - Hip Glasses | DJ Eggplant: Hip Glasses | Normal/Hard/Expert/Perfection | LEVEL_08_GLASSES_COLLECTED |
| 187256181 | Roots - Bucket Minion Trade | Bucket Minion: Hip Glasses trade | Normal/Hard/Expert/Perfection | ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES |
| 187256182 | Level 22 - Completion | King Ferdinand I — Completion | Normal/Hard/Expert/Perfection | Level_28 / #LOC_GR28_LevelDisplayName |
| 187256183 | Level 22 - 1 Star | King Ferdinand I — 1 Star | Normal/Hard/Expert/Perfection | Level_28 / #LOC_GR28_LevelDisplayName |
| 187256184 | Level 22 - 2 Stars | King Ferdinand I — 2 Stars | Hard/Expert/Perfection | Level_28 / #LOC_GR28_LevelDisplayName |
| 187256185 | Level 22 - 3 Stars | King Ferdinand I — 3 Stars | Expert/Perfection | Level_28 / #LOC_GR28_LevelDisplayName |
| 187256186 | Level 2 - Money Cassette | I Got Money cassette award | Normal/Hard/Expert/Perfection | Level_06/LevelVariant_BeeMode; Level_06/LevelVariant_Default |
| 187256187 | Cassette Source - The Little Things | The Little Things cassette award | Normal/Hard/Expert/Perfection | Level_24/LevelVariant_Default |
| 187256188 | Cassette Source - No Plan B | No Plan B cassette award | Normal/Hard/Expert/Perfection | Level_22/LevelVariant_Default |
| 187256189 | Cassette Source - Jolt City | Jolt City cassette award | Normal/Hard/Expert/Perfection | Level_23/LevelVariant_Default |
| 187256190 | Cassette Source - Quieres Bailar | Quieres Bailar cassette award | Normal/Hard/Expert/Perfection | Level_16/LevelVariant_Default |
| 187256191 | Cassette Source - Gold | Gold cassette award | Normal/Hard/Expert/Perfection | Level_05/LevelVariant_Default; Level_11/LevelVariant_Default; Level_11/LevelVariant_DevilMode |
| 187256192 | Cassette Source - Hippo and Frog | Hippo and Frog cassette award | Normal/Hard/Expert/Perfection | Level_07/LevelVariant_Default |
| 187256193 | Cassette Source - On the Way | On The Way cassette award | Normal/Hard/Expert/Perfection | Level_08/LevelVariant_Default; Level_11/LevelVariant_Default; Level_11/LevelVariant_DevilMode |
| 187256194 | Cassette Source - Badass | Badass cassette award | Normal/Hard/Expert/Perfection | Level_09/LevelVariant_Default |
| 187256195 | Cassette Source - Heavy Metal | Heavy Metal cassette award | Normal/Hard/Expert/Perfection | Level_09/LevelVariant_Default |
| 187256196 | Cassette Source - AOK | AOK cassette award | Normal/Hard/Expert/Perfection | Level_02/LevelVariant_Default; Level_02/LevelVariant_DevilMode |
| 187256197 | Cassette Source - Rainbow Melodies | Rainbow Melodies cassette award | Normal/Hard/Expert/Perfection | Level_11/LevelVariant_Default; Level_11/LevelVariant_DevilMode; Level_19/LevelVariant_Default |
| 187256198 | Cassette Source - Sneaking | Sneaking cassette award | Normal/Hard/Expert/Perfection | Level_19/LevelVariant_Default; Level_20/LevelVariant_Default |
| 187256199 | Cassette Source - The Heist | The Heist cassette award | Normal/Hard/Expert/Perfection | Level_20/LevelVariant_Default |
| 187256200 | Cassette Source - Money | Money cassette award | Normal/Hard/Expert/Perfection | Level_01/LevelVariant_Default |
| 187256201 | Cassette Source - Lets Go | Let's Go cassette award | Normal/Hard/Expert/Perfection | Level_12/LevelVariant_BeeMode; Level_12/LevelVariant_Default |
| 187256202 | Cassette Source - Bounce | Bounce cassette award | Normal/Hard/Expert/Perfection | Level_15/LevelVariant_Default |
| 187256203 | Cassette Source - Epical | The Epical cassette award | Normal/Hard/Expert/Perfection | Level_21/LevelVariant_Default |
| 187256204 | Cassette Source - Hollywood Trailer | Hollywood Trailer cassette award | Normal/Hard/Expert/Perfection | Level_21/LevelVariant_Default |
| 187256205 | Cassette Source - False Data | False Data cassette award | Normal/Hard/Expert/Perfection | Level_21/LevelVariant_Default |
| 187256206 | Cassette Source - Gotta Get Up | Gotta Get Up cassette award | Normal/Hard/Expert/Perfection | Level_03/LevelVariant_Default |
| 187256207 | Cassette Source - Fumblin Around | Fumblin' Around cassette award | Normal/Hard/Expert/Perfection | Level_13/LevelVariant_Default; Level_13/LevelVariant_DevilMode |
| 187256208 | Cassette Source - Party Non Stop | Party Non-Stop cassette award | Normal/Hard/Expert/Perfection | Level_25/LevelVariant_Default |
| 187256209 | Cassette Source - Keep On Hustlin | Keep on Hustlin' cassette award | Normal/Hard/Expert/Perfection | Level_14/LevelVariant_Default; Level_14/LevelVariant_DevilMode |
| 187256210 | Cassette Source - Another Day In Paradise | Another Day in Paradise cassette award | Normal/Hard/Expert/Perfection | Level_28/LevelVariant_Default |
| 187256211 | Level 1 - 1 Star | Light Humor — 1 Star | Normal/Hard/Expert/Perfection | Level_05 / #LOC_GR05_LevelDisplayName |
| 187256212 | Level 1 - 2 Stars | Light Humor — 2 Stars | Hard/Expert/Perfection | Level_05 / #LOC_GR05_LevelDisplayName |
| 187256213 | Level 1 - 3 Stars | Light Humor — 3 Stars | Expert/Perfection | Level_05 / #LOC_GR05_LevelDisplayName |
| 187256214 | Level 2 - 1 Star | Pop Party — 1 Star | Normal/Hard/Expert/Perfection | Level_06 / #LOC_GR06_LevelDisplayName |
| 187256215 | Level 2 - 2 Stars | Pop Party — 2 Stars | Hard/Expert/Perfection | Level_06 / #LOC_GR06_LevelDisplayName |
| 187256216 | Level 2 - 3 Stars | Pop Party — 3 Stars | Expert/Perfection | Level_06 / #LOC_GR06_LevelDisplayName |
| 187256217 | Level 3 - 1 Star | The Megafying Ritual — 1 Star | Normal/Hard/Expert/Perfection | Level_07 / #LOC_GR07_LevelDisplayName |
| 187256218 | Level 3 - 2 Stars | The Megafying Ritual — 2 Stars | Hard/Expert/Perfection | Level_07 / #LOC_GR07_LevelDisplayName |
| 187256219 | Level 3 - 3 Stars | The Megafying Ritual — 3 Stars | Expert/Perfection | Level_07 / #LOC_GR07_LevelDisplayName |
| 187256220 | Level 4 - Completion | DJ Eggplant — Completion | Normal/Hard/Expert/Perfection | Level_08 / #LOC_GR08_LevelDisplayName |
| 187256221 | Level 4 - 1 Star | DJ Eggplant — 1 Star | Normal/Hard/Expert/Perfection | Level_08 / #LOC_GR08_LevelDisplayName |
| 187256222 | Level 4 - 2 Stars | DJ Eggplant — 2 Stars | Hard/Expert/Perfection | Level_08 / #LOC_GR08_LevelDisplayName |
| 187256223 | Level 4 - 3 Stars | DJ Eggplant — 3 Stars | Expert/Perfection | Level_08 / #LOC_GR08_LevelDisplayName |
| 187256224 | Level 5 - Completion | Lift Quest — Completion | Normal/Hard/Expert/Perfection | Level_09 / #LOC_GR09_LevelDisplayName |
| 187256225 | Level 5 - 1 Star | Lift Quest — 1 Star | Normal/Hard/Expert/Perfection | Level_09 / #LOC_GR09_LevelDisplayName |
| 187256226 | Level 5 - 2 Stars | Lift Quest — 2 Stars | Hard/Expert/Perfection | Level_09 / #LOC_GR09_LevelDisplayName |
| 187256227 | Level 5 - 3 Stars | Lift Quest — 3 Stars | Expert/Perfection | Level_09 / #LOC_GR09_LevelDisplayName |
| 187256228 | Level 6 - Completion | Boring Room — Completion | Normal/Hard/Expert/Perfection | Level_02 / #LOC_GR02_LevelDisplayName |
| 187256229 | Level 6 - 1 Star | Boring Room — 1 Star | Normal/Hard/Expert/Perfection | Level_02 / #LOC_GR02_LevelDisplayName |
| 187256230 | Level 6 - 2 Stars | Boring Room — 2 Stars | Hard/Expert/Perfection | Level_02 / #LOC_GR02_LevelDisplayName |
| 187256231 | Level 6 - 3 Stars | Boring Room — 3 Stars | Expert/Perfection | Level_02 / #LOC_GR02_LevelDisplayName |
| 187256232 | Level 7 - Completion | Demolition Training — Completion | Normal/Hard/Expert/Perfection | Level_19 / #LOC_GR19_LevelDisplayName |
| 187256233 | Level 7 - 1 Star | Demolition Training — 1 Star | Normal/Hard/Expert/Perfection | Level_19 / #LOC_GR19_LevelDisplayName |
| 187256234 | Level 7 - 2 Stars | Demolition Training — 2 Stars | Hard/Expert/Perfection | Level_19 / #LOC_GR19_LevelDisplayName |
| 187256235 | Level 7 - 3 Stars | Demolition Training — 3 Stars | Expert/Perfection | Level_19 / #LOC_GR19_LevelDisplayName |
| 187256236 | Level 8 - Completion | Minim Tower — Completion | Normal/Hard/Expert/Perfection | Level_11 / #LOC_GR11_LevelDisplayName |
| 187256237 | Level 8 - 1 Star | Minim Tower — 1 Star | Normal/Hard/Expert/Perfection | Level_11 / #LOC_GR11_LevelDisplayName |
| 187256238 | Level 8 - 2 Stars | Minim Tower — 2 Stars | Hard/Expert/Perfection | Level_11 / #LOC_GR11_LevelDisplayName |
| 187256239 | Level 8 - 3 Stars | Minim Tower — 3 Stars | Expert/Perfection | Level_11 / #LOC_GR11_LevelDisplayName |
| 187256240 | Level 9 - Completion | School Trip — Completion | Normal/Hard/Expert/Perfection | Level_20 / #LOC_GR20_LevelDisplayName |
| 187256241 | Level 9 - 1 Star | School Trip — 1 Star | Normal/Hard/Expert/Perfection | Level_20 / #LOC_GR20_LevelDisplayName |
| 187256242 | Level 9 - 2 Stars | School Trip — 2 Stars | Hard/Expert/Perfection | Level_20 / #LOC_GR20_LevelDisplayName |
| 187256243 | Level 9 - 3 Stars | School Trip — 3 Stars | Expert/Perfection | Level_20 / #LOC_GR20_LevelDisplayName |
| 187256244 | Level 10 - Completion | The Vault — Completion | Normal/Hard/Expert/Perfection | Level_01 / #LOC_GR01_LevelDisplayName |
| 187256245 | Level 10 - 1 Star | The Vault — 1 Star | Normal/Hard/Expert/Perfection | Level_01 / #LOC_GR01_LevelDisplayName |
| 187256246 | Level 10 - 2 Stars | The Vault — 2 Stars | Hard/Expert/Perfection | Level_01 / #LOC_GR01_LevelDisplayName |
| 187256247 | Level 10 - 3 Stars | The Vault — 3 Stars | Expert/Perfection | Level_01 / #LOC_GR01_LevelDisplayName |
| 187256248 | Level 11 - Completion | Act 1: Flavor — Completion | Normal/Hard/Expert/Perfection | Level_12 / #LOC_GR12_LevelDisplayName |
| 187256249 | Level 11 - 1 Star | Act 1: Flavor — 1 Star | Normal/Hard/Expert/Perfection | Level_12 / #LOC_GR12_LevelDisplayName |
| 187256250 | Level 11 - 2 Stars | Act 1: Flavor — 2 Stars | Hard/Expert/Perfection | Level_12 / #LOC_GR12_LevelDisplayName |
| 187256251 | Level 11 - 3 Stars | Act 1: Flavor — 3 Stars | Expert/Perfection | Level_12 / #LOC_GR12_LevelDisplayName |
| 187256252 | Level 12 - Completion | Act 2: Sauce and Spice — Completion | Normal/Hard/Expert/Perfection | Level_15 / #LOC_GR15_LevelDisplayName |
| 187256253 | Level 12 - 1 Star | Act 2: Sauce and Spice — 1 Star | Normal/Hard/Expert/Perfection | Level_15 / #LOC_GR15_LevelDisplayName |
| 187256254 | Level 12 - 2 Stars | Act 2: Sauce and Spice — 2 Stars | Hard/Expert/Perfection | Level_15 / #LOC_GR15_LevelDisplayName |
| 187256255 | Level 12 - 3 Stars | Act 2: Sauce and Spice — 3 Stars | Expert/Perfection | Level_15 / #LOC_GR15_LevelDisplayName |
| 187256256 | Level 13 - Completion | Act 3: Montage — Completion | Normal/Hard/Expert/Perfection | Level_22 / #LOC_GR22_LevelDisplayName |
| 187256257 | Level 13 - 1 Star | Act 3: Montage — 1 Star | Normal/Hard/Expert/Perfection | Level_22 / #LOC_GR22_LevelDisplayName |
| 187256258 | Level 13 - 2 Stars | Act 3: Montage — 2 Stars | Hard/Expert/Perfection | Level_22 / #LOC_GR22_LevelDisplayName |
| 187256259 | Level 13 - 3 Stars | Act 3: Montage — 3 Stars | Expert/Perfection | Level_22 / #LOC_GR22_LevelDisplayName |
| 187256260 | Level 14 - Completion | Act 4: Habanero — Completion | Normal/Hard/Expert/Perfection | Level_23 / #LOC_GR23_LevelDisplayName |
| 187256261 | Level 14 - 1 Star | Act 4: Habanero — 1 Star | Normal/Hard/Expert/Perfection | Level_23 / #LOC_GR23_LevelDisplayName |
| 187256262 | Level 14 - 2 Stars | Act 4: Habanero — 2 Stars | Hard/Expert/Perfection | Level_23 / #LOC_GR23_LevelDisplayName |
| 187256263 | Level 14 - 3 Stars | Act 4: Habanero — 3 Stars | Expert/Perfection | Level_23 / #LOC_GR23_LevelDisplayName |
| 187256264 | Level 15 - Completion | Central Mainframe — Completion | Normal/Hard/Expert/Perfection | Level_16 / #LOC_GR16_LevelDisplayName |
| 187256265 | Level 15 - 1 Star | Central Mainframe — 1 Star | Normal/Hard/Expert/Perfection | Level_16 / #LOC_GR16_LevelDisplayName |
| 187256266 | Level 15 - 2 Stars | Central Mainframe — 2 Stars | Hard/Expert/Perfection | Level_16 / #LOC_GR16_LevelDisplayName |
| 187256267 | Level 15 - 3 Stars | Central Mainframe — 3 Stars | Expert/Perfection | Level_16 / #LOC_GR16_LevelDisplayName |
| 187256268 | Level 16 - Completion | The Thief Prince — Completion | Normal/Hard/Expert/Perfection | Level_24 / #LOC_GR24_LevelDisplayName |
| 187256269 | Level 16 - 1 Star | The Thief Prince — 1 Star | Normal/Hard/Expert/Perfection | Level_24 / #LOC_GR24_LevelDisplayName |
| 187256270 | Level 16 - 2 Stars | The Thief Prince — 2 Stars | Hard/Expert/Perfection | Level_24 / #LOC_GR24_LevelDisplayName |
| 187256271 | Level 16 - 3 Stars | The Thief Prince — 3 Stars | Expert/Perfection | Level_24 / #LOC_GR24_LevelDisplayName |
| 187256272 | Level 17 - Completion | Cold Storage — Completion | Normal/Hard/Expert/Perfection | Level_21 / #LOC_GR21_LevelDisplayName |
| 187256273 | Level 17 - 1 Star | Cold Storage — 1 Star | Normal/Hard/Expert/Perfection | Level_21 / #LOC_GR21_LevelDisplayName |
| 187256274 | Level 17 - 2 Stars | Cold Storage — 2 Stars | Hard/Expert/Perfection | Level_21 / #LOC_GR21_LevelDisplayName |
| 187256275 | Level 17 - 3 Stars | Cold Storage — 3 Stars | Expert/Perfection | Level_21 / #LOC_GR21_LevelDisplayName |
| 187256276 | Level 18 - Completion | The Darkness — Completion | Normal/Hard/Expert/Perfection | Level_03 / #LOC_GR03_LevelDisplayName |
| 187256277 | Level 18 - 1 Star | The Darkness — 1 Star | Normal/Hard/Expert/Perfection | Level_03 / #LOC_GR03_LevelDisplayName |
| 187256278 | Level 18 - 2 Stars | The Darkness — 2 Stars | Hard/Expert/Perfection | Level_03 / #LOC_GR03_LevelDisplayName |
| 187256279 | Level 18 - 3 Stars | The Darkness — 3 Stars | Expert/Perfection | Level_03 / #LOC_GR03_LevelDisplayName |
| 187256280 | Level 19 - Completion | Escape — Completion | Normal/Hard/Expert/Perfection | Level_13 / #LOC_GR13_LevelDisplayName |
| 187256281 | Level 19 - 1 Star | Escape — 1 Star | Normal/Hard/Expert/Perfection | Level_13 / #LOC_GR13_LevelDisplayName |
| 187256282 | Level 19 - 2 Stars | Escape — 2 Stars | Hard/Expert/Perfection | Level_13 / #LOC_GR13_LevelDisplayName |
| 187256283 | Level 19 - 3 Stars | Escape — 3 Stars | Expert/Perfection | Level_13 / #LOC_GR13_LevelDisplayName |
| 187256284 | Level 20 - Completion | Loneliness — Completion | Normal/Hard/Expert/Perfection | Level_25 / #LOC_GR25_LevelDisplayName |
| 187256285 | Level 20 - 1 Star | Loneliness — 1 Star | Normal/Hard/Expert/Perfection | Level_25 / #LOC_GR25_LevelDisplayName |
| 187256286 | Level 20 - 2 Stars | Loneliness — 2 Stars | Hard/Expert/Perfection | Level_25 / #LOC_GR25_LevelDisplayName |
| 187256287 | Level 20 - 3 Stars | Loneliness — 3 Stars | Expert/Perfection | Level_25 / #LOC_GR25_LevelDisplayName |
| 187256288 | Level 21 - Completion | Locker Room — Completion | Normal/Hard/Expert/Perfection | Level_14 / #LOC_GR14_LevelDisplayName |
| 187256289 | Level 21 - 1 Star | Locker Room — 1 Star | Normal/Hard/Expert/Perfection | Level_14 / #LOC_GR14_LevelDisplayName |
| 187256290 | Level 21 - 2 Stars | Locker Room — 2 Stars | Hard/Expert/Perfection | Level_14 / #LOC_GR14_LevelDisplayName |
| 187256291 | Level 21 - 3 Stars | Locker Room — 3 Stars | Expert/Perfection | Level_14 / #LOC_GR14_LevelDisplayName |
| 187256292 | Lobby - Important Letters Pickup | Important Letters pickup | Inactive/reserved | Inactive staging reservation; not an active gameplay check |
| 187256293 | Lobby - Bean Trumpet Award | Bean Trumpet award | Inactive/reserved | Inactive staging reservation; not an active gameplay check |

## Addressless solver events (not additional network checks)

| Event | Region | Meaning |
| --- | --- | --- |
| Bucket Minion Trade Complete | Roots | Solver event for the Hip Glasses trade |
| Combo Bucket Event | Roots | Solver event requiring the trade event and Chicken Bucket |
| Victory | Phone Hub | Current development goal: own all six Area Access items. Final boss gameplay Victory remains inactive. |

## Evidence and validation

Native level/song source: installed bundle `14e6a99bc73f210341b431badb80a894.bundle`, SHA-256 `8e16b913246653abec2be9b4ab33428b2994cf02ae391030a974f0c3c588bdea`. Names were resolved from LevelData/SongInformationProvider localization keys through GameText_en and GameText_untranslated. Music Lab chests were read from bundle `11e1657dcb37907aef75e48af324519f.bundle`. No native game/save changes were made.

Client/APWorld parity audit found no duplicate registered IDs and exact agreement across all registered names and generated difficulty sets. Some local medal logs can say SENT for a tier absent from the seed; MultiClient.Net 6.7.1 filters those IDs before transmission. This is a logging issue, not an additional location.

Naming regression tests use a compact independent native-data evidence fixture. Existing release binaries and current seed remain unchanged.

Validation: all 115 APWorld tests passed; client LevelCompletion tests passed; repository validation and diff whitespace checks passed. Release build succeeded with zero errors, four existing nullable warnings, and unavailable NuGet vulnerability-audit warnings. No deployment was performed.

User-reviewed naming update: `Roots - Level 3 - Plant Pipes Pickup` replaces `Roots - Level 3 - Frog and Hippo` at the same permanent ID `187256179`. The pickup is still Frog/Hippo's Plant Pipes reward in The Megafying Ritual. Existing generated seeds retain their original datapackage name; use the matching updated client/APWorld with a newly generated seed when deploying this rename. No deployment or seed replacement was performed.
