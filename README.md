# Deucarian Template Game - Idle Auto Defense

Package ID: `com.deucarian.template.game.idle-auto-defense`

Version: `0.1.1`

Playable Unity template package for an idle auto-defense starter game. The sample opens into a central-core defense loop with visible perimeter spawns, direct and projectile weapons, deterministic upgrade drafts, offline rewards, progression/save smoke paths, mock monetization placements, and a setup wizard that copies the starter into a product-owned folder.

The template owns starter-game glue, sample content, placement hooks, and setup/reporting helpers. Reusable gameplay, authoring, editor shell, and monetization abstractions stay in the lower Deucarian packages.

## When To Use This

Use this package when you want:

- A ready-to-open idle auto-defense starter scene.
- A wizard that creates a product-owned game folder, namespace, bootstrap script, scene, docs, and asset-flip checklist.
- A reference for composing Auto Defense Suite, Game Content Authoring, Gameplay Foundation, Editor, and Monetization in a starter game.
- Mock rewarded/interstitial placement hooks without shipping real ad SDKs.

Do not use this package as a reusable gameplay framework, monetization SDK adapter, package installer surface, or generic authoring framework. Those capabilities belong to lower reusable packages or other Deucarian owners.

## Install

Unity compatibility: `6000.3` or newer.

Install through Deucarian Package Installer:

```json
"com.deucarian.package-installer": "https://github.com/Deucarian/Package-Installer.git#main"
```

Then open:

```text
Tools > Deucarian > Package Installer
```

Find `Templates > Games > Idle Auto Defense` and install `Deucarian Template Game - Idle Auto Defense`.

You can also install the template directly with Unity Package Manager:

```json
"com.deucarian.template.game.idle-auto-defense": "https://github.com/Deucarian/Template-Game-Idle-Auto-Defense.git#main"
```

```json
"com.deucarian.template.game.idle-auto-defense": "https://github.com/Deucarian/Template-Game-Idle-Auto-Defense.git#develop"
```

Use `#main` for stable package consumption and `#develop` when testing active package work.

## Play In 3 Minutes

1. Install the template package.
2. Import the `Basic Idle Auto Defense Game` sample from Package Installer or Unity Package Manager.
3. Run `Tools > Deucarian > Templates > Idle Auto Defense > Create Game From Template`.
4. Choose a target folder under `Assets`, a C# namespace, and a game prefix.
5. Open the created scene if the wizard did not open it automatically.
6. Press Play.

The starter scene shows a central core, perimeter spawn markers, Pulse Cannon and Shard Launcher mounts, a placeholder enemy preview, runtime enemies, projectile launches, staged encounters, and a small status panel.

For a quick package-sample check without creating a product folder, import the sample and run:

```text
Tools > Deucarian > Templates > Idle Auto Defense > Open Starter Scene
```

## In-Game Actions

- Watch the core, spawn markers, direct weapon mount, projectile weapon mount, enemies, and projectile roots update automatically.
- Use `Save Snapshot` in the status panel to write a sample save file.
- Use `Reset Save` in the status panel or `Tools > Deucarian > Templates > Idle Auto Defense > Reset Sample Save` to clear it.
- Let the sample run long enough to see deterministic upgrade draft application and encounter rewards.

There are no player movement controls in this starter slice; the loop demonstrates automated defense, progression, content wiring, and setup flow.

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
- A `GameContentSetAsset` sample recipe and authoring provider for assembling attacks, enemies, waves, towers/weapons, and upgrades into a playable run.
- Template-local editor utilities under `Tools > Deucarian > Templates > Idle Auto Defense`.

## What To Customize First

After importing the sample or creating your product folder, start here:

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
- `Prefabs/`: place real core, enemy, weapon, and projectile prefabs here.
- `Scenes/<GamePrefix>IdleAutoDefense.unity`: product-owned scene created by the setup wizard.

The setup wizard writes `Docs/setup-report.md` and `Docs/asset-flip-checklist.md` into the created folder. It blocks existing files unless overwrite is explicitly enabled.

## Sample And API Map

- Sample scene: `Samples~/BasicIdleAutoDefenseGame/Scenes/BasicIdleAutoDefenseGame.unity`
- Sample bootstrap: `Samples~/BasicIdleAutoDefenseGame/Scripts/BasicIdleAutoDefenseGameBootstrap.cs`
- Runtime starter catalog and controller: `Runtime/IdleAutoDefenseTemplate.cs`
- Default stage/module descriptors: `Runtime/IdleAutoDefenseTemplateDefaultContent.cs`
- Game content set validation: `Runtime/GameContentSetValidation.cs`
- Game content pack validation: `Runtime/GameContentPackValidation.cs`
- Template menu: `Editor/IdleAutoDefenseTemplateMenu.cs`
- Setup wizard: `Editor/IdleAutoDefenseTemplateSetupWizard.cs`
- Quick start: `Documentation~/quick-start.md`
- Asset-flip workflow: `Documentation~/asset-flip-workflow.md`
- Validation notes: `Documentation~/validation.md`

