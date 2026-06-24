# Content

`starter-content.json` mirrors the constants used by the deterministic starter code. Replace these IDs and values when converting the template into a project-specific data pipeline.

This folder contains authored sample assets for attacks, enemies, and waves. Each root asset has focused section assets beside it so the sample can be copied out of `Samples~` by Unity. The authoring wizard creates the same structure as sub-assets when writing under normal project `Assets/GameContent/...` folders.

The attack IDs are:

- `attack.template.hitscan-beam`
- `attack.template.fire-orb`
- `attack.template.homing-pulse`

The enemy IDs are:

- `enemy.template.basic`
- `enemy.template.fast`
- `enemy.template.tank`

The wave IDs are:

- `wave.template.early`
- `wave.template.mixed`

Use `Deucarian/Game Content Authoring` to create persistent `AttackDefinitionAsset`, `EnemyDefinitionAsset`, and `WaveDefinitionAsset` files under `Assets/GameContent/...` and assign them to `IdleAutoDefenseTemplateController` when moving beyond the starter placeholders.

The assigned attack list must include:

- `attack.template.hitscan-beam`
- `attack.template.fire-orb`
- `attack.template.homing-pulse`

Those IDs are referenced by the starter weapon modules. The assigned enemy list must include `enemy.template.basic`, `enemy.template.fast`, and `enemy.template.tank`, and assigned waves must reference only resolved enemies. If any assigned set is incomplete or invalid, the controller logs a warning and uses generated transient content so the sample scene remains playable.

Sample audio is intentionally empty. The audio fields are ready for projects that provide clips, and runtime presentation skips missing optional audio/VFX references.
