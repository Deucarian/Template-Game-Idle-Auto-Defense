# Content

`starter-content.json` mirrors the constants used by the deterministic starter code. Replace these IDs and values when converting the template into a project-specific data pipeline.

The runtime sample now builds three attack recipes in code when no serialized assets are assigned:

- `attack.template.hitscan-beam`
- `attack.template.fire-orb`
- `attack.template.homing-pulse`

Use `Deucarian/Game Content Authoring` to create persistent `AttackDefinitionAsset` files under `Assets/GameContent/Attacks/{AttackId}` and assign them to `IdleAutoDefenseTemplateController` when moving beyond the generated placeholders.

The assigned recipe list must include:

- `attack.template.hitscan-beam`
- `attack.template.fire-orb`
- `attack.template.homing-pulse`

Those IDs are referenced by the starter weapon modules. If the assigned set is incomplete or invalid, the controller logs a warning and uses the generated transient recipes so the sample scene remains playable.
