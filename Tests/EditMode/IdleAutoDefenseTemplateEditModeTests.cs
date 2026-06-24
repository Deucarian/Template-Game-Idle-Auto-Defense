using System;
using System.IO;
using Deucarian.AutoDefense;
using Deucarian.IdleProgression;
using Deucarian.Monetization;
using Deucarian.Progression;
using Deucarian.RunUpgrades;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseTemplateEditModeTests
    {
        [Test]
        public void DefinitionHasCentralObjectivePerimeterEnemiesAndTwoWeaponModes()
        {
            AutoDefenseDefinition definition = BasicIdleAutoDefenseGame.CreateDefinition();

            Assert.AreEqual("template-core", definition.Objective.Id.Value);
            Assert.AreEqual(4, definition.SpawnRing.Channels.Count);
            Assert.AreEqual(1, definition.Enemies.Count);
            Assert.AreEqual(2, definition.Mounts.Count);
            Assert.AreEqual(2, definition.WeaponModules.Count);
            Assert.IsTrue(definition.Mounts[0].HasWeapon);
            Assert.IsTrue(definition.Mounts[1].HasWeapon);
        }

        [Test]
        public void UpgradeDraftOffersAtLeastThreeDeterministicChoices()
        {
            RunUpgradeCatalog catalog = BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog();
            var state = new RunUpgradeState();

            RunUpgradeDraft first = RunUpgradeDraftService.Generate(catalog, state, new RunUpgradeDraftRequest(3, 20260623));
            RunUpgradeDraft second = RunUpgradeDraftService.Generate(catalog, state, new RunUpgradeDraftRequest(3, 20260623));

            Assert.AreEqual(3, first.Choices.Count);
            Assert.AreEqual(first.Choices[0].Id.Value, second.Choices[0].Id.Value);
            Assert.AreEqual(first.Choices[1].Id.Value, second.Choices[1].Id.Value);
            Assert.AreEqual(first.Choices[2].Id.Value, second.Choices[2].Id.Value);
        }

        [Test]
        public void OfflineRewardIsCappedAndDeterministic()
        {
            IdleProgressionDefinition definition = BasicIdleAutoDefenseGame.CreateOfflineProgressionDefinition();
            DateTimeOffset start = DateTimeOffset.UnixEpoch;
            DateTimeOffset end = start.AddHours(1);

            IdleProgressionResult first = IdleProgressionCalculator.Calculate(start, end, definition);
            IdleProgressionResult second = IdleProgressionCalculator.Calculate(start, end, definition);

            Assert.AreEqual(IdleProgressionResultCode.Success, first.Code);
            Assert.AreEqual(900, first.Reward.CurrencyLines[0].Amount.Value);
            Assert.AreEqual(12, first.Reward.CurrencyLines[1].Amount.Value);
            Assert.AreEqual(first.Reward.CurrencyLines[0].Amount.Value, second.Reward.CurrencyLines[0].Amount.Value);
            Assert.AreEqual(first.Reward.CurrencyLines[1].Amount.Value, second.Reward.CurrencyLines[1].Amount.Value);
        }

        [Test]
        public void SaveLoadProgressionAndCorruptRecoverySmokePasses()
        {
            IdleAutoDefenseTemplateCompositionSmokeResult result = IdleAutoDefenseTemplateSaveProgressionComposition.RunSmoke();

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.ProfileSavedAndLoaded);
            Assert.IsTrue(result.RunSavedAndLoaded);
            Assert.IsTrue(result.SettingsSavedAndLoaded);
            Assert.IsTrue(result.RunRewardApplied);
            Assert.IsTrue(result.RunUpgradeSnapshotRestored);
            Assert.IsTrue(result.OfflineRewardCalculated);
            Assert.IsTrue(result.MissingSaveDefaulted);
            Assert.IsTrue(result.CorruptedPrimaryRecovered);
            Assert.IsTrue(result.MigrationApplied);
            Assert.That(result.Credits, Is.GreaterThanOrEqualTo(925));
            Assert.That(result.Parts, Is.GreaterThanOrEqualTo(12));
            Assert.That(result.Experience, Is.EqualTo(10));
        }

        [Test]
        public void EncounterCompletionRewardUsesTemplateCurrency()
        {
            ProgressionCatalog catalog = BasicIdleAutoDefenseGame.CreateProgressionCatalog();
            var state = new ProgressionState();

            ProgressionResult result = state.ApplyReward(catalog, new ProgressionOperationId("template.test.reward"), BasicIdleAutoDefenseGame.CreateEncounterCompletionReward());

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(25, state.GetBalance(BasicIdleAutoDefenseGame.Credits).Value);
            Assert.AreEqual(1, state.GetBalance(BasicIdleAutoDefenseGame.Parts).Value);
        }

        [Test]
        public void CanonicalFlowDocsAndDefaultContentPackArePresent()
        {
            string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BasicIdleAutoDefenseGame).Assembly).resolvedPath;

            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "canonical-game-flow.md"), "Boot");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "canonical-game-flow.md"), "resolve monetization availability");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "default-content-and-balance.md"), "DefaultBalance");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "default-content-and-balance.md"), "DefaultMonetization");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "override-guide.md"), "Copy `Samples~/BasicIdleAutoDefenseGame/Content`");

            string contentRoot = Path.Combine(packageRoot, "Samples~", "BasicIdleAutoDefenseGame", "Content");
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultBalance"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultEnemies"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultWeapons"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultWaves"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultUpgrades"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultProgression"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultMonetization"));

            AssertFileContains(Path.Combine(contentRoot, "DefaultBalance", "objective-and-loop.json"), "template-core");
            AssertFileContains(Path.Combine(contentRoot, "DefaultEnemies", "basic-idle-enemy.json"), "enemy.template.basic");
            AssertFileContains(Path.Combine(contentRoot, "DefaultWeapons", "default-weapons.json"), "weapon.template.projectile");
            AssertFileContains(Path.Combine(contentRoot, "DefaultWaves", "basic-encounter.json"), "wave.template.basic");
            AssertFileContains(Path.Combine(contentRoot, "DefaultUpgrades", "common-run-upgrades.json"), "upgrade.template.direct.damage");
            AssertFileContains(Path.Combine(contentRoot, "DefaultProgression", "currencies-rewards-saves.json"), "saveDocuments");
            AssertFileContains(Path.Combine(contentRoot, "DefaultMonetization", "mock-placements.json"), "template.rewarded.double-offline-reward");
        }

        [Test]
        public void TemplateOfflineDoubleRewardOfferUsesMockPlacement()
        {
            IdleAutoDefenseTemplateController controller = CreateController();
            try
            {
                controller.SimulateOfflineReward(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddHours(1));
                long credits = controller.OfflineRewardCredits;
                long parts = controller.OfflineRewardParts;
                Assert.That(credits, Is.GreaterThan(0));
                Assert.That(parts, Is.GreaterThan(0));

                MonetizationResult result = controller.OfferDoubleOfflineReward(
                    new RewardClaimId("template.test.offline.2x"),
                    DateTimeOffset.UnixEpoch.AddHours(1));

                Assert.AreEqual(MonetizationResultCode.Success, result.Code);
                Assert.AreEqual(credits * 2, controller.OfflineRewardCredits);
                Assert.AreEqual(parts * 2, controller.OfflineRewardParts);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void TemplateRerollOfferUsesMockPlacement()
        {
            IdleAutoDefenseTemplateController controller = CreateController();
            try
            {
                MonetizationResult result = controller.OfferUpgradeDraftReroll(
                    new RewardClaimId("template.test.reroll"),
                    DateTimeOffset.UnixEpoch.AddMinutes(1));

                Assert.AreEqual(MonetizationResultCode.Success, result.Code);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void TemplateReviveOfferUsesMockPlacement()
        {
            IdleAutoDefenseTemplateController controller = CreateController();
            try
            {
                MonetizationResult result = controller.OfferReviveAfterFailure(
                    new RewardClaimId("template.test.revive"),
                    DateTimeOffset.UnixEpoch.AddMinutes(2));

                Assert.AreEqual(MonetizationResultCode.Success, result.Code);
                Assert.IsTrue(controller.ReviveOfferAccepted);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void TemplateDoubleRewardOfferUsesMockPlacement()
        {
            IdleAutoDefenseTemplateController controller = CreateController();
            try
            {
                StepToTerminal(controller);
                long credits = controller.EncounterRewardCredits;
                long parts = controller.EncounterRewardParts;
                Assert.That(credits, Is.GreaterThan(0));
                Assert.That(parts, Is.GreaterThan(0));

                MonetizationResult result = controller.OfferDoubleRunReward(
                    new RewardClaimId("template.test.run.2x"),
                    DateTimeOffset.UnixEpoch.AddMinutes(3));

                Assert.AreEqual(MonetizationResultCode.Success, result.Code);
                Assert.AreEqual(credits * 2, controller.EncounterRewardCredits);
                Assert.AreEqual(parts * 2, controller.EncounterRewardParts);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void TemplateStillRunsWithNoOpProvider()
        {
            IdleAutoDefenseTemplateController controller = CreateController();
            try
            {
                controller.MonetizationSession = IdleAutoDefenseTemplateMonetization.CreateNoOpSession();
                StepToTerminal(controller);

                MonetizationResult bonus = controller.OfferSmallCurrencyBonus(
                    new RewardClaimId("template.test.noop.bonus"),
                    DateTimeOffset.UnixEpoch.AddMinutes(4));

                Assert.IsTrue(controller.EncounterCompleted || controller.EncounterFailed);
                Assert.AreEqual(MonetizationResultCode.NoOp, bonus.Code);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void TemplateTransitionInterstitialUsesMockPlacementAndPacing()
        {
            IdleAutoDefenseTemplateController controller = CreateController();
            try
            {
                StepToTerminal(controller);

                MonetizationResult first = controller.TryShowTransitionInterstitial(
                    afterFailure: false,
                    DateTimeOffset.UnixEpoch.AddMinutes(5));
                MonetizationResult second = controller.TryShowTransitionInterstitial(
                    afterFailure: true,
                    DateTimeOffset.UnixEpoch.AddMinutes(5).AddSeconds(30));

                Assert.AreEqual(MonetizationResultCode.Success, first.Code);
                Assert.AreEqual(MonetizationResultCode.CooldownActive, second.Code);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        private static void AssertDirectoryExists(string path)
        {
            Assert.IsTrue(Directory.Exists(path), "Expected directory to exist: " + path);
        }

        private static void AssertFileContains(string path, string expected)
        {
            Assert.IsTrue(File.Exists(path), "Expected file to exist: " + path);
            StringAssert.Contains(expected, File.ReadAllText(path));
        }

        private static IdleAutoDefenseTemplateController CreateController()
        {
            GameObject host = new GameObject("idle-auto-defense-template-editmode");
            return host.AddComponent<IdleAutoDefenseTemplateController>();
        }

        private static void StepToTerminal(IdleAutoDefenseTemplateController controller)
        {
            controller.Build();
            for (int i = 0; i < 240; i++)
                controller.Step(1, 0.05f);
            Assert.IsTrue(controller.EncounterCompleted || controller.EncounterFailed);
        }

        private static void DestroyController(IdleAutoDefenseTemplateController controller)
        {
            if (controller != null)
                UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }
    }
}
