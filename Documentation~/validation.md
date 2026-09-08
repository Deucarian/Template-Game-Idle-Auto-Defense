# Validation

Expected validation coverage:

- Package import through the shared validation project.
- No public Unity Package Manager sample entry.
- Setup service creates Basic only, Scrap Frontier only, or both; Both produces sibling content/presentation roots, two visible scenes, and one shared generated bootstrap.
- Repair mode reuses destination GUIDs, restores missing content, skips byte-identical files, and blocks conflicting files.
- Generated scene references generated product-owned content assets, not template-source GUIDs.
- Generated content pack, content set, reward catalog, economy, run profile, progression, offline progression, game rules, player experience, UI settings, tutorial, themes, and audio palette validate with zero errors.
- Generated controller uses all six authored-core owners in Play Mode with strict startup enabled and fallback false.
- Deucarian Control Center exposes setup and documentation under Authoring, with validation, audits, and inspection split between Authoring and Developer cards.
- EditMode tests cover definitions, Basic source hashes, duplicate/cross-pack GUID scans, two-pack generation, numeric parity, mutation isolation, strict startup blockers, authored presentation validity, safe-area math at five landscape targets, pack-scoped settings/progression/offline persistence, repair, GCA counts/claims/canonical references, and scene ownership.
- PlayMode smoke covers menu-first boot, tutorial persistence, HUD, module purchase, Overdrive/Redline, rewards, pause/build, theme persistence, offline claim idempotence, major-threat bar data, terminal summary/restart, strict authored binding, spawning, combat, failure, encounter rewards, and Scrap pack identity on the shared controller with fallback false.
- Safe-editing EditMode coverage generates disposable Basic and Scrap packs, verifies explicit field maps and closed path policy, exact-root persisted Attack and Wave discovery independent of active references, capability gating, source claims, Project Content exclusion, canonical same-pack Attack/Wave selection, projectile/direct compatibility, transient/scene/foreign target rejection, byte-neutral staging/cancel, collection Add/Remove/Move/Replace/restore, item identity, minimum/no-duplicate rules, in-session Undo/Redo, cloned-pack validation, Preview/Commit target revalidation, disappeared-target and stale Commit blocking, one-source locking, same-object GUID preservation, Unity Undo/Redo, exact Rollback, rollback refusal after a later edit, strict runtime conversion, and cross-pack hash isolation.
- PlayMode runtime proof binds authored editable values through the strict controller and confirms authored core remains active with fallback disabled. EditMode reference coverage additionally boots the strict controller after committed weapon-to-Attack and Run Profile Waves changes, verifies the selected authored targets/order are consumed with fallback disabled, and restores exact source bytes.

Use Deucarian Test Automation when available:

```text
Deucarian.TestAutomation.BatchTestRunner.RunEditMode
Deucarian.TestAutomation.BatchTestRunner.RunPlayMode
```

Also run `git diff --check`, the shared package validator, authored content validation, a fresh setup smoke, generated-scene strict binding smoke, and the manual checks in [playtesting.md](playtesting.md) and [mobile-testing.md](mobile-testing.md).

Mutation tests operate only on disposable generated packs and restore or delete every source. Final validation must confirm no fixture remains under `Assets`, no template-source asset changed, both named packs still validate, and package/dependency versions are unchanged.

Player composition coverage additionally checks simulation eligibility without menus or scenes, one terminal summary per run, menu/tutorial transitions, profile load/restore/save failure and idempotent teardown, failed initialization cleanup, authored audio throttling with live volume changes, and same-frame legacy gameplay commands followed by player actions. Keep the original generated bootstrap and component GUID compatibility while extending these tests.

Run composition coverage checks generated-versus-borrowed asset lifetime, construction failure/retry, paused-draft timing, restart state, wallet residual/rejections, purchase and health-effect order, module unlock idempotence, Overdrive cooldown, persistent restore/claim behavior, deterministic reward selection and queued draft progression. Combat cases use explicit fake backends to verify targeting ties/ranges, cadence, live build math, damage/death ordering, launch deferral, reverse-order due impacts, misses, rejection, expiry, retargeting and cleanup. Material lifetime cases include repeated Configure/Bind and inactive generated prefabs, with borrowed source material preservation.

When moving implementation, source-location assertions must inspect the owning source file while keeping their behavioral payloads. Compare the complete public component signatures, serialized declarations, original `.meta` GUIDs and authored asset bytes against the package baseline. A compile-only pass is not evidence that EditMode or PlayMode tests executed.
