# AP Stars combined candidate

Client v0.75.0 / APWorld v0.28.0, schema 19. Prepared September 17, 2026; not installed or published.

## Included

- Exactly 66 individual AP Stars, with goal 1–66 (default 50).
- Generated normal campaign entry requirements; the first two levels open at zero. Gates cap at half the goal for reliable progression; victory still requires the full goal.
- Authoritative received-item history, reconnect isolation and duplicate protection.
- AP Star total and level-preview requirements; five Star Eaters use the transmitted AP thresholds. Legacy test overrides do not apply to this new contract.
- A successful normal Level 22 clear after reaching the goal sends victory. An earlier clear followed by another Star does not win; replay is required. Native earned ratings remain unchanged.
- Cell Tower Star Eater and the actual King Ferdinand unlock event become checks. King ownership alone cannot trigger the source.
- Corrected Tower statue branch/ability dependencies. Existing item quantities and Music Lab's 180-point economy retained.
- Removed the once-per-second consumed Roots-item diagnostic. This is log cleanup, not a proven fix for the reported temporary level lag.

## Verification

- All 32 client regression projects passed, including 30 production-connection Star integration assertions.
- All 138 APWorld/repository tests passed; repository validator passed.
- Final Python world code matches the package that passed 48 actual generation/playthrough calculations: four difficulties, goals 1/25/50/66, three seeds each.
- Release build passed with zero errors, four existing nullable warnings and unavailable package-vulnerability metadata warning.
- Native gameplay acceptance is pending. Simulated boundaries and successful seed filling do not prove every native route or UI interaction.

## Next gameplay pass

After installation approval, back up the current client, configuration and slot 4; use a fresh v0.28 seed and fresh save. Connect the seed before loading the new save.

1. Confirm initial AP Stars and requirements display. A native earned performance star must send its check without directly increasing AP Stars, unless its randomized reward is a Star.
2. Grant test Stars through the test server in controlled batches: verify below/at-threshold level entry and Star Eater interaction, with no consumption and no duplicate count after reconnect.
3. Confirm Music Lab still displays its separate point total and Garage cartridge insertion remains native.
4. Confirm Cell Tower feed and the final King unlock each send one source check.
5. Test an early Level 22 clear below goal; grant the missing Star afterward and confirm no victory. Replay the level and confirm victory after successful persistence. Repeat/reconnect must remain idempotent.
6. Check the Tower statue route prerequisites on a clean progression pass; report any unavailable source despite modeled access.

The previously skipped Demonic Tower/Escape/Lockers and Demolition Certificate tests remain unverified and do not need repeating for this pass. Current installed client v0.74.2, running server and save have not been changed.
