using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Deucarian.Attacks.Authoring;
using Deucarian.IdleProgression;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseAuthoredCoreEditModeTests
    {
        [Test]
        public void AuthoredCoreParityFixturePreservesEffectiveGameplayValues()
        {
            GameContentSetAsset contentSet = CreateContentSet(10, 0, 5600);
            GameContentSetValidationReport validation = GameContentSetValidator.Validate(contentSet);

            Assert.That(validation.IsValid, Is.True, FormatIssues(validation));
            Assert.That(contentSet.RewardCatalog.FirstDraftSeconds, Is.EqualTo(30f));
            Assert.That(contentSet.RewardCatalog.Settings.ChoiceCount, Is.EqualTo(3));
            Assert.That(contentSet.RewardCatalog.Settings.NormalEnemyExperience, Is.EqualTo(9));
            Assert.That(contentSet.RewardCatalog.Settings.EliteEnemyExperience, Is.EqualTo(42));
            Assert.That(contentSet.RewardCatalog.Settings.BossEnemyExperience, Is.EqualTo(120));
            Assert.That(contentSet.RewardCatalog.Settings.WaveCompletionExperience, Is.EqualTo(18));
            Assert.That(contentSet.RewardCatalog.Settings.BaseExperienceToNextLevel, Is.EqualTo(38));
            Assert.That(contentSet.RewardCatalog.Settings.ExperienceToNextLevelGrowth, Is.EqualTo(18));
            Assert.That(contentSet.RewardCatalog.Settings.NormalInvestmentsForEpic, Is.EqualTo(3));
            Assert.That(contentSet.RewardCatalog.Settings.EpicInvestmentsForLegendary, Is.EqualTo(3));
            Assert.That(contentSet.RewardCatalog.Settings.LevelUpRarityWeights.Common, Is.EqualTo(100d));
            Assert.That(contentSet.RewardCatalog.Settings.EliteRarityWeights.Epic, Is.EqualTo(44d));
            Assert.That(contentSet.RewardCatalog.Settings.BossRarityWeights.Legendary, Is.EqualTo(42d));
            Assert.That(contentSet.RewardCatalog.Catalog.WeaponUnlocks.Count, Is.EqualTo(3));
            Assert.That(contentSet.RewardCatalog.Catalog.NormalWeaponRewards.Count, Is.EqualTo(12));
            Assert.That(contentSet.RewardCatalog.Catalog.EpicWeaponRewards.Count, Is.EqualTo(12));
            Assert.That(contentSet.RewardCatalog.Catalog.LegendaryWeaponRewards.Count, Is.EqualTo(4));
            Assert.That(contentSet.RewardCatalog.Catalog.BaseRewards.Count, Is.EqualTo(6));

            Assert.That(contentSet.Economy.StartingCredits, Is.EqualTo(10));
            Assert.That(contentSet.Economy.StartingParts, Is.EqualTo(0));
            Assert.That(contentSet.Economy.PassiveIncomeAmount, Is.EqualTo(1));
            Assert.That(contentSet.Economy.PassiveIncomeIntervalTicks, Is.EqualTo(60));
            Assert.That(contentSet.Economy.CalculateUpgradeCost(IdleAutoDefenseEconomyAsset.DamageUpgradeCostId, 0), Is.EqualTo(20));
            Assert.That(contentSet.Economy.CalculateUpgradeCost(IdleAutoDefenseEconomyAsset.DamageUpgradeCostId, 1), Is.EqualTo(36));
            Assert.That(contentSet.Economy.CalculateUpgradeCost(IdleAutoDefenseEconomyAsset.FireRateUpgradeCostId, 0), Is.EqualTo(18));
            Assert.That(contentSet.Economy.CalculateUpgradeCost(IdleAutoDefenseEconomyAsset.RangeUpgradeCostId, 0), Is.EqualTo(18));
            Assert.That(contentSet.Economy.CalculateUpgradeCost(IdleAutoDefenseEconomyAsset.RepairUpgradeCostId, 0), Is.EqualTo(16));
            Assert.That(contentSet.Economy.CalculateUpgradeCost(IdleAutoDefenseEconomyAsset.OverdriveCostId, 0), Is.EqualTo(22));
            Assert.That(contentSet.Economy.EncounterCompletionCredits, Is.EqualTo(60));
            Assert.That(contentSet.Economy.EncounterCompletionParts, Is.EqualTo(3));
            Assert.That(contentSet.Economy.EncounterCompletionAccountXp, Is.EqualTo(35));
            Assert.That(contentSet.Economy.SmallCurrencyBonus, Is.EqualTo(5));
            Assert.That(contentSet.Economy.RunRewardClaimMultiplier, Is.EqualTo(2d));
            Assert.That(contentSet.EnemyPool.All(enemy => enemy != null && enemy.Stats != null && enemy.Stats.RewardValue == 5), Is.True);

            Assert.That(contentSet.RunProfile.SimulationTicksPerSecond, Is.EqualTo(20));
            Assert.That(contentSet.RunProfile.SessionLengthTicks, Is.EqualTo(5600));
            Assert.That(contentSet.RunProfile.SessionLengthSeconds, Is.EqualTo(280d));
            Assert.That(contentSet.RunProfile.Waves.Count, Is.EqualTo(7));
            Assert.That(contentSet.RunProfile.DifficultyMultiplier, Is.EqualTo(1f));
            Assert.That(contentSet.RunProfile.RewardMultiplier, Is.EqualTo(1f));
            Assert.That(contentSet.RunProfile.Endless, Is.False);
            Assert.That(contentSet.RunProfile.VictoryRule, Is.EqualTo(IdleAutoDefenseVictoryRule.AllAuthoredWavesCleared));
            Assert.That(contentSet.RunProfile.DefeatRule, Is.EqualTo(IdleAutoDefenseDefeatRule.ObjectiveDestroyed));

            Assert.That(contentSet.GameRules.ObjectiveMaximumHealth, Is.EqualTo(240d));
            Assert.That(contentSet.GameRules.ObjectiveLives, Is.EqualTo(60));
            Assert.That(contentSet.GameRules.ObjectiveLifeRestoreAmount, Is.EqualTo(2));
            Assert.That(contentSet.GameRules.SpawnRingRadius, Is.EqualTo(18.5f));
            Assert.That(contentSet.GameRules.GetModule(IdleAutoDefenseModuleRole.PrecisionBeam).BuildCost, Is.EqualTo(34));
            Assert.That(contentSet.GameRules.GetModule(IdleAutoDefenseModuleRole.AreaBurst).BuildCost, Is.EqualTo(62));
            Assert.That(contentSet.GameRules.GetModule(IdleAutoDefenseModuleRole.HomingProjectile).BuildCost, Is.EqualTo(78));
            Assert.That(contentSet.GameRules.OverdriveDurationSeconds, Is.EqualTo(7f));
            Assert.That(contentSet.GameRules.OverdriveCooldownSeconds, Is.EqualTo(18f));
            Assert.That(contentSet.GameRules.OverdriveDamageMultiplier, Is.EqualTo(1.55d));

            Assert.That(contentSet.Progression.Tracks.Count, Is.EqualTo(1));
            Assert.That(contentSet.Progression.Tracks[0].CumulativeThresholds, Is.EqualTo(new long[] { 100, 250, 500, 900 }));
            Assert.That(contentSet.Progression.ResearchNodes.Count, Is.EqualTo(4));
            Assert.That(contentSet.Progression.FindResearchNode("research.idle-auto-defense.core-plating").RankCosts, Is.EqualTo(new long[] { 25, 75, 160 }));
            Assert.That(contentSet.Progression.FindResearchNode("research.idle-auto-defense.core-plating").EffectAmountPerRank, Is.EqualTo(8d));
            Assert.That(contentSet.Progression.FindResearchNode("research.idle-auto-defense.offline-routing").RankCosts, Is.EqualTo(new long[] { 40, 120 }));

            Assert.That(contentSet.OfflineProgression.Enabled, Is.True);
            Assert.That(contentSet.OfflineProgression.MaximumOfflineSeconds, Is.EqualTo(28800d));
            Assert.That(contentSet.OfflineProgression.ProductionAmountPerSecond, Is.EqualTo(0.35d));
            Assert.That(contentSet.OfflineProgression.CycleRewardAmount, Is.EqualTo(1));
            Assert.That(contentSet.OfflineProgression.CycleDurationSeconds, Is.EqualTo(240d));
            Assert.That(contentSet.OfflineProgression.ClaimMultiplier, Is.EqualTo(2d));
        }

        [Test]
        public void StrictAssignedContentBindsEveryAuthoredOwnerWithoutFallback()
        {
            GameContentSetAsset contentSet = CreateContentSet();
            IdleAutoDefenseTemplateController controller = CreateStrictController(contentSet);
            try
            {
                Assert.That(controller.StrictAuthoredStartup, Is.True);
                Assert.That(controller.StartupBlocked, Is.False, controller.StartupError);
                Assert.That(controller.FallbackModeActive, Is.False);
                Assert.That(controller.UsingAssignedContentSet, Is.True);
                Assert.That(controller.UsingAuthoredCore, Is.True);
                Assert.That(controller.ActiveRewardCatalogId, Is.EqualTo(contentSet.RewardCatalog.Id));
                Assert.That(controller.ActiveEconomyId, Is.EqualTo(contentSet.Economy.Id));
                Assert.That(controller.ActiveRunProfileId, Is.EqualTo(contentSet.RunProfile.Id));
                Assert.That(controller.ActiveProgressionId, Is.EqualTo(contentSet.Progression.Id));
                Assert.That(controller.ActiveOfflineProgressionId, Is.EqualTo(contentSet.OfflineProgression.Id));
                Assert.That(controller.ActiveGameRulesId, Is.EqualTo(contentSet.GameRules.Id));
                Assert.That(controller.Runtime, Is.Not.Null);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [TestCase("RewardCatalog")]
        [TestCase("Economy")]
        [TestCase("RunProfile")]
        [TestCase("Progression")]
        [TestCase("OfflineProgression")]
        [TestCase("GameRules")]
        public void StrictStartupBlocksEachMissingAuthoredOwner(string missingOwner)
        {
            GameContentSetAsset contentSet = CreateContentSet();
            contentSet.ConfigureAuthoredCore(
                missingOwner == "RewardCatalog" ? null : contentSet.RewardCatalog,
                missingOwner == "Economy" ? null : contentSet.Economy,
                missingOwner == "RunProfile" ? null : contentSet.RunProfile,
                missingOwner == "Progression" ? null : contentSet.Progression,
                missingOwner == "OfflineProgression" ? null : contentSet.OfflineProgression,
                missingOwner == "GameRules" ? null : contentSet.GameRules);
            LogAssert.Expect(LogType.Error, new Regex("Strict authored startup blocked:"));

            IdleAutoDefenseTemplateController controller = CreateStrictController(contentSet);
            try
            {
                Assert.That(controller.StartupBlocked, Is.True);
                Assert.That(controller.FallbackModeActive, Is.False);
                Assert.That(controller.Runtime, Is.Null);
                Assert.That(controller.StartupError, Does.Contain("missing or invalid"));
                Assert.That(controller.InvalidAssignedContentSetIssueCount, Is.GreaterThan(0));
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void InvalidRunProfileAndProgressionReferenceBlockStrictStartup()
        {
            GameContentSetAsset invalidRun = CreateContentSet();
            invalidRun.RunProfile.Configure(
                invalidRun.RunProfile.Id,
                invalidRun.RunProfile.DisplayName,
                0,
                invalidRun.RunProfile.SessionLengthTicks,
                invalidRun.RunProfile.Waves,
                invalidRun.RunProfile.DifficultyMultiplier,
                invalidRun.RunProfile.Endless,
                invalidRun.RunProfile.VictoryRule,
                invalidRun.RunProfile.DefeatRule,
                invalidRun.RunProfile.RewardMultiplier,
                invalidRun.RunProfile.PreparationTicks,
                invalidRun.RunProfile.EncounterSeed);
            LogAssert.Expect(LogType.Error, new Regex("Strict authored startup blocked:"));
            IdleAutoDefenseTemplateController invalidRunController = CreateStrictController(invalidRun);
            Assert.That(invalidRunController.StartupBlocked, Is.True);
            DestroyController(invalidRunController);

            GameContentSetAsset invalidProgression = CreateContentSet();
            var node = new IdleAutoDefenseResearchNodeRecord(
                "research.test.invalid-prerequisite",
                "Invalid Prerequisite",
                1,
                invalidProgression.Economy.PrimaryCurrencyId,
                new long[] { 1 },
                new[] { new IdleAutoDefenseResearchPrerequisiteRecord("research.missing", 1) },
                Array.Empty<string>(),
                IdleAutoDefenseProgressionEffectKind.ObjectiveMaximumHealth,
                invalidProgression.GameRules.ObjectiveId,
                1d);
            ConfigureProgression(invalidProgression.Progression, new[] { node });
            GameContentSetValidationReport report = GameContentSetValidator.Validate(invalidProgression);
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues.Any(issue => issue.Path.Contains("Prerequisites")), Is.True, FormatIssues(report));
            LogAssert.Expect(LogType.Error, new Regex("Strict authored startup blocked:"));
            IdleAutoDefenseTemplateController invalidProgressionController = CreateStrictController(invalidProgression);
            Assert.That(invalidProgressionController.StartupBlocked, Is.True);
            DestroyController(invalidProgressionController);
        }

        [Test]
        public void UnboundDebugHostUsesObservableTransientFallback()
        {
            GameObject host = new GameObject("idle-auto-defense-explicit-fallback-test");
            IdleAutoDefenseTemplateController controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            controller.Build();
            try
            {
                Assert.That(controller.StrictAuthoredStartup, Is.False);
                Assert.That(controller.StartupBlocked, Is.False);
                Assert.That(controller.FallbackModeActive, Is.True);
                Assert.That(controller.UsingAuthoredCore, Is.False);
                Assert.That(controller.Runtime, Is.Not.Null);
                Assert.That(controller.AssignedContentSetStatus, Does.Contain("fallback"));
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void AuthoredRewardAmountAndWeightChangeDeterministicRuntimeDraft()
        {
            GameContentSetAsset contentSet = CreateContentSet();
            IdleAutoDefenseBaseRewardDefinition target = contentSet.RewardCatalog.Catalog.BaseRewards.Single(value => value.Id == "base.damage");
            foreach (IdleAutoDefenseBaseRewardDefinition reward in contentSet.RewardCatalog.Catalog.BaseRewards)
                reward.Weight = 0.000001d;
            foreach (IdleAutoDefenseWeaponRewardDefinition reward in contentSet.RewardCatalog.Catalog.NormalWeaponRewards)
                reward.Weight = 0.000001d;
            target.Amount = 7d;
            target.Weight = 1_000_000_000d;

            IdleAutoDefenseTemplateController controller = CreateStrictController(contentSet);
            try
            {
                controller.RequestRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
                int choiceIndex = controller.RewardDraftChoices.ToList().FindIndex(value => value.Id == target.Id);
                Assert.That(choiceIndex, Is.GreaterThanOrEqualTo(0), "The authored high-weight reward should be selected by the deterministic draft.");
                Assert.That(controller.RewardDraftChoices[choiceIndex].AuthoredAmount, Is.EqualTo(7d));
                Assert.That(controller.RewardDraftChoices[choiceIndex].AuthoredWeight, Is.EqualTo(1_000_000_000d));
                Assert.That(controller.TryChooseRewardDraftChoice(choiceIndex), Is.True);
                Assert.That(controller.DamageUpgradeRank, Is.EqualTo(7));
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void AuthoredStartingCreditsAndModuleCostDriveRuntimePurchases()
        {
            GameContentSetAsset contentSet = CreateContentSet();
            contentSet.Economy.GetCurrency(contentSet.Economy.PrimaryCurrencyId).StartingAmount = 123;
            WeaponDefinitionAsset pulse = contentSet.AvailableWeapons.Single(value => value.Id == BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value);
            WeaponStatsDefinitionAsset stats = pulse.Stats;
            stats.Configure(
                stats.FireMode,
                stats.Attack,
                stats.ProjectileDefinitionId,
                stats.CooldownTicks,
                stats.Range,
                stats.BurstCount,
                stats.VolleyCount,
                stats.SpreadDegrees,
                47,
                stats.TargetingRoleId,
                stats.MuzzleRoleId);

            IdleAutoDefenseTemplateController controller = CreateStrictController(contentSet);
            try
            {
                Assert.That(controller.RuntimeCurrency, Is.EqualTo(123));
                Assert.That(controller.PulseBeamUnlockCost, Is.EqualTo(47));
                Assert.That(controller.TryPurchasePulseBeamModule(), Is.True);
                Assert.That(controller.RuntimeCurrency, Is.EqualTo(76));
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void AuthoredEnemyRewardChangesRealKillGrant()
        {
            GameContentSetAsset contentSet = CreateContentSet();
            contentSet.Economy.GetCurrency(contentSet.Economy.PrimaryCurrencyId).StartingAmount = 0;
            ConfigureEconomy(contentSet.Economy, 0);
            foreach (EnemyDefinitionAsset enemy in contentSet.EnemyPool)
            {
                EnemyStatsDefinitionAsset stats = enemy.Stats;
                stats.Configure(1f, stats.MoveSpeed, 17, stats.ContactDamage, stats.DamageTypeId, stats.CollisionRadius);
            }

            IdleAutoDefenseTemplateController controller = CreateStrictController(contentSet);
            controller.RewardDraftPausesCombat = false;
            try
            {
                for (int i = 0; i < 2000 && controller.DirectOrCombatKillCount + controller.ProjectileAdapterKillCount == 0; i++)
                    controller.Step(1, 0.05f);
                int kills = controller.DirectOrCombatKillCount + controller.ProjectileAdapterKillCount;
                Assert.That(kills, Is.GreaterThan(0), controller.StatusSummary);
                Assert.That(controller.RuntimeCurrency, Is.EqualTo(kills * 17L));
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void AuthoredRunDurationProgressionAndOfflineValuesDriveRuntime()
        {
            GameContentSetAsset contentSet = CreateContentSet();
            contentSet.Economy.GetCurrency(contentSet.Economy.PrimaryCurrencyId).StartingAmount = 500;
            contentSet.RunProfile.Configure(
                contentSet.RunProfile.Id,
                contentSet.RunProfile.DisplayName,
                20,
                20,
                contentSet.RunProfile.Waves,
                contentSet.RunProfile.DifficultyMultiplier,
                false,
                IdleAutoDefenseVictoryRule.SurviveSessionDuration,
                contentSet.RunProfile.DefeatRule,
                contentSet.RunProfile.RewardMultiplier,
                contentSet.RunProfile.PreparationTicks,
                contentSet.RunProfile.EncounterSeed);
            const string nodeId = "research.test.authored-core";
            var node = new IdleAutoDefenseResearchNodeRecord(
                nodeId,
                "Authored Core Test",
                1,
                contentSet.Economy.PrimaryCurrencyId,
                new long[] { 17 },
                Array.Empty<IdleAutoDefenseResearchPrerequisiteRecord>(),
                Array.Empty<string>(),
                IdleAutoDefenseProgressionEffectKind.ObjectiveMaximumHealth,
                contentSet.GameRules.ObjectiveId,
                11d);
            ConfigureProgression(contentSet.Progression, new[] { node });
            contentSet.OfflineProgression.Configure(
                contentSet.OfflineProgression.Id,
                contentSet.OfflineProgression.DisplayName,
                true,
                60d,
                0d,
                contentSet.Economy.PrimaryCurrencyId,
                2d,
                contentSet.Economy.SecondaryCurrencyId,
                0,
                240d,
                2d,
                contentSet.OfflineProgression.SaveTimestampKey);

            IdleAutoDefenseTemplateController controller = CreateStrictController(contentSet);
            try
            {
                Assert.That(controller.SessionLengthTicks, Is.EqualTo(20));
                Assert.That(controller.SessionLengthSeconds, Is.EqualTo(1d));
                double originalMaximumHealth = controller.ObjectiveMaximumHealth;
                Assert.That(controller.TryPurchasePersistentUpgrade(nodeId), Is.True);
                Assert.That(controller.GetPersistentResearchRank(nodeId), Is.EqualTo(1));
                Assert.That(controller.ObjectiveMaximumHealth, Is.EqualTo(originalMaximumHealth + 11d));

                IdleProgressionResult offline = controller.SimulateOfflineReward(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(120));
                Assert.That(controller.LastOfflineRewardCode, Is.EqualTo(IdleProgressionResultCode.Capped));
                Assert.That(offline.Reward.CurrencyLines[0].Amount.Value, Is.EqualTo(120));
                Assert.That(controller.OfflineRewardCredits, Is.EqualTo(603), "500 starting credits - 17 research cost + 120 capped offline credits.");
                Assert.That(controller.OfflineRewardParts, Is.EqualTo(0));

                for (int i = 0; i < 19; i++) controller.Step(1, 0.05f);
                Assert.That(controller.RunProfileVictoryReached, Is.False);
                controller.Step(1, 0.05f);
                Assert.That(controller.RunProfileVictoryReached, Is.True);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void DisabledOfflineProgressionIsExplicitAndDeterministic()
        {
            GameContentSetAsset contentSet = CreateContentSet();
            contentSet.OfflineProgression.Configure(
                contentSet.OfflineProgression.Id,
                contentSet.OfflineProgression.DisplayName,
                false,
                28800d,
                0d,
                contentSet.Economy.PrimaryCurrencyId,
                0.35d,
                contentSet.Economy.SecondaryCurrencyId,
                1,
                240d,
                2d,
                contentSet.OfflineProgression.SaveTimestampKey);

            GameContentSetValidationReport validation = GameContentSetValidator.Validate(contentSet);
            Assert.That(validation.IsValid, Is.True, FormatIssues(validation));
            IdleProgressionResult result = contentSet.OfflineProgression.Calculate(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddHours(1));
            Assert.That(result.Code, Is.EqualTo(IdleProgressionResultCode.NoElapsedTime));
        }

        [Test]
        public void LegalAuthoredAttackAndCurrencyIdChangesPropagateWithoutFallback()
        {
            GameContentSetAsset contentSet = CreateContentSet(500);
            IdleAutoDefenseModuleRule startingModule = contentSet.GameRules.GetModule(IdleAutoDefenseModuleRole.StartingProjectile);
            AttackDefinitionAsset startingAttack = startingModule.Weapon.Stats.Attack;
            const string renamedAttackId = "attack.test.renamed-starting-projectile";
            startingAttack.Configure(
                renamedAttackId,
                startingAttack.DisplayName,
                startingAttack.Icon,
                startingAttack.Tags,
                startingAttack.Mechanics,
                startingAttack.Targeting,
                startingAttack.Delivery,
                startingAttack.StatusEffects,
                startingAttack.Presentation,
                startingAttack.UpgradeHookId,
                startingAttack.BalancingNotes);

            const string renamedCreditsId = "currency.test.renamed-credits";
            string partsId = contentSet.Economy.SecondaryCurrencyId;
            IdleAutoDefenseCurrencyRecord oldCredits = contentSet.Economy.GetCurrency(contentSet.Economy.PrimaryCurrencyId);
            IdleAutoDefenseCurrencyRecord oldParts = contentSet.Economy.GetCurrency(partsId);
            var currencies = new[]
            {
                new IdleAutoDefenseCurrencyRecord(renamedCreditsId, oldCredits.DisplayName, oldCredits.Capacity, oldCredits.StartingAmount),
                new IdleAutoDefenseCurrencyRecord(partsId, oldParts.DisplayName, oldParts.Capacity, oldParts.StartingAmount)
            };
            IdleAutoDefenseCostCurve[] costs = contentSet.Economy.UpgradeCosts
                .Select(cost => new IdleAutoDefenseCostCurve(cost.Id, cost.DisplayName, renamedCreditsId, cost.BaseCost, cost.CostPerRank))
                .ToArray();
            contentSet.Economy.Configure(
                contentSet.Economy.Id,
                contentSet.Economy.DisplayName,
                currencies,
                renamedCreditsId,
                partsId,
                renamedCreditsId,
                contentSet.Economy.PassiveIncomeAmount,
                contentSet.Economy.PassiveIncomeIntervalTicks,
                costs,
                contentSet.Economy.EncounterCompletionCredits,
                contentSet.Economy.EncounterCompletionParts,
                contentSet.Economy.EncounterCompletionAccountXp,
                contentSet.Economy.SmallCurrencyBonus,
                contentSet.Economy.RunRewardClaimMultiplier);

            const string nodeId = "research.test.renamed-currency-offline-bonus";
            var offlineBonusNode = new IdleAutoDefenseResearchNodeRecord(
                nodeId,
                "Renamed Currency Offline Bonus",
                1,
                renamedCreditsId,
                new long[] { 1 },
                Array.Empty<IdleAutoDefenseResearchPrerequisiteRecord>(),
                Array.Empty<string>(),
                IdleAutoDefenseProgressionEffectKind.OfflineRewardMultiplier,
                contentSet.OfflineProgression.Id,
                0.5d);
            ConfigureProgression(contentSet.Progression, new[] { offlineBonusNode });
            contentSet.OfflineProgression.Configure(
                contentSet.OfflineProgression.Id,
                contentSet.OfflineProgression.DisplayName,
                true,
                60d,
                0d,
                renamedCreditsId,
                1d,
                partsId,
                0,
                240d,
                contentSet.OfflineProgression.ClaimMultiplier,
                contentSet.OfflineProgression.SaveTimestampKey);

            GameContentSetValidationReport validation = GameContentSetValidator.Validate(contentSet);
            Assert.That(validation.IsValid, Is.True, FormatIssues(validation));
            Assert.That(contentSet.GameRules.GetModuleByAttackId(renamedAttackId), Is.SameAs(startingModule));

            IdleAutoDefenseTemplateController controller = CreateStrictController(contentSet);
            controller.RewardDraftPausesCombat = false;
            try
            {
                Assert.That(controller.FallbackModeActive, Is.False);
                Assert.That(controller.TryPurchasePersistentUpgrade(nodeId), Is.True);
                IdleProgressionResult result = controller.SimulateOfflineReward(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(60));
                Assert.That(result.Reward.CurrencyLines[0].CurrencyId.Value, Is.EqualTo(renamedCreditsId));
                Assert.That(controller.OfflineRewardCredits, Is.EqualTo(589), "500 starting credits - 1 research cost + 60 offline credits + 30 authored progression bonus.");

                for (int i = 0; i < 600 && controller.ProjectileLaunchCount == 0; i++)
                    controller.Step(1, 0.05f);
                Assert.That(controller.ProjectileLaunchCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.That(controller.FallbackModeActive, Is.False);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        private static GameContentSetAsset CreateContentSet(int startingCredits = 10, int startingParts = 0, int sessionLengthTicks = 5600)
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            WaveDefinitionAsset[] waves = BasicIdleAutoDefenseGame.CreateWaveDefinitions();
            RunUpgradeDefinitionAsset[] upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);
            return GameContentSetAsset.CreateTransient(
                "contentset.test.strict-authored-core",
                "Strict Authored Core Test",
                weapons[0],
                weapons,
                enemies,
                waves,
                upgrades,
                startingCredits,
                startingParts,
                1f,
                1f,
                sessionLengthTicks,
                false,
                "Strict authored-core fixture.",
                new[] { "test", "strict-authored" });
        }

        private static IdleAutoDefenseTemplateController CreateStrictController(GameContentSetAsset contentSet)
        {
            GameObject host = new GameObject("idle-auto-defense-strict-authored-core-test");
            host.SetActive(false);
            IdleAutoDefenseTemplateController controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            FieldInfo field = typeof(IdleAutoDefenseTemplateController).GetField("_contentSet", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(controller, contentSet);
            controller.ConfigureStrictAuthoredStartup(true);
            host.SetActive(true);
            controller.Build();
            return controller;
        }

        private static void ConfigureEconomy(IdleAutoDefenseEconomyAsset economy, long passiveIncomeAmount)
        {
            economy.Configure(
                economy.Id,
                economy.DisplayName,
                economy.Currencies,
                economy.PrimaryCurrencyId,
                economy.SecondaryCurrencyId,
                economy.PassiveIncomeCurrencyId,
                passiveIncomeAmount,
                economy.PassiveIncomeIntervalTicks,
                economy.UpgradeCosts,
                economy.EncounterCompletionCredits,
                economy.EncounterCompletionParts,
                economy.EncounterCompletionAccountXp,
                economy.SmallCurrencyBonus,
                economy.RunRewardClaimMultiplier);
        }

        private static void ConfigureProgression(
            IdleAutoDefenseProgressionAsset progression,
            IdleAutoDefenseResearchNodeRecord[] nodes)
        {
            progression.Configure(
                progression.Id,
                progression.DisplayName,
                progression.Tracks,
                nodes,
                progression.EncounterCompletionUnlockIds,
                progression.AccountTrackId,
                progression.ProfileDocumentId,
                progression.RunDocumentId,
                progression.SettingsDocumentId,
                progression.SaveVersion);
        }

        private static void DestroyController(IdleAutoDefenseTemplateController controller)
        {
            if (controller != null && controller.gameObject != null)
                UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }

        private static string FormatIssues(GameContentSetValidationReport report)
        {
            var builder = new StringBuilder();
            foreach (GameContentSetValidationIssue issue in report.Issues)
                builder.Append(issue.Severity).Append(' ').Append(issue.Path).Append(": ").AppendLine(issue.Message);
            return builder.ToString();
        }
    }
}
