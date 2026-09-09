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
    internal interface IIdleAutoDefenseCombatEnemies
    {
        bool IsAvailable { get; }
        AutoDefenseRuntimeSnapshot CreateSnapshot();
        bool TryKillEnemy(long id);
    }

    internal interface IIdleAutoDefenseCombatProjectiles
    {
        bool IsAvailable { get; }
        bool IsNavigationAvailable { get; }
        ProjectileLaunchResult Launch(ProjectileLaunchRequest request);
        ProjectileImpactResult ReportImpact(ProjectileImpactRequest request);
        void Cleanup(ProjectileInstanceId id, ProjectileExpiryReason reason);
        MovementSnapshot CreateMovementSnapshot();
    }

    internal interface IIdleAutoDefenseCombatPresentation
    {
        Vector3 ResolveTowerMuzzlePosition(AttackDefinitionAsset attack);
        Color ResolveAttackColor(AttackDefinitionAsset attack);
        void PlayWeaponFirePresentation(AttackDefinitionAsset attack, Vector3 target);
        void EmitAttackEvent(AttackDefinitionAsset attack, AttackPresentationEventKind kind, Vector3 position, long targetId = 0);
        void EmitAttackTracer(Vector3 origin, Vector3 destination, Color color);
        void EmitDamageNumber(Vector3 position, double amount, Color color, string prefix);
        void EmitEnemyPresentationEvent(AutoDefenseEnemySnapshot enemy, EnemyPresentationEventKind kind);
    }
}
