# Default Content And Balance

The template sample carries an explicit default content pack under:

```text
Samples~/BasicIdleAutoDefenseGame/Content
```

The pack mirrors the values used by `BasicIdleAutoDefenseGame` and `IdleAutoDefenseTemplateController`. It is meant to be copied into a product project and edited there.

## Pack Layout

| Folder | Purpose |
| --- | --- |
| `DefaultBalance` | Core loop, objective, spawn ring, draft cadence, and deterministic seed. |
| `DefaultStages` | Stage list, stage-to-encounter mapping, rewards, and stage-scoped content references. |
| `DefaultEnemies` | Swarm, Runner, Tank, Shielded, Elite, and Boss archetypes with spawnable IDs. |
| `DefaultWeapons` | Supported Pulse Cannon and Shard Launcher modules plus future Arc Emitter and Orbital Shot intents. |
| `DefaultWaves` | First Orbit, Pressure Ring, Boss Pulse, and Endless placeholder encounters. |
| `DefaultUpgrades` | Run upgrade IDs, effects, ranks, rarity, and draft settings. |
| `DefaultProgression` | Currencies, rewards, account XP, stage/module unlocks, research-like defaults, idle/offline rates, and sample save DTO documents. |
| `DefaultMonetization` | Mock rewarded, interstitial, and IAP placeholder placement IDs and pacing values. |

## Starter Values

- Objective: `template-core`, 42 health, 4 lives.
- Spawn ring: four perimeter channels at radius 7.
- Stages: `stage.template.first-orbit`, `stage.template.pressure-ring`, `stage.template.boss-pulse`, and `stage.template.endless-placeholder`.
- Enemy archetypes: Swarm, Runner, Tank, Shielded, Elite, and Boss.
- Runtime-supported modules: `weapon.template.pulse-cannon` for direct single target damage and `weapon.template.shard-launcher` for projectile play.
- Future module intents: `weapon.template.arc-emitter` and `weapon.template.orbital-shot` are data-only until reusable weapon systems support chain/beam and delayed-area behavior.
- Run upgrades: 14 defaults covering damage, fire-rate intent, projectile count intent, projectile speed, core health, repair, shield intent, rewards, offline gains, reroll intent, crit intent, and direct/projectile specializations.
- Encounter reward: 60 credits, 3 parts, 35 account XP, starter/stage/module unlocks.
- Offline reward: 0.35 credits per second, 1 part every 4 minutes, capped at 8 hours.
- Progression defaults: soft currency, parts, account XP thresholds, stage unlocks, module unlocks, and sample research nodes for core plating, pulse capacitor, shard loader, and offline routing.
- Rewarded placements: double run reward, revive once after failure, reroll upgrade draft, 2x offline reward claim, and small optional currency bonus.
- Interstitial placements: after run completion and after run failure, transition-only, never during combat, never before the first terminal run, global 120 second cooldown, and 3 per-session cap.
- IAP placeholders: no forced ads entitlement, starter pack, and currency pack. These are placeholders only and do not include billing.
- Save documents: profile, run resume, settings, with a profile migration smoke.

These values are intentionally readable rather than commercially tuned. They now support a meaningful vertical-slice session: complete First Orbit, push into Pressure Ring/Boss Pulse tuning, receive rewards, save progression, apply offline rewards on next boot, and restart.

## Override Rule

Product games should copy the pack into product-owned folders and change IDs away from the `template.*` namespace when the content becomes product-specific. Keep shared runtime systems in Deucarian packages.
