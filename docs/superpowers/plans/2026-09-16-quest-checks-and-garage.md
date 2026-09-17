# Quest checks and Garage persistence repair

Date: 2026-09-16. Continue in existing isolated worktree; preserve all prior changes.

User scope: randomize Plunger pickup, make Meoo and Maniac AP unlock items with
hand-in checks, add Roots Star Eater fed check, repair Bloody Tears consumption
and native score persistence. Normal AP seed intentionally exposes Bronze only.

1. Preserve live logs and trace Garage native cartridge consumption, insertion,
   selected song and result persistence. Add reproducing tests before repairs.
   Keep active-check filtering distinct from native medal/score persistence.
2. Verify native Plunger source and consumption markers; both character unlock
   requests and hand-in availability when the character arrives early; Star Eater
   fed flag. Do not invent IDs or rely on character ownership as proof of hand-in.
3. Add schema-17/world-0.26 quest contract, permanent IDs and graph tests. Normal
   native Star Eater threshold stays three stars; until stars are modeled, prevent
   required progression placement on that check. Preserve schema-16 compatibility.
4. Implement managed quest ownership/check state with authenticated generation,
   selected-save stability, replay-safe grant and source suppression. Hand-in
   consumption must persist independently of character unlock ownership; receipt
   of a character early must not remove the hand-in check. Cover both event orders.
5. Run focused regressions then full client/APWorld suites and repository validation.
   Build without installing. Review exact candidate, document limitations, package.
6. Request explicit deployment approval for the concrete client candidate per AGENTS.
   Current seed can exercise Garage repair; new checks require a new generated seed.
   Never erase/reseed the user's current save as part of this work.
