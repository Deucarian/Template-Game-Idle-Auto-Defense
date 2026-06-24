# Architecture

The package is a template, not a reusable gameplay framework. It composes the Auto Defense Suite through starter application glue:

- `BasicIdleAutoDefenseGame` creates deterministic content definitions.
- `IdleAutoDefenseTemplateController` wires spawning, navigation, auto-defense runtime, weapons, projectiles, upgrades, idle rewards, progression, and placeholder visuals.
- `IdleAutoDefenseTemplateSaveProgressionComposition` validates profile, run, and settings save paths plus recovery and migration behavior.

Attack recipes are the main authored-content handoff in this slice. The template accepts serialized `AttackDefinitionAsset` references, validates them with runtime-friendly attack recipe validation, and only uses an assigned set when it contains the three attack IDs referenced by the starter weapons. That keeps custom content predictable without hiding broken recipe wiring behind unknown-attack failures.

Replace this glue as a real game matures. New reusable systems should move into lower packages instead of growing inside the template.
