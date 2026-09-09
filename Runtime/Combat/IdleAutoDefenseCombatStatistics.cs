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
    internal sealed class IdleAutoDefenseCombatStatistics
    {
        internal int DirectOrCombatKillCount;
        internal int ProjectileLaunchCount;
        internal int ProjectileAdapterKillCount;
        internal int ProjectileVisualSpawnCount;
        internal int AuthoredProjectileVisualSpawnCount;
        internal int ProjectileMotionObservedCount;
        internal int ProjectileDamageAppliedCount;
        internal int ProjectileImpactCallbackCount;
        internal int ProjectileDamageResolvedFromImpactCount;
        internal int ProjectileImpactRetargetCount;
        internal int ProjectileImpactMissCount;
        internal int ProjectileImpactRejectedCount;
        internal int ProjectileExpiryDeferralCount;
        internal int MuzzleProjectileLaunchCount;
        internal int EnemyDamageSurvivedCount;
        internal int RangeRejectedTargetCount;
        internal int EnemiesSpawnedBeyondStartingRangeCount;
        internal int EliteOrBossSpawnCount;
        internal int ModuleActivationCount;
        internal float MinimumSpawnDistance = float.MaxValue;
        internal float ClosestObjectiveDistance = float.MaxValue;

        internal void RecordDirectKills(int count) => DirectOrCombatKillCount += count;

        internal void Reset()
        {
            DirectOrCombatKillCount = 0;
            ProjectileLaunchCount = 0;
            ProjectileAdapterKillCount = 0;
            ProjectileVisualSpawnCount = 0;
            AuthoredProjectileVisualSpawnCount = 0;
            ProjectileMotionObservedCount = 0;
            ProjectileDamageAppliedCount = 0;
            ProjectileImpactCallbackCount = 0;
            ProjectileDamageResolvedFromImpactCount = 0;
            ProjectileImpactRetargetCount = 0;
            ProjectileImpactMissCount = 0;
            ProjectileImpactRejectedCount = 0;
            ProjectileExpiryDeferralCount = 0;
            MuzzleProjectileLaunchCount = 0;
            EnemyDamageSurvivedCount = 0;
            RangeRejectedTargetCount = 0;
            EnemiesSpawnedBeyondStartingRangeCount = 0;
            EliteOrBossSpawnCount = 0;
            ModuleActivationCount = 0;
            MinimumSpawnDistance = float.MaxValue;
            ClosestObjectiveDistance = float.MaxValue;
        }

    }
}
