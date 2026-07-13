# Validation

Expected validation coverage:

- Package import through the shared validation project.
- No public Unity Package Manager sample entry.
- Setup service creates Basic only, Scrap Frontier only, or both; Both produces sibling content/presentation roots, two visible scenes, and one shared generated bootstrap.
- Repair mode reuses destination GUIDs, restores missing content, skips byte-identical files, and blocks conflicting files.
- Generated scene references generated product-owned content assets, not template-source GUIDs.
- Generated content pack, content set, reward catalog, economy, run profile, progression, offline progression, game rules, player experience, UI settings, tutorial, themes, and audio palette validate with zero errors.
- Generated controller uses all six authored-core owners in Play Mode with strict startup enabled and fallback false.
- Menu checks expose only `Create Playable Game` and `Open Template Docs` under `Tools > Deucarian > Templates > Idle Auto Defense`.
- EditMode tests cover definitions, Basic source hashes, duplicate/cross-pack GUID scans, two-pack generation, numeric parity, mutation isolation, strict startup blockers, authored presentation validity, safe-area math at five landscape targets, pack-scoped settings/progression/offline persistence, repair, GCA counts/claims/canonical references, and scene ownership.
- PlayMode smoke covers menu-first boot, tutorial persistence, HUD, module purchase, Overdrive/Redline, rewards, pause/build, theme persistence, offline claim idempotence, major-threat bar data, terminal summary/restart, strict authored binding, spawning, combat, failure, encounter rewards, and Scrap pack identity on the shared controller with fallback false.

Use Deucarian Test Automation when available:

```text
Deucarian.TestAutomation.BatchTestRunner.RunEditMode
Deucarian.TestAutomation.BatchTestRunner.RunPlayMode
```

Also run `git diff --check`, the shared package validator, authored content validation, a fresh setup smoke, generated-scene strict binding smoke, and the manual checks in [playtesting.md](playtesting.md) and [mobile-testing.md](mobile-testing.md).
