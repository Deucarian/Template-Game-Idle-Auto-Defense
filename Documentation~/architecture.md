# Architecture

The package is a template, not a reusable gameplay framework. It composes the Auto Defense Suite through starter application glue:

- `BasicIdleAutoDefenseGame` creates deterministic content definitions.
- `IdleAutoDefenseTemplateController` wires spawning, navigation, auto-defense runtime, weapons, projectiles, upgrades, idle rewards, progression, and placeholder visuals.
- `IdleAutoDefenseTemplateSaveProgressionComposition` validates profile, run, and settings save paths plus recovery and migration behavior.

Authored content is the main data handoff in this slice. The template accepts serialized `AttackDefinitionAsset`, `EnemyDefinitionAsset`, and `WaveDefinitionAsset` references. It validates attacks with runtime-friendly attack validation, enemies with asset-creation validation when assigned so prefab references are present, and waves with runtime-friendly wave validation plus an enemy-reference check.

Assigned attacks are used only when they include the three attack IDs referenced by the starter weapons. Assigned enemies are used only when they include the three template enemy IDs referenced by the starter waves. Assigned waves are used only when every entry references a resolved enemy. Invalid sets log a warning and fall back to transient built-in content so the sample remains playable while broken authoring is visible.

Package sample assets store root and section data as sibling `.asset` files because Unity imports `Samples~` content into a project. The editor wizard creates the same structure as root assets with focused sub-assets under normal `Assets/GameContent/...` folders.

Replace this glue as a real game matures. New reusable systems should move into lower packages instead of growing inside the template.
