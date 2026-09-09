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
    internal sealed class IdleAutoDefenseVisibleDamage
    {
        private readonly IIdleAutoDefenseCombatEnemies _enemies;
        private readonly IIdleAutoDefenseCombatPresentation _presentation;
        private readonly IdleAutoDefenseCombatStatistics _stats;
        private readonly IdleAutoDefenseCombatTargets _targets;
        private readonly IdleAutoDefenseCombatFeedback _feedback;
        private readonly Action<AutoDefenseEnemySnapshot> _reward;
        private readonly Dictionary<long, double> _sampleEnemyDamageById = new Dictionary<long, double>();

        internal IdleAutoDefenseVisibleDamage(IIdleAutoDefenseCombatEnemies enemies, IIdleAutoDefenseCombatPresentation presentation,
            IdleAutoDefenseCombatStatistics stats, IdleAutoDefenseCombatTargets targets, IdleAutoDefenseCombatFeedback feedback,
            Action<AutoDefenseEnemySnapshot> reward)
        {
            _enemies = enemies;
            _presentation = presentation;
            _stats = stats;
            _targets = targets;
            _feedback = feedback;
            _reward = reward;
        }

        internal void Clear() => _sampleEnemyDamageById.Clear();

        internal bool TryDamageEnemyWithPresentation(AutoDefenseEnemySnapshot enemy, AttackDefinitionAsset attack, double damageAmount, out bool killed)
        {
            killed = false;
            Vector3 origin = _presentation.ResolveTowerMuzzlePosition(attack);
            Vector3 destination = IdleAutoDefenseCombatTargets.CreateEnemyAimPosition(enemy.Position);
            _presentation.PlayWeaponFirePresentation(attack, destination);
            _presentation.EmitAttackEvent(attack, AttackPresentationEventKind.OnCast, origin);
            _presentation.EmitAttackEvent(attack, AttackPresentationEventKind.OnFire, origin);
            _presentation.EmitAttackTracer(origin, destination, _presentation.ResolveAttackColor(attack));
            _presentation.EmitAttackEvent(attack, AttackPresentationEventKind.OnImpact, destination, enemy.Id);
            return TryApplyVisibleEnemyDamage(enemy, attack, damageAmount, destination, out killed);
        }

        internal bool TryKillEnemyAfterFeedback(AutoDefenseEnemySnapshot enemy)
        {
            if (!_enemies.IsAvailable || enemy.Id <= 0 || !_enemies.TryKillEnemy(enemy.Id)) return false;
            _feedback.EmitEnemyDeathFeedback(enemy);
            _sampleEnemyDamageById.Remove(enemy.Id);
            _reward(enemy);
            return true;
        }

        internal bool TryApplyVisibleEnemyDamage(AutoDefenseEnemySnapshot enemy, AttackDefinitionAsset attack, double damageAmount, Vector3 impactPosition, out bool killed)
        {
            killed = false;
            if (!_enemies.IsAvailable || enemy.Id <= 0 || damageAmount <= 0d) return false;
            if (!_targets.TryFindActiveEnemy(enemy.Id, out AutoDefenseEnemySnapshot activeEnemy)) return false;

            double previousDamage = _sampleEnemyDamageById.TryGetValue(activeEnemy.Id, out double storedDamage) ? storedDamage : 0d;
            double remainingBefore = Math.Max(0d, activeEnemy.Health - previousDamage);
            double appliedDamage = Math.Max(0.25d, damageAmount);
            _sampleEnemyDamageById[activeEnemy.Id] = previousDamage + appliedDamage;

            _presentation.EmitEnemyPresentationEvent(activeEnemy, EnemyPresentationEventKind.OnHit);
            _presentation.EmitDamageNumber(impactPosition, appliedDamage, _presentation.ResolveAttackColor(attack), "-");

            if (remainingBefore - appliedDamage > 0.001d)
            {
                _stats.EnemyDamageSurvivedCount++;
                return true;
            }
            if (!TryKillEnemyAfterFeedback(activeEnemy)) return true;
            killed = true;
            return true;
        }
    }
}
