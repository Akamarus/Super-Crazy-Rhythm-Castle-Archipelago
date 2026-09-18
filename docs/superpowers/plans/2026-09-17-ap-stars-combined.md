# Combined missing checks and AP Stars implementation plan

> For agentic workers: use superpowers:subagent-driven-development for independent implementation and scoped review.

**Goal:** Implement Cell Tower Star Eater and King Ferdinand unlock source checks together with solver-valid AP Stars, campaign gates and post-threshold Level22 Victory.

**Spec:** docs/superpowers/specs/2026-09-11-star-victory-and-quest-capacity-design.md plus current user authorization to implement together and 33/37-check acceptance. Four user-skipped tests remain unverified; do not require replay now.

**Architecture:** New exact schema19 / APWorld0.28.0 / client0.75.0 contract retains legacy18 behavior. Exactly66 individual Star items, unchanged existing item counts; use real native-source checks and proven dependencies to establish progression-safe capacity. Receipt history supplies AP Star total; earned native result stars still send performance checks. Generated requirements govern normal campaign entry. Level22 is enterable below goal and only a persisted successful completion captured with sufficient AP Stars can send Victory. No native rating writes. Star Eater requirements must be consistent with the same AP Star currency and source rules in the new contract; legacy seeds retain their old behavior.

**Global constraints:** Work only in retained isolated worktree. No deployment, game/server/save mutation, commit, publish or merge in this implementation pass. Do not infer enough progression-safe capacity from raw totals. Do not promote checks based solely on area name; model native prerequisites conservatively. No cache/milestone locations or reduced Music Lab Point economy. Do not allocate native mappings speculatively. Cell334 and King335 are planned IDs only after source proof. King source is a native unlock check; no new randomized King ownership item is assumed.

- [x] Audit new APWorld capacity and meaningful source dependencies; validate actual placement across all difficulties and goals1/25/50/66.
- [x] Audit native gate/HUD/Star Eater/Level22 completion boundaries without writes.
- [x] Implement two missing check sources with exact schema/version maps and native-proof tests.
- [x] Implement AP Stars authoritative history, identity isolation, display, gates and nonretroactive Victory using native audit evidence.
- [x] Reduce repeated consumed-item diagnostics and investigate bounded cassette work around reported lag; do not claim causation without evidence.
- [x] Run all client tests, all world tests, broad actual generation/fill matrix, validator and Release build. Review contracts and cross-language parity.
- [x] Package candidate plus accurate acceptance checklist and report concrete install approval as final deployment step.

## Decisions

- User approved combined implementation; existing approved66-Star design governs. Do not ask for design approval again.
- Prior raw-count claim is insufficient: schema18 Normal has89 eligible slots against134 progression instances after adding Stars. Resolve with verified rules or report actual remaining blocker; never silently weaken capacity validation.
- Live native acceptance is distinct from automated generation. Keep installed0.74.2 and running test seed intact.

## Final verification

Client suites32/32; world/repository tests138/138; validator passed; final actual generation matrix48/48; Release build zero errors. Candidate archives and test plan are in workspace outputs/ap-stars-v0.75.0. Installation and native gameplay acceptance remain pending explicit deployment approval. Temporary level lag remains unproven; repeated consumed-item logging was removed, with no claim that this fixes lag.
