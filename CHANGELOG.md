# Changelog

## Unreleased

- Removed unused Kenney gallery preview, sample, and generated overview files from both the runtime resources and generated-game template source so the package contents match the third-party notice.
- Added safe ordered Run Profile Waves editing for generated Basic Idle Auto Defense and Scrap Frontier named packs.
- Added exact-root persistent Wave discovery, canonical same-pack target checks, minimum/no-duplicate rules, staged collection operations, cloned-profile validation, and run restart/rebind guidance.
- Proved collection Commit, strict runtime order consumption, Unity Undo/Redo, stale-aware exact Rollback, and Basic/Scrap isolation without changing shipped Wave assets or sequences.
- Added canonical `Weapon / Tower -> Attack` reference editing for generated Basic Idle Auto Defense and Scrap Frontier named packs.
- Added exact-root persisted Attack discovery, canonical same-pack selection, source-claim and delivery compatibility checks, target revalidation, and stale/disappeared-target coverage.
- Proved reference Commit, Unity Undo/Redo, explicit Rollback, strict authored runtime consumption, and Basic/Scrap isolation without changing shipped authored content.
- Added provider-owned staged scalar editing for claimed Basic Idle Auto Defense and Scrap Frontier attack, enemy, mounted-weapon, and run-upgrade ScriptableObjects.
- Added cloned-pack validation, source revision checks, one-group Unity Undo/Redo, stale-aware exact rollback, pack isolation, and runtime-consumption coverage without changing shipped gameplay values.
- Documented the narrow writable field set and kept IDs, all references except the mounted-weapon Attack link, collections, nested catalogs, presentation structures, and JSON editing deferred.

## 0.1.1 - 2026-06-23

- Removed the public UPM sample path; generated product-owned games are now the single onboarding path.
- Renamed the template menu to `Create Playable Game` and removed starter-scene and sample-save menu commands.
- Added GUID remapping for copied template-source assets so generated games own their content references.
- Improved generated starter usability with visible spawn lanes, weapon mounts, clearer runtime object names, simple status UI, and save/reset coverage.
- Expanded package documentation for generated setup, content replacement, wave tuning, reset flow, and troubleshooting.

## 0.1.0 - 2026-06-23

- Added the Idle Auto Defense template package.
- Added a Basic Idle Auto Defense Game sample with scene, bootstrap script, content notes, placeholder prefab notes, and sample tests.
- Added package EditMode and PlayMode smoke tests for definition setup, upgrade drafts, offline rewards, save/load, corrupted save recovery, progression rewards, and deterministic runtime behavior.
