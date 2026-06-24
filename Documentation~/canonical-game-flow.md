# Canonical Idle Auto Defense Game Flow

The Idle Auto Defense template owns the default starter game flow. Deucarian packages provide reusable systems for persistence, progression, encounters, spawning, navigation, combat, projectiles, weapons, upgrades, idle rewards, and auto-defense runtime behavior. Product games should start by overriding content and balance before forking the overall flow.

## Flow

```text
Boot
-> Load sample profile/save
-> Apply offline reward
-> Show/start run state
-> Start run
-> Spawn waves
-> Auto weapons fire
-> Run upgrade draft moments
-> Win/fail
-> Apply rewards
-> Save
-> Restart/return
```

## Runtime Ownership

`IdleAutoDefenseTemplateController` is the canonical starter orchestration. It builds a central objective, perimeter spawns, enemy and projectile spawning, direct and projectile weapon modules, run upgrades, encounter rewards, offline rewards, and sample save/reset behavior.

The reusable packages stay generic:

- Auto Defense Suite supplies the systems.
- The template wires those systems into a playable starter loop.
- Product games copy or reference the template flow intentionally, then replace content and balance first.

## State Flow

| Step | Template Responsibility | Product Override Point |
| --- | --- | --- |
| Boot | Build the default runtime graph. | Replace scene bootstrap only when a product needs a different entry point. |
| Load sample profile/save | Demonstrate profile, run, and settings DTOs. | Rename documents and expand DTOs for product saves. |
| Apply offline reward | Use the default idle reward definition. | Tune reward rates, caps, and currencies. |
| Start run | Start a deterministic starter encounter. | Swap encounter IDs, waves, and enemy groups. |
| Spawn waves | Emit four perimeter groups. | Add channels, patterns, enemy types, and wave timing. |
| Auto weapons fire | Use one direct weapon and one projectile weapon. | Tune attacks, projectile behavior, mounts, and visual prefabs. |
| Run upgrade draft moments | Draft common upgrades every fixed tick interval. | Replace upgrade IDs, rarity, effects, and draft cadence. |
| Win/fail | Finish when the encounter/runtime reaches terminal state. | Add product-specific fail rules only when needed. |
| Apply rewards | Grant credits and parts. | Change currencies, amounts, XP, unlocks, and operations. |
| Save | Persist sample snapshot and composition smoke DTOs. | Store real product profile/run/settings documents. |
| Restart/return | Keep the scene immediately replayable. | Route into menus, maps, or meta progression later. |

## Guardrail

Moss and other product games should not copy reusable Deucarian package source into the game project. Start by editing content packs, prefabs, scene composition, product DTOs, and balance values. Fork the loop only after a product requirement cannot be expressed by content/balance or a narrow product bootstrap.
