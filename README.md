# Deucarian Template Game - Idle Auto Defense

Package ID: `com.deucarian.template.game.idle-auto-defense`

Version: `0.1.1`

This package is a starter Unity game template for an idle auto-defense loop. It depends only on `com.deucarian.auto-defense-suite`; reusable gameplay systems stay in the suite packages, while this package owns starter-game glue, sample content, and developer-facing setup helpers.

## Quick Start

1. Install Deucarian Package Installer.
2. Open `Tools > Deucarian > Package Installer`.
3. Find `Templates > Games > Idle Auto Defense`.
4. Install `Deucarian Template Game - Idle Auto Defense`.
5. Import the `Basic Idle Auto Defense Game` sample from the Package Installer details panel or Unity Package Manager.
6. Run `Tools > Deucarian > Templates > Idle Auto Defense > Open Starter Scene`.
7. Press Play.

The starter scene shows a central core, perimeter spawn markers, a direct weapon mount, a projectile weapon mount, a placeholder enemy preview, runtime enemies, projectile launches, and a small status panel.

## What This Template Includes

- Central objective with health, lives, contact damage, completion, and failure paths.
- Four perimeter spawn channels.
- One placeholder enemy archetype.
- One direct weapon mount and one projectile weapon mount.
- Deterministic upgrade drafts with at least three choices.
- Offline reward calculation.
- Progression currency reward application.
- Save/load, missing-save defaults, corrupted-save recovery, and migration smoke coverage.
- A sample-local save snapshot and reset flow.
- A documented canonical game flow and explicit default content/balance pack.
- Template-local editor utilities under `Tools > Deucarian > Templates > Idle Auto Defense`.

## What To Edit First

After importing the sample, start here:

- `Content/starter-content.json`: human-readable constants for the core, spawn ring, weapons, upgrades, and offline rewards.
- `Content/DefaultBalance`: objective, spawn ring, run loop, and draft cadence values.
- `Content/DefaultEnemies`: starter enemy definitions.
- `Content/DefaultWeapons`: direct/projectile weapons, attacks, and projectile settings.
- `Content/DefaultWaves`: starter encounter and wave groups.
- `Content/DefaultUpgrades`: common run upgrades.
- `Content/DefaultProgression`: currencies, rewards, offline rewards, and save DTO setup.
- `Scripts/BasicIdleAutoDefenseGameBootstrap.cs`: sample UI, sample save snapshot, and the first place to add project-specific scene glue.
- `Prefabs/`: place your real core, enemy, weapon, and projectile prefabs here.
- `Scenes/BasicIdleAutoDefenseGame.unity`: duplicate this scene before making production changes.

The template owns the default full loop from boot through save/restart. Product games should override content and balance first, and fork the flow only when a product requirement needs it.

## Replace Enemies

1. Add your enemy prefab under the imported sample's `Prefabs/Enemies` folder.
2. Replace the generated enemy placeholder with your prefab provider in your copied game code.
3. Update `starter-content.json` enemy IDs, health, speed, and contact damage.
4. Keep one simple placeholder enemy in the scene while tuning so Play Mode remains easy to inspect.

## Replace Weapons

The template includes two weapon modes:

- Direct weapon: instant damage through the Attack package.
- Projectile weapon: launches a visible projectile through the Projectiles package.

To customize them:

1. Add weapon mount prefabs under `Prefabs/Weapons`.
2. Change weapon IDs and fire modes in your copied definition code.
3. Update the matching entries in `starter-content.json`.
4. Keep one direct and one projectile example until your replacement weapons are both validated.

## Tune Waves

Wave pacing is defined in `BasicIdleAutoDefenseGame.CreateEncounterDefinition()`.

Useful values to tune first:

- Spawn channels: `perimeter-north`, `perimeter-east`, `perimeter-south`, `perimeter-west`.
- Spawn count per group.
- Initial delay ticks.
- Repeat interval ticks.
- Encounter seed.

Keep the first tuning pass deterministic. Once the starter loop feels right, introduce project-specific content loading.

## Reset Sample Save

The sample writes a small snapshot file to:

```text
<Unity persistentDataPath>/Deucarian/IdleAutoDefenseTemplateSample/sample-state.json
```

Reset it from:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Reset Sample Save
```

The in-game status panel also has a `Reset Save` button.

## Create Your Own Game From This

1. Duplicate the imported sample folder into your project, outside `Assets/Samples`.
2. Rename the assembly definition, namespace, scene, and bootstrap class.
3. Replace placeholder prefabs and IDs with project-specific content.
4. Move copied starter logic out of the template namespace.
5. Keep the Auto Defense Suite dependency unless you intentionally split packages later.
6. Delete the template package only after your copied game code no longer references it.

## Sample Folder Map

```text
Basic Idle Auto Defense Game
├── Content
│   ├── DefaultBalance
│   ├── DefaultEnemies
│   ├── DefaultWeapons
│   ├── DefaultWaves
│   ├── DefaultUpgrades
│   ├── DefaultProgression
│   └── starter-content.json
├── Prefabs
│   └── README.md
├── Scenes
│   └── BasicIdleAutoDefenseGame.unity
├── Scripts
│   └── BasicIdleAutoDefenseGameBootstrap.cs
└── Tests
    └── BasicIdleAutoDefenseGameSampleTests.cs
```

## Troubleshooting

- Starter scene command cannot find the scene: import the `Basic Idle Auto Defense Game` sample first.
- Sample import does not appear: install the package first, then refresh Package Installer or Unity Package Manager.
- Play Mode shows no enemies: check the Console for package resolution errors, then run the shared EditMode tests.
- Reset says nothing was found: press Play once or use `Save Snapshot` in the status panel, then reset again.
- Package dependencies do not resolve in a local validation project: use explicit file references to lower Deucarian packages, or install the suite from the promoted registry URLs.

## Tests

Package tests live under `Tests/EditMode` and `Tests/PlayMode`. The sample also includes smoke tests under `Samples~/BasicIdleAutoDefenseGame/Tests` for projects that import the sample.

## Dependency Graph

```text
com.deucarian.template.game.idle-auto-defense
└── com.deucarian.auto-defense-suite
```
