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
    internal sealed class IdleAutoDefenseCombatFeedback
    {
        private readonly IIdleAutoDefenseCombatEnemies _enemies;
        private readonly IIdleAutoDefenseCombatPresentation _presentation;
        private readonly IdleAutoDefenseCombatStatistics _stats;
        private readonly IdleAutoDefenseCombatContent _content;
        private readonly IdleAutoDefenseCombatRules _rules;
        private readonly Action<AutoDefenseEnemySnapshot> _reward;
        private readonly HashSet<long> _seenEnemyIds = new HashSet<long>();
        private readonly HashSet<long> _enemyDeathPresentationIds = new HashSet<long>();

        internal IdleAutoDefenseCombatFeedback(IIdleAutoDefenseCombatEnemies enemies, IIdleAutoDefenseCombatPresentation presentation,
            IdleAutoDefenseCombatStatistics stats, IdleAutoDefenseCombatContent content, IdleAutoDefenseCombatRules rules,
            Action<AutoDefenseEnemySnapshot> reward)
        {
            _enemies = enemies;
            _presentation = presentation;
            _stats = stats;
            _content = content;
            _rules = rules;
            _reward = reward;
        }

        internal void Clear()
        {
            _seenEnemyIds.Clear();
            _enemyDeathPresentationIds.Clear();
        }

        internal void EmitDirectWeaponPresentation(WeaponFireResult fireResult, AutoDefenseRuntimeSnapshot beforeCombat, AutoDefenseRuntimeSnapshot afterCombat)
        {
            if (fireResult == null) return;
            for (int i = 0; i < fireResult.Intents.Count; i++)
            {
                WeaponIntent intent = fireResult.Intents[i];
                if (intent.Kind != WeaponIntentKind.DirectAttack || intent.AttackIntent == null) continue;

                AttackDefinitionAsset attack = _content.FindAttackRecipeForPresentation(intent.AttackIntent.DefinitionId.Value);
                CombatantId targetId = intent.AttackIntent.Selection.Target.CombatantId;
                bool hadBefore = IdleAutoDefenseCombatTargets.TryFindEnemyByCombatant(beforeCombat, targetId, out AutoDefenseEnemySnapshot beforeTarget);
                bool hasAfter = IdleAutoDefenseCombatTargets.TryFindEnemyByCombatant(afterCombat, targetId, out AutoDefenseEnemySnapshot afterTarget);
                AutoDefenseEnemySnapshot target = hasAfter ? afterTarget : beforeTarget;
                Vector3 targetPosition = hadBefore || hasAfter ? IdleAutoDefenseCombatTargets.CreateEnemyAimPosition(target.Position) : Vector3.zero;

                Vector3 origin = _presentation.ResolveTowerMuzzlePosition(attack);
                _presentation.PlayWeaponFirePresentation(attack, targetPosition);
                _presentation.EmitAttackEvent(attack, AttackPresentationEventKind.OnCast, origin);
                _presentation.EmitAttackEvent(attack, AttackPresentationEventKind.OnFire, origin);
                if (hadBefore || hasAfter)
                    _presentation.EmitAttackTracer(origin, targetPosition, _presentation.ResolveAttackColor(attack));
                _presentation.EmitAttackEvent(attack, AttackPresentationEventKind.OnImpact, targetPosition, target.Id);
                if (hadBefore)
                {
                    double damage = hasAfter
                        ? Math.Max(0d, beforeTarget.Health - afterTarget.Health)
                        : Math.Max(_rules.ResolveAttackDamage(attack), beforeTarget.Health);
                    _presentation.EmitDamageNumber(targetPosition, damage, _presentation.ResolveAttackColor(attack), "-");
                }

                if (hasAfter && afterTarget.Lifecycle == AutoDefenseEnemyLifecycle.Active)
                    _presentation.EmitEnemyPresentationEvent(afterTarget, EnemyPresentationEventKind.OnHit);
                else if (hadBefore)
                    EmitEnemyDeathFeedback(beforeTarget);
            }
        }

        internal void EmitMissingKillFeedback(AutoDefenseRuntimeSnapshot beforeCombat, AutoDefenseRuntimeSnapshot afterCombat, int maxKills, AttackDefinitionAsset attack)
        {
            if (maxKills <= 0 || beforeCombat == null) return;
            int emitted = 0;
            for (int i = 0; i < beforeCombat.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = beforeCombat.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (IdleAutoDefenseCombatTargets.TryFindEnemy(afterCombat, enemy.Id, out AutoDefenseEnemySnapshot afterEnemy) &&
                    afterEnemy.Lifecycle == AutoDefenseEnemyLifecycle.Active)
                {
                    continue;
                }

                if (attack != null)
                {
                    Vector3 origin = _presentation.ResolveTowerMuzzlePosition(attack);
                    Vector3 destination = IdleAutoDefenseCombatTargets.CreateEnemyAimPosition(enemy.Position);
                    _presentation.PlayWeaponFirePresentation(attack, destination);
                    _presentation.EmitAttackTracer(origin, destination, _presentation.ResolveAttackColor(attack));
                    _presentation.EmitAttackEvent(attack, AttackPresentationEventKind.OnImpact, IdleAutoDefenseCombatTargets.CreateEnemyAimPosition(enemy.Position), enemy.Id);
                }

                _presentation.EmitDamageNumber(IdleAutoDefenseCombatTargets.CreateEnemyAimPosition(enemy.Position), Math.Max(_rules.ResolveAttackDamage(attack), enemy.Health), _presentation.ResolveAttackColor(attack), "-");
                EmitEnemyDeathFeedback(enemy);
                _reward(enemy);
                emitted++;
                if (emitted >= maxKills) return;
            }
        }

        internal void EmitSpawnFeedbackForNewEnemies()
        {
            if (!_enemies.IsAvailable) return;
            AutoDefenseRuntimeSnapshot snapshot = _enemies.CreateSnapshot();
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                if (!_seenEnemyIds.Add(enemy.Id)) continue;
                float distance = Vector3.Distance(enemy.Position, Vector3.zero);
                _stats.MinimumSpawnDistance = Mathf.Min(_stats.MinimumSpawnDistance, distance);
                if (distance > IdleAutoDefenseCombatDefaults.ManualTowerBaseRange + 0.5d)
                    _stats.EnemiesSpawnedBeyondStartingRangeCount++;
                if (_rules.IsEliteEnemy(enemy) || _rules.IsBossEnemy(enemy))
                    _stats.EliteOrBossSpawnCount++;
                _presentation.EmitEnemyPresentationEvent(enemy, EnemyPresentationEventKind.OnSpawn);
            }
        }

        internal void ObserveEnemyPressure(AutoDefenseRuntimeSnapshot snapshot)
        {
            if (snapshot == null) return;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot enemy = snapshot.Enemies[i];
                if (enemy.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                _stats.ClosestObjectiveDistance = Mathf.Min(_stats.ClosestObjectiveDistance, Vector3.Distance(enemy.Position, Vector3.zero));
            }
        }

        internal void EmitEnemyDeathFeedback(AutoDefenseEnemySnapshot enemy)
        {
            if (enemy.Id <= 0 || !_enemyDeathPresentationIds.Add(enemy.Id)) return;
            _presentation.EmitEnemyPresentationEvent(enemy, EnemyPresentationEventKind.OnDeath);
        }

        internal void RecordProjectileVisualSpawn(AttackDefinitionAsset attack)
        {
            _stats.ProjectileVisualSpawnCount++;
            if (attack != null && attack.Delivery != null && attack.Delivery.ProjectilePrefab != null)
                _stats.AuthoredProjectileVisualSpawnCount++;
        }
    }
}
