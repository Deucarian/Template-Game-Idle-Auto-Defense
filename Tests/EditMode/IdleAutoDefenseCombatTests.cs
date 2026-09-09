using System;
using System.Linq;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Projectiles;
using Deucarian.WorldSpawning;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseCombatTests
    {
        [Test]
        public void RangeSelectionUsesActiveProgressPriorityAndCountsOnlyActiveRejections()
        {
            using var host = new IdleCombatTestHost();
            host.AddEnemy(1, 10d, new Vector3(20f, 0f, 0f), 0.9f);
            host.AddEnemy(2, 10d, Vector3.forward * 2f, 0.6f);
            host.AddEnemy(3, 10d, Vector3.forward * 3f, 0.6f);
            Assert.That(host.Combat.Targets.TrySelectPriorityEnemyWithinRange(host.CreateSnapshot(), 4d, out var target), Is.True);
            Assert.That(target.Id, Is.EqualTo(2), "Equal progress retains the first eligible snapshot entry.");
            Assert.That(host.Combat.Targets.TrySelectPriorityEnemyWithinRange(host.CreateSnapshot(), 1d, out _), Is.False);
            Assert.That(host.Combat.Statistics.RangeRejectedTargetCount, Is.EqualTo(1));
            host.Enemies.Clear();
            Assert.That(host.Combat.Targets.TrySelectPriorityEnemyWithinRange(host.CreateSnapshot(), 1d, out _), Is.False);
            Assert.That(host.Combat.Statistics.RangeRejectedTargetCount, Is.EqualTo(1));
        }

        [TestCase(AttackRecipeTargetingMode.LowestHealth, 8L)]
        [TestCase(AttackRecipeTargetingMode.Strongest, 9L)]
        [TestCase(AttackRecipeTargetingMode.Random, 7L)]
        [TestCase(AttackRecipeTargetingMode.Nearest, 8L)]
        public void AuthoredTargetModesRetainTheirExistingDeterministicTieRules(AttackRecipeTargetingMode mode, long expected)
        {
            using var host = new IdleCombatTestHost();
            var attack = host.CreateAttack(mode: mode);
            host.AddEnemy(9, 12d, Vector3.forward, 0.2f);
            host.AddEnemy(7, 5d, Vector3.forward * 2f, 0.7f);
            host.AddEnemy(8, 5d, Vector3.forward * 3f, 0.9f);
            Assert.That(host.Combat.Targets.TrySelectAttackTarget(attack, host.CreateSnapshot(), out var target), Is.True);
            Assert.That(target.Id, Is.EqualTo(expected));
        }

        [Test]
        public void ManualCadenceUsesMinimumTickAndOnlyKillsAfterAccumulatedVisibleDamage()
        {
            using var host = new IdleCombatTestHost();
            host.AddEnemy(1, 10d, Vector3.forward * 2f, 0.8f);
            Assert.That(host.Combat.Cadence.FireManualTowerShotIfReady(33), Is.Zero);
            Assert.That(host.Events, Is.Empty);
            Assert.That(host.Combat.Cadence.FireManualTowerShotIfReady(0), Is.Zero);
            Assert.That(host.Numbers.Single(), Is.EqualTo(3.2d).Within(0.0001d));
            Assert.That(host.Combat.Statistics.EnemyDamageSurvivedCount, Is.EqualTo(1));
            Assert.That(host.Combat.Cadence.FireManualTowerShotIfReady(34), Is.Zero);
            Assert.That(host.Combat.Cadence.FireManualTowerShotIfReady(34), Is.Zero);
            Assert.That(host.Combat.Cadence.FireManualTowerShotIfReady(34), Is.EqualTo(1));
            Assert.That(host.Combat.Statistics.DirectOrCombatKillCount, Is.EqualTo(1));
            Assert.That(host.Events.Count(item => item == "reward:1"), Is.EqualTo(1));
        }

        [Test]
        public void VisibleDamageFloorsPositiveHitsAndOrdersKillBeforeDeathAndReward()
        {
            using var host = new IdleCombatTestHost();
            var enemy = host.AddEnemy(1, 0.5d, Vector3.forward, 0.8f);
            Assert.That(host.Combat.Damage.TryApplyVisibleEnemyDamage(enemy, null, 0.1d, enemy.Position, out bool killed), Is.True);
            Assert.That(killed, Is.False);
            host.Events.Clear();
            Assert.That(host.Combat.Damage.TryApplyVisibleEnemyDamage(enemy, null, 0.1d, enemy.Position, out killed), Is.True);
            Assert.That(killed, Is.True);
            CollectionAssert.AreEqual(new[] { "enemy:OnHit:1", "number", "kill:1", "enemy:OnDeath:1", "reward:1" }, host.Events);
            Assert.That(host.Numbers, Is.EqualTo(new[] { 0.25d, 0.25d }));
            host.Events.Clear();
            Assert.That(host.Combat.Damage.TryApplyVisibleEnemyDamage(enemy, null, 1d, enemy.Position, out _), Is.False);
            Assert.That(host.Events, Is.Empty);
        }

        [Test]
        public void WeaponLaunchOnlyQueuesDamageAndDueImpactsRemoveBeforeBackendCallbacks()
        {
            using var host = new IdleCombatTestHost();
            var attack = host.CreateAttack(projectile: true);
            host.AddEnemy(1, 10d, Vector3.forward * 2f, 0.8f);
            var definition = host.ProjectileDefinitions.Single();
            var source = new AttackSourceId("source.test");
            var request = new ProjectileLaunchRequest(definition.Id, source, new AttackDefinitionId(attack.Id),
                new AttackSourceSnapshot(source, new CombatantId("objective.test")), Vector3.zero, Vector3.forward * 2f);
            host.Combat.Projectiles.LaunchFromWeaponResult(new[] { request }, host.CreateSnapshot());
            Assert.That(host.Combat.Projectiles.PendingCount, Is.EqualTo(1));
            Assert.That(host.Numbers, Is.Empty, "Launching must not apply visible damage.");
            Assert.That(host.Combat.Statistics.ProjectileDamageAppliedCount, Is.Zero);
            host.BeforeImpactReport = () => Assert.That(host.Combat.Projectiles.PendingCount, Is.Zero);
            int delay = host.Combat.Rules.CalculateProjectileImpactDelayTicks(host.Muzzle, Vector3.forward * 2f + Vector3.up * 0.35f, definition.Speed);
            host.Combat.Projectiles.ResolvePendingProjectileImpacts(delay - 1);
            Assert.That(host.Numbers, Is.Empty);
            host.Events.Clear();
            host.Combat.Projectiles.ResolvePendingProjectileImpacts(0);
            CollectionAssert.AreEqual(new[] { "report:1", "attack:OnImpact:1", "enemy:OnHit:1", "number" }, host.Events);
            Assert.That(host.Combat.Statistics.ProjectileImpactCallbackCount, Is.EqualTo(1));
            Assert.That(host.Combat.Statistics.ProjectileDamageAppliedCount, Is.EqualTo(1));
        }

        [Test]
        public void SimultaneousDueImpactsResolveInReverseQueueOrderWithMinimumTicks()
        {
            using var host = new IdleCombatTestHost();
            host.AddEnemy(1, 10d, Vector3.forward, 0.8f);
            host.AddEnemy(2, 10d, Vector3.right, 0.7f);
            host.Combat.Projectiles.QueueImpact(new ProjectileInstanceId(1), 1, null, Vector3.forward, 2d, 2);
            host.Combat.Projectiles.QueueImpact(new ProjectileInstanceId(2), 2, null, Vector3.right, 2d, 2);
            host.Combat.Projectiles.ResolvePendingProjectileImpacts(0);
            Assert.That(host.Events, Is.Empty);
            host.Combat.Projectiles.ResolvePendingProjectileImpacts(-10);
            CollectionAssert.AreEqual(new[] { "report:2", "report:1" }, host.Events.Where(item => item.StartsWith("report:")));
            Assert.That(host.Combat.Projectiles.PendingCount, Is.Zero);
        }

        [Test]
        public void MissingAndRejectedImpactsExpireWithoutApplyingDamage()
        {
            using var host = new IdleCombatTestHost();
            host.Combat.Projectiles.QueueImpact(new ProjectileInstanceId(1), 1, null, Vector3.forward, 2d, 1);
            host.Combat.Projectiles.ResolvePendingProjectileImpacts(1);
            CollectionAssert.AreEqual(new[] { "attack:OnExpire:0", "cleanup:1:ManualCleanup" }, host.Events);
            Assert.That(host.Combat.Statistics.ProjectileImpactMissCount, Is.EqualTo(1));
            host.AddEnemy(2, 10d, Vector3.right, 0.7f);
            host.AcceptImpacts = false;
            host.Events.Clear();
            host.Combat.Projectiles.QueueImpact(new ProjectileInstanceId(2), 2, null, Vector3.right, 2d, 1);
            host.Combat.Projectiles.ResolvePendingProjectileImpacts(1);
            CollectionAssert.AreEqual(new[] { "report:2", "attack:OnExpire:0" }, host.Events);
            Assert.That(host.Combat.Statistics.ProjectileImpactRejectedCount, Is.EqualTo(1));
            Assert.That(host.Combat.Statistics.ProjectileDamageAppliedCount, Is.Zero);
            Assert.That(host.Numbers, Is.Empty);
        }

        [Test]
        public void RetargetUsesCurrentDraftRadiusAndPreservesFirstEqualDistanceCandidate()
        {
            using var host = new IdleCombatTestHost();
            host.Draft.ProjectileRetargetRadius = 0.5f;
            host.AddEnemy(2, 10d, Vector3.right * 0.4f, 0.7f);
            host.AddEnemy(3, 10d, Vector3.left * 0.4f, 0.9f);
            host.AddEnemy(4, 10d, Vector3.forward * 0.7f, 1f);
            host.Combat.Projectiles.QueueImpact(new ProjectileInstanceId(1), 99, null, Vector3.up * 0.35f, 2d, 1);
            host.Combat.Projectiles.ResolvePendingProjectileImpacts(1);
            Assert.That(host.Events, Does.Contain("report:2"));
            Assert.That(host.Combat.Statistics.ProjectileImpactRetargetCount, Is.EqualTo(1));
            host.Draft.ProjectileRetargetRadius = 0.1f;
            host.Events.Clear();
            host.Combat.Projectiles.QueueImpact(new ProjectileInstanceId(2), 99, null, Vector3.up * 0.35f, 2d, 1);
            host.Combat.Projectiles.ResolvePendingProjectileImpacts(1);
            Assert.That(host.Combat.Statistics.ProjectileImpactMissCount, Is.EqualTo(1));
            Assert.That(host.Events.Any(item => item.StartsWith("report:")), Is.False);
        }

        [Test]
        public void BackendExpiryDefersPendingImpactInsteadOfSilentlyApplyingDamage()
        {
            using var host = new IdleCombatTestHost();
            host.AddEnemy(1, 10d, Vector3.forward, 0.8f);
            var id = new ProjectileInstanceId(1);
            host.Combat.Projectiles.QueueImpact(id, 1, null, Vector3.forward, 2d, 5);
            host.Combat.Projectiles.EmitProjectileExpiryFeedback(new ProjectileTickResult(new[] { new ProjectileExpiryEvent(id, ProjectileExpiryReason.LifetimeExpired) }));
            Assert.That(host.Combat.Projectiles.PendingCount, Is.EqualTo(1));
            Assert.That(host.Combat.Statistics.ProjectileExpiryDeferralCount, Is.EqualTo(1));
            Assert.That(host.Numbers, Is.Empty);
            host.AcceptImpacts = false;
            host.Combat.Projectiles.ResolvePendingProjectileImpacts(5);
            Assert.That(host.Combat.Statistics.ProjectileImpactRejectedCount, Is.EqualTo(1));
            Assert.That(host.Numbers, Is.Empty);
        }

        [Test]
        public void ReleaseClearsTrackingWhileResetAlsoResetsCadenceAndStatistics()
        {
            using var host = new IdleCombatTestHost();
            host.AddEnemy(1, 10d, Vector3.forward, 0.8f);
            host.Combat.Feedback.EmitSpawnFeedbackForNewEnemies();
            host.Combat.Cadence.FireManualTowerShotIfReady(1);
            host.Combat.Projectiles.QueueImpact(new ProjectileInstanceId(1), 1, null, Vector3.forward, 2d, 5);
            host.Combat.Statistics.RecordDirectKills(4);
            host.Combat.Release();
            Assert.That(host.Combat.Projectiles.PendingCount, Is.Zero);
            Assert.That(host.Combat.Statistics.DirectOrCombatKillCount, Is.EqualTo(4));
            host.Events.Clear();
            host.Combat.Feedback.EmitSpawnFeedbackForNewEnemies();
            Assert.That(host.Events, Does.Contain("enemy:OnSpawn:1"));
            host.Combat.Cadence.FireManualTowerShotIfReady(33);
            Assert.That(host.Numbers.Count, Is.EqualTo(1), "Release preserves the old post-dispose cooldown semantics.");
            host.Combat.Reset();
            host.Numbers.Clear();
            host.Combat.Cadence.FireManualTowerShotIfReady(33);
            Assert.That(host.Numbers, Is.Empty);
            Assert.That(host.Combat.Statistics.DirectOrCombatKillCount, Is.Zero);
        }

        [Test]
        public void UnlockedModulesDispatchPulseArcThenHomingAndResetTheirCooldowns()
        {
            using var host = new IdleCombatTestHost();
            host.CreateAttack(id: BasicIdleAutoDefenseGame.PulseAttackId.Value);
            host.CreateAttack(id: BasicIdleAutoDefenseGame.ArcBurstAttackId.Value);
            host.CreateAttack(id: BasicIdleAutoDefenseGame.HomingPulseAttackId.Value);
            host.AddEnemy(1, 100d, Vector3.forward, 0.8f);
            host.Build.UnlockPulseBeamModule();
            host.Build.UnlockArcBurstModule();
            host.Build.UnlockHomingPulseModule();
            host.Combat.Cadence.FireUnlockedModulesIfReady(71);
            Assert.That(host.FiredAttackIds, Is.Empty);
            host.Combat.Cadence.FireUnlockedModulesIfReady(37);
            CollectionAssert.AreEqual(new[] { BasicIdleAutoDefenseGame.PulseAttackId.Value,
                BasicIdleAutoDefenseGame.ArcBurstAttackId.Value, BasicIdleAutoDefenseGame.ArcBurstAttackId.Value,
                BasicIdleAutoDefenseGame.HomingPulseAttackId.Value }, host.FiredAttackIds);
            Assert.That(host.Combat.Statistics.ModuleActivationCount, Is.EqualTo(3));
            host.FiredAttackIds.Clear();
            host.Combat.Cadence.FireUnlockedModulesIfReady(0);
            Assert.That(host.FiredAttackIds, Is.Empty);
        }

        [Test]
        public void MuzzleResolverUsesOnlyItsQueryPortAndFlattensTargetDirection()
        {
            using var host = new IdleCombatTestHost();
            host.Muzzle = new Vector3(2f, 1f, 3f);
            host.AddEnemy(1, 10d, new Vector3(7f, 999f, 3f), 0.8f);
            var channel = new WorldSpawnChannelId("projectile-origin");
            var resolver = new TemplateProjectileMuzzlePoseResolver(host.Combat.MuzzleQueries, channel);
            var request = new WorldSpawnRequest(new WorldSpawnableId("projectile.test"), channel, 1);
            var result = resolver.TryResolvePose(request);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Pose.Position, Is.EqualTo(host.Muzzle));
            Assert.That(Vector3.Dot(result.Pose.Rotation * Vector3.forward, Vector3.right), Is.GreaterThan(0.999f));
            host.Enemies.Clear();
            result = resolver.TryResolvePose(request);
            Assert.That(Vector3.Dot(result.Pose.Rotation * Vector3.forward, Vector3.forward), Is.GreaterThan(0.999f));
            Assert.That(resolver.TryResolvePose(new WorldSpawnRequest(new WorldSpawnableId("projectile.test"), new WorldSpawnChannelId("other"), 1)).Succeeded, Is.False);
            Assert.That(typeof(TemplateProjectileMuzzlePoseResolver).GetConstructors()
                .SelectMany(constructor => constructor.GetParameters()).Any(parameter => parameter.ParameterType == typeof(IdleAutoDefenseTemplateController)), Is.False);
        }

        [Test]
        public void CombatMathReadsLiveRunBuildUpgradeAndOverdriveState()
        {
            using var host = new IdleCombatTestHost(bindEconomy: true);
            Assert.That(host.Combat.Rules.ResolveManualTowerDamage(), Is.EqualTo(3.2d));
            host.Wallet.SetStartingBalance(1000);
            Assert.That(host.Build.DamageUpgradeCost, Is.EqualTo(20));
            Assert.That(host.Build.TryPurchaseDamageUpgrade(), Is.True);
            Assert.That(host.Combat.Rules.ResolveManualTowerDamage(), Is.EqualTo(4.8d).Within(0.0001d));
            Assert.That(host.Build.TryPurchaseRangeUpgrade(), Is.True);
            Assert.That(host.Combat.Rules.ResolveManualTowerDamage(), Is.EqualTo(5.7d).Within(0.0001d));
            Assert.That(host.Combat.Rules.ResolveManualTowerRange(), Is.EqualTo(8.8d).Within(0.0001d));
            Assert.That(host.Build.TryPurchaseOverdrive(), Is.True);
            Assert.That(host.Combat.Rules.ResolveModuleDamage(10d), Is.EqualTo(15.5d).Within(0.0001d));
        }
    }
}
