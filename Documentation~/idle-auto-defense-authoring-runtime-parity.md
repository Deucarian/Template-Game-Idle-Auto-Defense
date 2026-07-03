# Idle Auto Defense Authoring Runtime Parity

Generated: 2026-07-03T21:45:16.8576075+02:00

Main content set: `Assets/GameContent/IdleAutoDefense/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset`

## Towers

| Content ID | Authoring Prefab | Runtime Prefab | Match | Notes |
| ---------- | ---------------- | -------------- | ----- | ----- |
| weapon.template.shard-launcher | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyShardLauncherWeapon.prefab (KenneyShardLauncherWeapon) | KenneyShardLauncherWeapon / Shard Launcher Authored Weapon Visual | Yes |  |
| weapon.template.pulse-cannon | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyPulseBeamWeapon.prefab (KenneyPulseBeamWeapon) |  | Yes |  |
| weapon.template.arc-burst-tower | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyArcBurstWeapon.prefab (KenneyArcBurstWeapon) |  | Yes |  |
| weapon.template.homing-spire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyHomingSpireWeapon.prefab (KenneyHomingSpireWeapon) |  | Yes |  |

## Enemies

| Content ID | Authoring Prefab | Runtime Prefab | Match | Notes |
| ---------- | ---------------- | -------------- | ----- | ----- |
| enemy.template.swarm | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneySwarmEnemy.prefab (KenneySwarmEnemy) | KenneySwarmEnemy / Kenney Enemy Runtime Prefab enemy.template.swarm(Clone) | Yes |  |
| enemy.template.runner | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyRunnerEnemy.prefab (KenneyRunnerEnemy) | KenneyRunnerEnemy / Kenney Enemy Runtime Prefab enemy.template.runner(Clone) | Yes |  |
| enemy.template.tank | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyTankEnemy.prefab (KenneyTankEnemy) | KenneyTankEnemy / Kenney Enemy Runtime Prefab enemy.template.tank(Clone) | Yes |  |
| enemy.template.shielded | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyTankEnemy.prefab (KenneyTankEnemy) |  | Yes |  |
| enemy.template.elite | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyTankEnemy.prefab (KenneyTankEnemy) |  | Yes |  |
| enemy.template.boss | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyTankEnemy.prefab (KenneyTankEnemy) |  | Yes |  |

## Attacks

| Attack ID | Mode | Authoring Projectile/Beam | Runtime Projectile/Beam | Match | Notes |
| --------- | ---- | ------------------------- | ----------------------- | ----- | ----- |
| attack.template.shard-launcher | Projectile | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyShardProjectile.prefab (KenneyShardProjectile) | KenneyShardProjectile / Kenney Projectile Runtime Prefab projectile.template.shard(Clone) | Yes |  |
| attack.template.pulse-cannon | Hitscan | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/PulseBeamVfx.prefab (PulseBeamVfx) |  | Yes |  |
| attack.template.arc-burst | Area |  |  | Yes |  |
| attack.template.homing-pulse | Projectile | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyHomingProjectile.prefab (KenneyHomingProjectile) |  | Yes |  |

## VFX

