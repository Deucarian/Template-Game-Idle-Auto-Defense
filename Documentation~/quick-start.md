# Idle Auto Defense Template Quick Start

1. Install `com.deucarian.package-installer`.
2. Open `Tools > Deucarian > Package Installer`.
3. Select `Templates > Games > Idle Auto Defense`.
4. Install `com.deucarian.template.game.idle-auto-defense`.
5. Import the `Basic Idle Auto Defense Game` sample.
6. Run `Tools > Deucarian > Templates > Idle Auto Defense > Create Game From Template`.
7. Choose a target folder under `Assets`, a namespace, and a game prefix.
8. Open the created scene if the wizard did not open it automatically.
9. Press Play.

In Play Mode, verify these visible starter pieces:

- `Central Objective - Template Core`
- `Spawn Marker - North/East/South/West Perimeter`
- `Direct Weapon Mount - Close Range`
- `Projectile Weapon Mount - Launcher`
- `Enemy Placeholder Preview - Replace Me`
- Runtime enemy and projectile roots
- The on-screen Idle Auto Defense Starter status panel

Use `Save Snapshot` in the status panel to write a sample save file. Use `Reset Save` or `Tools > Deucarian > Templates > Idle Auto Defense > Reset Sample Save` to delete it.

Start customization in the product-owned folder created by the wizard:

- `Content/starter-content.json`
- `Docs/asset-flip-checklist.md`
- `Scripts/<GamePrefix>IdleAutoDefenseGameBootstrap.cs`
- `Scenes/<GamePrefix>IdleAutoDefense.unity`
- `Prefabs/`

The manual sample path still works: import the starter sample and use `Open Starter Scene`. Prefer the setup wizard for new product folders because it renames the bootstrap script and writes a setup report.
