# Idle Auto Defense Authoring Runtime Parity

Generated: 2026-07-03T22:22:34.1201589+02:00

Main content set: `Assets/GameContent/IdleAutoDefense/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset`

## Towers

| Content ID | Authoring Prefab | Runtime Prefab | Match | Notes |
| ---------- | ---------------- | -------------- | ----- | ----- |
| weapon.idle-auto-defense.shard-launcher | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyShardLauncherWeapon.prefab (KenneyShardLauncherWeapon) | KenneyShardLauncherWeapon / Shard Launcher Authored Weapon Visual | Yes |  |
| weapon.idle-auto-defense.pulse-beam | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyPulseBeamWeapon.prefab (KenneyPulseBeamWeapon) |  | Yes |  |
| weapon.idle-auto-defense.arc-burst | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyArcBurstWeapon.prefab (KenneyArcBurstWeapon) |  | Yes |  |
| weapon.idle-auto-defense.homing-pulse | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyHomingSpireWeapon.prefab (KenneyHomingSpireWeapon) |  | Yes |  |

## Enemies

| Content ID | Authoring Prefab | Runtime Prefab | Match | Notes |
| ---------- | ---------------- | -------------- | ----- | ----- |
| enemy.idle-auto-defense.swarm | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneySwarmEnemy.prefab (KenneySwarmEnemy) | KenneySwarmEnemy / Kenney Enemy Runtime Prefab enemy.idle-auto-defense.swarm(Clone) | Yes |  |
| enemy.idle-auto-defense.runner | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyRunnerEnemy.prefab (KenneyRunnerEnemy) | KenneyRunnerEnemy / Kenney Enemy Runtime Prefab enemy.idle-auto-defense.runner(Clone) | Yes |  |
| enemy.idle-auto-defense.tank | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyTankEnemy.prefab (KenneyTankEnemy) | KenneyTankEnemy / Kenney Enemy Runtime Prefab enemy.idle-auto-defense.tank(Clone) | Yes |  |
| enemy.idle-auto-defense.shielded | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyTankEnemy.prefab (KenneyTankEnemy) |  | Yes |  |
| enemy.idle-auto-defense.elite | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyTankEnemy.prefab (KenneyTankEnemy) |  | Yes |  |
| enemy.idle-auto-defense.boss | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyTankEnemy.prefab (KenneyTankEnemy) |  | Yes |  |

## Attacks

| Attack ID | Mode | Authoring Projectile/Beam | Runtime Projectile/Beam | Match | Notes |
| --------- | ---- | ------------------------- | ----------------------- | ----- | ----- |
| attack.idle-auto-defense.shard-projectile | Projectile | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyShardProjectile.prefab (KenneyShardProjectile) | KenneyShardProjectile / Kenney Projectile Runtime Prefab projectile.idle-auto-defense.shard(Clone) | Yes |  |
| attack.idle-auto-defense.pulse-beam | Hitscan | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/PulseBeamVfx.prefab (PulseBeamVfx) |  | Yes |  |
| attack.idle-auto-defense.arc-burst | Area |  |  | Yes |  |
| attack.idle-auto-defense.homing-pulse | Projectile | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyHomingProjectile.prefab (KenneyHomingProjectile) |  | Yes |  |

## VFX

