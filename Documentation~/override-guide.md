# Override Guide

Use this guide when turning the template into a product game.

## First Overrides

1. Copy `Samples~/BasicIdleAutoDefenseGame/Content` into a product-owned content folder.
2. Rename IDs from `template.*` to the product namespace.
3. Replace tuning in this order: objective, enemies, waves, weapons, upgrades, progression, monetization placeholders.
4. Keep the canonical flow until a product requirement proves it needs a fork.

## Content Override Points

- Objective/core: health, lives, contact radius, model/prefab, scene placement.
- Enemies: spawnable ID, health, movement speed, contact damage, visual prefab, pool limits.
- Waves: channels, group counts, delays, wave order, encounter objectives, seed.
- Weapons: direct weapon damage/range, projectile weapon damage/speed/lifetime, mounts.
- Upgrades: IDs, rarity, max rank, effects, target IDs, draft cadence.
- Progression: currency caps, completion rewards, XP, unlocks, offline reward rates, max offline time.
- Monetization: rewarded placement IDs, interstitial cooldowns, session caps, no-ads entitlement placeholder, starter/currency pack placeholders, and product claim IDs.
- Saves: document IDs, DTO shape, validators, migrations, slots.

## What To Keep In Packages

Reusable system code should remain in Deucarian packages: runtime services, catalogs, adapters, persistence, progression, spawning, navigation, weapons, projectiles, upgrades, and monetization abstractions.

Product games can add thin bootstraps, scene composition, product DTOs, product prefabs, product content files, and product monetization override configs. Real ad SDK adapters and billing should live in future integration packages, not only in a product game. Moss can customize the flow later, but the template flow remains the default contract.
