# Architecture

The package is a template, not a reusable gameplay framework. It composes the Auto Defense Suite through starter application glue:

- `BasicIdleAutoDefenseGame` creates deterministic fallback definitions for tests and smoke runs.
- `IdleAutoDefenseTemplateController` wires spawning, navigation, combat, weapons, projectiles, upgrades, idle rewards, progression, and simple scene visuals.
- `BasicIdleAutoDefenseGameBootstrap` is private template-source code copied and renamed into generated product folders.
- `IdleAutoDefenseTemplateSetupService` creates a product-owned game root from `TemplateSource~/BasicIdleAutoDefenseGame` and a discoverable authored content root under `Assets/GameContent`.

The generated scene prefers its assigned `GameContentPackAsset` and `GameContentSetAsset`. If generated content is invalid, the controller logs a warning and falls back to direct assigned arrays or built-in transient content so failures remain visible, but the normal setup path should validate with zero pack/set errors.

Template source assets store root and section data as sibling `.asset` files. During setup, copied `.meta` files receive fresh GUIDs and copied YAML references are rewritten across the generated game root and `Assets/GameContent` so generated scenes and assets point at product-owned authored content.

Reusable systems belong in lower Deucarian packages. Keep product-specific scene composition, prefabs, content IDs, authored data, and save DTOs in the product project.
