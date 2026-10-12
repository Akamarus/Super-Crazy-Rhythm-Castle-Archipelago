# October 6 async-safe repairs: combined verification

Development source only. Installed plugin and APWorld SHA-256 hashes exactly
match the pre-work baseline. No native save/config changes, installation, game or
server restart, publication or commit occurred.

- All 39 client test projects passed. Source-reading projects initially ran from
  the wrong directory; rerunning from the repository root passed. The LevelCompletion
  fixture was updated to compile the real new receipt adapter, then passed.
- All 158 APWorld tests passed, including Cell Star Eater Violance dependency and
  absence of a forced vanilla Lobby source prerequisite.
- Repository validator passed; git diff --check passed.
- Combined Release client build passed with -SkipInstall. NU1900 indicates the
  vulnerability metadata feed was unreachable; compilation had zero errors.
- Independent review caught the native Weed Killer consumption/consequence gap.
  Exact-save consumption-event guards and failing-then-passing tests now cover it.
  Final re-review found no outstanding blocking finding in the scoped changes.

Ready for separate native acceptance: Weed Killer/Hip Glasses/Chicken Bucket live
receipt without restart and consumption without regrant; early Regular/Pro selector;
Roots booth visibility before/after Gecko pickup; already-owned Bloody Tears and
Gradius source chests. Do not install into the ongoing async without user approval.

Still open: actual movement defect (diagnostics improved only), the separate
Zen/cassette inventory report, AP 0.6.8 compatibility, and generation-scale testing
of the combined unreleased logic. Historical cases where flag205 was already true
but selector unavailable also require native acceptance; do not claim all selector
problems resolved solely from the new direct-start bookkeeping fix.
