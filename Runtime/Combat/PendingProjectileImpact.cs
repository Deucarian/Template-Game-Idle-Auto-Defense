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
    internal struct PendingProjectileImpact
    {
        public PendingProjectileImpact(
            ProjectileInstanceId projectileId,
            long targetEnemyId,
            AttackDefinitionAsset attack,
            Vector3 destination,
            double damageThreshold,
            int remainingTicks)
        {
            ProjectileId = projectileId;
            TargetEnemyId = targetEnemyId;
            Attack = attack;
            Destination = destination;
            DamageThreshold = damageThreshold;
            RemainingTicks = remainingTicks;
        }

        public ProjectileInstanceId ProjectileId;
        public long TargetEnemyId;
        public AttackDefinitionAsset Attack;
        public Vector3 Destination;
        public double DamageThreshold;
        public int RemainingTicks;
    }
}
