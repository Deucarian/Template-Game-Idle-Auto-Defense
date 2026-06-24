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
| `DefaultEnemies` | Starter enemy definitions and spawnable IDs. |
| `DefaultWeapons` | Direct weapon, projectile weapon, attack, projectile, mount, and damage IDs. |
| `DefaultWaves` | Starter encounter, wave, spawn groups, channels, and timing. |
| `DefaultUpgrades` | Run upgrade IDs, effects, ranks, rarity, and draft settings. |
| `DefaultProgression` | Currencies, rewards, idle/offline rates, and sample save DTO documents. |
| `DefaultMonetization` | Mock rewarded, interstitial, and IAP placeholder placement IDs and pacing values. |

## Starter Values

- Objective: `template-core`, 28 health, 3 lives.
- Spawn ring: four perimeter channels at radius 7.
- Enemy: `enemy.template.basic`, 8 health, 2.2 movement speed, 3 contact damage.
- Direct weapon: `weapon.template.direct`, 15 range/targeting value, 8 attack damage.
- Projectile weapon: `weapon.template.projectile`, 5 range/targeting value, projectile speed 8.
- Run upgrades: direct damage, projectile speed, objective repair, enemy pacing.
- Encounter reward: 25 credits and 1 part.
- Offline reward: 0.25 credits per second, 1 part every 5 minutes, capped at 8 hours.
- Rewarded placements: double run reward, revive once after failure, reroll upgrade draft, 2x offline reward claim, and small optional currency bonus.
- Interstitial placements: after run completion and after run failure, transition-only, never during combat, never before the first terminal run, global 120 second cooldown, and 3 per-session cap.
- IAP placeholders: no forced ads entitlement, starter pack, and currency pack. These are placeholders only and do not include billing.
- Save documents: profile, run resume, settings, with a profile migration smoke.

These values are intentionally readable rather than commercially tuned. They make the starter scene run immediately and provide a coherent first place to edit.

## Override Rule

Product games should copy the pack into product-owned folders and change IDs away from the `template.*` namespace when the content becomes product-specific. Keep shared runtime systems in Deucarian packages.
