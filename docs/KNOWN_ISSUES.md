# Known issues — v0.28.0 / Client v0.75.4

Current release status; older testing checklists are historical evidence.

## Known bugs

- **Temporary severe lag around Combo Bucket conversion / Lift Quest:** reported during the broad playtest, recovering partway through the level. The cause and a complete fix are not established.
- **Hypno Pan crafting when already owned:** pre-granting Hypno Pan can make Frog and Hippo's crafting interaction unavailable. Ability use was confirmed working. A separate crafting check has not been added because its independent completion marker and safe reward replacement remain unresolved.

## Follow-ups deferred until after the full playthrough

Recorded 2026-09-18 during the Hard / 40 AP Star run on Client v0.75.2. Quicksand remains deferred. The player subsequently paused the playthrough to prioritize lag throughout the game; v0.75.4 performance changes are installed and substantially improved live play; residual hitches remain under investigation.

- **Quicksand cassette appears to leak its vanilla reward:** the 32-point chest correctly sent its randomized Gradius Remix Cartridge, but Quicksand was already marked `HAVE_DEPOSITED` when its actual AP item arrived later from Fumblin Around - Bronze. Logs support the reported early native grant; the precise bypass still needs tracing. Review the shared reward path for all five Music Lab cassette chests, preserving legitimate AP receipt and insertion behavior. Keep the current seed/save.
- **General gameplay lag, including Music Lab songs (active high-priority scoring issue):** the player reports lag everywhere, with Music Lab and 10-4 Good Buddy as specific examples, and warns that it makes good scores impractical at higher difficulties. Client v0.75.4 removes the measured repeated cassette scan cost (roughly 9.7 ms to 0.007 ms per frame), and live testing is substantially smoother. Occasional hitches still cost notes; their remaining cause is not established. Profile multiple Music Lab songs, investigate shared gameplay overhead, and verify that timing is stable enough for higher-difficulty medal requirements. Do not assume it shares the earlier Combo Bucket lag cause.

## Testing limits and remaining work

- The new AP Star HUD, native entry gates, new Star Eater thresholds, Cell Tower feed check, King unlock check and post-threshold victory still need final in-game acceptance together on a fresh seed/save. Generation success does not prove every native prerequisite or UI boundary.
- 33 of the previous 37 supplemental checks were confirmed in the broad gameplay pass. Demonic Tower, Demonic Escape, Demonic Lockers and Demolition Certificate are implemented but were not manually played in that pass. Royal/Bunker feeding was tested with temporary 25-star thresholds, not the original 40/66 thresholds.
- Failed special-level attempts, full save/seed-isolation gameplay, true cross-player notification coverage and the complete native campaign/cassette route matrix remain incompletely verified. Automated tests cover relevant policy and isolation cases.
- Local co-op and online co-op are not verified release features.
- Remaining source investigations: separate Hypno Pan crafting; Bizzle/Clive switches; independent Super Nectar component awards; Demon Key acquisition/use; final demon interaction versus its existing cartridge source. Bean Trumpet is still native and shares the Letters pickup; Notepad/Data Stick share the Gecko interaction; Bizzle/Clive acquisition shares Meet the Bees. These are not additional independent checks in this release.

## Fixed in the September 18 hotfix

The Roots-entry/Star Eater interaction crash was traced to unsafe native reference assignment. Client v0.75.4 includes the direct object-reference setter and verified rollback repair, along with the tested metadata-cache performance fixes. Continue reporting any new crash with the client version and log.
