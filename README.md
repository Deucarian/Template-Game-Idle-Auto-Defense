# Deucarian Template Game - Idle Auto Defense

Package ID: `com.deucarian.template.game.idle-auto-defense`

Version: `0.1.1`

This package is a starter Unity game template for an idle auto-defense loop. It depends on `com.deucarian.auto-defense-suite` and `com.deucarian.monetization`; reusable gameplay and monetization abstractions stay in Deucarian packages, while this package owns starter-game glue, sample content, placement hooks, and developer-facing setup helpers.

## Quick Start

1. Install Deucarian Package Installer.
2. Open `Tools > Deucarian > Package Installer`.
3. Find `Templates > Games > Idle Auto Defense`.
4. Install `Deucarian Template Game - Idle Auto Defense`.
5. Import the `Basic Idle Auto Defense Game` sample from the Package Installer details panel or Unity Package Manager.
6. Run `Tools > Deucarian > Templates > Idle Auto Defense > Create Game From Template`.
7. Choose a target folder under `Assets`, a namespace, and a game prefix.
8. Open the created scene if needed, then press Play.

The starter scene shows a central core, perimeter spawn markers, Pulse Cannon and Shard Launcher mounts, a placeholder enemy preview, runtime enemies, projectile launches, staged encounters, and a small status panel.

## What This Template Includes

- Central objective with health, lives, contact damage, completion, and failure paths.
- Four perimeter spawn channels.
- First Orbit, Pressure Ring, Boss Pulse, and Endless placeholder stage definitions.
- Six placeholder enemy archetypes: Swarm, Runner, Tank, Shielded, Elite, and Boss.
- Pulse Cannon direct module and Shard Launcher projectile module, with Arc Emitter and Orbital Shot kept as future data-only intent.
- Deterministic upgrade drafts with at least three choices from a 14-upgrade starter catalog.
- Offline reward calculation.
- Mock/no-op rewarded and interstitial placement hooks with no real ad SDKs.
- Progression currency, account XP, stage/module unlock, and sample research reward application.
- Save/load, missing-save defaults, corrupted-save recovery, and migration smoke coverage.
- A sample-local save snapshot and reset flow.
- A documented canonical game flow and explicit default content/balance pack.
- A setup wizard for creating a product-owned game folder from the starter.
- Template-local editor utilities under `Tools > Deucarian > Templates > Idle Auto Defense`.

## What To Edit First

After importing the sample, start here:

- `Content/starter-content.json`: human-readable constants for the core, spawn ring, stages, enemies, weapons, upgrades, and offline rewards.
- `Content/DefaultBalance`: objective, spawn ring, run loop, and draft cadence values.
- `Content/DefaultStages`: stage routing, stage rewards, and stage-scoped content references.
- `Content/DefaultEnemies`: Swarm, Runner, Tank, Shielded, Elite, and Boss archetypes.
- `Content/DefaultWeapons`: Pulse Cannon, Shard Launcher, future Arc Emitter intent, and future Orbital Shot intent.
- `Content/DefaultWaves`: First Orbit, Pressure Ring, Boss Pulse, and Endless placeholder wave groups.
- `Content/DefaultUpgrades`: 14 run upgrades covering damage, survival, reward, offline, reroll, crit intent, and specialization choices.
- `Content/DefaultProgression`: currencies, rewards, account XP, unlocks, research-like defaults, offline rewards, and save DTO setup.
- `Content/DefaultMonetization`: mock rewarded/interstitial placements and IAP placeholders.
- `Scripts/<GamePrefix>IdleAutoDefenseGameBootstrap.cs`: product UI, save snapshot, and the first place to add project-specific scene glue after running the setup wizard.
- `Prefabs/`: place your real core, enemy, weapon, and projectile prefabs here.
- `Scenes/<GamePrefix>IdleAutoDefense.unity`: product-owned scene created by the setup wizard.

The setup wizard writes `Docs/setup-report.md` and `Docs/asset-flip-checklist.md` into the created folder. It blocks existing files unless overwrite is explicitly enabled.

The template owns the default full loop from boot through save/restart. Product games should override content and balance first, and fork the flow only when a product requirement needs it.

## Replace Enemies

1. Add your enemy prefab under the imported sample's `Prefabs/Enemies` folder.
2. Replace the generated enemy placeholder with your prefab provider in your copied game code.
3. Update `starter-content.json` and `DefaultEnemies/enemy-archetypes.json` enemy IDs, health, speed, and contact damage.
4. Keep one simple placeholder enemy in the scene while tuning so Play Mode remains easy to inspect.

## Replace Weapons

The template includes two weapon modes:

- Pulse Cannon: instant single-target damage through the Attack package.
- Shard Launcher: launches a visible projectile through the Projectiles package.

To customize them:

1. Add weapon mount prefabs under `Prefabs/Weapons`.
2. Change weapon IDs and fire modes in your copied definition code.
3. Update the matching entries in `starter-content.json` and `DefaultWeapons/default-weapons.json`.
4. Keep one direct and one projectile example until your replacement weapons are both validated.

## Tune Waves

Wave pacing is defined in `BasicIdleAutoDefenseGame.CreateEncounterDefinitions()` and mirrored in `Content/DefaultWaves/stages-and-encounters.json`.

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

1. Run `Tools > Deucarian > Templates > Idle Auto Defense > Create Game From Template`.
2. Choose a target folder under `Assets`, a C# namespace, and a game prefix.
3. Replace placeholder prefabs and IDs with project-specific content in the generated folder.
4. Follow `Docs/asset-flip-checklist.md` and `Docs/setup-report.md`.
5. Keep the Auto Defense Suite and Monetization dependencies unless you intentionally split packages later.
6. Delete the template package only after your generated game code no longer references it.

## Sample Folder Map

```text
Basic Idle Auto Defense Game
├── Content
│   ├── DefaultBalance
│   ├── DefaultStages
│   ├── DefaultEnemies
│   ├── DefaultWeapons
│   ├── DefaultWaves
│   ├── DefaultUpgrades
│   ├── DefaultProgression
│   ├── DefaultMonetization
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
├── com.deucarian.auto-defense-suite
└── com.deucarian.monetization
```
