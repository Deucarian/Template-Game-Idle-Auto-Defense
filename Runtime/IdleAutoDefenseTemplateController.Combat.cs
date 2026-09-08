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
        private IdleAutoDefenseCombatRuntime _combatRuntime;
        private IdleAutoDefenseCombatRuntime CombatRuntime => _combatRuntime ??= CreateCombatRuntime();

        private IdleAutoDefenseCombatRuntime CreateCombatRuntime()
        {
            var runtime = new IdleAutoDefenseCombatRuntimeAdapters(() => _runtime, () => _projectiles, () => _projectileNavigation);
            var presentation = new IdleAutoDefenseCombatPresentationAdapter(ResolveTowerMuzzlePosition, ResolveAttackColor,
                PlayWeaponFirePresentation, EmitAttackEvent, EmitAttackTracer, EmitDamageNumber, EmitEnemyPresentationEvent);
            return new IdleAutoDefenseCombatRuntime(runtime, runtime, presentation, RunBuild,
                () => _resolvedAttackRecipes, () => _resolvedEnemyDefinitions, () => _resolvedProjectileDefinitions,
                () => _activeGameRules, () => _activeRunProfile, () => RewardDraftSettings,
                () => RuntimeObjectiveId, RecordEnemyDefeatedForRewards);
        }

        internal AttackDefinitionAsset FindAttackRecipeForPresentation(string attackId) => CombatRuntime.Content.FindAttackRecipeForPresentation(attackId);

        private AttackDefinitionAsset FindAttackRecipeForProjectile(ProjectileDefinition definition) => CombatRuntime.Content.FindAttackRecipeForProjectile(definition);

        private EnemyDefinitionAsset FindEnemyDefinitionForPresentation(WorldSpawnableId spawnableId) => CombatRuntime.Content.FindEnemyDefinitionForPresentation(spawnableId);

        private ProjectileDefinition FindProjectileDefinition(ProjectileDefinitionId id) => CombatRuntime.Content.FindProjectileDefinition(id);

        private double ResolveManualTowerDamage() => CombatRuntime.Rules.ResolveManualTowerDamage();

        private double ResolveManualTowerRange() => CombatRuntime.Rules.ResolveManualTowerRange();

        private double ResolveModuleRange(double baseRange) => CombatRuntime.Rules.ResolveModuleRange(baseRange);

        private double ResolveModuleDamage(double baseDamage) => CombatRuntime.Rules.ResolveModuleDamage(baseDamage);

        private bool IsEliteEnemy(AutoDefenseEnemySnapshot enemy) => CombatRuntime.Rules.IsEliteEnemy(enemy);

        private bool IsBossEnemy(AutoDefenseEnemySnapshot enemy) => CombatRuntime.Rules.IsBossEnemy(enemy);

        private int CalculateProjectileImpactDelayTicks(Vector3 origin, Vector3 destination, float speed) => CombatRuntime.Rules.CalculateProjectileImpactDelayTicks(origin, destination, speed);

        private static float ResolveProjectileSpeed(AttackDefinitionAsset attack) => IdleAutoDefenseCombatRules.ResolveProjectileSpeed(attack);

        private double ResolveAttackDamage(AttackDefinitionAsset attack) => CombatRuntime.Rules.ResolveAttackDamage(attack);

        private double ResolvePresentationRange(AttackDefinitionAsset attack) => CombatRuntime.Rules.ResolvePresentationRange(attack);

        private bool TrySelectPriorityEnemy(AutoDefenseRuntimeSnapshot snapshot, out AutoDefenseEnemySnapshot selected) => CombatRuntime.Targets.TrySelectPriorityEnemy(snapshot, out selected);

        private bool TrySelectPriorityEnemyWithinRange(AutoDefenseRuntimeSnapshot snapshot, double range, out AutoDefenseEnemySnapshot selected) => CombatRuntime.Targets.TrySelectPriorityEnemyWithinRange(snapshot, range, out selected);

        private bool TrySelectAttackTarget(AttackDefinitionAsset attack, AutoDefenseRuntimeSnapshot snapshot, out AutoDefenseEnemySnapshot selected) => CombatRuntime.Targets.TrySelectAttackTarget(attack, snapshot, out selected);

        private static bool IsBetterTarget(AutoDefenseEnemySnapshot candidate, AutoDefenseEnemySnapshot current, AttackRecipeTargetingMode mode) => IdleAutoDefenseCombatTargets.IsBetterTarget(candidate, current, mode);

        private bool TryFindActiveEnemy(long enemyId, out AutoDefenseEnemySnapshot enemy) => CombatRuntime.Targets.TryFindActiveEnemy(enemyId, out enemy);

        private static bool TryFindEnemy(AutoDefenseRuntimeSnapshot snapshot, long enemyId, out AutoDefenseEnemySnapshot enemy) => IdleAutoDefenseCombatTargets.TryFindEnemy(snapshot, enemyId, out enemy);

        private static bool TryFindEnemyByCombatant(AutoDefenseRuntimeSnapshot snapshot, CombatantId combatantId, out AutoDefenseEnemySnapshot enemy) => IdleAutoDefenseCombatTargets.TryFindEnemyByCombatant(snapshot, combatantId, out enemy);

        private static bool TrySelectPresentationEnemyWithinRange(AutoDefenseRuntimeSnapshot snapshot, double range, out AutoDefenseEnemySnapshot selected) => IdleAutoDefenseCombatTargets.TrySelectPresentationEnemyWithinRange(snapshot, range, out selected);

        internal bool TrySelectPresentationEnemyWithinAnyRange(out AutoDefenseEnemySnapshot selected) => CombatRuntime.Targets.TrySelectPresentationEnemyWithinAnyRange(out selected);

        private static Vector3 CreateTowerMuzzlePosition(Vector3 objectivePosition) => IdleAutoDefenseCombatTargets.CreateTowerMuzzlePosition(objectivePosition);

        internal static Vector3 CreateEnemyAimPosition(Vector3 enemyPosition) => IdleAutoDefenseCombatTargets.CreateEnemyAimPosition(enemyPosition);

        public bool TryGetPrimaryMajorThreat(out IdleAutoDefenseMajorThreatSnapshot threat) => CombatRuntime.Targets.TryGetPrimaryMajorThreat(out threat);

        private void EmitDirectWeaponPresentation(WeaponFireResult fireResult, AutoDefenseRuntimeSnapshot beforeCombat, AutoDefenseRuntimeSnapshot afterCombat) => CombatRuntime.Feedback.EmitDirectWeaponPresentation(fireResult, beforeCombat, afterCombat);

        private void EmitMissingKillFeedback(AutoDefenseRuntimeSnapshot beforeCombat, AutoDefenseRuntimeSnapshot afterCombat, int maxKills, AttackDefinitionAsset attack) => CombatRuntime.Feedback.EmitMissingKillFeedback(beforeCombat, afterCombat, maxKills, attack);

        private void EmitSpawnFeedbackForNewEnemies() => CombatRuntime.Feedback.EmitSpawnFeedbackForNewEnemies();

        private void ObserveEnemyPressure(AutoDefenseRuntimeSnapshot snapshot) => CombatRuntime.Feedback.ObserveEnemyPressure(snapshot);

        private void EmitEnemyDeathFeedback(AutoDefenseEnemySnapshot enemy) => CombatRuntime.Feedback.EmitEnemyDeathFeedback(enemy);

        private void RecordProjectileVisualSpawn(AttackDefinitionAsset attack) => CombatRuntime.Feedback.RecordProjectileVisualSpawn(attack);

        private bool TryDamageEnemyWithPresentation(AutoDefenseEnemySnapshot enemy, AttackDefinitionAsset attack, double damageAmount, out bool killed) => CombatRuntime.Damage.TryDamageEnemyWithPresentation(enemy, attack, damageAmount, out killed);

        private bool TryKillEnemyAfterFeedback(AutoDefenseEnemySnapshot enemy) => CombatRuntime.Damage.TryKillEnemyAfterFeedback(enemy);

        private bool TryApplyVisibleEnemyDamage(AutoDefenseEnemySnapshot enemy, AttackDefinitionAsset attack, double damageAmount, Vector3 impactPosition, out bool killed) => CombatRuntime.Damage.TryApplyVisibleEnemyDamage(enemy, attack, damageAmount, impactPosition, out killed);

        private ProjectileLaunchRequest CreateVisibleProjectileLaunchRequest(
            ProjectileLaunchRequest original,
            AutoDefenseRuntimeSnapshot snapshot,
            out AttackDefinitionAsset attack,
            out AutoDefenseEnemySnapshot target,
            out int impactDelayTicks,
            out double damageThreshold) => CombatRuntime.Projectiles.CreateVisibleProjectileLaunchRequest(original, snapshot, out attack, out target, out impactDelayTicks, out damageThreshold);

        private int ResolvePendingProjectileImpacts(int ticks) => CombatRuntime.Projectiles.ResolvePendingProjectileImpacts(ticks);

        private bool TryReportProjectileImpact(
            PendingProjectileImpact pending,
            AutoDefenseEnemySnapshot impactTarget,
            out ProjectileImpactResult result) => CombatRuntime.Projectiles.TryReportProjectileImpact(pending, impactTarget, out result);

        private void CleanupProjectileWithoutDamage(ProjectileInstanceId projectileId) => CombatRuntime.Projectiles.CleanupProjectileWithoutDamage(projectileId);

        private void EmitProjectileExpiryFeedback(ProjectileTickResult result) => CombatRuntime.Projectiles.EmitProjectileExpiryFeedback(result);

        private bool TryLaunchVisibleProjectileAtEnemy(AutoDefenseEnemySnapshot enemy, AttackDefinitionAsset attack, double damageAmount) => CombatRuntime.Projectiles.TryLaunchVisibleProjectileAtEnemy(enemy, attack, damageAmount);

        private bool TryFindProjectileImpactTarget(PendingProjectileImpact pending, out AutoDefenseEnemySnapshot target) => CombatRuntime.Projectiles.TryFindProjectileImpactTarget(pending, out target);

        private int FindPendingProjectileImpactIndex(ProjectileInstanceId projectileId) => CombatRuntime.Projectiles.FindPendingProjectileImpactIndex(projectileId);

        private void ObserveProjectileMotion() => CombatRuntime.Projectiles.ObserveProjectileMotion();

        private int ApplyDirectDamageBonusIfReady() => CombatRuntime.Cadence.ApplyDirectDamageBonusIfReady();

        private int FireManualTowerShotIfReady(int ticks) => CombatRuntime.Cadence.FireManualTowerShotIfReady(ticks);

        private int FireUnlockedModulesIfReady(int ticks) => CombatRuntime.Cadence.FireUnlockedModulesIfReady(ticks);

        private int TryKillPriorityEnemies(double damageThreshold, int maxKills, string attackId, double range, bool preferProjectileVisual = false) => CombatRuntime.Cadence.TryKillPriorityEnemies(damageThreshold, maxKills, attackId, range, preferProjectileVisual);

    }
}