| VFX ID | Authoring Prefab/Material | Runtime Object | Match | Notes |
| ------ | ------------------------- | -------------- | ----- | ----- |
| attack.template.shard-launcher.delivery.impact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.shard-launcher.delivery.projectile | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyShardProjectile.prefab (KenneyShardProjectile) | KenneyShardProjectile / Kenney Projectile Runtime Prefab projectile.template.shard(Clone) | Yes |  |
| attack.template.shard-launcher.presentation.OnCast | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyPlacementRingVfx.prefab (KenneyPlacementRingVfx) |  | Yes |  |
| attack.template.shard-launcher.presentation.OnFire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.shard-launcher.presentation.OnImpact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.shard-launcher.presentation.OnTick | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.shard-launcher.presentation.OnExpire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.pulse-cannon.delivery.impact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.pulse-cannon.delivery.beam | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/PulseBeamVfx.prefab (PulseBeamVfx) |  | Yes |  |
| attack.template.pulse-cannon.presentation.OnCast | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/PulseBeamVfx.prefab (PulseBeamVfx) |  | Yes |  |
| attack.template.pulse-cannon.presentation.OnFire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/PulseBeamVfx.prefab (PulseBeamVfx) |  | Yes |  |
| attack.template.pulse-cannon.presentation.OnImpact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.pulse-cannon.presentation.OnTick | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.pulse-cannon.presentation.OnExpire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.arc-burst.delivery.impact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.arc-burst.presentation.OnCast | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyPlacementRingVfx.prefab (KenneyPlacementRingVfx) |  | Yes |  |
| attack.template.arc-burst.presentation.OnFire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.arc-burst.presentation.OnImpact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.arc-burst.presentation.OnTick | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.arc-burst.presentation.OnExpire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.homing-pulse.delivery.impact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.homing-pulse.delivery.projectile | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyHomingProjectile.prefab (KenneyHomingProjectile) |  | Yes |  |
| attack.template.homing-pulse.presentation.OnCast | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyPlacementRingVfx.prefab (KenneyPlacementRingVfx) |  | Yes |  |
| attack.template.homing-pulse.presentation.OnFire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.homing-pulse.presentation.OnImpact | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.homing-pulse.presentation.OnTick | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| attack.template.homing-pulse.presentation.OnExpire | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyImpactBurstVfx.prefab (KenneyImpactBurstVfx) |  | Yes |  |
| enemy.template.swarm.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.template.swarm.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.template.swarm.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |
| enemy.template.runner.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.template.runner.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.template.runner.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |
| enemy.template.tank.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.template.tank.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.template.tank.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |
| enemy.template.shielded.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.template.shielded.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.template.shielded.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |
| enemy.template.elite.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.template.elite.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.template.elite.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |
| enemy.template.boss.presentation.OnSpawn | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemySpawnVfx.prefab (KenneyEnemySpawnVfx) |  | Yes |  |
| enemy.template.boss.presentation.OnHit | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyHitVfx.prefab (KenneyEnemyHitVfx) |  | Yes |  |
| enemy.template.boss.presentation.OnDeath | Assets/IdleAutoDefenseAudit/Visuals/Prefabs/KenneyEnemyDeathVfx.prefab (KenneyEnemyDeathVfx) |  | Yes |  |

## Upgrades

| Upgrade ID | Authoring Effect | Runtime Effect | Visible? | Match |
| ---------- | ---------------- | -------------- | -------- | ----- |
| upgrade.template.authored.damage-up | AttackDamage Additive 1.5 -> weapon.template.shard-launcher | AttackDamage Additive 1.5 -> weapon.template.shard-launcher | Reward feedback authored through content-set reward catalog | Partial |
| upgrade.template.authored.fire-rate-up | AttackRate Additive 1 -> weapon.template.shard-launcher | AttackRate Additive 1 -> weapon.template.shard-launcher | Reward feedback authored through content-set reward catalog | Partial |
| upgrade.template.authored.range-up | Range Additive 1.25 -> weapon.template.shard-launcher | Range Additive 1.25 -> weapon.template.shard-launcher | Reward feedback authored through content-set reward catalog | Partial |
| upgrade.template.authored.projectile-speed-up | ProjectileSpeed Multiplicative 0.35 -> weapon.template.shard-launcher | ProjectileSpeed Multiplicative 0.35 -> weapon.template.shard-launcher | Reward feedback authored through content-set reward catalog | Partial |
| upgrade.template.authored.core-reinforcement | WeaponStat Additive 6 -> objective.template-core | WeaponStat Additive 6 -> objective.template-core | Reward feedback authored through content-set reward catalog | Partial |
| upgrade.template.authored.credit-reward | EnemyReward Multiplicative 0.15 -> reward.template.run | EnemyReward Multiplicative 0.15 -> reward.template.run | Reward feedback authored through content-set reward catalog | Partial |

