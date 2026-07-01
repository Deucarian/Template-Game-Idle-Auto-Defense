# Override Guide

Use this guide when turning the template into a product game.

## First Overrides

1. Generate a product-owned folder with `Create Playable Game`.
2. Rename content IDs from `template.*` to product IDs.
3. Tune `Enemies`, `Attacks`, `Weapons`, spawn profiles, and `Upgrades`.
4. Replace starter prefabs and presentation assets.
5. Keep the canonical flow until a product requirement proves it needs a fork.

## Content Override Points

- Enemies: ID, health, speed, contact damage, collision radius, prefab.
- Attacks: delivery mode, damage, range, projectile settings, targeting.
- Weapons: tower ID, fire mode, attack reference, range, cost, prefab.
- Spawn profiles: entries, counts, batch sizes, start ticks, intervals, spawn channels.
- Upgrades: rarity, rank cap, costs, effects, target references.
- Content pack: pack ID, default content set, tags, compatibility notes, icon, banner.

Reusable system code should remain in Deucarian packages. Product games can add thin bootstraps, scene composition, product DTOs, product prefabs, and product content assets.
