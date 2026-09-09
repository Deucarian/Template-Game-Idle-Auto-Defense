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
    internal sealed class IdleAutoDefenseCombatProjectiles
    {
        private readonly IIdleAutoDefenseCombatEnemies _enemies;
        private readonly IIdleAutoDefenseCombatProjectiles _projectiles;
        private readonly IIdleAutoDefenseCombatPresentation _presentation;
        private readonly IdleAutoDefenseCombatStatistics _stats;
        private readonly IdleAutoDefenseCombatTargets _targets;
        private readonly IdleAutoDefenseCombatContent _content;
        private readonly IdleAutoDefenseCombatRules _rules;
        private readonly IdleAutoDefenseVisibleDamage _damage;
        private readonly IdleAutoDefenseCombatFeedback _feedback;
        private readonly Func<string> _objectiveId;
        private readonly List<PendingProjectileImpact> _pendingProjectileImpacts = new List<PendingProjectileImpact>();
        private readonly Dictionary<long, Vector3> _lastProjectileAgentPositions = new Dictionary<long, Vector3>();
        internal int PendingCount => _pendingProjectileImpacts.Count;

        internal IdleAutoDefenseCombatProjectiles(IIdleAutoDefenseCombatEnemies enemies, IIdleAutoDefenseCombatProjectiles projectiles,
            IIdleAutoDefenseCombatPresentation presentation, IdleAutoDefenseCombatStatistics stats, IdleAutoDefenseCombatTargets targets,
            IdleAutoDefenseCombatContent content, IdleAutoDefenseCombatRules rules, IdleAutoDefenseVisibleDamage damage,
            IdleAutoDefenseCombatFeedback feedback, Func<string> objectiveId)
        {
            _enemies = enemies;
            _projectiles = projectiles;
            _presentation = presentation;
            _stats = stats;
            _targets = targets;
            _content = content;
            _rules = rules;
            _damage = damage;
            _feedback = feedback;
            _objectiveId = objectiveId;
        }

        internal void Clear()
        {
            _pendingProjectileImpacts.Clear();
            _lastProjectileAgentPositions.Clear();
        }

        internal void QueueImpact(ProjectileInstanceId projectileId, long targetId, AttackDefinitionAsset attack,
            Vector3 destination, double damage, int delayTicks)
        {
            _pendingProjectileImpacts.Add(new PendingProjectileImpact(projectileId, targetId, attack, destination, damage, delayTicks));
        }

        internal void LaunchFromWeaponResult(IReadOnlyList<ProjectileLaunchRequest> requests, AutoDefenseRuntimeSnapshot snapshot)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                ProjectileLaunchRequest request = CreateVisibleProjectileLaunchRequest(requests[i], snapshot,
                    out AttackDefinitionAsset attack, out AutoDefenseEnemySnapshot target, out int delay, out double damage);
                _presentation.EmitAttackEvent(attack, AttackPresentationEventKind.OnCast, request.Origin);
                _presentation.EmitAttackEvent(attack, AttackPresentationEventKind.OnFire, request.Origin);
                ProjectileLaunchResult launch = _projectiles.Launch(request);
                if (!launch.Succeeded) continue;
                _stats.ProjectileLaunchCount++;
                _feedback.RecordProjectileVisualSpawn(attack);
                QueueImpact(launch.ProjectileId, target.Id, attack, request.Destination, damage, delay);
            }
        }

        internal ProjectileLaunchRequest CreateVisibleProjectileLaunchRequest(
            ProjectileLaunchRequest original,
            AutoDefenseRuntimeSnapshot snapshot,
            out AttackDefinitionAsset attack,
            out AutoDefenseEnemySnapshot target,
            out int impactDelayTicks,
            out double damageThreshold)
        {
            attack = _content.FindAttackRecipeForPresentation(original.AttackDefinitionId.Value);
            if (!_targets.TrySelectAttackTarget(attack, snapshot, out target))
                target = default;

            Vector3 origin = _presentation.ResolveTowerMuzzlePosition(attack);
            Vector3 destination = target.Id > 0
                ? IdleAutoDefenseCombatTargets.CreateEnemyAimPosition(target.Position)
                : (original.Destination == Vector3.zero ? origin + Vector3.forward * 4f : original.Destination);
            if (target.Id > 0)
                _presentation.PlayWeaponFirePresentation(attack, destination);
            _stats.MuzzleProjectileLaunchCount++;
            ProjectileDefinition projectile = _content.FindProjectileDefinition(original.DefinitionId);
            float speed = projectile == null ? IdleAutoDefenseCombatRules.ResolveProjectileSpeed(attack) : projectile.Speed;
            impactDelayTicks = _rules.CalculateProjectileImpactDelayTicks(origin, destination, speed);
            damageThreshold = projectile == null ? _rules.ResolveAttackDamage(attack) : projectile.BaseDamage;

            return new ProjectileLaunchRequest(
                original.DefinitionId,
                original.AttackSourceId,
                original.AttackDefinitionId,
                original.Source,
                origin,
                destination,
                original.Path);
        }

        internal int ResolvePendingProjectileImpacts(int ticks)
        {
            if (_pendingProjectileImpacts.Count == 0) return 0;
            int kills = 0;
            int step = Math.Max(1, ticks);
            for (int i = _pendingProjectileImpacts.Count - 1; i >= 0; i--)
            {
                PendingProjectileImpact pending = _pendingProjectileImpacts[i];
                pending.RemainingTicks -= step;
                if (pending.RemainingTicks > 0)
                {
                    _pendingProjectileImpacts[i] = pending;
                    continue;
                }

                _pendingProjectileImpacts.RemoveAt(i);
                bool hasImpactTarget = TryFindProjectileImpactTarget(pending, out AutoDefenseEnemySnapshot impactTarget);
                Vector3 impactPosition = hasImpactTarget
                    ? IdleAutoDefenseCombatTargets.CreateEnemyAimPosition(impactTarget.Position)
                    : pending.Destination;

                if (!hasImpactTarget)
                {
                    _stats.ProjectileImpactMissCount++;
                    _presentation.EmitAttackEvent(pending.Attack, AttackPresentationEventKind.OnExpire, impactPosition);
                    CleanupProjectileWithoutDamage(pending.ProjectileId);
                    continue;
                }

                if (!TryReportProjectileImpact(pending, impactTarget, out _))
                {
                    _stats.ProjectileImpactRejectedCount++;
                    _presentation.EmitAttackEvent(pending.Attack, AttackPresentationEventKind.OnExpire, impactPosition);
                    continue;
                }

                _stats.ProjectileImpactCallbackCount++;
                _presentation.EmitAttackEvent(pending.Attack, AttackPresentationEventKind.OnImpact, impactPosition, impactTarget.Id);
                _stats.ProjectileDamageResolvedFromImpactCount++;
                _stats.ProjectileDamageAppliedCount++;
                if (!_damage.TryApplyVisibleEnemyDamage(impactTarget, pending.Attack, pending.DamageThreshold, impactPosition, out bool killed))
                    continue;
                if (killed)
                {
                    _stats.ProjectileAdapterKillCount++;
                    kills++;
                }
            }

            return kills;
        }

        internal bool TryReportProjectileImpact(
            PendingProjectileImpact pending,
            AutoDefenseEnemySnapshot impactTarget,
            out ProjectileImpactResult result)
        {
            result = default;
            if (!_projectiles.IsAvailable || pending.ProjectileId.Value <= 0 || impactTarget.Id <= 0 || impactTarget.CombatantId.IsEmpty)
                return false;

            double currentHealth = Math.Max(0.01d, impactTarget.Health);
            double maximumHealth = Math.Max(currentHealth, Math.Max(1d, _rules.ResolveAttackDamage(pending.Attack)));
            var targetHealth = new HealthState(impactTarget.CombatantId, maximumHealth, currentHealth);
            result = _projectiles.ReportImpact(new ProjectileImpactRequest(pending.ProjectileId, impactTarget.CombatantId, targetHealth));
            return result.Succeeded;
        }

        internal void CleanupProjectileWithoutDamage(ProjectileInstanceId projectileId)
        {
            if (!_projectiles.IsAvailable || projectileId.Value <= 0) return;
            _projectiles.Cleanup(projectileId, ProjectileExpiryReason.ManualCleanup);
        }

        internal void EmitProjectileExpiryFeedback(ProjectileTickResult result)
        {
            if (result == null || result.Expiries.Count == 0) return;
            for (int i = 0; i < result.Expiries.Count; i++)
            {
                ProjectileExpiryEvent expiry = result.Expiries[i];
                int pendingIndex = FindPendingProjectileImpactIndex(expiry.ProjectileId);
                if (pendingIndex < 0) continue;
                _stats.ProjectileExpiryDeferralCount++;
            }
        }

        internal bool TryLaunchVisibleProjectileAtEnemy(AutoDefenseEnemySnapshot enemy, AttackDefinitionAsset attack, double damageAmount)
        {
            if (!_projectiles.IsAvailable || attack == null || attack.Delivery == null) return false;
            if (attack.Delivery.Mode != AttackRecipeDeliveryMode.Projectile) return false;
            if (string.IsNullOrWhiteSpace(attack.Delivery.ProjectileDefinitionId)) return false;
            ProjectileDefinition projectile = _content.FindProjectileDefinition(new ProjectileDefinitionId(attack.Delivery.ProjectileDefinitionId));
            if (projectile == null) return false;

            Vector3 origin = _presentation.ResolveTowerMuzzlePosition(attack);
            Vector3 destination = IdleAutoDefenseCombatTargets.CreateEnemyAimPosition(enemy.Position);
            _presentation.PlayWeaponFirePresentation(attack, destination);
            _stats.MuzzleProjectileLaunchCount++;
            int impactDelayTicks = _rules.CalculateProjectileImpactDelayTicks(origin, destination, projectile.Speed);
            var launchRequest = new ProjectileLaunchRequest(
                projectile.Id,
                new AttackSourceId("source.idle-auto-defense.visual." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(attack.Id)),
                new AttackDefinitionId(attack.Id),
                new AttackSourceSnapshot(new AttackSourceId("source.idle-auto-defense.visual." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(attack.Id)), new CombatantId(_objectiveId())),
                origin,
                destination);

            _presentation.EmitAttackEvent(attack, AttackPresentationEventKind.OnCast, origin);
            _presentation.EmitAttackEvent(attack, AttackPresentationEventKind.OnFire, origin);
            ProjectileLaunchResult launch = _projectiles.Launch(launchRequest);
            if (!launch.Succeeded) return false;

            _stats.ProjectileLaunchCount++;
            _feedback.RecordProjectileVisualSpawn(attack);
            _pendingProjectileImpacts.Add(new PendingProjectileImpact(
                launch.ProjectileId,
                enemy.Id,
                attack,
                destination,
                damageAmount,
                impactDelayTicks));
            return true;
        }

        internal bool TryFindProjectileImpactTarget(PendingProjectileImpact pending, out AutoDefenseEnemySnapshot target)
        {
            if (_targets.TryFindActiveEnemy(pending.TargetEnemyId, out target))
                return true;

            target = default;
            if (!_enemies.IsAvailable) return false;
            AutoDefenseRuntimeSnapshot snapshot = _enemies.CreateSnapshot();
            float bestDistance = float.MaxValue;
            bool found = false;
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                AutoDefenseEnemySnapshot candidate = snapshot.Enemies[i];
                if (candidate.Lifecycle != AutoDefenseEnemyLifecycle.Active) continue;
                float distance = Vector3.Distance(IdleAutoDefenseCombatTargets.CreateEnemyAimPosition(candidate.Position), pending.Destination);
                if (distance > _rules.ProjectileRetargetRadius) continue;
                if (found && distance >= bestDistance) continue;
                target = candidate;
                bestDistance = distance;
                found = true;
            }

            if (found)
                _stats.ProjectileImpactRetargetCount++;
            return found;
        }

        internal int FindPendingProjectileImpactIndex(ProjectileInstanceId projectileId)
        {
            for (int i = 0; i < _pendingProjectileImpacts.Count; i++)
                if (_pendingProjectileImpacts[i].ProjectileId.Equals(projectileId))
                    return i;
            return -1;
        }

        internal void ObserveProjectileMotion()
        {
            if (!_projectiles.IsNavigationAvailable) return;
            MovementSnapshot snapshot = _projectiles.CreateMovementSnapshot();
            var activeIds = new HashSet<long>();
            for (int i = 0; i < snapshot.Agents.Count; i++)
            {
                MovementAgentSnapshot agent = snapshot.Agents[i];
                long id = agent.Id.Value;
                activeIds.Add(id);
                if (_lastProjectileAgentPositions.TryGetValue(id, out Vector3 previous) &&
                    Vector3.Distance(previous, agent.Position) > 0.01f)
                {
                    _stats.ProjectileMotionObservedCount++;
                }

                _lastProjectileAgentPositions[id] = agent.Position;
            }

            var staleIds = new List<long>();
            foreach (long id in _lastProjectileAgentPositions.Keys)
                if (!activeIds.Contains(id))
                    staleIds.Add(id);
            for (int i = 0; i < staleIds.Count; i++)
                _lastProjectileAgentPositions.Remove(staleIds[i]);
        }
    }
}
