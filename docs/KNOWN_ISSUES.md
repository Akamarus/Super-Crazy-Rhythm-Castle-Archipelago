# Known issues — v0.28.0 / Client v0.75.0

Current release status; older testing checklists are historical evidence.

## Known bugs

- **Temporary severe lag around Combo Bucket conversion / Lift Quest:** reported during the broad playtest, recovering partway through the level. The cause and a complete fix are not established.
- **Hypno Pan crafting when already owned:** pre-granting Hypno Pan can make Frog and Hippo's crafting interaction unavailable. Ability use was confirmed working. A separate crafting check has not been added because its independent completion marker and safe reward replacement remain unresolved.

## Testing limits and remaining work

- The new AP Star HUD, native entry gates, new Star Eater thresholds, Cell Tower feed check, King unlock check and post-threshold victory still need final in-game acceptance together on a fresh seed/save. Generation success does not prove every native prerequisite or UI boundary.
- 33 of the previous 37 supplemental checks were confirmed in the broad gameplay pass. Demonic Tower, Demonic Escape, Demonic Lockers and Demolition Certificate are implemented but were not manually played in that pass. Royal/Bunker feeding was tested with temporary 25-star thresholds, not the original 40/66 thresholds.
- Failed special-level attempts, full save/seed-isolation gameplay, true cross-player notification coverage and the complete native campaign/cassette route matrix remain incompletely verified. Automated tests cover relevant policy and isolation cases.
- Local co-op and online co-op are not verified release features.
- Remaining source investigations: separate Hypno Pan crafting; Bizzle/Clive switches; independent Super Nectar component awards; Demon Key acquisition/use; final demon interaction versus its existing cartridge source. Bean Trumpet is still native and shares the Letters pickup; Notepad/Data Stick share the Gecko interaction; Bizzle/Clive acquisition shares Meet the Bees. These are not additional independent checks in this release.