| VFX ID | Authoring Prefab/Material | Runtime Object | Match | Notes |
| ------ | ------------------------- | -------------- | ----- | ----- |
| attack.idle-auto-defense.shard-projectile.delivery.impact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.shard-projectile.delivery.projectile | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyShardProjectile.prefab (KenneyShardProjectile) | KenneyShardProjectile / Kenney Projectile Runtime Prefab projectile.idle-auto-defense.shard(Clone) | Yes |  |
| attack.idle-auto-defense.shard-projectile.presentation.OnCast | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyPlacementRingVfx.prefab (KenneyPlacementRingVfx) |  | Yes |  |
| attack.idle-auto-defense.shard-projectile.presentation.OnFire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.shard-projectile.presentation.OnImpact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.shard-projectile.presentation.OnTick | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.shard-projectile.presentation.OnExpire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.pulse-beam.delivery.impact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.pulse-beam.delivery.beam | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/PulseBeamVfx.prefab (PulseBeamVfx) |  | Yes |  |
| attack.idle-auto-defense.pulse-beam.presentation.OnCast | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/PulseBeamVfx.prefab (PulseBeamVfx) |  | Yes |  |
| attack.idle-auto-defense.pulse-beam.presentation.OnFire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/PulseBeamVfx.prefab (PulseBeamVfx) |  | Yes |  |
| attack.idle-auto-defense.pulse-beam.presentation.OnImpact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.pulse-beam.presentation.OnTick | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.pulse-beam.presentation.OnExpire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.arc-burst.delivery.impact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.arc-burst.presentation.OnCast | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyPlacementRingVfx.prefab (KenneyPlacementRingVfx) |  | Yes |  |
| attack.idle-auto-defense.arc-burst.presentation.OnFire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.arc-burst.presentation.OnImpact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.arc-burst.presentation.OnTick | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.arc-burst.presentation.OnExpire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.homing-pulse.delivery.impact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.homing-pulse.delivery.projectile | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyHomingProjectile.prefab (KenneyHomingProjectile) |  | Yes |  |
| attack.idle-auto-defense.homing-pulse.presentation.OnCast | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyPlacementRingVfx.prefab (KenneyPlacementRingVfx) |  | Yes |  |
| attack.idle-auto-defense.homing-pulse.presentation.OnFire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.homing-pulse.presentation.OnImpact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.homing-pulse.presentation.OnTick | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.idle-auto-defense.homing-pulse.presentation.OnExpire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| enemy.idle-auto-defense.swarm.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.idle-auto-defense.swarm.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.idle-auto-defense.swarm.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |
| enemy.idle-auto-defense.runner.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.idle-auto-defense.runner.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.idle-auto-defense.runner.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |
| enemy.idle-auto-defense.tank.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.idle-auto-defense.tank.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.idle-auto-defense.tank.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |
| enemy.idle-auto-defense.shielded.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.idle-auto-defense.shielded.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.idle-auto-defense.shielded.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |
| enemy.idle-auto-defense.elite.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.idle-auto-defense.elite.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.idle-auto-defense.elite.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |
| enemy.idle-auto-defense.boss.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.idle-auto-defense.boss.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.idle-auto-defense.boss.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |

## Upgrades

| Upgrade ID | Authoring Effect | Runtime Effect | Visible? | Match |
| ---------- | ---------------- | -------------- | -------- | ----- |
| upgrade.idle-auto-defense.damage-up | AttackDamage Additive 1.5 -> weapon.idle-auto-defense.shard-launcher | AttackDamage Additive 1.5 -> weapon.idle-auto-defense.shard-launcher | Reward feedback authored through content-set reward catalog | Partial |
| upgrade.idle-auto-defense.fire-rate-up | AttackRate Additive 1 -> weapon.idle-auto-defense.shard-launcher | AttackRate Additive 1 -> weapon.idle-auto-defense.shard-launcher | Reward feedback authored through content-set reward catalog | Partial |
| upgrade.idle-auto-defense.range-up | Range Additive 1.25 -> weapon.idle-auto-defense.shard-launcher | Range Additive 1.25 -> weapon.idle-auto-defense.shard-launcher | Reward feedback authored through content-set reward catalog | Partial |
| upgrade.idle-auto-defense.projectile-speed-up | ProjectileSpeed Multiplicative 0.35 -> weapon.idle-auto-defense.shard-launcher | ProjectileSpeed Multiplicative 0.35 -> weapon.idle-auto-defense.shard-launcher | Reward feedback authored through content-set reward catalog | Partial |
| upgrade.idle-auto-defense.core-reinforcement | WeaponStat Additive 6 -> objective.idle-auto-defense.core | WeaponStat Additive 6 -> objective.idle-auto-defense.core | Reward feedback authored through content-set reward catalog | Partial |
| upgrade.idle-auto-defense.credit-reward | EnemyReward Multiplicative 0.15 -> reward.idle-auto-defense.run | EnemyReward Multiplicative 0.15 -> reward.idle-auto-defense.run | Reward feedback authored through content-set reward catalog | Partial |

