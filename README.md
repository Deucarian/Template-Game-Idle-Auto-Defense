# Deucarian Template Game - Idle Auto Defense

Package ID: `com.deucarian.template.game.idle-auto-defense`

Version: `0.1.1`

This package creates a product-owned idle auto-defense game folder. It owns template glue, setup helpers, starter authored content, and smoke coverage. Reusable gameplay systems stay in lower Deucarian packages.

No Unity Package Manager sample import is required. The private template source lives under `TemplateSource~/BasicIdleAutoDefenseGame` so the setup wizard can create product-owned files.

## Quick Start

1. Install the template package.
2. Run `Tools > Deucarian > Templates > Idle Auto Defense > Create Playable Game`.
3. Choose a target folder under `Assets`, a namespace, and a game prefix.
4. Open the created scene.
5. Press Play.

The generated scene opens into a complete starter loop: a tower in the center, enemies spawning outside view, automatic attacks, currency rewards, buyable upgrades, tower damage, loss state, HUD, save, reset, and restart.

## Generated Game

The created folder includes:

- `Scenes`: the playable idle auto-defense scene.
- `Scripts`: a renamed bootstrap and save/reset helper in the chosen namespace.
- `Content`: product-owned Game Content Authoring assets, content pack, and content set.
- `Prefabs`, `Visuals`, and `Audio`: placeholder assets for replacing the starter look.
- `Docs`: setup report and asset-flip checklist.

The generated scene references the generated content pack and content set. The controller should report `UsingAssignedContentSet == true` with zero content pack/set validation errors.

## Template Source

The package-owned source lives at:

```text
TemplateSource~/BasicIdleAutoDefenseGame
|-- Content
|   |-- Attacks
|   |-- ContentPacks
|   |-- ContentSets
|   |-- Enemies
|   |-- Upgrades
|   |-- Waves
|   |-- Weapons
|   `-- starter-content.json
|-- Prefabs
|-- Scenes
|   `-- BasicIdleAutoDefenseGame.unity
|-- Scripts
|   `-- BasicIdleAutoDefenseGameBootstrap.cs
|-- Visuals
`-- Audio
```

This source is not a public package sample. It is copied by the setup wizard with product-owned namespaces, assembly names, scene references, and remapped GUIDs.

## Editing Content

Open `Tools > Deucarian > Game Content Authoring` and tune the generated assets under the product folder. Replace placeholder visuals, tune waves/upgrades/progression, and rename `template.*` IDs into product-owned IDs as the game becomes real product content.

The starter content intentionally stays generic and reusable:

- 4 enemies
- 4 attacks
- 4 upgrades
- 4 spawn profiles

## Package Boundary

This template depends on:

- `com.deucarian.auto-defense-suite` for the reusable auto-defense gameplay stack.
- `com.deucarian.editor` for shared editor shell/resources used by template setup tools.
- `com.deucarian.game-content-authoring` for content authoring provider integration.
- `com.deucarian.gameplay-foundation` for shared IDs, validation, and gameplay primitives used by template glue.
- `com.deucarian.monetization` for SDK-free placement and mock/no-op monetization abstractions.

Keep product-specific starter glue, setup reporting, template scene composition, placeholder content, and asset-flip helpers local to this template. Move reusable behavior down only through explicit governance.

## Tests

Package tests live under `Tests/EditMode` and `Tests/PlayMode`. Template source files under `TemplateSource~` are not user-importable samples.

## Validation

Before committing package changes, run:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
git diff --check
```

Run existing Unity EditMode and PlayMode tests when changing code, asmdefs, package dependencies, template source content, setup wizard behavior, or starter gameplay behavior.

Durable batch entry points are:

```text
Deucarian.TestAutomation.BatchTestRunner.RunEditMode
Deucarian.TestAutomation.BatchTestRunner.RunPlayMode
```

## License

MIT. See `LICENSE.md`.
