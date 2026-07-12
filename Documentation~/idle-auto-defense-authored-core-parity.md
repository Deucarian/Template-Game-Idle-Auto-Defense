# Idle Auto Defense Authored Core Parity

This inventory captures the effective gameplay values before the strict authored-core pass and their authoritative owners afterward. The pass moves ownership; it does not retune the run.

| Area | Preserved effective value | Previous source | Authored owner |
| --- | --- | --- | --- |
| Starting resources | 10 credits, 0 parts | Content-set scalar/default | `Content/Economy/economy.idle-auto-defense.playable.asset` currencies |
| Kill reward | 5 credits per defeated enemy | Runtime constant; old mixed enemy values were not consumed | Each `EnemyStatsDefinitionAsset._rewardValue` |
| Passive income | 1 credit every 60 simulation ticks | Controller constants | Economy asset |
| Encounter reward | 60 credits, 3 parts, 35 account XP | Code-owned reward bundle | Economy plus progression assets |
| Small reward | 5 credits | Controller constant | Economy asset |
| Reward claim multiplier | 2x | Controller constant | Economy asset |
| Manual upgrade costs | Damage 20+16/rank; fire rate 18+15; range 18+15; repair 16+14 | Controller constants/formula | Economy cost curves |
| Overdrive | 22 credits; 7s duration; 18s cooldown; 10 tick cooldown bonus; 1.55x damage | Controller constants | Economy and game-rules assets |
| Module costs | Pulse 34; Arc 62; Homing 78 | Controller constants | Weapon build costs referenced by game rules |
| Objective | 240 HP; 60 lives; restore 2; contact radius 0.45 | Controller definition constants | Game-rules asset |
| Spawn arena | 18.5 radius; eight authored perimeter channels | Controller definitions | Game-rules asset |
| Session | 5,600 fixed-rate ticks at 20 Hz, 280 seconds | Nested content-set hint plus frame-driven controller | Run-profile asset |
| Run rules | 7 waves; difficulty 1x; rewards 1x; authored-wave victory; objective-destroyed defeat; endless disabled; seed 20260623 | Content-set scalars and controller defaults | Run-profile asset |
| First draft | 30 seconds | Controller constant | Reward-catalog asset |
| Draft size | 3 choices | Nested runtime settings | Reward-catalog settings |
| Draft XP | normal 9, elite 42, boss 120, wave 18 | Nested runtime settings | Reward-catalog settings |
| XP curve | 38 base +18 per level | Nested runtime settings | Reward-catalog settings |
| Rank gates | 3 normal ranks for Epic; 3 Epic ranks for Legendary | Nested runtime settings | Reward-catalog settings and choice prerequisites |
| Level rarity weights | 100 / 70 / 36 / 12 / 2 | Nested runtime settings | Reward-catalog settings |
| Elite rarity weights | 22 / 56 / 78 / 44 / 10 | Nested runtime settings | Reward-catalog settings |
| Boss rarity weights | 8 / 24 / 58 / 86 / 42 | Nested runtime settings | Reward-catalog settings |
| Persistent track | Account thresholds 100, 250, 500, 900 | Code-owned progression catalog | Progression asset |
| Persistent nodes | Core 25/75/160, +8 HP; Pulse 2/5 parts, +1 damage; Shard 2/5 parts, +1 projectile; Offline 40/120, +0.1 after Core rank 1 | Code-owned node array | Progression asset |
| Offline | 0.35 credits/s; 1 part/240s; 8 hour cap; floor rounding; 2x claim | Controller and code-owned idle definition | Offline-progression asset |

Reward-card parity is explicit in `Content/Rewards/reward-catalog.idle-auto-defense.playable.asset`:

- 3 authored module unlock choices.
- 12 normal choices: each weapon follows `+2 damage`, `+2 fire rate`, then its existing `+1` weapon specialty.
- 12 Epic choices: Shard `+2 projectiles`, `+0.25 speed`, `+0.30 global damage`; Pulse `+1 power`, `+1 fire rate`, `+0.30 global damage`; Arc `+1 power`, `+1 range`, `+0.30 global damage`; Homing `+1 power`, `+0.30 global damage`, `+1 fire rate`.
- 4 Legendary choices: Shard `+3 projectiles`; Pulse, Arc, and Homing `+2` specialty power.
- 6 base choices: damage `+2`, fire rate `+2`, range `+1`, repair `+1`, projectile speed `+0.25`, credit multiplier `+0.25`.

The EditMode parity fixture asserts these values and the mutation fixtures prove that authored amounts, weights, resources, costs, enemy rewards, run duration, progression effects, and offline rates/caps reach runtime.
