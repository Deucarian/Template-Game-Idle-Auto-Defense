# Override Guide

Use this guide when turning the template into a product game.

## First Overrides

1. Copy `Samples~/BasicIdleAutoDefenseGame/Content` into a product-owned content folder.
2. Rename IDs from `template.*` to the product namespace.
3. Replace tuning in this order: objective, stages, enemies, waves, weapons, upgrades, progression, monetization placeholders.
4. Keep the canonical flow until a product requirement proves it needs a fork.

## Content Override Points

- Objective/core: health, lives, contact radius, model/prefab, scene placement.
- Stages: display names, encounter IDs, unlock order, reward references, enemy/module/upgrade availability, and endless-mode placeholder routing.
- Enemies: spawnable ID, health, movement speed, contact damage, visual prefab, pool limits.
- Waves: channels, group counts, delays, wave order, encounter objectives, seed.
- Weapons: Pulse Cannon and Shard Launcher IDs, attacks, projectile settings, mounts, and future Arc Emitter/Orbital Shot intent records.
- Upgrades: IDs, rarity, max rank, effects, target IDs, draft cadence.
- Progression: currency caps, completion rewards, XP, unlocks, offline reward rates, max offline time.
- Monetization: rewarded placement IDs, interstitial cooldowns, session caps, no-ads entitlement placeholder, starter/currency pack placeholders, and product claim IDs.
- Saves: document IDs, DTO shape, validators, migrations, slots.

## What To Keep In Packages

Reusable system code should remain in Deucarian packages: runtime services, catalogs, adapters, persistence, progression, spawning, navigation, weapons, projectiles, upgrades, and monetization abstractions.

Product games can add thin bootstraps, scene composition, product DTOs, product prefabs, product content files, and product monetization override configs. Real ad SDK adapters and billing should live in future integration packages, not only in a product game. Moss can customize the flow later, but the template flow remains the default contract.

## Asset-Flip Path

For a first product pass, mirror the template folder structure in the game project and edit only product-owned files:

1. Copy `DefaultStages` and rename stage IDs into the product namespace.
2. Copy `DefaultEnemies` and map each archetype to product names, placeholder prefabs, and product spawnable IDs.
3. Copy `DefaultWeapons` and keep Pulse Cannon and Shard Launcher behavior until a reusable package adds new fire modes.
4. Copy `DefaultWaves` and tune group counts/timing before adding new encounter rules.
5. Copy `DefaultUpgrades` and rename every upgrade/effect/target ID while preserving at least one damage, survival, reward, offline, reroll, and specialization upgrade.
6. Copy `DefaultProgression` and rename currencies, account XP, unlocks, research nodes, save documents, and reward operations.
7. Copy `DefaultMonetization` and keep mock/no-op providers until real SDK adapters exist in reusable integration packages.

Do not copy framework scripts into a product project to make a simple asset flip. If the default boot/run/reward/save/offline loop is too narrow, first decide whether the missing behavior is reusable package logic or starter-game orchestration, then add it in the correct Deucarian repository.
