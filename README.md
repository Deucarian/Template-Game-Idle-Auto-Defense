# Deucarian Template Game - Idle Auto Defense

Package ID: `com.deucarian.template.game.idle-auto-defense`

Version: `0.1.1`

This package creates a product-owned idle auto-defense game folder. It owns template glue, setup helpers, starter authored content, and smoke coverage. Reusable gameplay systems stay in lower Deucarian packages.

## Quick Start

1. Install the template package.
2. Run `Tools > Deucarian > Templates > Idle Auto Defense > Create Playable Game`.
3. Choose a target folder under `Assets`, a namespace, and a game prefix.
4. Open the created scene.
5. Press Play.

No Unity Package Manager sample import is required. The private template source lives under `TemplateSource~/BasicIdleAutoDefenseGame` only so the setup wizard can create product-owned files.

## Generated Game

The created folder includes:

- `Scenes`: the playable idle auto-defense scene.
- `Scripts`: a renamed bootstrap and save/reset helper in the chosen namespace.
- `Content`: product-owned Game Content Authoring assets, content pack, and content set.
- `Prefabs`, `Visuals`, and `Audio`: placeholder assets for replacing the starter look.
- `Docs`: setup report and asset-flip checklist.

The generated scene references the generated content pack and content set. The controller should report `UsingAssignedContentSet == true` with zero content pack/set validation errors.

## Editing Content

Open `Tools > Deucarian > Game Content Authoring` and tune the generated assets under the product folder. Replace placeholder visuals, tune waves/upgrades/progression, and rename `template.*` IDs into product-owned IDs as the game becomes real product content.

## Tests

Package tests live under `Tests/EditMode` and `Tests/PlayMode`. Template source files under `TemplateSource~` are not user-importable samples.