## Create A Game / Run Content Set

1. Open `Tools > Deucarian > Game Content Authoring`.
2. Use the Attack, Enemy, Wave, Tower / Weapon, and Upgrade providers to create the assets for a run.
3. Select `Game / Run Content Set`.
4. Assign the starting tower/weapon, available tower/weapon list, enemy pool, wave list, upgrade pool, resources, economy hints, tags, and optional icon/banner.
5. Create under `Assets/GameContent/ContentSets/{ContentSetId}/`.
6. Assign the created `GameContentSetAsset` to an `IdleAutoDefenseTemplateController`.

The content set is a root asset that references existing authored assets; it does not own their sub-assets. Runtime conversion stays in gameplay packages: weapons still point at attack definitions, waves still point at enemies, and upgrades still point at included weapons, attacks, enemies, or projectile IDs. Missing optional icon/banner/audio/VFX/model references are safe metadata gaps. Missing required weapons, attacks, enemies, or waves block validation.

When a valid content set is assigned, the template uses it as the source of truth. When it is missing or invalid, the controller logs a clear warning and falls back to direct assigned arrays or built-in sample content.

## Package Boundary

This template depends on:

- `com.deucarian.auto-defense-suite` for the reusable auto-defense gameplay stack.
- `com.deucarian.editor` for shared editor shell/resources used by template setup tools.
- `com.deucarian.game-content-authoring` for content authoring provider integration.
- `com.deucarian.gameplay-foundation` for shared IDs, validation, and gameplay primitives used by template glue.
- `com.deucarian.monetization` for SDK-free placement and mock/no-op monetization abstractions.

Keep product-specific starter glue, setup reporting, sample scene composition, placeholder content, and asset-flip helpers local to this template. Move reusable behavior down only through explicit governance.

## Validation

Use Game Content Authoring validation cards when creating or editing content packs and game/run content sets. Preview and validation do not dirty the scene; `Apply To Scene` intentionally writes the selected content pack and content set onto the controller.

Before committing package changes, run:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
git diff --check
```

Run existing Unity EditMode and PlayMode tests when changing code, asmdefs, package dependencies, sample content, setup wizard behavior, or starter gameplay behavior.

Expected Unity coverage is documented in `Documentation~/validation.md`; durable batch entry points are:

```text
Deucarian.TestAutomation.BatchTestRunner.RunEditMode
Deucarian.TestAutomation.BatchTestRunner.RunPlayMode
```

## Screenshots And Media

Sample icon/banner textures and placeholder visual assets are committed under `Samples~/BasicIdleAutoDefenseGame/Visuals`. No gameplay screenshot or GIF is committed yet. Add `Documentation~/media/` captures once the starter scene has stable visual direction, then link one first-run GIF and one setup wizard screenshot from this section.

## Troubleshooting

- Starter scene command cannot find the scene: import the `Basic Idle Auto Defense Game` sample first.
- Sample import does not appear: install the package first, then refresh Package Installer or Unity Package Manager.
- Play Mode shows no enemies: check the Console for package resolution errors, then run the shared EditMode tests.
- Content pack preview has errors: add a default game/run content set, starting tower/weapon, enemy pool, wave list, and available weapon list.
- Reset says nothing was found: press Play once or use `Save Snapshot` in the status panel, then reset again.
- Package dependencies do not resolve in a local validation project: use explicit file references to lower Deucarian packages, or install the suite from the promoted registry URLs.

## Sample Folder Map

```text
Basic Idle Auto Defense Game
|-- Content
|   |-- DefaultBalance
|   |-- DefaultStages
|   |-- DefaultEnemies
|   |-- DefaultWeapons
|   |-- DefaultWaves
|   |-- DefaultUpgrades
|   |-- DefaultProgression
|   |-- DefaultMonetization
|   |-- ContentSets
|   |-- ContentPacks
|   |-- RuntimeEnemies
|   |-- RuntimeWaves
|   `-- starter-content.json
|-- Prefabs
|   |-- Enemies
|   |-- Projectiles
|   |-- Weapons
|   `-- README.md
|-- Scenes
|   `-- BasicIdleAutoDefenseGame.unity
|-- Scripts
|   `-- BasicIdleAutoDefenseGameBootstrap.cs
`-- Tests
    `-- BasicIdleAutoDefenseGameSampleTests.cs
```

## Tests

Package tests live under `Tests/EditMode` and `Tests/PlayMode`. The sample also includes smoke tests under `Samples~/BasicIdleAutoDefenseGame/Tests` for projects that import the sample.

## Dependency Graph

```text
com.deucarian.template.game.idle-auto-defense
|-- com.deucarian.auto-defense-suite
|-- com.deucarian.editor
|-- com.deucarian.game-content-authoring
|-- com.deucarian.gameplay-foundation
`-- com.deucarian.monetization
```

## License

MIT. See `LICENSE.md`.
