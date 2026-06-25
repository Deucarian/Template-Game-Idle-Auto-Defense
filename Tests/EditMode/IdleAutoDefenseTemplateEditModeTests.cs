using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Encounters;
using Deucarian.GameContentAuthoring.Editor;
using Deucarian.IdleProgression;
using Deucarian.Monetization;
using Deucarian.Progression;
using Deucarian.RunUpgrades;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.TemplateGameIdleAutoDefense.Editor;
using Deucarian.WeaponSystems;
using Deucarian.WeaponSystems.Authoring;
using NUnit.Framework;
using UnityEditor.SceneManagement;
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
        public void EnemyAndWaveRecipesCreateRuntimeDefinitions()
        {
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            WaveDefinitionAsset[] waves = BasicIdleAutoDefenseGame.CreateWaveDefinitions();

            Assert.AreEqual(6, enemies.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.SwarmEnemySpawnableId.Value, enemies[0].Id);
            Assert.AreEqual(EnemyRole.Fast, enemies[1].Role);
            Assert.AreEqual(EnemyRole.Boss, enemies[5].Role);
            Assert.AreEqual(6, BasicIdleAutoDefenseGame.CreateAutoDefenseEnemyDefinitions(enemies).Length);

            Assert.AreEqual(2, waves.Length);
            Assert.AreEqual("wave.template.first-orbit.opening", waves[0].Id);
            Assert.AreEqual("wave.template.first-orbit.pressure", waves[1].Id);
            Assert.AreEqual(2, BasicIdleAutoDefenseGame.CreateEncounterWaves(waves).Length);
            Assert.AreEqual(2, waves[1].Entries.Entries.Count);
        }

        [Test]
        public void AttackRecipesCreateRuntimeDefinitionsAndProjectiles()
        {
            AttackDefinitionAsset[] recipes = BasicIdleAutoDefenseGame.CreateAttackRecipes();

            Assert.AreEqual(2, recipes.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.PulseAttackId.Value, recipes[0].Id);
            Assert.AreEqual(AttackRecipeDeliveryMode.Hitscan, recipes[0].Delivery.Mode);
            Assert.AreEqual(BasicIdleAutoDefenseGame.ShardAttackId.Value, recipes[1].Id);
            Assert.AreEqual(AttackRecipeDeliveryMode.Projectile, recipes[1].Delivery.Mode);

            Assert.AreEqual(2, BasicIdleAutoDefenseGame.CreateAttackDefinitions(recipes).Length);
            Assert.AreEqual(1, BasicIdleAutoDefenseGame.CreateProjectileDefinitions(recipes).Length);
            Assert.AreEqual(0, recipes[1].CreateStatusDefinitions().Length);
        }

        [Test]
        public void WeaponAndUpgradeRecipesCreateRuntimeDefinitions()
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
            RunUpgradeDefinitionAsset[] upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);

            Assert.AreEqual(2, weapons.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, weapons[0].Id);
            Assert.AreEqual(WeaponFireMode.DirectAttack, weapons[0].Stats.FireMode);
            Assert.AreEqual(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, weapons[1].Id);
            Assert.AreEqual(WeaponFireMode.Projectile, weapons[1].Stats.FireMode);
            Assert.AreEqual(2, BasicIdleAutoDefenseGame.CreateWeaponDefinitions(weapons).Length);
            Assert.AreEqual(2, BasicIdleAutoDefenseGame.CreateDefinition(null, weapons).WeaponModules.Count);

            Assert.AreEqual(4, upgrades.Length);
            Assert.AreEqual("upgrade.template.damage-up", upgrades[0].Id);
            Assert.AreEqual(4, BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitions(upgrades).Length);
            Assert.IsTrue(BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog(upgrades).TryGet(new RunUpgradeId("upgrade.template.projectile-speed-up"), out _));
        }

        [Test]
        public void AssignedAttackRecipesFallBackWhenRequiredTemplateIdsAreMissing()
        {
            AttackDefinitionAsset customOnly = AttackDefinitionAsset.CreateTransient(
                "attack.custom.only",
                "Custom Only",
                AttackRecipeDeliveryMode.Hitscan,
                BasicIdleAutoDefenseGame.DamageType.Value,
                3,
                0,
                5,
                AttackRecipeTargetingMode.Nearest);

            AttackDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveAttackRecipesForTemplate(new[] { customOnly, null }, out int rejectedRecipeCount);

            Assert.That(rejectedRecipeCount, Is.GreaterThan(0));
            Assert.AreEqual(2, resolved.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.PulseAttackId.Value, resolved[0].Id);
            Assert.AreEqual(BasicIdleAutoDefenseGame.ShardAttackId.Value, resolved[1].Id);
        }

        [Test]
        public void AssignedEnemyDefinitionsFallBackWhenRequiredTemplateIdsAreMissing()
        {
            EnemyDefinitionAsset customOnly = EnemyDefinitionAsset.CreateTransient(
                "enemy.custom.only",
                "Custom Only",
                EnemyRole.Basic,
                9f,
                2f,
                1,
                3f,
                BasicIdleAutoDefenseGame.DamageType.Value,
                prefab: new GameObject("custom-enemy-prefab"));

            try
            {
                EnemyDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveEnemyDefinitionsForTemplate(new[] { customOnly, null }, out int rejectedDefinitionCount);

                Assert.That(rejectedDefinitionCount, Is.GreaterThan(0));
                Assert.AreEqual(6, resolved.Length);
                Assert.AreEqual(BasicIdleAutoDefenseGame.SwarmEnemySpawnableId.Value, resolved[0].Id);
                Assert.AreEqual(BasicIdleAutoDefenseGame.BossEnemySpawnableId.Value, resolved[5].Id);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(customOnly.Presentation.Prefab);
            }
        }

        [Test]
        public void AssignedWaveDefinitionsFallBackWhenEnemyReferencesAreMissing()
        {
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            EnemyDefinitionAsset missingEnemy = EnemyDefinitionAsset.CreateTransient(
                "enemy.custom.missing",
                "Missing",
                EnemyRole.Basic,
                5f,
                2f,
                1,
                2f,
                BasicIdleAutoDefenseGame.DamageType.Value);
            WaveDefinitionAsset invalidWave = WaveDefinitionAsset.CreateTransient(
                "wave.custom.invalid",
                "Invalid",
                0,
                new[] { new WaveEntryRecipe(missingEnemy, 2, 1, 0, 10, "perimeter-north") });

            WaveDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveWaveDefinitionsForTemplate(new[] { invalidWave }, enemies, out int rejectedDefinitionCount);

            Assert.That(rejectedDefinitionCount, Is.GreaterThan(0));
            Assert.AreEqual(2, resolved.Length);
            Assert.AreEqual("wave.template.first-orbit.opening", resolved[0].Id);
            Assert.AreEqual("wave.template.first-orbit.pressure", resolved[1].Id);
        }

        [Test]
        public void ValidAssignedAuthoredContentResolvesWithoutFallback()
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            EnemyDefinitionAsset[] enemies = CreateAssignedTemplateEnemiesWithPrefabs();
            WaveDefinitionAsset[] waves = CreateAssignedTemplateWaves(enemies);
            WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
            RunUpgradeDefinitionAsset[] upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);

            try
            {
                AttackDefinitionAsset[] resolvedAttacks = BasicIdleAutoDefenseGame.ResolveAttackRecipesForTemplate(attacks, out int rejectedRecipeCount);
                EnemyDefinitionAsset[] resolvedEnemies = BasicIdleAutoDefenseGame.ResolveEnemyDefinitionsForTemplate(enemies, out int rejectedEnemyCount);
                WaveDefinitionAsset[] resolvedWaves = BasicIdleAutoDefenseGame.ResolveWaveDefinitionsForTemplate(waves, resolvedEnemies, out int rejectedWaveCount);
                WeaponDefinitionAsset[] resolvedWeapons = BasicIdleAutoDefenseGame.ResolveWeaponDefinitionsForTemplate(weapons, resolvedAttacks, out int rejectedWeaponCount);
                RunUpgradeDefinitionAsset[] resolvedUpgrades = BasicIdleAutoDefenseGame.ResolveUpgradeDefinitionsForTemplate(upgrades, out int rejectedUpgradeCount);

                Assert.AreEqual(0, rejectedRecipeCount);
                Assert.AreEqual(0, rejectedEnemyCount);
                Assert.AreEqual(0, rejectedWaveCount);
                Assert.AreEqual(0, rejectedWeaponCount);
                Assert.AreEqual(0, rejectedUpgradeCount);
                Assert.AreEqual(2, resolvedAttacks.Length);
                Assert.AreEqual(6, resolvedEnemies.Length);
                Assert.AreEqual(2, resolvedWaves.Length);
                Assert.AreEqual(2, resolvedWeapons.Length);
                Assert.AreEqual(4, resolvedUpgrades.Length);
                Assert.AreSame(enemies[5], resolvedEnemies[5]);
                Assert.AreSame(waves[1], resolvedWaves[1]);
                Assert.AreSame(weapons[1], resolvedWeapons[1]);
                Assert.AreSame(upgrades[3], resolvedUpgrades[3]);
            }
            finally
            {
                DestroyEnemyPrefabs(enemies);
            }
        }

        [Test]
        public void ValidGameContentSetValidationPassesAndResolvesRecipe()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);
            GameContentSetResolution resolution = BasicIdleAutoDefenseGame.ResolveGameContentSetForTemplate(contentSet);

            Assert.IsTrue(report.IsValid, FormatIssues(report));
            Assert.IsTrue(resolution.IsValid, FormatIssues(resolution.Report));
            Assert.AreEqual(2, resolution.AttackRecipes.Count);
            Assert.AreEqual(6, resolution.Enemies.Count);
            Assert.AreEqual(2, resolution.Waves.Count);
            Assert.AreEqual(2, resolution.Weapons.Count);
            Assert.AreEqual(4, resolution.Upgrades.Count);
            Assert.AreSame(contentSet.StartingWeapon, resolution.Weapons[0]);
        }

        [Test]
        public void GameContentSetValidationBlocksMissingStartingWeapon()
        {
            GameContentSetAsset contentSet = CreateValidContentSet(omitStartingWeapon: true);

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsFalse(report.IsValid);
            AssertHasIssue(report, "StartingWeapon");
        }

        [Test]
        public void GameContentSetValidationBlocksMissingWaves()
        {
            GameContentSetAsset contentSet = CreateValidContentSet(wavesOverride: Array.Empty<WaveDefinitionAsset>());

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsFalse(report.IsValid);
            AssertHasIssue(report, "WaveSet");
        }

        [Test]
        public void GameContentSetValidationBlocksWeaponWithMissingAttackReference()
        {
            WeaponDefinitionAsset invalidWeapon = WeaponDefinitionAsset.CreateTransient(
                "weapon.content-set.invalid",
                "Invalid Weapon",
                WeaponFireMode.DirectAttack,
                null,
                6,
                7f);
            GameContentSetAsset contentSet = CreateValidContentSet(startingWeaponOverride: invalidWeapon, weaponsOverride: new[] { invalidWeapon });

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsFalse(report.IsValid);
            AssertHasIssue(report, "AvailableWeapons[0].Stats.Attack");
        }

        [Test]
        public void GameContentSetValidationBlocksWaveReferencesMissingEnemies()
        {
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            EnemyDefinitionAsset missingEnemy = EnemyDefinitionAsset.CreateTransient(
                "enemy.content-set.missing",
                "Missing Enemy",
                EnemyRole.Basic,
                5f,
                2f,
                1,
                1f,
                BasicIdleAutoDefenseGame.DamageType.Value);
            WaveDefinitionAsset invalidWave = WaveDefinitionAsset.CreateTransient(
                "wave.content-set.invalid",
                "Invalid Wave",
                0,
                new[] { new WaveEntryRecipe(missingEnemy, 1, 1, 0, 0, "perimeter-north") });
            GameContentSetAsset contentSet = CreateValidContentSet(enemiesOverride: enemies, wavesOverride: new[] { invalidWave });

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsFalse(report.IsValid);
            AssertHasIssue(report, "WaveSet[0].Entries[0].Enemy");
        }

        [Test]
        public void GameContentSetValidationWarnsWhenUpgradeTargetsOutsideSet()
        {
            RunUpgradeDefinitionAsset outsideTarget = RunUpgradeDefinitionAsset.CreateTransient(
                "upgrade.content-set.external-target",
                "External Target",
                RunUpgradeRarity.Common,
                1,
                1,
                new[]
                {
                    new RunUpgradeEffectRecipe(
                        RunUpgradeAuthoringTargetKind.AttackDamage,
                        RunUpgradeModifierType.Additive,
                        1,
                        targetIdOverride: "weapon.external.not-in-set")
                });
            GameContentSetAsset contentSet = CreateValidContentSet(upgradesOverride: new[] { outsideTarget });

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsTrue(report.IsValid, FormatIssues(report));
            Assert.That(report.WarningCount, Is.GreaterThan(0));
            AssertHasIssue(report, "UpgradePool[0].Effects[0].Target");
        }

        [Test]
        public void ControllerUsesValidAssignedGameContentSetWithoutFallback()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            IdleAutoDefenseTemplateController controller = CreateControllerWithContentSet(contentSet);
            try
            {
                Assert.IsTrue(controller.UsingAssignedContentSet, controller.AssignedContentSetStatus);
                Assert.AreEqual(0, controller.InvalidAssignedContentSetIssueCount);
                Assert.AreEqual(0, controller.InvalidAssignedRecipeCount);
                Assert.AreEqual(0, controller.InvalidAssignedEnemyCount);
                Assert.AreEqual(0, controller.InvalidAssignedWaveCount);
                Assert.AreEqual(0, controller.InvalidAssignedWeaponCount);
                Assert.AreEqual(0, controller.InvalidAssignedUpgradeCount);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void ControllerFallsBackSafelyWhenAssignedGameContentSetIsInvalid()
        {
            GameContentSetAsset contentSet = CreateValidContentSet(omitStartingWeapon: true);
            IdleAutoDefenseTemplateController controller = CreateControllerWithContentSet(contentSet);
            try
            {
                Assert.IsFalse(controller.UsingAssignedContentSet);
                Assert.That(controller.InvalidAssignedContentSetIssueCount, Is.GreaterThan(0));
                Assert.IsNotNull(controller.Runtime);
                Assert.AreEqual("Running", controller.RuntimeStateName);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void GameContentSetPreviewSummaryDoesNotThrowWithMissingOptionalAssets()
        {
            GameContentSetAuthoringState state = CreateValidContentSetAuthoringState();
            state.Icon = null;
            state.Banner = null;
            GameContentSetAsset preview = GameContentSetAsset.CreateTransient(
                state.ContentSetId,
                state.DisplayName,
                state.StartingWeapon,
                state.AvailableWeapons,
                state.EnemyPool,
                state.WaveSet,
                state.UpgradePool);
            GameContentSetValidationReport report = GameContentSetValidator.Validate(preview);

            Assert.DoesNotThrow(() => GameContentSetAuthoringPreviewSummaries.BuildSummaryRows(state));
            Assert.DoesNotThrow(() => GameContentSetAuthoringPreviewSummaries.BuildDependencyRows(state));
            Assert.DoesNotThrow(() => GameContentSetAuthoringPreviewSummaries.BuildWarnings(report));
        }

        [Test]
        public void GameContentSetPreviewSummaryDoesNotDirtyActiveScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            bool wasDirty = scene.isDirty;
            GameContentSetAuthoringState state = CreateValidContentSetAuthoringState();

            GameContentSetAuthoringPreviewSummaries.BuildSummaryRows(state);
            GameContentSetAuthoringPreviewSummaries.BuildDependencyRows(state);

            Assert.AreEqual(wasDirty, EditorSceneManager.GetActiveScene().isDirty);
        }

        [Test]
        public void GameContentSetProviderRegistersWithSharedAuthoringWindow()
        {
            Assert.AreEqual("Tools/Deucarian/Game Content Authoring", GameContentAuthoringWindow.MenuPath);
            Assert.IsTrue(GameContentAuthoringProviderRegistry.IsProviderRegistered("com.deucarian.template.idle-auto-defense.game-content-set"));
        }

        [Test]
        public void AssignedEnemyDefinitionsRejectDuplicatesAndFallbackWhenIncomplete()
        {
            EnemyDefinitionAsset[] enemies = CreateAssignedTemplateEnemiesWithPrefabs();
            try
            {
                enemies[5].Configure(
                    BasicIdleAutoDefenseGame.RunnerEnemySpawnableId.Value,
                    "Duplicate Runner",
                    null,
                    EnemyRole.Boss,
                    Array.Empty<string>(),
                    enemies[5].Stats,
                    enemies[5].Presentation);

                EnemyDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveEnemyDefinitionsForTemplate(enemies, out int rejectedDefinitionCount);

                Assert.That(rejectedDefinitionCount, Is.GreaterThan(0));
                Assert.AreEqual(6, resolved.Length);
                Assert.AreEqual(BasicIdleAutoDefenseGame.BossEnemySpawnableId.Value, resolved[5].Id);
                Assert.AreNotSame(enemies[5], resolved[5]);
            }
            finally
            {
                DestroyEnemyPrefabs(enemies);
            }
        }

        [Test]
        public void AssignedWaveDefinitionsRejectDuplicateIds()
        {
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            WaveDefinitionAsset first = WaveDefinitionAsset.CreateTransient(
                "wave.custom.duplicate",
                "Duplicate One",
                0,
                new[] { new WaveEntryRecipe(enemies[0], 2, 1, 0, 10, "perimeter-north") });
            WaveDefinitionAsset second = WaveDefinitionAsset.CreateTransient(
                "wave.custom.duplicate",
                "Duplicate Two",
                10,
                new[] { new WaveEntryRecipe(enemies[1], 2, 1, 0, 10, "perimeter-east") });

            WaveDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveWaveDefinitionsForTemplate(new[] { first, second }, enemies, out int rejectedDefinitionCount);

            Assert.That(rejectedDefinitionCount, Is.GreaterThan(0));
            Assert.AreEqual(1, resolved.Length);
            Assert.AreSame(first, resolved[0]);
        }

        [Test]
        public void AssignedWeaponDefinitionsFallBackWhenRequiredIdsOrAttackRefsAreMissing()
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            WeaponDefinitionAsset customOnly = WeaponDefinitionAsset.CreateTransient(
                "weapon.custom.only",
                "Custom Only",
                WeaponFireMode.DirectAttack,
                attacks[0],
                6,
                8f);

            WeaponDefinitionAsset[] missingRequired = BasicIdleAutoDefenseGame.ResolveWeaponDefinitionsForTemplate(new[] { customOnly }, attacks, out int missingRequiredRejected);

            Assert.That(missingRequiredRejected, Is.GreaterThan(0));
            Assert.AreEqual(2, missingRequired.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, missingRequired[0].Id);

            AttackDefinitionAsset foreignAttack = AttackDefinitionAsset.CreateTransient(
                "attack.foreign.only",
                "Foreign Attack",
                AttackRecipeDeliveryMode.Hitscan,
                BasicIdleAutoDefenseGame.DamageType.Value,
                5,
                0,
                6,
                AttackRecipeTargetingMode.Nearest);
            WeaponDefinitionAsset invalidAttack = WeaponDefinitionAsset.CreateTransient(
                BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value,
                "Invalid Attack Ref",
                WeaponFireMode.DirectAttack,
                foreignAttack,
                6,
                8f);

            WeaponDefinitionAsset[] missingAttack = BasicIdleAutoDefenseGame.ResolveWeaponDefinitionsForTemplate(new[] { invalidAttack }, attacks, out int missingAttackRejected);

            Assert.That(missingAttackRejected, Is.GreaterThan(0));
            Assert.AreEqual(2, missingAttack.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, missingAttack[1].Id);
        }

        [Test]
        public void AssignedUpgradeDefinitionsRejectDuplicateIdsAndFallback()
        {
            WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(BasicIdleAutoDefenseGame.CreateAttackRecipes());
            RunUpgradeDefinitionAsset[] upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);
            RunUpgradeDefinitionAsset duplicate = RunUpgradeDefinitionAsset.CreateTransient(
                upgrades[0].Id,
                "Duplicate Damage",
                RunUpgradeRarity.Common,
                1,
                1,
                new[] { new RunUpgradeEffectRecipe(RunUpgradeAuthoringTargetKind.AttackDamage, RunUpgradeModifierType.Additive, 1, targetIdOverride: weapons[0].Id) });

            RunUpgradeDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveUpgradeDefinitionsForTemplate(new[] { upgrades[0], duplicate }, out int rejectedDefinitionCount);

            Assert.That(rejectedDefinitionCount, Is.GreaterThan(0));
            Assert.AreEqual(4, resolved.Length);
            Assert.AreEqual("upgrade.template.damage-up", resolved[0].Id);
            Assert.AreNotSame(upgrades[0], resolved[0]);
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
            string tempRoot = "Assets/T";
            string targetRoot = tempRoot + "/W" + Guid.NewGuid().ToString("N").Substring(0, 8);
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
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Content/ContentSets"));
                AssertFileExists(targetRoot + "/Content/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset");
                AssertFileExists(targetRoot + "/Content/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset.meta");
                string contentSetGuid = ReadMetaGuid(AssetPathToFullPath(targetRoot + "/Content/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset.meta"));
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Scenes/WizardSmokeIdleAutoDefense.unity"), contentSetGuid);
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

        private static EnemyDefinitionAsset[] CreateAssignedTemplateEnemiesWithPrefabs()
        {
            return new[]
            {
                EnemyDefinitionAsset.CreateTransient(BasicIdleAutoDefenseGame.SwarmEnemySpawnableId.Value, "Assigned Swarm Enemy", EnemyRole.Swarm, 7f, 2.8f, 1, 2f, BasicIdleAutoDefenseGame.DamageType.Value, 0.25f, new GameObject("assigned-swarm-enemy-prefab")),
                EnemyDefinitionAsset.CreateTransient(BasicIdleAutoDefenseGame.RunnerEnemySpawnableId.Value, "Assigned Runner Enemy", EnemyRole.Fast, 6f, 4.0f, 1, 2f, BasicIdleAutoDefenseGame.DamageType.Value, 0.24f, new GameObject("assigned-runner-enemy-prefab")),
                EnemyDefinitionAsset.CreateTransient(BasicIdleAutoDefenseGame.TankEnemySpawnableId.Value, "Assigned Tank Enemy", EnemyRole.Tank, 24f, 1.35f, 3, 5f, BasicIdleAutoDefenseGame.DamageType.Value, 0.42f, new GameObject("assigned-tank-enemy-prefab")),
                EnemyDefinitionAsset.CreateTransient(BasicIdleAutoDefenseGame.ShieldedEnemySpawnableId.Value, "Assigned Shielded Enemy", EnemyRole.Basic, 18f, 1.8f, 2, 4f, BasicIdleAutoDefenseGame.DamageType.Value, 0.34f, new GameObject("assigned-shielded-enemy-prefab")),
                EnemyDefinitionAsset.CreateTransient(BasicIdleAutoDefenseGame.EliteEnemySpawnableId.Value, "Assigned Elite Enemy", EnemyRole.Boss, 34f, 2.15f, 4, 7f, BasicIdleAutoDefenseGame.DamageType.Value, 0.36f, new GameObject("assigned-elite-enemy-prefab")),
                EnemyDefinitionAsset.CreateTransient(BasicIdleAutoDefenseGame.BossEnemySpawnableId.Value, "Assigned Boss Enemy", EnemyRole.Boss, 96f, 0.95f, 8, 16f, BasicIdleAutoDefenseGame.DamageType.Value, 0.65f, new GameObject("assigned-boss-enemy-prefab"))
            };
        }

        private static WaveDefinitionAsset[] CreateAssignedTemplateWaves(EnemyDefinitionAsset[] enemies)
        {
            return new[]
            {
                WaveDefinitionAsset.CreateTransient(
                    "wave.assigned.opening",
                    "Assigned Opening",
                    0,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[0], 2, 1, 0, 10, "perimeter-north"),
                        new WaveEntryRecipe(enemies[1], 2, 1, 4, 10, "perimeter-east")
                    }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.assigned.pressure",
                    "Assigned Pressure",
                    24,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[2], 1, 1, 0, 16, "perimeter-south"),
                        new WaveEntryRecipe(enemies[3], 1, 1, 8, 16, "perimeter-west"),
                        new WaveEntryRecipe(enemies[5], 1, 1, 16, 0, "perimeter-north")
                    })
            };
        }

        private static GameContentSetAsset CreateValidContentSet(
            bool omitStartingWeapon = false,
            WeaponDefinitionAsset startingWeaponOverride = null,
            WeaponDefinitionAsset[] weaponsOverride = null,
            EnemyDefinitionAsset[] enemiesOverride = null,
            WaveDefinitionAsset[] wavesOverride = null,
            RunUpgradeDefinitionAsset[] upgradesOverride = null)
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            WeaponDefinitionAsset[] weapons = weaponsOverride ?? BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
            EnemyDefinitionAsset[] enemies = enemiesOverride ?? BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            WaveDefinitionAsset[] waves = wavesOverride ?? CreateAssignedTemplateWaves(enemies);
            RunUpgradeDefinitionAsset[] upgrades = upgradesOverride ?? BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);
            WeaponDefinitionAsset startingWeapon = omitStartingWeapon ? null : startingWeaponOverride ?? weapons[0];
            return GameContentSetAsset.CreateTransient(
                "contentset.test.basic-idle-auto-defense",
                "Test Basic Idle Auto Defense Content Set",
                startingWeapon,
                weapons,
                enemies,
                waves,
                upgrades,
                startingCredits: 60,
                startingParts: 2,
                rewardMultiplier: 1.1f,
                difficultyMultiplier: 1f,
                sessionLengthTicks: 180,
                description: "Test-authored content set.",
                tags: new[] { "test", "content-set" });
        }

        private static GameContentSetAuthoringState CreateValidContentSetAuthoringState()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            var state = new GameContentSetAuthoringState
            {
                ContentSetId = contentSet.Id,
                DisplayName = contentSet.DisplayName,
                Description = contentSet.Description,
                StartingWeapon = contentSet.StartingWeapon,
                StartingCredits = contentSet.StartingCredits,
                StartingParts = contentSet.StartingParts,
                RewardMultiplier = contentSet.RewardMultiplier,
                DifficultyMultiplier = contentSet.DifficultyMultiplier,
                SessionLengthTicks = contentSet.SessionLengthTicks,
                Endless = contentSet.Endless
            };
            state.AvailableWeapons.AddRange(contentSet.AvailableWeapons);
            state.EnemyPool.AddRange(contentSet.EnemyPool);
            state.WaveSet.AddRange(contentSet.WaveSet);
            state.UpgradePool.AddRange(contentSet.UpgradePool);
            return state;
        }

        private static IdleAutoDefenseTemplateController CreateControllerWithContentSet(GameContentSetAsset contentSet)
        {
            GameObject host = new GameObject("idle-auto-defense-template-content-set-editmode");
            host.SetActive(false);
            IdleAutoDefenseTemplateController controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            FieldInfo field = typeof(IdleAutoDefenseTemplateController).GetField("_contentSet", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(controller, contentSet);
            host.SetActive(true);
            controller.RestartRun();
            return controller;
        }

        private static void AssertHasIssue(GameContentSetValidationReport report, string path)
        {
            for (int i = 0; i < report.Issues.Count; i++)
                if (report.Issues[i].Path.Contains(path))
                    return;
            Assert.Fail("Expected validation issue containing path '" + path + "'. Issues: " + FormatIssues(report));
        }

        private static string FormatIssues(GameContentSetValidationReport report)
        {
            if (report == null || report.Issues.Count == 0) return "No issues.";
            var messages = new List<string>();
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentSetValidationIssue issue = report.Issues[i];
                messages.Add(issue.Path + ": " + issue.Message);
            }

            return string.Join(" | ", messages);
        }

        private static void DestroyEnemyPrefabs(EnemyDefinitionAsset[] enemies)
        {
            if (enemies == null) return;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].Presentation != null && enemies[i].Presentation.Prefab != null)
                    UnityEngine.Object.DestroyImmediate(enemies[i].Presentation.Prefab);
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

        private static string ReadMetaGuid(string metaPath)
        {
            Assert.IsTrue(File.Exists(metaPath), "Expected meta file to exist: " + metaPath);
            string[] lines = File.ReadAllLines(metaPath);
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith("guid:", StringComparison.Ordinal))
                    return lines[i].Substring("guid:".Length).Trim();
            Assert.Fail("Expected meta file to contain a guid: " + metaPath);
            return string.Empty;
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
