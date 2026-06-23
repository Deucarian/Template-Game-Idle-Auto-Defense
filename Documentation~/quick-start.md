# Idle Auto Defense Template Quick Start

1. Install `com.deucarian.package-installer`.
2. Open `Tools > Deucarian > Package Installer`.
3. Select `Templates > Games > Idle Auto Defense`.
4. Install `com.deucarian.template.game.idle-auto-defense`.
5. Import the `Basic Idle Auto Defense Game` sample.
6. Run `Tools > Deucarian > Templates > Idle Auto Defense > Open Starter Scene`.
7. Press Play.

In Play Mode, verify these visible starter pieces:

- `Central Objective - Template Core`
- `Spawn Marker - North/East/South/West Perimeter`
- `Direct Weapon Mount - Close Range`
- `Projectile Weapon Mount - Launcher`
- `Enemy Placeholder Preview - Replace Me`
- Runtime enemy and projectile roots
- The on-screen Idle Auto Defense Starter status panel

Use `Save Snapshot` in the status panel to write a sample save file. Use `Reset Save` or `Tools > Deucarian > Templates > Idle Auto Defense > Reset Sample Save` to delete it.

Start customization by duplicating the imported sample folder out of `Assets/Samples`, then rename namespaces and edit:

- `Content/starter-content.json`
- `Scripts/BasicIdleAutoDefenseGameBootstrap.cs`
- `Scenes/BasicIdleAutoDefenseGame.unity`
- `Prefabs/`
