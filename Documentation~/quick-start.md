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

## Create Your First Authored Run

1. Open `Tools > Deucarian > Game Content Authoring`.
2. Create or choose authored Attack, Enemy, Wave, Tower / Weapon, and Upgrade assets.
3. Select `Game / Run Content Set`.
4. Assign a stable ID, display name, starting weapon, available weapons, enemy pool, wave list, and upgrade pool.
5. Keep the output root under `Assets/GameContent/ContentSets`.
6. Review the playable-run preview and validation card.
7. Create the content set, then assign the created `GameContentSetAsset` to an `IdleAutoDefenseTemplateController`.

The content set is a root asset that references existing authored assets. It does not copy attack, enemy, wave, weapon, or upgrade sub-assets. At runtime the template prefers a complete assigned content set; if the set is missing or invalid, it logs a warning and falls back to the direct assigned arrays or built-in sample content.

Current limitations: the content set drives the template's authored attacks, enemies, waves, weapons, upgrades, starting resources, and reward multiplier. Difficulty/session values are stored and shown in preview, but deeper run-length and scaling behavior remain template-specific follow-up work. Next planned authoring types remain content-set hardening and future gameplay-specific providers; this phase does not add towers beyond Weapon-Systems, loot, abilities, or new game modes.
