using System;
using System.Collections.Generic;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Projectiles;
using Deucarian.WeaponSystems;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public partial class IdleAutoDefenseTemplateController
    {
        public int DirectOrCombatKillCount { get => CombatRuntime.Statistics.DirectOrCombatKillCount; private set => CombatRuntime.Statistics.DirectOrCombatKillCount = value; }
        public int ProjectileLaunchCount { get => CombatRuntime.Statistics.ProjectileLaunchCount; private set => CombatRuntime.Statistics.ProjectileLaunchCount = value; }
        public int ProjectileAdapterKillCount { get => CombatRuntime.Statistics.ProjectileAdapterKillCount; private set => CombatRuntime.Statistics.ProjectileAdapterKillCount = value; }
        public int ProjectileVisualSpawnCount { get => CombatRuntime.Statistics.ProjectileVisualSpawnCount; private set => CombatRuntime.Statistics.ProjectileVisualSpawnCount = value; }
        public int AuthoredProjectileVisualSpawnCount { get => CombatRuntime.Statistics.AuthoredProjectileVisualSpawnCount; private set => CombatRuntime.Statistics.AuthoredProjectileVisualSpawnCount = value; }
        public int ProjectileMotionObservedCount { get => CombatRuntime.Statistics.ProjectileMotionObservedCount; private set => CombatRuntime.Statistics.ProjectileMotionObservedCount = value; }
        public int ProjectileDamageAppliedCount { get => CombatRuntime.Statistics.ProjectileDamageAppliedCount; private set => CombatRuntime.Statistics.ProjectileDamageAppliedCount = value; }
        public int ProjectileImpactCallbackCount { get => CombatRuntime.Statistics.ProjectileImpactCallbackCount; private set => CombatRuntime.Statistics.ProjectileImpactCallbackCount = value; }
        public int ProjectileDamageResolvedFromImpactCount { get => CombatRuntime.Statistics.ProjectileDamageResolvedFromImpactCount; private set => CombatRuntime.Statistics.ProjectileDamageResolvedFromImpactCount = value; }
        public int ProjectileImpactRetargetCount { get => CombatRuntime.Statistics.ProjectileImpactRetargetCount; private set => CombatRuntime.Statistics.ProjectileImpactRetargetCount = value; }
        public int ProjectileImpactMissCount { get => CombatRuntime.Statistics.ProjectileImpactMissCount; private set => CombatRuntime.Statistics.ProjectileImpactMissCount = value; }
        public int ProjectileImpactRejectedCount { get => CombatRuntime.Statistics.ProjectileImpactRejectedCount; private set => CombatRuntime.Statistics.ProjectileImpactRejectedCount = value; }
        public int ProjectileExpiryDeferralCount { get => CombatRuntime.Statistics.ProjectileExpiryDeferralCount; private set => CombatRuntime.Statistics.ProjectileExpiryDeferralCount = value; }
        public int MuzzleProjectileLaunchCount { get => CombatRuntime.Statistics.MuzzleProjectileLaunchCount; private set => CombatRuntime.Statistics.MuzzleProjectileLaunchCount = value; }
        public int EnemyDamageSurvivedCount { get => CombatRuntime.Statistics.EnemyDamageSurvivedCount; private set => CombatRuntime.Statistics.EnemyDamageSurvivedCount = value; }
        public int RangeRejectedTargetCount { get => CombatRuntime.Statistics.RangeRejectedTargetCount; private set => CombatRuntime.Statistics.RangeRejectedTargetCount = value; }
        public int EnemiesSpawnedBeyondStartingRangeCount { get => CombatRuntime.Statistics.EnemiesSpawnedBeyondStartingRangeCount; private set => CombatRuntime.Statistics.EnemiesSpawnedBeyondStartingRangeCount = value; }
        public int EliteOrBossSpawnCount { get => CombatRuntime.Statistics.EliteOrBossSpawnCount; private set => CombatRuntime.Statistics.EliteOrBossSpawnCount = value; }
        public int ModuleActivationCount { get => CombatRuntime.Statistics.ModuleActivationCount; private set => CombatRuntime.Statistics.ModuleActivationCount = value; }

        public float MinimumEnemySpawnDistance => CombatRuntime.Statistics.MinimumSpawnDistance == float.MaxValue ? 0f : CombatRuntime.Statistics.MinimumSpawnDistance;
        public float ClosestEnemyDistanceToObjective => CombatRuntime.Statistics.ClosestObjectiveDistance == float.MaxValue ? 0f : CombatRuntime.Statistics.ClosestObjectiveDistance;
    }
}
