# Validation

Expected validation coverage:

- Package import through the shared validation project.
- No public Unity Package Manager sample entry.
- Setup service creates product-owned `Scripts`, `Prefabs`, `Visuals`, `Audio`, `Resources`, and `Docs` in the generated game root, creates the visible playable scene under `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame`, and creates authored gameplay data under `Assets/GameContent`.
- Generated scene references generated product-owned content assets, not template-source GUIDs.
- Generated content pack, content set, reward catalog, economy, run profile, progression, offline progression, game rules, player experience, UI settings, tutorial, themes, and audio palette validate with zero errors.
- Generated controller uses all six authored-core owners in Play Mode with strict startup enabled and fallback false.
- Menu checks expose only `Create Playable Game` and `Open Template Docs` under `Tools > Deucarian > Templates > Idle Auto Defense`.
- EditMode tests cover definitions, parity values, strict startup blockers, authored presentation validity, safe-area math at five landscape targets, settings/progression persistence, corrupted save recovery, setup copying, GUID remapping, GCA counts, claims, and canonical references.
- PlayMode smoke covers menu-first boot, tutorial persistence, HUD, module purchase, Overdrive, rewards, pause/build, theme persistence, offline claim idempotence, major-threat bar data, terminal summary/restart, strict authored binding, spawning, combat, failure, and encounter rewards.

Use Deucarian Test Automation when available:

```text
Deucarian.TestAutomation.BatchTestRunner.RunEditMode
Deucarian.TestAutomation.BatchTestRunner.RunPlayMode
```

Also run `git diff --check`, the shared package validator, authored content validation, a fresh setup smoke, generated-scene strict binding smoke, and the manual checks in [playtesting.md](playtesting.md) and [mobile-testing.md](mobile-testing.md).
