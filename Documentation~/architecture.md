# Architecture

The package is a template, not a reusable gameplay framework. It composes the Auto Defense Suite through starter application glue:

- `BasicIdleAutoDefenseGame` creates deterministic content definitions.
- `IdleAutoDefenseTemplateDefaultContent` exposes stage and module descriptors used by tests and product override docs. It belongs in the template because it describes starter-game content, including future module intent, rather than reusable framework behavior.
- `IdleAutoDefenseTemplateController` wires spawning, navigation, auto-defense runtime, weapons, projectiles, upgrades, idle rewards, progression, and placeholder visuals.
- `IdleAutoDefenseTemplateSaveProgressionComposition` validates profile, run, and settings save paths plus recovery and migration behavior.
- `BasicIdleAutoDefenseGameBootstrap` adds sample-only status UI and sample save reset behavior after the sample is imported.
- `IdleAutoDefenseTemplateMenu` adds editor-only setup helpers under `Tools > Deucarian > Templates > Idle Auto Defense`.
- `IdleAutoDefenseTemplateSetupService` owns testable setup logic for product-owned folders. `IdleAutoDefenseTemplateSetupWizardWindow` stays a thin editor UI over that service.

Authored content is the main data handoff in this slice. The template accepts serialized `AttackDefinitionAsset`, `EnemyDefinitionAsset`, and `WaveDefinitionAsset` references. It validates attacks with runtime-friendly attack validation, enemies with asset-creation validation when assigned so prefab references are present, and waves with runtime-friendly wave validation plus an enemy-reference check.

Assigned attacks are used only when they include the two attack IDs referenced by the starter weapons: `attack.template.pulse-cannon` and `attack.template.shard-launcher`. Assigned enemies are used only when they include the six template enemy IDs referenced by the starter waves: swarm, runner, tank, shielded, elite, and boss. Assigned waves are used only when every entry references a resolved enemy. Invalid sets log a warning and fall back to transient built-in content so the sample remains playable while broken authoring is visible.

Package sample assets store root and section data as sibling `.asset` files because Unity imports `Samples~` content into a project. The editor wizard creates the same structure as root assets with focused sub-assets under normal `Assets/GameContent/...` folders.

Replace this glue as a real game matures. New reusable systems should move into lower packages instead of growing inside the template.
