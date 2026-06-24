using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.AutoDefense;
using Deucarian.Encounters;
using Deucarian.IdleProgression;
using Deucarian.Monetization;
using Deucarian.Progression;
using Deucarian.RunUpgrades;
using Deucarian.TemplateGameIdleAutoDefense.Editor;
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
            Assert.AreEqual(6, definition.Enemies.Count);
            Assert.AreEqual(2, definition.Mounts.Count);
            Assert.AreEqual(2, definition.WeaponModules.Count);
            Assert.IsTrue(definition.Mounts[0].HasWeapon);
            Assert.IsTrue(definition.Mounts[1].HasWeapon);
            Assert.AreEqual(BasicIdleAutoDefenseGame.SwarmEnemySpawnableId, definition.Enemies[0].SpawnableId);
            Assert.AreEqual(BasicIdleAutoDefenseGame.BossEnemySpawnableId, definition.Enemies[5].SpawnableId);
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
            Assert.AreEqual(1260, first.Reward.CurrencyLines[0].Amount.Value);
            Assert.AreEqual(15, first.Reward.CurrencyLines[1].Amount.Value);
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
            Assert.That(result.Credits, Is.GreaterThanOrEqualTo(1320));
            Assert.That(result.Parts, Is.GreaterThanOrEqualTo(18));
            Assert.That(result.Experience, Is.EqualTo(35));
        }

        [Test]
        public void EncounterCompletionRewardUsesTemplateCurrency()
        {
            ProgressionCatalog catalog = BasicIdleAutoDefenseGame.CreateProgressionCatalog();
            var state = new ProgressionState();

            ProgressionResult result = state.ApplyReward(catalog, new ProgressionOperationId("template.test.reward"), BasicIdleAutoDefenseGame.CreateEncounterCompletionReward());

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(60, state.GetBalance(BasicIdleAutoDefenseGame.Credits).Value);
            Assert.AreEqual(3, state.GetBalance(BasicIdleAutoDefenseGame.Parts).Value);
            Assert.AreEqual(35, state.GetTrackTotal(BasicIdleAutoDefenseGame.AccountXp).Value);
            Assert.IsTrue(state.IsUnlocked(BasicIdleAutoDefenseGame.Stage2Unlock));
            Assert.IsTrue(state.IsUnlocked(BasicIdleAutoDefenseGame.PulseCannonUnlock));
            Assert.IsTrue(state.IsUnlocked(BasicIdleAutoDefenseGame.ShardLauncherUnlock));
        }

        [Test]
        public void CanonicalFlowDocsAndDefaultContentPackArePresent()
        {
            string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BasicIdleAutoDefenseGame).Assembly).resolvedPath;

            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "canonical-game-flow.md"), "Boot");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "canonical-game-flow.md"), "resolve monetization availability");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "default-content-and-balance.md"), "DefaultBalance");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "default-content-and-balance.md"), "DefaultMonetization");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "asset-flip-workflow.md"), "Create Game From Template");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "override-guide.md"), "Copy `Samples~/BasicIdleAutoDefenseGame/Content`");

            string contentRoot = Path.Combine(packageRoot, "Samples~", "BasicIdleAutoDefenseGame", "Content");
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultBalance"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultStages"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultEnemies"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultWeapons"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultWaves"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultUpgrades"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultProgression"));
            AssertDirectoryExists(Path.Combine(contentRoot, "DefaultMonetization"));

            AssertFileContains(Path.Combine(contentRoot, "DefaultBalance", "objective-and-loop.json"), "template-core");
            AssertFileContains(Path.Combine(contentRoot, "DefaultStages", "stages.json"), "stage.template.boss-pulse");
            AssertFileContains(Path.Combine(contentRoot, "DefaultEnemies", "enemy-archetypes.json"), "enemy.template.boss");
            AssertFileContains(Path.Combine(contentRoot, "DefaultWeapons", "default-weapons.json"), "weapon.template.shard-launcher");
            AssertFileContains(Path.Combine(contentRoot, "DefaultWaves", "stages-and-encounters.json"), "wave.template.boss-pulse.boss");
            AssertFileContains(Path.Combine(contentRoot, "DefaultUpgrades", "common-run-upgrades.json"), "upgrade.template.projectile-specialization");
            AssertFileContains(Path.Combine(contentRoot, "DefaultProgression", "currencies-rewards-saves.json"), "research.template.core-plating");
            AssertFileContains(Path.Combine(contentRoot, "DefaultMonetization", "mock-placements.json"), "template.rewarded.double-offline-reward");
            AssertFileContains(Path.Combine(packageRoot, "Samples~", "BasicIdleAutoDefenseGame", "Prefabs", "Enemies", "README.md"), "Swarm");
            AssertFileContains(Path.Combine(packageRoot, "Samples~", "BasicIdleAutoDefenseGame", "Prefabs", "Weapons", "README.md"), "Pulse Cannon");
            AssertFileContains(Path.Combine(packageRoot, "Samples~", "BasicIdleAutoDefenseGame", "Prefabs", "Projectiles", "README.md"), "projectile");
        }

        [Test]
        public void DefaultContentIdsAreUniqueAndVerticalSliceSized()
        {
            AutoDefenseDefinition definition = BasicIdleAutoDefenseGame.CreateDefinition();
            RunUpgradeCatalog upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog();
            StageDefinition[] stages = BasicIdleAutoDefenseGame.CreateStageDefinitions();
            EncounterDefinition[] encounters = BasicIdleAutoDefenseGame.CreateEncounterDefinitions();
            IdleAutoDefenseTemplateModuleContent[] modules = IdleAutoDefenseTemplateDefaultContent.CreateModules();

            Assert.AreEqual(4, stages.Length);
            Assert.AreEqual(4, encounters.Length);
            Assert.AreEqual(4, modules.Length);
            Assert.That(upgrades.Definitions.Count, Is.GreaterThanOrEqualTo(12));
            AssertUnique("enemy", definition.Enemies, enemy => enemy.SpawnableId.Value);
            AssertUnique("stage", stages, stage => stage.Id.Value);
            AssertUnique("encounter", encounters, encounter => encounter.Id.Value);
            AssertUnique("upgrade", upgrades.Definitions, upgrade => upgrade.Id.Value);
            AssertUnique("module", modules, module => module.Id);
        }

        [Test]
        public void StageDefinitionsReferenceKnownEnemiesWeaponsAndUpgrades()
        {
            var enemyIds = new HashSet<string>();
            AutoDefenseDefinition definition = BasicIdleAutoDefenseGame.CreateDefinition();
            for (int i = 0; i < definition.Enemies.Count; i++)
                enemyIds.Add(definition.Enemies[i].SpawnableId.Value);

            var weaponIds = new HashSet<string>();
            IdleAutoDefenseTemplateModuleContent[] modules = IdleAutoDefenseTemplateDefaultContent.CreateModules();
            for (int i = 0; i < modules.Length; i++)
                weaponIds.Add(modules[i].Id);

            var upgradeIds = new HashSet<string>();
            RunUpgradeCatalog upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog();
            for (int i = 0; i < upgrades.Definitions.Count; i++)
                upgradeIds.Add(upgrades.Definitions[i].Id.Value);

            IdleAutoDefenseTemplateStageContent[] stages = IdleAutoDefenseTemplateDefaultContent.CreateStages();
            for (int i = 0; i < stages.Length; i++)
            {
                Assert.That(stages[i].EnemyIds.Length, Is.GreaterThan(0), stages[i].Id);
                Assert.That(stages[i].WeaponIds.Length, Is.GreaterThan(0), stages[i].Id);
                Assert.That(stages[i].UpgradeIds.Length, Is.GreaterThan(0), stages[i].Id);
                AssertKnown(stages[i].EnemyIds, enemyIds, stages[i].Id + " enemy");
                AssertKnown(stages[i].WeaponIds, weaponIds, stages[i].Id + " weapon");
                AssertKnown(stages[i].UpgradeIds, upgradeIds, stages[i].Id + " upgrade");
            }
        }

        [Test]
        public void UpgradeDefinitionsAreValidAndContainPlayableAndFutureIntentSet()
        {
            RunUpgradeCatalog catalog = BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog();
            Assert.That(catalog.Definitions.Count, Is.GreaterThanOrEqualTo(12));
            AssertUpgradeExists(catalog, "upgrade.template.damage-up");
            AssertUpgradeExists(catalog, "upgrade.template.fire-rate-up");
            AssertUpgradeExists(catalog, "upgrade.template.projectile-count-up");
            AssertUpgradeExists(catalog, "upgrade.template.projectile-speed-up");
            AssertUpgradeExists(catalog, "upgrade.template.objective-max-health-up");
            AssertUpgradeExists(catalog, "upgrade.template.objective-repair");
            AssertUpgradeExists(catalog, "upgrade.template.shield-restore-intent");
            AssertUpgradeExists(catalog, "upgrade.template.enemy-reward-up");
            AssertUpgradeExists(catalog, "upgrade.template.offline-gain-up");
            AssertUpgradeExists(catalog, "upgrade.template.reroll-bonus");
            AssertUpgradeExists(catalog, "upgrade.template.crit-chance-intent");
            AssertUpgradeExists(catalog, "upgrade.template.crit-damage-intent");
            AssertUpgradeExists(catalog, "upgrade.template.direct-specialization");
            AssertUpgradeExists(catalog, "upgrade.template.projectile-specialization");

            for (int i = 0; i < catalog.Definitions.Count; i++)
            {
                RunUpgradeDefinition upgrade = catalog.Definitions[i];
                Assert.That(upgrade.MaxRank, Is.GreaterThan(0), upgrade.Id.Value);
                Assert.That(upgrade.Weight, Is.GreaterThan(0), upgrade.Id.Value);
                Assert.That(upgrade.Effects.Count, Is.GreaterThan(0), upgrade.Id.Value);
            }
        }

        [Test]
        public void TemplateCanCompleteFailRewardRestartAndApplyOfflineReward()
        {
            IdleAutoDefenseTemplateController controller = CreateController();
            try
            {
                StepToTerminal(controller, BasicIdleAutoDefenseGame.CreateFirstOrbitEncounterDefinition(), expectCompletion: true);
                Assert.That(controller.EncounterRewardCredits, Is.GreaterThanOrEqualTo(60));
                Assert.That(controller.EncounterRewardParts, Is.GreaterThanOrEqualTo(3));

                controller.RestartRun(BasicIdleAutoDefenseGame.CreateBossPulseEncounterDefinition());
                StepUntilTerminal(controller, 720);
                Assert.IsTrue(controller.EncounterFailed, "Boss Pulse should be a fail-capable default slice.");

                controller.SimulateOfflineReward(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddHours(1));
                Assert.AreEqual(IdleProgressionResultCode.Success, controller.LastOfflineRewardCode);
                Assert.That(controller.OfflineRewardCredits, Is.GreaterThanOrEqualTo(1260));
                Assert.That(controller.OfflineRewardParts, Is.GreaterThanOrEqualTo(15));
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void ProgressionResearchDefaultsCanBePurchased()
        {
            ProgressionCatalog catalog = BasicIdleAutoDefenseGame.CreateProgressionCatalog();
            var state = new ProgressionState();
            state.ApplyReward(
                catalog,
                new ProgressionOperationId("template.test.seed"),
                new RewardBundle(
                    new[]
                    {
                        new CurrencyLine(BasicIdleAutoDefenseGame.Credits, new ProgressionAmount(500), true),
                        new CurrencyLine(BasicIdleAutoDefenseGame.Parts, new ProgressionAmount(20), true)
                    },
                    unlocks: new[] { BasicIdleAutoDefenseGame.PulseCannonUnlock, BasicIdleAutoDefenseGame.ShardLauncherUnlock }));

            Assert.IsTrue(state.PurchaseResearch(catalog, new ProgressionOperationId("template.test.core-rank-1"), BasicIdleAutoDefenseGame.CorePlatingResearch).Succeeded);
            Assert.IsTrue(state.PurchaseResearch(catalog, new ProgressionOperationId("template.test.offline-rank-1"), BasicIdleAutoDefenseGame.OfflineRoutingResearch).Succeeded);
            Assert.IsTrue(state.PurchaseResearch(catalog, new ProgressionOperationId("template.test.pulse-rank-1"), BasicIdleAutoDefenseGame.PulseCapacitorResearch).Succeeded);
            Assert.IsTrue(state.PurchaseResearch(catalog, new ProgressionOperationId("template.test.shard-rank-1"), BasicIdleAutoDefenseGame.ShardLoaderResearch).Succeeded);
            Assert.AreEqual(1, state.GetResearchRank(BasicIdleAutoDefenseGame.CorePlatingResearch));
            Assert.AreEqual(1, state.GetResearchRank(BasicIdleAutoDefenseGame.OfflineRoutingResearch));
        }

        [Test]
        public void SetupWizardCopiesStarterToProjectOwnedFolderAndBlocksOverwrite()
        {
            string tempRoot = "Assets/Temp/IdleAutoDefenseWizardTests";
            string targetRoot = tempRoot + "/WizardSmoke" + Guid.NewGuid().ToString("N");
            var request = new IdleAutoDefenseTemplateSetupRequest
            {
                TargetRootAssetPath = targetRoot,
                GameNamespace = "WizardSmoke.IdleAutoDefense",
                GamePrefix = "Wizard Smoke",
                AllowOverwrite = false,
                OpenCreatedScene = false,
                RefreshAssetDatabase = false
            };

            try
            {
                IdleAutoDefenseTemplateSetupResult result = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);

                Assert.IsTrue(result.Succeeded, result.CreateSummary());
                Assert.AreEqual(IdleAutoDefenseTemplateSetupStatus.Succeeded, result.Status);
                AssertCreatedPathsStayUnderTarget(result, targetRoot);
                AssertFileExists(targetRoot + "/Scenes/WizardSmokeIdleAutoDefense.unity");
                AssertFileExists(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs");
                AssertFileExists(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs.meta");
                AssertFileExists(targetRoot + "/WizardSmoke.IdleAutoDefense.asmdef");
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Content/DefaultStages"));
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Content/DefaultEnemies"));
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Content/DefaultWeapons"));
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Content/DefaultWaves"));
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Content/DefaultUpgrades"));
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Content/DefaultProgression"));
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Content/DefaultMonetization"));
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Docs/asset-flip-checklist.md"), "product-owned");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Docs/setup-report.md"), "Deucarian.TemplateGameIdleAutoDefense");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs"), "namespace WizardSmoke.IdleAutoDefense");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs"), "WizardSmokeIdleAutoDefenseGameBootstrap");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/WizardSmoke.IdleAutoDefense.asmdef"), "Deucarian.TemplateGameIdleAutoDefense");

                string reportPath = AssetPathToFullPath(targetRoot + "/Docs/setup-report.md");
                File.WriteAllText(reportPath, "existing report");
                IdleAutoDefenseTemplateSetupResult blocked = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
                Assert.AreEqual(IdleAutoDefenseTemplateSetupStatus.BlockedByExistingFiles, blocked.Status);
                Assert.That(blocked.BlockedFiles.Count, Is.GreaterThan(0));
                Assert.AreEqual("existing report", File.ReadAllText(reportPath));

                request.AllowOverwrite = true;
                IdleAutoDefenseTemplateSetupResult overwritten = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
                Assert.IsTrue(overwritten.Succeeded, overwritten.CreateSummary());
                AssertCreatedPathsStayUnderTarget(overwritten, targetRoot);
                StringAssert.Contains("Idle Auto Defense Setup Report", File.ReadAllText(reportPath));
            }
            finally
            {
                DeleteDirectoryIfExists(AssetPathToFullPath(tempRoot));
            }
        }

        [Test]
        public void SetupWizardRejectsTargetsOutsideAssets()
        {
            var request = new IdleAutoDefenseTemplateSetupRequest
            {
                TargetRootAssetPath = "Assets/../Packages/WizardSmoke",
                GameNamespace = "WizardSmoke.IdleAutoDefense",
                GamePrefix = "Wizard Smoke",
                AllowOverwrite = false,
                OpenCreatedScene = false,
                RefreshAssetDatabase = false
            };

            IdleAutoDefenseTemplateSetupResult result = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);

            Assert.AreEqual(IdleAutoDefenseTemplateSetupStatus.Failed, result.Status);
            Assert.That(result.Messages.Count, Is.GreaterThan(0));
            StringAssert.Contains("Assets", result.Messages[0]);
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

        private static void AssertFileExists(string assetPath)
        {
            Assert.IsTrue(File.Exists(AssetPathToFullPath(assetPath)), "Expected file to exist: " + assetPath);
        }

        private static string AssetPathToFullPath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void AssertCreatedPathsStayUnderTarget(IdleAutoDefenseTemplateSetupResult result, string targetRoot)
        {
            for (int i = 0; i < result.CreatedFiles.Count; i++)
            {
                StringAssert.StartsWith(targetRoot + "/", result.CreatedFiles[i]);
                StringAssert.DoesNotContain("Packages/", result.CreatedFiles[i]);
                StringAssert.DoesNotContain("/Runtime/", result.CreatedFiles[i]);
                StringAssert.DoesNotContain("/Editor/", result.CreatedFiles[i]);
            }

            for (int i = 0; i < result.CreatedDirectories.Count; i++)
            {
                StringAssert.StartsWith(targetRoot, result.CreatedDirectories[i]);
                StringAssert.DoesNotContain("Packages/", result.CreatedDirectories[i]);
                StringAssert.DoesNotContain("/Runtime/", result.CreatedDirectories[i]);
                StringAssert.DoesNotContain("/Editor/", result.CreatedDirectories[i]);
            }
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            string metaPath = path + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);
        }

        private static IdleAutoDefenseTemplateController CreateController()
        {
            GameObject host = new GameObject("idle-auto-defense-template-editmode");
            return host.AddComponent<IdleAutoDefenseTemplateController>();
        }

        private static void StepToTerminal(IdleAutoDefenseTemplateController controller)
        {
            StepToTerminal(controller, BasicIdleAutoDefenseGame.CreateEncounterDefinition(), expectCompletion: null);
        }

        private static void StepToTerminal(IdleAutoDefenseTemplateController controller, EncounterDefinition encounter, bool? expectCompletion)
        {
            controller.RestartRun(encounter);
            StepUntilTerminal(controller, 720);
            if (expectCompletion.HasValue)
            {
                if (expectCompletion.Value) Assert.IsTrue(controller.EncounterCompleted, controller.StatusSummary);
                else Assert.IsTrue(controller.EncounterFailed, controller.StatusSummary);
            }
        }

        private static void StepUntilTerminal(IdleAutoDefenseTemplateController controller, int maxTicks)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                controller.Step(1, 0.05f);
                if (controller.EncounterCompleted || controller.EncounterFailed)
                    break;
            }

            Assert.IsTrue(controller.EncounterCompleted || controller.EncounterFailed);
        }

        private static void AssertUpgradeExists(RunUpgradeCatalog catalog, string id)
        {
            Assert.IsTrue(catalog.TryGet(new RunUpgradeId(id), out _), "Expected upgrade: " + id);
        }

        private static void AssertKnown(string[] actual, HashSet<string> known, string label)
        {
            for (int i = 0; i < actual.Length; i++)
                Assert.IsTrue(known.Contains(actual[i]), "Unknown " + label + ": " + actual[i]);
        }

        private static void AssertUnique<T>(string label, IReadOnlyList<T> values, Func<T, string> getId)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < values.Count; i++)
            {
                string id = getId(values[i]);
                Assert.IsFalse(string.IsNullOrEmpty(id), "Empty " + label + " id at " + i);
                Assert.IsTrue(seen.Add(id), "Duplicate " + label + " id: " + id);
            }
        }

        private static void DestroyController(IdleAutoDefenseTemplateController controller)
        {
            if (controller != null)
                UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }
    }
}
