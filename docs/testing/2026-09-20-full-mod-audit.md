# Client v0.75.6 audit candidate — September 20, 2026

Status: installed with approval; corrected native method lookup verified at startup. Published release remains v0.28.0 / Client v0.75.4. In-game acceptance is required before calling native repairs verified.

## Repairs

- Music Lab cassette chest grants bypassed the managed request hook through an inlined native dispatch. Intercept the five audited reward reactors (Quicksand, Flamenco, Ten-Four Good Buddy, Zen and Wiggle), with compatible-slot and stable-save admission. Preserve chest collection flags, randomized checks, persistence and native insertion. Suppress the matching native cassette popup; the instant-step base still completes the sequence. Popup interception installs only after reward interception succeeds. Existing leaked ownership is not automatically erased. See [native trace and regression evidence](2026-09-20-cassette-source-audit.md).
- Successfully clearing a cassette source now sends its location even when AP already supplied that cassette. The former ownership check could strand its source location. Failed and unreadable results retain conservative handling.
- Invalid or discontinuous AP Star histories suspend entry/HUD/qualification authority until a complete replay. An older connection cannot suspend a newer generation; ordinary offline handling is retained.
- First quest-save binding requires both freshness flags explicitly false. An unreadable flag defers binding and item application; established bindings remain usable.
- Sent-item deduplication now lasts for the seed/slot identity instead of evicting after 256 sends. A replay spanning all 316 Perfection locations no longer duplicates early sends.
- F6 history measures wrapped rows and scrolls. An oversized popup gets a scrollable body and cannot indefinitely block the remaining queue. Existing visible-time timers and retained pending rewards are preserved.
- Removed the obsolete recursive post-result SaveDataProbe (including its helper methods), its diagnostic-only result postfix, and the unused selected-save event stub. The production result prefix still installs independently.
- Detailed Music Lab member dumps and the read-only bottom-HUD diagnostic are opt-in through Developer.EnableVerboseDiscovery, default false. Production reconciliation remains enabled independently.
- Character-quest event handling rejects unrelated flags and rooms before reflection or ownership snapshots. Regression measurement confirms zero allocations and zero reflected reads for 100 unrelated request/event pairs. Native values are not cached.
- Guarded nullable native request construction and cleaned nullable diagnostics. Corrected stale APWorld slot/example descriptions that still called active Star gates and Victory previews.

## Review coverage

Reviewed APWorld modules, option/ID/cassette contracts and packaging; connection generations, replay/retry and notification lifecycle; AP Star state, result/journal boundaries; quest and character state/binding; expanded checks and native readers; garage initialization, holder/door/consumption/medal and persistence paths; cassette grant/source/insertion boundaries; recurring client keepers, reflection caching and automatic discovery work. Existing native crash repairs and mutable-save safeguards are preserved.

This is a subsystem/code-path audit supported by tests, not proof that every native combination is defect-free. Large mixed-purpose Plugin.cs and historical rule construction still warrant staged simplification. No broad rewrite was made without an observed correctness or runtime benefit.

## Validation

- All 35 client test projects passed across the full run and the post-fix QuestRuntime rerun (55 assertions). The first run encountered the new failing freshness regression while that repair was in progress.
- Full client Release build: zero errors and zero C# warnings. NuGet vulnerability lookup could not reach its service; dependency vulnerability status is not established by this build.
- Twelve actual packaged seed-generation/playthrough cases passed: Normal, Hard, Expert and Perfection at goals 1, 40 and 66.
- Retained optional-level seed audit: its 24 exclusions cover all 23 modeled checks dependent on skipped Levels 7/9 plus Demolition Certificate; 186 safe slots remain for 137 non-filler items. No seed/save mutation.
- APWorld tests and repository contract validation are recorded with the final verification results in TESTING.md.

## Remaining acceptance and issues

Test an unopened cassette chest: randomized check once, no native cassette grant/popup, chest animation finishes. Then test legitimate AP cassette receipt, native insertion and persistence after restart. Test a level cassette source after receiving its AP item early. Confirm long notifications/history and blocked-entry messaging in the real game.

Profile representative full songs and Combo Bucket conversion again. This audit removes demonstrated unnecessary work but does not establish that the remaining timing hitches are gone. The Hypno Pan already-owned crafting interaction, unverified special levels, multiplayer/co-op, and additional independent source investigations remain as documented in KNOWN_ISSUES.md. Do not repair existing leaked Quicksand ownership by guessing at save fields.

## Approved installation and follow-up

Both cassette native hooks installed successfully after correcting the generated-wrapper underscore name to the native explicit-interface name `TriggerReactor.OnTrigger`. Connected to the retained Jack seed; installed DLL SHA256 `eee95891513f14d185e030b31d46e4affcd05ad27ada72969d734e60cd8fc8eb`. Previous plugin/configuration/log/native saves were backed up before replacement.

The player reported severe Fumblin Around lag and perceived a crash while the maintainer was closing the game normally for the hook correction. Latest log contains shutdown OnDisable exceptions; no new session crash dump was found. The normal close likely explains disappearance, but the lag is unresolved. Existing aggregate performance diagnostics enabled for the next short song test; verbose discovery remains off. Do not claim lag or chest gameplay acceptance from successful startup.

## Fullscreen comparison — September 20, Client v0.75.6

The player repeated the affected Fumblin Around section in fullscreen and reported no issues. Fullscreen is a confirmed workaround for this reproduction, not proof that all performance issues are resolved. The preceding windowed trace showed continued game frame submission during a roughly4.3-second pause in displayed-frame statistics, without a corresponding long CPU scheduling pause. The comparison supports investigating the windowed presentation/compositor path; the exact driver, overlay or compositor cause remains unproven.

Keep fullscreen for the current playthrough. Windowed-mode stutter remains open. Combo Bucket conversion and other songs still require separate acceptance. Temporary performance diagnostics are disabled in configuration for the next launch; the running game is unchanged. No driver/registry changes, save repair, restart or release publication was performed.
