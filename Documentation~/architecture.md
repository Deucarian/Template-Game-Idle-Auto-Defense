# Architecture

The package is a template, not a reusable gameplay framework. It composes the Auto Defense Suite through starter application glue:

- `BasicIdleAutoDefenseGame` creates deterministic transient fallback definitions only for explicit unbound tests and debug hosts.
- `IdleAutoDefenseTemplateController` wires spawning, navigation, combat, weapons, projectiles, upgrades, idle rewards, progression, and simple scene visuals.
- `IdleAutoDefensePlayerExperienceController` wraps that runtime with menu-first state, HUD, module actions, Overdrive, reward cards, pause/build/settings, tutorial, offline claim, threat presentation, and run summary.
- `IdleAutoDefensePlayerExperienceAsset` references the generated UI settings, tutorial, audio palette, and two theme assets.
- `IdleAutoDefensePlayerProfileStore` uses `Deucarian.Persistence` envelopes, backups, validation, and recovery for settings, tutorial/theme state, offline timestamps, totals, and a template-owned progression snapshot DTO.
- `BasicIdleAutoDefenseGameBootstrap` is private template-source code copied and renamed into generated product folders.
- `IdleAutoDefenseTemplateSetupService` creates a product-owned game root from `TemplateSource~/BasicIdleAutoDefenseGame` and a discoverable authored content root under `Assets/GameContent`.

The generated scene requires its assigned `GameContentPackAsset`, `GameContentSetAsset`, and `IdleAutoDefensePlayerExperienceAsset`. Invalid strict gameplay blocks Start Run. Invalid or missing theme selection falls back visibly to the authored default without changing gameplay content.

Unbound package smoke fixtures may use deterministic transient content. This intentional path reports `FallbackModeActive == true` and is not an editable source of truth. See [template-contract.md](template-contract.md).

The legacy reward draft members on `IdleAutoDefenseContentSetRuntimeSettings` are non-serialized compatibility drafts used only to seed transient editor/test content. Persisted gameplay always reads the first-class `GameContentSetAsset.RewardCatalog` asset.

Template source assets store root and section data as sibling `.asset` files. During setup, copied `.meta` files receive fresh GUIDs and copied YAML references are rewritten across the generated game root and `Assets/GameContent` so generated scenes and assets point at product-owned authored content.

Reusable systems belong in lower Deucarian packages. Keep product-specific scene composition, UI composition, prefabs, content IDs, authored data, and save DTOs in the product project. The current generic Progression API exposes snapshots but no restore constructor; this template replays its small authored research graph through public reward/research APIs instead of modifying the dependency or using reflection.
