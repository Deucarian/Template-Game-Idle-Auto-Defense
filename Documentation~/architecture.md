# Architecture

The package is a template, not a reusable gameplay framework. It composes the Auto Defense Suite through starter application glue:

- `BasicIdleAutoDefenseGame` creates deterministic content definitions.
- `IdleAutoDefenseTemplateDefaultContent` exposes stage and module descriptors used by tests and product override docs. It belongs in the template because it describes starter-game content, including future module intent, rather than reusable framework behavior.
- `IdleAutoDefenseTemplateController` wires spawning, navigation, auto-defense runtime, weapons, projectiles, upgrades, idle rewards, progression, and placeholder visuals.
- `IdleAutoDefenseTemplateSaveProgressionComposition` validates profile, run, and settings save paths plus recovery and migration behavior.
- `BasicIdleAutoDefenseGameBootstrap` adds sample-only status UI and sample save reset behavior after the sample is imported.
- `IdleAutoDefenseTemplateMenu` adds editor-only setup helpers under `Tools > Deucarian > Templates > Idle Auto Defense`.
- `IdleAutoDefenseTemplateSetupService` owns testable setup logic for product-owned folders. `IdleAutoDefenseTemplateSetupWizardWindow` stays a thin editor UI over that service.

Replace this glue as a real game matures. New reusable systems should move into lower packages instead of growing inside the template.
