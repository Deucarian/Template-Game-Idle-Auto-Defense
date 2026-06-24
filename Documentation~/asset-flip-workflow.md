# Asset-Flip Workflow

The template is meant to become a new game by copying the starter into a project-owned folder, then replacing assets and content values before changing core systems.

## Recommended Flow

```text
install template
import starter
Tools > Deucarian > Templates > Idle Auto Defense > Create Game From Template
choose target root and namespace
copy into a project-owned folder
replace content assets
press Play
```

## What The Setup Wizard Creates

- Product assembly definition that references `Deucarian.TemplateGameIdleAutoDefense`.
- Product bootstrap script in the requested namespace.
- Product-owned scene copied from the starter scene.
- Product-owned `Content`, `Prefabs`, `Scripts`, and `Docs` folders.
- `Docs/asset-flip-checklist.md`.
- `Docs/setup-report.md`.

The wizard does not copy reusable package source. Deucarian systems stay package dependencies.

## Content Checklist

- Enemies
- Weapons
- Projectiles
- Stages
- Waves
- Run upgrades
- Progression values
- Monetization placements
- Save/profile names

## Replace Enemy Visuals

Put enemy prefabs under `Prefabs/Enemies`, then update the product bootstrap or future content loading path to map product enemy IDs to those prefabs. Keep the six default archetypes while doing the first art pass so tuning remains comparable.

## Replace Weapon Visuals

Put weapon mount prefabs under `Prefabs/Weapons`. Keep Pulse Cannon and Shard Launcher behavior until a reusable package supports additional fire modes.

## Replace Projectile Visuals

Put projectile prefabs under `Prefabs/Projectiles`. Tune projectile speed, damage, lifetime, and pierce in `Content/DefaultWeapons/default-weapons.json` before changing runtime behavior.

## Tune Waves

Edit `Content/DefaultStages/stages.json` and `Content/DefaultWaves/stages-and-encounters.json`. Start with counts, batch sizes, start ticks, interval ticks, and channel choices.

## Tune Upgrades

Edit `Content/DefaultUpgrades/common-run-upgrades.json`. Keep at least one damage, survival, reward, offline, reroll, and specialization upgrade so the starter loop keeps its full shape.

## Tune Rewards

Edit completion rewards, account XP, unlocks, and research-like defaults in `Content/DefaultProgression/currencies-rewards-saves.json`.

## Tune Offline Progression

Adjust `offlineReward.maxHours`, credit production, and cycle rewards in `Content/DefaultProgression/currencies-rewards-saves.json`. Keep mock monetization placements until real provider adapters exist.

## Rename Game

Use product namespaces, folder names, save document IDs, placement IDs, stage IDs, enemy IDs, weapon IDs, upgrade IDs, and progression IDs. Rename IDs gradually while tests stay green.

## Change Theme Later

Theme, branding, UI, audio, and real art direction should land after the copied folder plays with product-owned content. Do not fork the boot/run/reward/save/offline loop merely for visual changes.
