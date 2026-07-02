using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Editor;
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
using UnityEditor;
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
        public void ValidGameContentPackValidationPassesAndCollectsDependencies()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            GameContentPackAsset pack = CreateValidContentPack(contentSet);

            GameContentPackValidationReport report = GameContentPackValidator.Validate(pack);
            GameContentPackResolution resolution = GameContentPackValidator.Resolve(pack);
            GameContentPackDependencySummary dependencies = GameContentPackValidator.CollectDependencies(pack);

            Assert.IsTrue(report.IsValid, FormatIssues(report));
            Assert.IsTrue(resolution.IsValid, FormatIssues(resolution.PackReport));
            Assert.AreSame(contentSet, resolution.SelectedContentSet);
            Assert.AreEqual(1, dependencies.ContentSetCount);
            Assert.AreEqual(2, dependencies.WeaponCount);
            Assert.AreEqual(2, dependencies.AttackCount);
            Assert.AreEqual(6, dependencies.EnemyCount);
            Assert.AreEqual(2, dependencies.WaveCount);
            Assert.AreEqual(4, dependencies.UpgradeCount);
        }

        [Test]
        public void GameContentPackValidationBlocksMissingDefaultContentSet()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            GameContentPackAsset pack = GameContentPackAsset.CreateTransient(
                "contentpack.test.missing-default",
                "Missing Default Pack",
                new[] { contentSet },
                null);

            GameContentPackValidationReport report = GameContentPackValidator.Validate(pack);

            Assert.IsFalse(report.IsValid);
            AssertHasIssue(report, "DefaultContentSet");
        }

        [Test]
        public void GameContentPackValidationBlocksMissingIncludedContentSet()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            GameContentPackAsset pack = GameContentPackAsset.CreateTransient(
                "contentpack.test.missing-included-set",
                "Missing Included Set Pack",
                new GameContentSetAsset[] { contentSet, null },
                contentSet);

            GameContentPackValidationReport report = GameContentPackValidator.Validate(pack);

            Assert.IsFalse(report.IsValid);
            AssertHasIssue(report, "ContentSets[1]");
        }

        [Test]
        public void GameContentPackValidationBlocksInvalidReferencedContentSet()
        {
            GameContentSetAsset contentSet = CreateValidContentSet(omitStartingWeapon: true);
            GameContentPackAsset pack = CreateValidContentPack(contentSet);

            GameContentPackValidationReport report = GameContentPackValidator.Validate(pack);

            Assert.IsFalse(report.IsValid);
            AssertHasIssue(report, "ContentSets[0].StartingWeapon");
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
        public void GameContentSetValidationBlocksEmptyEnemyPool()
        {
            GameContentSetAsset contentSet = CreateValidContentSet(
                enemiesOverride: Array.Empty<EnemyDefinitionAsset>(),
                wavesOverride: Array.Empty<WaveDefinitionAsset>());

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsFalse(report.IsValid);
            AssertHasIssue(report, "EnemyPool");
        }

        [Test]
        public void GameContentSetValidationAllowsEmptyUpgradePoolAsWarning()
        {
            GameContentSetAsset contentSet = CreateValidContentSet(upgradesOverride: Array.Empty<RunUpgradeDefinitionAsset>());

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsTrue(report.IsValid, FormatIssues(report));
            Assert.That(report.WarningCount, Is.GreaterThan(0));
            AssertHasIssue(report, "UpgradePool");
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
        public void GameContentSetValidationBlocksInvalidEconomyDifficultyAndSessionValues()
        {
            GameContentSetAsset contentSet = CreateValidContentSet(
                startingCredits: -1,
                startingParts: -1,
                rewardMultiplier: 0f,
                difficultyMultiplier: float.PositiveInfinity,
                sessionLengthTicks: 0);

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsFalse(report.IsValid);
            AssertHasIssue(report, "Economy.StartingCredits");
            AssertHasIssue(report, "Economy.StartingParts");
            AssertHasIssue(report, "Economy.RewardMultiplier");
            AssertHasIssue(report, "Difficulty.Multiplier");
            AssertHasIssue(report, "Run.SessionLengthTicks");
        }

        [Test]
        public void GameContentSetValidationWarnsWhenEndlessSessionLengthIsOnlyPreviewHint()
        {
            GameContentSetAsset contentSet = CreateValidContentSet(sessionLengthTicks: 0, endless: true);

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsTrue(report.IsValid, FormatIssues(report));
            AssertHasIssue(report, "Run.SessionLengthTicks");
        }

        [Test]
        public void GameContentSetDuplicateIdsAreDetectedBySharedAuthoringScan()
        {
            const string tempRoot = "Assets/T";
            string targetFolder = tempRoot + "/GcsDuplicate" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string assetPath = targetFolder + "/DuplicateContentSet.asset";
            bool createdRoot = false;
            if (!AssetDatabase.IsValidFolder(tempRoot))
            {
                AssetDatabase.CreateFolder("Assets", "T");
                createdRoot = true;
            }

            AssetDatabase.CreateFolder(tempRoot, Path.GetFileName(targetFolder));
            GameContentSetAsset existing = CreateValidContentSet();
            existing.hideFlags = HideFlags.None;
            try
            {
                AssetDatabase.CreateAsset(existing, assetPath);
                AssetDatabase.SaveAssets();

                Assert.IsTrue(GameContentAuthoringEditorAssets.HasDuplicateId<GameContentSetAsset>(existing.Id, asset => asset.Id));
            }
            finally
            {
                AssetDatabase.DeleteAsset(targetFolder);
                if (createdRoot) AssetDatabase.DeleteAsset(tempRoot);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void GameContentPackDuplicateIdsAreDetectedBySharedAuthoringScan()
        {
            const string tempRoot = "Assets/T";
            string targetFolder = tempRoot + "/GcpDuplicate" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string assetPath = targetFolder + "/DuplicateContentPack.asset";
            bool createdRoot = false;
            if (!AssetDatabase.IsValidFolder(tempRoot))
            {
                AssetDatabase.CreateFolder("Assets", "T");
                createdRoot = true;
            }

            AssetDatabase.CreateFolder(tempRoot, Path.GetFileName(targetFolder));
            GameContentSetAsset contentSet = CreateValidContentSet();
            GameContentPackAsset existing = CreateValidContentPack(contentSet);
            existing.hideFlags = HideFlags.None;
            try
            {
                AssetDatabase.CreateAsset(existing, assetPath);
                AssetDatabase.SaveAssets();

                Assert.IsTrue(GameContentAuthoringEditorAssets.HasDuplicateId<GameContentPackAsset>(existing.Id, asset => asset.Id));
            }
            finally
            {
                AssetDatabase.DeleteAsset(targetFolder);
                if (createdRoot) AssetDatabase.DeleteAsset(tempRoot);
                AssetDatabase.Refresh();
            }
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
        public void ControllerUsesValidAssignedGameContentPackWithoutFallback()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            GameContentPackAsset pack = CreateValidContentPack(contentSet);
            IdleAutoDefenseTemplateController controller = CreateControllerWithContentPack(pack, null);
            try
            {
                Assert.IsTrue(controller.UsingAssignedContentPack, controller.AssignedContentPackStatus);
                Assert.IsTrue(controller.UsingAssignedContentSet, controller.AssignedContentSetStatus);
                Assert.AreEqual(0, controller.InvalidAssignedContentPackIssueCount);
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
        public void ControllerFallsBackSafelyWhenAssignedGameContentPackIsInvalid()
        {
            GameContentSetAsset contentSet = CreateValidContentSet(omitStartingWeapon: true);
            GameContentPackAsset pack = CreateValidContentPack(contentSet);
            IdleAutoDefenseTemplateController controller = CreateControllerWithContentPack(pack, null);
            try
            {
                Assert.IsFalse(controller.UsingAssignedContentPack);
                Assert.IsFalse(controller.UsingAssignedContentSet);
                Assert.That(controller.InvalidAssignedContentPackIssueCount, Is.GreaterThan(0));
                Assert.IsNotNull(controller.Runtime);
                Assert.AreEqual("Running", controller.RuntimeStateName);
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void ContentPackSceneSetupPreviewDoesNotDirtyAndApplyMarksSceneDirty()
        {
            const string tempRoot = "Assets/T";
            string scenePath = tempRoot + "/ContentPackSetup_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".unity";
            bool createdRoot = false;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject host = new GameObject("content-pack-setup-controller");
            IdleAutoDefenseTemplateController controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            GameContentSetAsset contentSet = CreateValidContentSet();
            GameContentPackAsset pack = CreateValidContentPack(contentSet);

            try
            {
                if (!AssetDatabase.IsValidFolder(tempRoot))
                {
                    AssetDatabase.CreateFolder("Assets", "T");
                    createdRoot = true;
                }

                Assert.IsTrue(EditorSceneManager.SaveScene(scene, scenePath));
                Assert.IsFalse(scene.isDirty);

                GameContentAuthoringValidationResult validation = GameContentPackSceneSetupUtility.Validate(controller, pack, null);
                string preview = GameContentPackSceneSetupUtility.CreatePreviewSummary(pack, null);

                Assert.IsTrue(validation.IsValid);
                Assert.That(preview, Does.Contain("Scene is unchanged"));
                Assert.IsFalse(scene.isDirty);

                GameContentCreationResult apply = GameContentPackSceneSetupUtility.Apply(controller, pack, null);

                Assert.IsTrue(apply.Succeeded, apply.Message);
                Assert.IsTrue(scene.isDirty);
            }
            finally
            {
                DestroyController(controller);
                AssetDatabase.DeleteAsset(scenePath);
                if (createdRoot) AssetDatabase.DeleteAsset(tempRoot);
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
            Assert.IsTrue(GameContentAuthoringProviderRegistry.IsProviderRegistered("com.deucarian.attacks.attack"));
            Assert.IsTrue(GameContentAuthoringProviderRegistry.IsProviderRegistered("com.deucarian.attacks.enemy"));
            Assert.IsTrue(GameContentAuthoringProviderRegistry.IsProviderRegistered("com.deucarian.attacks.wave"));
            Assert.IsTrue(GameContentAuthoringProviderRegistry.IsProviderRegistered("com.deucarian.weapon-systems.weapon"));
            Assert.IsTrue(GameContentAuthoringProviderRegistry.IsProviderRegistered("com.deucarian.run-upgrades.upgrade"));
            Assert.IsTrue(GameContentAuthoringProviderRegistry.IsProviderRegistered("com.deucarian.template.idle-auto-defense.game-content-set"));
            Assert.IsTrue(GameContentAuthoringProviderRegistry.IsProviderRegistered("com.deucarian.template.idle-auto-defense.content-pack"));
        }

        [Test]
        public void ContentSetContentPackAndContentLibraryProvidersUseCustomV2Surfaces()
        {
            var contentSetProvider = new GameContentSetAuthoringProvider();
            var contentPackProvider = new GameContentPackAuthoringProvider();
            var contentLibraryProvider = new GameContentLibraryProvider();

            Assert.That(contentSetProvider, Is.InstanceOf<IGameContentAuthoringSurfaceProvider>());
            Assert.That(contentPackProvider, Is.InstanceOf<IGameContentAuthoringSurfaceProvider>());
            Assert.That(contentLibraryProvider, Is.InstanceOf<IGameContentAuthoringSurfaceProvider>());
            Assert.That(GameContentSetProviderV2PreviewModel.ExposesRedundantSelectButton, Is.False);
            Assert.That(GameContentPackProviderV2PreviewModel.ExposesRedundantSelectButton, Is.False);
            Assert.That(GameContentLibraryV2UiContract.MainRowActionLabels, Does.Not.Contain("Select"));
            Assert.That(GetContentSetProviderV2State(contentSetProvider), Is.Not.Null);
            Assert.That(GetContentPackProviderV2State(contentPackProvider), Is.Not.Null);
        }

        [Test]
        public void ContentSetProviderV2ListModel_ClassifiesCountsDurationAndSearch()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            try
            {
                GameContentSetProviderV2ListItem item = GameContentSetProviderV2ListItem.FromAssetForTests(contentSet);

                Assert.That(item.HasStartingWeapon, Is.True);
                Assert.That(item.WeaponCount, Is.EqualTo(contentSet.AvailableWeapons.Count));
                Assert.That(item.WaveCount, Is.EqualTo(contentSet.WaveSet.Count));
                Assert.That(item.EnemyCount, Is.GreaterThan(0));
                Assert.That(item.DurationTicks, Is.GreaterThan(0));
                Assert.That(item.Matches("basic-idle"), Is.True);
                Assert.That(item.Matches(item.EnemyCount.ToString(System.Globalization.CultureInfo.InvariantCulture)), Is.True);
            }
            finally
            {
                GameContentSetAssetCreator.DestroyTransient(contentSet);
            }
        }

        [Test]
        public void ContentSetProviderV2Preview_ScopesAndChipsExposeDraftUnsavedAndDebug()
        {
            GameContentSetAuthoringState state = CreateValidContentSetAuthoringState();
            var previewState = new GameContentSetProviderV2State
            {
                PreviewRenderMode = GameContentAuthoringActionPreviewRenderMode.Debug,
                PreviewSpeed = 2f
            };
            GameContentSetAsset preview = GameContentSetAssetCreator.BuildTransient(state);
            GameContentSetValidationReport report;
            try
            {
                report = GameContentSetValidator.Validate(preview);
            }
            finally
            {
                GameContentSetAssetCreator.DestroyTransient(preview);
            }

            Assert.That(GameContentSetProviderV2PreviewModel.GetScopeLabel(true, false), Is.EqualTo("Draft"));
            Assert.That(GameContentSetProviderV2PreviewModel.GetScopeLabel(false, true), Is.EqualTo("Unsaved"));
            AssertChip(GameContentSetProviderV2PreviewModel.BuildChips(state, previewState, report), "Debug", DeucarianEditorStatus.Warning);
            AssertChip(GameContentSetProviderV2PreviewModel.BuildChips(state, previewState, report), "2x", DeucarianEditorStatus.Info);
            Assert.That(GameContentSetProviderV2View.BuildWeaponAttackSummary(state.StartingWeapon), Is.Not.EqualTo("Missing attack"));
            Assert.That(GameContentSetProviderV2View.BuildEnemyMixSummary(state.WaveSet), Does.Contain("x"));
        }

        [Test]
        public void ContentSetProviderV2Preview_DraftFieldChangesUpdateFingerprintAndPreview()
        {
            GameContentSetAuthoringState state = CreateValidContentSetAuthoringState();
            string before = GameContentSetProviderV2View.BuildStateFingerprint(state);
            int durationBefore = GameContentSetProviderV2View.ApproximateDuration(state.WaveSet);

            state.StartingCredits += 25;
            state.WaveSet.RemoveAt(state.WaveSet.Count - 1);
            state.UpgradePool.Clear();
            string after = GameContentSetProviderV2View.BuildStateFingerprint(state);

            Assert.That(after, Is.Not.EqualTo(before));
            Assert.That(GameContentSetProviderV2View.ApproximateDuration(state.WaveSet), Is.LessThanOrEqualTo(durationBefore));
            Assert.That(GameContentSetProviderV2View.CountAssigned(state.UpgradePool), Is.EqualTo(0));
        }

        [Test]
        public void GameContentSetAssetCreator_UpdateExistingAssetSavesSelectedContentSet()
        {
            string rootFolder = "Assets/__ContentSetGcaV2EditTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(rootFolder));
            try
            {
                GameContentSetAsset contentSet = CreateValidContentSet();
                contentSet.hideFlags = HideFlags.None;
                string assetPath = rootFolder + "/ContentSet.asset";
                AssetDatabase.CreateAsset(contentSet, assetPath);
                AssetDatabase.SaveAssets();
                GameContentSetAsset asset = AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(assetPath);
                GameContentSetAuthoringState edit = GameContentSetProviderV2View.FromContentSetAsset(asset);
                edit.DisplayName = "Saved Content Set";
                edit.StartingCredits = 125;
                edit.SessionLengthTicks = 240;
                edit.TagsCsv = "saved, content-set";

                GameContentCreationResult saved = GameContentSetAssetCreator.UpdateExistingAsset(asset, edit);

                Assert.That(saved.Succeeded, Is.True, saved.Message);
                Assert.That(asset.DisplayName, Is.EqualTo("Saved Content Set"));
                Assert.That(asset.StartingCredits, Is.EqualTo(125));
                Assert.That(asset.SessionLengthTicks, Is.EqualTo(240));
                Assert.That(GameContentSetAssetCreator.ValidateForUpdate(GameContentSetProviderV2View.FromContentSetAsset(asset), asset).IsValid, Is.True);
            }
            finally
            {
                AssetDatabase.DeleteAsset(rootFolder);
            }
        }

        [Test]
        public void ContentSetProviderV2RevertReloadsSavedContentSetData()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            try
            {
                GameContentSetAuthoringState edit = GameContentSetProviderV2View.FromContentSetAsset(contentSet);
                edit.DisplayName = "Unsaved Content Set";
                edit.StartingCredits = 999;
                edit.AvailableWeapons.RemoveAt(edit.AvailableWeapons.Count - 1);
                string dirtyFingerprint = GameContentSetProviderV2View.BuildStateFingerprint(edit);

                GameContentSetAuthoringState reverted = GameContentSetProviderV2View.FromContentSetAsset(contentSet);

                Assert.That(GameContentSetProviderV2View.BuildStateFingerprint(reverted), Is.Not.EqualTo(dirtyFingerprint));
                Assert.That(reverted.DisplayName, Is.EqualTo(contentSet.DisplayName));
                Assert.That(reverted.StartingCredits, Is.EqualTo(contentSet.StartingCredits));
                Assert.That(reverted.AvailableWeapons.Count, Is.EqualTo(contentSet.AvailableWeapons.Count));
            }
            finally
            {
                GameContentSetAssetCreator.DestroyTransient(contentSet);
            }
        }

        [Test]
        public void ContentSetProviderV2State_ProviderSwitchClearsDraftAndUnsavedPreviewState()
        {
            var provider = new GameContentSetAuthoringProvider();
            GameContentSetProviderV2State state = GetContentSetProviderV2State(provider);
            state.BeginCreate();
            state.EditingState = new GameContentSetAuthoringState { DisplayName = "Dirty Content Set" };
            state.EditingContext = new GameContentAuthoringObjectEditorContext(null, "saved");
            state.PreviewStatus = "Previewing unsaved edit";

            provider.OnSelected();

            Assert.That(state.Creating, Is.False);
            Assert.That(state.EditingState, Is.Null);
            Assert.That(state.EditingContext, Is.Null);
            Assert.That(state.PreviewStatus, Is.EqualTo("Preview idle"));
        }

        [Test]
        public void GameContentSetAssetCreator_UpdateValidationBlocksMissingStartingWeaponAndWaves()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            try
            {
                GameContentSetAuthoringState edit = GameContentSetProviderV2View.FromContentSetAsset(contentSet);
                edit.StartingWeapon = null;
                edit.WaveSet.Clear();

                GameContentAuthoringValidationResult validation = GameContentSetAssetCreator.ValidateForUpdate(edit, contentSet);
                GameContentCreationResult saved = GameContentSetAssetCreator.UpdateExistingAsset(contentSet, edit);

                Assert.That(validation.IsValid, Is.False);
                Assert.That(FindIssue(validation, "StartingWeapon", GameContentAuthoringValidationSeverity.Error), Is.True);
                Assert.That(FindIssue(validation, "WaveSet", GameContentAuthoringValidationSeverity.Error), Is.True);
                Assert.That(saved.Succeeded, Is.False);
            }
            finally
            {
                GameContentSetAssetCreator.DestroyTransient(contentSet);
            }
        }

        [Test]
        public void ContentPackProviderV2ListModel_ClassifiesReadinessDefaultDependenciesAndSearch()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            GameContentPackAsset pack = CreateValidContentPack(contentSet);
            try
            {
                GameContentPackProviderV2ListItem item = GameContentPackProviderV2ListItem.FromAssetForTests(pack);

                Assert.That(item.HasDefaultContentSet, Is.True);
                Assert.That(item.ContentSetCount, Is.EqualTo(1));
                Assert.That(item.DependencyCount, Is.GreaterThan(0));
                Assert.That(item.CompatibilityLabel, Is.EqualTo(pack.RequiredPackages.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " pkg"));
                Assert.That(item.Matches("basic-idle"), Is.True);
                Assert.That(item.Matches("default"), Is.True);
                Assert.That(item.Matches(item.DependencyCount.ToString(System.Globalization.CultureInfo.InvariantCulture)), Is.True);
            }
            finally
            {
                GameContentPackAssetCreator.DestroyTransient(pack);
                GameContentSetAssetCreator.DestroyTransient(contentSet);
            }
        }

        [Test]
        public void ContentPackProviderV2Preview_ScopesAndChipsExposeDraftUnsavedAndDebug()
        {
            GameContentPackAuthoringState state = CreateValidContentPackAuthoringState();
            var previewState = new GameContentPackProviderV2State
            {
                PreviewRenderMode = GameContentAuthoringActionPreviewRenderMode.Debug,
                PreviewSpeed = 2f
            };
            GameContentPackAsset preview = GameContentPackAssetCreator.BuildTransient(state);
            GameContentPackValidationReport report;
            GameContentPackDependencySummary dependencies;
            try
            {
                report = GameContentPackValidator.Validate(preview);
                dependencies = GameContentPackValidator.CollectDependencies(preview);
            }
            finally
            {
                GameContentPackAssetCreator.DestroyTransient(preview);
            }

            Assert.That(GameContentPackProviderV2PreviewModel.GetScopeLabel(true, false), Is.EqualTo("Draft"));
            Assert.That(GameContentPackProviderV2PreviewModel.GetScopeLabel(false, true), Is.EqualTo("Unsaved"));
            AssertChip(GameContentPackProviderV2PreviewModel.BuildChips(state, previewState, report, dependencies), "Debug", DeucarianEditorStatus.Warning);
            AssertChip(GameContentPackProviderV2PreviewModel.BuildChips(state, previewState, report, dependencies), "2x", DeucarianEditorStatus.Info);
            Assert.That(GameContentPackProviderV2View.BuildDependencyTotalSummary(state), Does.Contain("authored asset"));
            Assert.That(GameContentPackProviderV2View.BuildCompatibilitySummary(state), Does.Contain("required package"));
        }

        [Test]
        public void ContentPackProviderV2Preview_DraftFieldChangesUpdateFingerprintAndPreview()
        {
            GameContentPackAuthoringState state = CreateValidContentPackAuthoringState();
            string before = GameContentPackProviderV2View.BuildStateFingerprint(state);
            string dependencyBefore = GameContentPackProviderV2View.BuildDependencyTotalSummary(state);

            state.DisplayName = "Draft Pack Rename";
            state.RequiredPackagesCsv = "com.deucarian.template.game.idle-auto-defense";
            state.MinimumVersionsCsv = "1.0.0";
            state.TagsCsv = "draft, renamed";
            string after = GameContentPackProviderV2View.BuildStateFingerprint(state);

            Assert.That(after, Is.Not.EqualTo(before));
            Assert.That(GameContentPackProviderV2View.BuildDependencyTotalSummary(state), Is.EqualTo(dependencyBefore));
            Assert.That(GameContentPackProviderV2View.BuildCompatibilitySummary(state), Is.EqualTo("1 required package(s)"));
        }

        [Test]
        public void ContentPackProviderV2Preview_IncludedAndDefaultChangesUpdateFingerprint()
        {
            GameContentSetAsset primary = CreateValidContentSet();
            GameContentSetAsset secondary = CreateValidContentSet(startingCredits: 90, sessionLengthTicks: 220);
            secondary.Configure(
                "contentset.test.secondary",
                "Secondary Test Content Set",
                secondary.Description,
                null,
                null,
                secondary.StartingWeapon,
                secondary.AvailableWeapons,
                secondary.EnemyPool,
                secondary.WaveSet,
                secondary.UpgradePool,
                secondary.StartingCredits,
                secondary.StartingParts,
                secondary.RewardMultiplier,
                secondary.DifficultyMultiplier,
                secondary.SessionLengthTicks,
                secondary.Endless,
                secondary.Tags);
            try
            {
                var state = new GameContentPackAuthoringState
                {
                    PackId = "contentpack.test.multi",
                    DisplayName = "Multi Set Pack",
                    DefaultContentSet = primary
                };
                state.ContentSets.Add(primary);
                string before = GameContentPackProviderV2View.BuildStateFingerprint(state);

                state.ContentSets.Add(secondary);
                state.DefaultContentSet = secondary;
                string after = GameContentPackProviderV2View.BuildStateFingerprint(state);

                Assert.That(after, Is.Not.EqualTo(before));
                Assert.That(GameContentPackProviderV2View.CountAssigned(state.ContentSets), Is.EqualTo(2));
                Assert.That(GameContentPackProviderV2View.BuildDependencySummary(state).ContentSetCount, Is.EqualTo(2));
            }
            finally
            {
                GameContentSetAssetCreator.DestroyTransient(primary);
                GameContentSetAssetCreator.DestroyTransient(secondary);
            }
        }

        [Test]
        public void GameContentPackAssetCreator_UpdateExistingAssetSavesSelectedContentPack()
        {
            string rootFolder = "Assets/__ContentPackGcaV2EditTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(rootFolder));
            try
            {
                GameContentSetAsset contentSet = CreateValidContentSet();
                contentSet.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(contentSet, rootFolder + "/ContentSet.asset");

                GameContentPackAsset pack = CreateValidContentPack(contentSet);
                pack.hideFlags = HideFlags.None;
                string assetPath = rootFolder + "/ContentPack.asset";
                AssetDatabase.CreateAsset(pack, assetPath);
                AssetDatabase.SaveAssets();
                GameContentPackAsset asset = AssetDatabase.LoadAssetAtPath<GameContentPackAsset>(assetPath);
                GameContentPackAuthoringState edit = GameContentPackProviderV2View.FromContentPackAsset(asset);
                edit.DisplayName = "Saved Content Pack";
                edit.Version = "2.0.0";
                edit.Author = "V2 Test";
                edit.TagsCsv = "saved, content-pack";

                GameContentCreationResult saved = GameContentPackAssetCreator.UpdateExistingAsset(asset, edit);

                Assert.That(saved.Succeeded, Is.True, saved.Message);
                Assert.That(asset.DisplayName, Is.EqualTo("Saved Content Pack"));
                Assert.That(asset.Version, Is.EqualTo("2.0.0"));
                Assert.That(asset.Author, Is.EqualTo("V2 Test"));
                Assert.That(GameContentPackAssetCreator.ValidateForUpdate(GameContentPackProviderV2View.FromContentPackAsset(asset), asset).IsValid, Is.True);
            }
            finally
            {
                AssetDatabase.DeleteAsset(rootFolder);
            }
        }

        [Test]
        public void ContentPackProviderV2RevertReloadsSavedContentPackData()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            GameContentPackAsset pack = CreateValidContentPack(contentSet);
            try
            {
                GameContentPackAuthoringState edit = GameContentPackProviderV2View.FromContentPackAsset(pack);
                edit.DisplayName = "Unsaved Content Pack";
                edit.RequiredPackagesCsv = "com.deucarian.fake";
                edit.ContentSets.Clear();
                string dirtyFingerprint = GameContentPackProviderV2View.BuildStateFingerprint(edit);

                GameContentPackAuthoringState reverted = GameContentPackProviderV2View.FromContentPackAsset(pack);

                Assert.That(GameContentPackProviderV2View.BuildStateFingerprint(reverted), Is.Not.EqualTo(dirtyFingerprint));
                Assert.That(reverted.DisplayName, Is.EqualTo(pack.DisplayName));
                Assert.That(reverted.RequiredPackagesCsv, Does.Contain("com.deucarian.attacks"));
                Assert.That(reverted.ContentSets.Count, Is.EqualTo(pack.ContentSets.Count));
            }
            finally
            {
                GameContentPackAssetCreator.DestroyTransient(pack);
                GameContentSetAssetCreator.DestroyTransient(contentSet);
            }
        }

        [Test]
        public void ContentPackProviderV2State_ProviderSwitchClearsDraftAndUnsavedPreviewState()
        {
            var provider = new GameContentPackAuthoringProvider();
            GameContentPackProviderV2State state = GetContentPackProviderV2State(provider);
            state.BeginCreate();
            state.EditingState = new GameContentPackAuthoringState { DisplayName = "Dirty Content Pack" };
            state.EditingContext = new GameContentAuthoringObjectEditorContext(null, "saved");
            state.PreviewStatus = "Previewing unsaved edit";

            provider.OnSelected();

            Assert.That(state.Creating, Is.False);
            Assert.That(state.EditingState, Is.Null);
            Assert.That(state.EditingContext, Is.Null);
            Assert.That(state.PreviewStatus, Is.EqualTo("Preview idle"));
        }

        [Test]
        public void GameContentPackAssetCreator_UpdateValidationBlocksMissingDefaultAndIncludedSet()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            GameContentPackAsset pack = CreateValidContentPack(contentSet);
            GameContentSetAsset externalDefault = CreateValidContentSet(startingCredits: 90);
            externalDefault.Configure(
                "contentset.test.external-default",
                "External Default",
                externalDefault.Description,
                null,
                null,
                externalDefault.StartingWeapon,
                externalDefault.AvailableWeapons,
                externalDefault.EnemyPool,
                externalDefault.WaveSet,
                externalDefault.UpgradePool,
                externalDefault.StartingCredits,
                externalDefault.StartingParts,
                externalDefault.RewardMultiplier,
                externalDefault.DifficultyMultiplier,
                externalDefault.SessionLengthTicks,
                externalDefault.Endless,
                externalDefault.Tags);
            try
            {
                GameContentPackAuthoringState missing = GameContentPackProviderV2View.FromContentPackAsset(pack);
                missing.DefaultContentSet = null;
                missing.ContentSets.Clear();

                GameContentAuthoringValidationResult missingValidation = GameContentPackAssetCreator.ValidateForUpdate(missing, pack);
                GameContentCreationResult missingSave = GameContentPackAssetCreator.UpdateExistingAsset(pack, missing);

                Assert.That(missingValidation.IsValid, Is.False);
                Assert.That(FindIssue(missingValidation, "DefaultContentSet", GameContentAuthoringValidationSeverity.Error), Is.True);
                Assert.That(FindIssue(missingValidation, "ContentSets", GameContentAuthoringValidationSeverity.Error), Is.True);
                Assert.That(missingSave.Succeeded, Is.False);

                GameContentPackAuthoringState outsideDefault = GameContentPackProviderV2View.FromContentPackAsset(pack);
                outsideDefault.DefaultContentSet = externalDefault;
                GameContentAuthoringValidationResult outsideDefaultValidation = GameContentPackAssetCreator.ValidateForUpdate(outsideDefault, pack);

                Assert.That(outsideDefaultValidation.IsValid, Is.False);
                Assert.That(FindIssue(outsideDefaultValidation, "DefaultContentSet", GameContentAuthoringValidationSeverity.Error), Is.True);
            }
            finally
            {
                GameContentPackAssetCreator.DestroyTransient(pack);
                GameContentSetAssetCreator.DestroyTransient(contentSet);
                GameContentSetAssetCreator.DestroyTransient(externalDefault);
            }
        }

        [Test]
        public void AssignedEnemyDefinitionsRejectDuplicatesAndFallbackWhenIncomplete()
        {
            EnemyDefinitionAsset[] enemies = CreateAssignedTemplateEnemiesWithPrefabs();
            try
            {
                enemies[3].Configure(
                    BasicIdleAutoDefenseGame.RunnerEnemySpawnableId.Value,
                    "Duplicate Runner",
                    null,
                    EnemyRole.Basic,
                    Array.Empty<string>(),
                    enemies[3].Stats,
                    enemies[3].Presentation);

                EnemyDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveEnemyDefinitionsForTemplate(enemies, out int rejectedDefinitionCount);

                Assert.That(rejectedDefinitionCount, Is.GreaterThan(0));
                Assert.AreEqual(6, resolved.Length);
                Assert.AreEqual(BasicIdleAutoDefenseGame.ShieldedEnemySpawnableId.Value, resolved[3].Id);
                Assert.AreNotSame(enemies[3], resolved[3]);
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
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "canonical-game-flow.md"), "spawn profiles");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "canonical-game-flow.md"), "apply upgrade drafts");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "default-content-and-balance.md"), "TemplateSource~");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "default-content-and-balance.md"), "ContentPacks");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "asset-flip-workflow.md"), "Create Playable Game");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "override-guide.md"), "Spawn profiles");
            AssertFileContains(Path.Combine(packageRoot, "package.json"), "\"com.deucarian.editor\"");
            AssertFileContains(Path.Combine(packageRoot, "package.json"), "\"com.deucarian.game-content-authoring\"");
            AssertFileContains(Path.Combine(packageRoot, "deucarian-package.json"), "\"com.deucarian.editor\"");
            AssertFileContains(Path.Combine(packageRoot, "deucarian-package.json"), "\"com.deucarian.game-content-authoring\"");
            AssertFileDoesNotContain(Path.Combine(packageRoot, "package.json"), "\"samples\"");
            Assert.IsFalse(Directory.Exists(Path.Combine(packageRoot, "Samples~")), "The template should not expose a public UPM sample.");

            string menuPath = Path.Combine(packageRoot, "Editor", "IdleAutoDefenseTemplateMenu.cs");
            AssertFileContains(menuPath, "Create Playable Game");
            AssertFileContains(menuPath, "Open Template Docs");
            AssertFileDoesNotContain(menuPath, "Open Starter Scene");
            AssertFileDoesNotContain(menuPath, "Reset Sample Save");

            string contentRoot = Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame", "Content");
            AssertDirectoryExists(Path.Combine(contentRoot, "Attacks"));
            AssertDirectoryExists(Path.Combine(contentRoot, "Enemies"));
            AssertDirectoryExists(Path.Combine(contentRoot, "Weapons"));
            AssertDirectoryExists(Path.Combine(contentRoot, "Waves"));
            AssertDirectoryExists(Path.Combine(contentRoot, "Upgrades"));
            AssertDirectoryExists(Path.Combine(contentRoot, "ContentSets"));
            AssertDirectoryExists(Path.Combine(contentRoot, "ContentPacks"));

            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.template.swarm", "enemy.template.swarm_EnemyDefinition.asset"), "_id: enemy.template.swarm");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.template.runner", "enemy.template.runner_EnemyDefinition.asset"), "_id: enemy.template.runner");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.template.shielded", "enemy.template.shielded_EnemyDefinition.asset"), "_id: enemy.template.shielded");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.template.hitscan-beam", "attack.template.hitscan-beam_AttackDefinition.asset"), "_id: attack.template.pulse-cannon");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.template.fire-orb", "attack.template.fire-orb_AttackDefinition.asset"), "_id: attack.template.shard-launcher");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.template.arc-burst", "attack.template.arc-burst_AttackDefinition.asset"), "_id: attack.template.arc-burst");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.template.homing-pulse", "attack.template.homing-pulse_AttackDefinition.asset"), "_id: attack.template.homing-pulse");
            AssertFileContains(Path.Combine(contentRoot, "Waves", "wave.template.authored.opening", "wave.template.authored.opening_WaveDefinition.asset"), "Opening Wave");
            AssertFileContains(Path.Combine(contentRoot, "Waves", "wave.template.authored.runner-pressure", "wave.template.authored.runner-pressure_WaveDefinition.asset"), "Runner Pressure");
            AssertFileContains(Path.Combine(contentRoot, "Waves", "wave.template.authored.final", "wave.template.authored.final_WaveDefinition.asset"), "Final Surge");
            AssertFileContains(Path.Combine(contentRoot, "Upgrades", "upgrade.template.authored.projectile-speed-up", "upgrade.template.authored.projectile-speed-up_RunUpgradeDefinition.asset"), "Projectile Speed");
            AssertFileContains(Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame", "Prefabs", "Enemies", "README.md"), "Swarm");
            AssertFileContains(Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame", "Prefabs", "Weapons", "README.md"), "Pulse Cannon");
            AssertFileContains(Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame", "Prefabs", "Projectiles", "README.md"), "projectile");
            AssertFileContains(Path.Combine(contentRoot, "ContentPacks", "contentpack.template.basic-idle-auto-defense", "contentpack.template.basic-idle-auto-defense_ContentPack.asset"), "contentpack.template.basic-idle-auto-defense");
        }

        [Test]
        public void TemplateSourceAuthoredAssetIdsAreUniqueForContentLibrary()
        {
            string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BasicIdleAutoDefenseGame).Assembly).resolvedPath;
            string contentRoot = Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame", "Content");

            AssertSampleAuthoredDefinitionIdsAreUnique(contentRoot);
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.template.swarm", "enemy.template.swarm_EnemyDefinition.asset"), "_id: enemy.template.swarm");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.template.shielded", "enemy.template.shielded_EnemyDefinition.asset"), "_id: enemy.template.shielded");
            AssertFileContains(Path.Combine(contentRoot, "Upgrades", "upgrade.template.authored.projectile-speed-up", "upgrade.template.authored.projectile-speed-up_RunUpgradeDefinition.asset"), "_id: upgrade.template.authored.projectile-speed-up");
            AssertFileContains(Path.Combine(contentRoot, "Upgrades", "upgrade.template.authored.core-reinforcement", "upgrade.template.authored.core-reinforcement_RunUpgradeDefinition.asset"), "_id: upgrade.template.authored.core-reinforcement");
            AssertFileContains(Path.Combine(contentRoot, "Upgrades", "upgrade.template.authored.credit-reward", "upgrade.template.authored.credit-reward_RunUpgradeDefinition.asset"), "_id: upgrade.template.authored.credit-reward");
            AssertFileContains(Path.Combine(contentRoot, "README.md"), "four generic enemy definitions");
        }

        [Test]
        public void TemplateSourcePlayableStarterRoundIsCompleteAndPresentable()
        {
            string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BasicIdleAutoDefenseGame).Assembly).resolvedPath;
            string templateSourceRoot = Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame");
            string contentRoot = Path.Combine(templateSourceRoot, "Content");

            Assert.AreEqual(4, CountAuthoredDefinitionFiles(contentRoot, "*_AttackDefinition.asset", "AttackDefinitionAsset"));
            Assert.AreEqual(4, CountAuthoredDefinitionFiles(Path.Combine(contentRoot, "Enemies"), "*_EnemyDefinition.asset", "EnemyDefinitionAsset"));
            Assert.AreEqual(5, CountAuthoredDefinitionFiles(Path.Combine(contentRoot, "Waves"), "*_WaveDefinition.asset", "WaveDefinitionAsset"));
            Assert.AreEqual(4, CountAuthoredDefinitionFiles(contentRoot, "*_WeaponDefinition.asset", "WeaponDefinitionAsset"));
            Assert.AreEqual(6, CountAuthoredDefinitionFiles(contentRoot, "*_RunUpgradeDefinition.asset", "RunUpgradeDefinitionAsset"));

            AssertFileContains(Path.Combine(contentRoot, "ContentPacks", "contentpack.template.basic-idle-auto-defense", "contentpack.template.basic-idle-auto-defense_ContentPack.asset"), "contentpack.template.basic-idle-auto-defense");
            AssertFileContains(Path.Combine(contentRoot, "ContentSets", "contentset.template.basic-idle-auto-defense", "contentset.template.basic-idle-auto-defense_GameContentSet.asset"), "five spawn profiles");
            AssertFileContains(Path.Combine(contentRoot, "ContentSets", "contentset.template.basic-idle-auto-defense", "contentset.template.basic-idle-auto-defense_GameContentSet.asset"), "139ea81c2ca6259408bcf0527b568e74");
            AssertFileContains(Path.Combine(contentRoot, "ContentSets", "contentset.template.basic-idle-auto-defense", "contentset.template.basic-idle-auto-defense_GameContentSet.asset"), "9bfe6c935b0d4599b65986b90fca9e3a");
            AssertFileContains(Path.Combine(contentRoot, "ContentSets", "contentset.template.basic-idle-auto-defense", "contentset.template.basic-idle-auto-defense_GameContentSet.asset"), "03d0c000098e49699eb86ed1176892d4");
            AssertFileContains(Path.Combine(contentRoot, "ContentSets", "contentset.template.basic-idle-auto-defense", "contentset.template.basic-idle-auto-defense_GameContentSet.asset"), "02b74debbe2246c4b09d6d043c80536b");
            AssertFileContains(Path.Combine(contentRoot, "ContentSets", "contentset.template.basic-idle-auto-defense", "contentset.template.basic-idle-auto-defense_GameContentSet.asset"), "9832cf788c584c0a9c8cd160b57f84a2");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.template.fire-orb", "attack.template.fire-orb_Delivery.asset"), "_mode: 0");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.template.fire-orb", "attack.template.fire-orb_Delivery.asset"), "projectile.template.shard");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.template.hitscan-beam", "attack.template.hitscan-beam_Delivery.asset"), "_mode: 1");
            AssertFileDoesNotContain(Path.Combine(contentRoot, "Attacks", "attack.template.fire-orb", "attack.template.fire-orb_Delivery.asset"), "projectile.template.fire-orb");

            AssertDirectoryExists(Path.Combine(templateSourceRoot, "Audio"));
            AssertDirectoryExists(Path.Combine(templateSourceRoot, "Visuals", "Prefabs"));
            AssertDirectoryExists(Path.Combine(templateSourceRoot, "Visuals", "Textures"));
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Textures", "template-starter-pack-icon.png.meta"), "TextureImporter");
            AssertFileExistsAtFullPath(Path.Combine(templateSourceRoot, "Visuals", "Textures", "template-starter-pack-banner.png"));
            AssertFileExistsAtFullPath(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "template-projectile.prefab"));
            AssertFileExistsAtFullPath(Path.Combine(templateSourceRoot, "Audio", "template-fire.wav"));
            AssertFileExistsAtFullPath(Path.Combine(templateSourceRoot, "Audio", "template-impact.wav"));
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

                controller.RestartRun(CreateFailCapablePressureEncounterDefinition());
                StepUntilTerminal(controller, 720);
                Assert.IsTrue(controller.EncounterFailed, "The template should still support fail-capable pressure runs.");

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
            string contentRoot = "Assets/GameContent/W" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string secondContentRoot = "Assets/GameContent/W" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var request = new IdleAutoDefenseTemplateSetupRequest
            {
                TargetRootAssetPath = targetRoot,
                ContentRootAssetPath = contentRoot,
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
                AssertCreatedPathsStayUnderAllowedRoots(result, targetRoot, contentRoot);
                AssertFileExists(targetRoot + "/Scenes/WizardSmokeIdleAutoDefense.unity");
                AssertFileExists(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs");
                AssertFileExists(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs.meta");
                AssertFileExists(targetRoot + "/WizardSmoke.IdleAutoDefense.asmdef");
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Attacks"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Enemies"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Weapons"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Waves"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Upgrades"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/ContentSets"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/ContentPacks"));
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Visuals/Prefabs"));
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Audio"));
                AssertFileExists(contentRoot + "/Enemies/enemy.template.swarm/enemy.template.swarm_EnemyDefinition.asset");
                AssertFileExists(contentRoot + "/Waves/wave.template.authored.runner-pressure/wave.template.authored.runner-pressure_WaveDefinition.asset");
                AssertFileExists(contentRoot + "/Upgrades/upgrade.template.authored.projectile-speed-up/upgrade.template.authored.projectile-speed-up_RunUpgradeDefinition.asset");
                AssertFileExists(contentRoot + "/Upgrades/upgrade.template.authored.core-reinforcement/upgrade.template.authored.core-reinforcement_RunUpgradeDefinition.asset");
                AssertFileExists(contentRoot + "/Upgrades/upgrade.template.authored.credit-reward/upgrade.template.authored.credit-reward_RunUpgradeDefinition.asset");
                AssertFileExists(contentRoot + "/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset");
                AssertFileExists(contentRoot + "/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset.meta");
                AssertFileExists(contentRoot + "/ContentPacks/contentpack.template.basic-idle-auto-defense/contentpack.template.basic-idle-auto-defense_ContentPack.asset");
                AssertFileExists(contentRoot + "/ContentPacks/contentpack.template.basic-idle-auto-defense/contentpack.template.basic-idle-auto-defense_ContentPack.asset.meta");
                string contentSetGuid = ReadMetaGuid(AssetPathToFullPath(contentRoot + "/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset.meta"));
                string contentPackGuid = ReadMetaGuid(AssetPathToFullPath(contentRoot + "/ContentPacks/contentpack.template.basic-idle-auto-defense/contentpack.template.basic-idle-auto-defense_ContentPack.asset.meta"));
                string generatedPulseWeaponGuid = ReadMetaGuid(AssetPathToFullPath(contentRoot + "/Weapons/weapon.template.pulse-cannon/weapon.template.pulse-cannon_WeaponDefinition.asset.meta"));
                string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BasicIdleAutoDefenseGame).Assembly).resolvedPath;
                string templateSourceRoot = Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame");
                string sourceContentSetGuid = ReadMetaGuid(Path.Combine(templateSourceRoot, "Content", "ContentSets", "contentset.template.basic-idle-auto-defense", "contentset.template.basic-idle-auto-defense_GameContentSet.asset.meta"));
                string sourceContentPackGuid = ReadMetaGuid(Path.Combine(templateSourceRoot, "Content", "ContentPacks", "contentpack.template.basic-idle-auto-defense", "contentpack.template.basic-idle-auto-defense_ContentPack.asset.meta"));
                string sourcePulseWeaponGuid = ReadMetaGuid(Path.Combine(templateSourceRoot, "Content", "Weapons", "weapon.template.pulse-cannon", "weapon.template.pulse-cannon_WeaponDefinition.asset.meta"));
                Assert.AreNotEqual(sourceContentSetGuid, contentSetGuid);
                Assert.AreNotEqual(sourceContentPackGuid, contentPackGuid);
                Assert.AreNotEqual(sourcePulseWeaponGuid, generatedPulseWeaponGuid);
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Scenes/WizardSmokeIdleAutoDefense.unity"), contentSetGuid);
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Scenes/WizardSmokeIdleAutoDefense.unity"), contentPackGuid);
                AssertFileDoesNotContain(AssetPathToFullPath(targetRoot + "/Scenes/WizardSmokeIdleAutoDefense.unity"), sourceContentSetGuid);
                AssertFileDoesNotContain(AssetPathToFullPath(targetRoot + "/Scenes/WizardSmokeIdleAutoDefense.unity"), sourceContentPackGuid);
                AssertFileContains(AssetPathToFullPath(contentRoot + "/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset"), generatedPulseWeaponGuid);
                AssertFileDoesNotContain(AssetPathToFullPath(contentRoot + "/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset"), sourcePulseWeaponGuid);
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Docs/asset-flip-checklist.md"), "product-owned");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Docs/asset-flip-checklist.md"), contentRoot);
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Docs/setup-report.md"), "Deucarian.TemplateGameIdleAutoDefense");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Docs/setup-report.md"), contentRoot);
                AssertFileContains(AssetPathToFullPath(targetRoot + "/README.md"), contentRoot);
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs"), "namespace WizardSmoke.IdleAutoDefense");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs"), "WizardSmokeIdleAutoDefenseGameBootstrap");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/WizardSmoke.IdleAutoDefense.asmdef"), "Deucarian.TemplateGameIdleAutoDefense");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                AssertGeneratedContentIsDiscoverableInGameContentLibrary(contentRoot);

                string secondTargetRoot = tempRoot + "/W" + Guid.NewGuid().ToString("N").Substring(0, 8);
                var secondRequest = new IdleAutoDefenseTemplateSetupRequest
                {
                    TargetRootAssetPath = secondTargetRoot,
                    ContentRootAssetPath = secondContentRoot,
                    GameNamespace = "WizardSmoke.SecondIdleAutoDefense",
                    GamePrefix = "Wizard Second",
                    AllowOverwrite = false,
                    OpenCreatedScene = false,
                    RefreshAssetDatabase = false
                };
                IdleAutoDefenseTemplateSetupResult second = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(secondRequest);
                Assert.IsTrue(second.Succeeded, second.CreateSummary());
                string secondContentSetGuid = ReadMetaGuid(AssetPathToFullPath(secondContentRoot + "/ContentSets/contentset.template.basic-idle-auto-defense/contentset.template.basic-idle-auto-defense_GameContentSet.asset.meta"));
                Assert.AreNotEqual(contentSetGuid, secondContentSetGuid);

                string reportPath = AssetPathToFullPath(targetRoot + "/Docs/setup-report.md");
                File.WriteAllText(reportPath, "existing report");
                IdleAutoDefenseTemplateSetupResult blocked = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
                Assert.AreEqual(IdleAutoDefenseTemplateSetupStatus.BlockedByExistingFiles, blocked.Status);
                Assert.That(blocked.BlockedFiles.Count, Is.GreaterThan(0));
                Assert.AreEqual("existing report", File.ReadAllText(reportPath));

                request.AllowOverwrite = true;
                IdleAutoDefenseTemplateSetupResult overwritten = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
                Assert.IsTrue(overwritten.Succeeded, overwritten.CreateSummary());
                AssertCreatedPathsStayUnderAllowedRoots(overwritten, targetRoot, contentRoot);
                StringAssert.Contains("Idle Auto Defense Setup Report", File.ReadAllText(reportPath));
            }
            finally
            {
                DeleteDirectoryIfExists(AssetPathToFullPath(tempRoot));
                DeleteDirectoryIfExists(AssetPathToFullPath(contentRoot));
                DeleteDirectoryIfExists(AssetPathToFullPath(secondContentRoot));
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

        private static void AssertChip(IReadOnlyList<DeucarianEditorStatusChip> chips, string label, DeucarianEditorStatus status)
        {
            Assert.That(chips, Is.Not.Null);
            for (int i = 0; i < chips.Count; i++)
            {
                if (!string.Equals(chips[i].Label, label, StringComparison.Ordinal))
                    continue;

                Assert.That(chips[i].Status, Is.EqualTo(status), "Preview chip " + label + " had the wrong status.");
                return;
            }

            Assert.Fail("Expected preview chip '" + label + "' was not found.");
        }

        private static bool FindIssue(GameContentAuthoringValidationResult validation, string path, GameContentAuthoringValidationSeverity severity)
        {
            Assert.That(validation, Is.Not.Null);
            for (int i = 0; i < validation.Issues.Count; i++)
            {
                GameContentAuthoringValidationIssue issue = validation.Issues[i];
                if (issue.Severity == severity && string.Equals(issue.Path, path, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static GameContentSetProviderV2State GetContentSetProviderV2State(GameContentSetAuthoringProvider provider)
        {
            FieldInfo field = typeof(GameContentSetAuthoringProvider).GetField("_v2State", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "GameContentSetAuthoringProvider._v2State was not found.");
            return (GameContentSetProviderV2State)field.GetValue(provider);
        }

        private static GameContentPackProviderV2State GetContentPackProviderV2State(GameContentPackAuthoringProvider provider)
        {
            FieldInfo field = typeof(GameContentPackAuthoringProvider).GetField("_v2State", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "GameContentPackAuthoringProvider._v2State was not found.");
            return (GameContentPackProviderV2State)field.GetValue(provider);
        }

        private static GameContentSetAsset CreateValidContentSet(
            bool omitStartingWeapon = false,
            WeaponDefinitionAsset startingWeaponOverride = null,
            WeaponDefinitionAsset[] weaponsOverride = null,
            EnemyDefinitionAsset[] enemiesOverride = null,
            WaveDefinitionAsset[] wavesOverride = null,
            RunUpgradeDefinitionAsset[] upgradesOverride = null,
            int startingCredits = 60,
            int startingParts = 2,
            float rewardMultiplier = 1.1f,
            float difficultyMultiplier = 1f,
            int sessionLengthTicks = 180,
            bool endless = false)
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
                startingCredits: startingCredits,
                startingParts: startingParts,
                rewardMultiplier: rewardMultiplier,
                difficultyMultiplier: difficultyMultiplier,
                sessionLengthTicks: sessionLengthTicks,
                endless: endless,
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

        private static GameContentPackAsset CreateValidContentPack(GameContentSetAsset contentSet)
        {
            return GameContentPackAsset.CreateTransient(
                "contentpack.test.basic-idle-auto-defense",
                "Test Basic Idle Auto Defense Pack",
                new[] { contentSet },
                contentSet,
                description: "Test-authored content pack.",
                requiredPackages: new[]
                {
                    "com.deucarian.template.game.idle-auto-defense",
                    "com.deucarian.attacks",
                    "com.deucarian.weapon-systems",
                    "com.deucarian.run-upgrades"
                },
                tags: new[] { "test", "content-pack" });
        }

        private static GameContentPackAuthoringState CreateValidContentPackAuthoringState()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            GameContentPackAsset pack = CreateValidContentPack(contentSet);
            GameContentPackAuthoringState state = GameContentPackProviderV2View.FromContentPackAsset(pack);
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

        private static IdleAutoDefenseTemplateController CreateControllerWithContentPack(GameContentPackAsset contentPack, GameContentSetAsset selectedContentSet)
        {
            GameObject host = new GameObject("idle-auto-defense-template-content-pack-editmode");
            host.SetActive(false);
            IdleAutoDefenseTemplateController controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            FieldInfo packField = typeof(IdleAutoDefenseTemplateController).GetField("_contentPack", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo setField = typeof(IdleAutoDefenseTemplateController).GetField("_contentSet", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(packField);
            Assert.IsNotNull(setField);
            packField.SetValue(controller, contentPack);
            setField.SetValue(controller, selectedContentSet);
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

        private static void AssertHasIssue(GameContentPackValidationReport report, string path)
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

        private static string FormatIssues(GameContentPackValidationReport report)
        {
            if (report == null || report.Issues.Count == 0) return "No issues.";
            var messages = new List<string>();
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentPackValidationIssue issue = report.Issues[i];
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
            Assert.IsTrue(DirectoryExists(path), "Expected directory to exist: " + path);
        }

        private static void AssertFileContains(string path, string expected)
        {
            Assert.IsTrue(FileExists(path), "Expected file to exist: " + path);
            StringAssert.Contains(expected, ReadAllText(path));
        }

        private static void AssertFileDoesNotContain(string path, string unexpected)
        {
            Assert.IsTrue(FileExists(path), "Expected file to exist: " + path);
            StringAssert.DoesNotContain(unexpected, ReadAllText(path));
        }

        private static void AssertFileExistsAtFullPath(string path)
        {
            Assert.IsTrue(FileExists(path), "Expected file to exist: " + path);
        }

        private static void AssertFileExists(string assetPath)
        {
            Assert.IsTrue(FileExists(AssetPathToFullPath(assetPath)), "Expected file to exist: " + assetPath);
        }

        private static int CountAuthoredDefinitionFiles(string contentRoot, string searchPattern, string editorClassIdentifier)
        {
            string[] assetPaths = GetFiles(contentRoot, searchPattern);
            int count = 0;
            for (int i = 0; i < assetPaths.Length; i++)
                if (ReadAllText(assetPaths[i]).Contains(editorClassIdentifier))
                    count++;
            return count;
        }

        private static void AssertSampleAuthoredDefinitionIdsAreUnique(string contentRoot)
        {
            AssertDirectoryExists(contentRoot);
            string[] assetPaths = GetFiles(contentRoot, "*.asset");
            var pathsByTypeAndId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < assetPaths.Length; i++)
            {
                if (!TryReadAuthoredDefinitionIdentity(assetPaths[i], out string typeLabel, out string id))
                    continue;

                string key = typeLabel + "\n" + id;
                if (!pathsByTypeAndId.TryGetValue(key, out List<string> paths))
                {
                    paths = new List<string>();
                    pathsByTypeAndId.Add(key, paths);
                }

                paths.Add(ToContentRelativePath(contentRoot, assetPaths[i]));
            }

            var duplicates = new List<string>();
            foreach (KeyValuePair<string, List<string>> pair in pathsByTypeAndId)
            {
                if (pair.Value.Count <= 1)
                    continue;

                string[] parts = pair.Key.Split(new[] { '\n' }, 2);
                duplicates.Add(parts[0] + " '" + parts[1] + "': " + string.Join(", ", pair.Value));
            }

            Assert.That(duplicates, Is.Empty, "Duplicate authored sample IDs:\n" + string.Join("\n", duplicates));
        }

        private static bool TryReadAuthoredDefinitionIdentity(string assetPath, out string typeLabel, out string id)
        {
            typeLabel = string.Empty;
            id = string.Empty;
            string[] lines = ReadAllLines(assetPath);

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.StartsWith("m_EditorClassIdentifier:", StringComparison.Ordinal))
                    typeLabel = ReadAuthoredDefinitionTypeLabel(trimmed);
                else if (trimmed.StartsWith("_id:", StringComparison.Ordinal))
                    id = trimmed.Substring("_id:".Length).Trim();
            }

            return !string.IsNullOrWhiteSpace(typeLabel) && !string.IsNullOrWhiteSpace(id);
        }

        private static string ReadAuthoredDefinitionTypeLabel(string editorClassIdentifier)
        {
            if (editorClassIdentifier.Contains("AttackDefinitionAsset")) return "Attack";
            if (editorClassIdentifier.Contains("EnemyDefinitionAsset")) return "Enemy";
            if (editorClassIdentifier.Contains("WaveDefinitionAsset")) return "Wave";
            if (editorClassIdentifier.Contains("WeaponDefinitionAsset")) return "Tower / Weapon";
            if (editorClassIdentifier.Contains("RunUpgradeDefinitionAsset")) return "Upgrade";
            if (editorClassIdentifier.Contains("GameContentSetAsset")) return "Game / Run Content Set";
            if (editorClassIdentifier.Contains("GameContentPackAsset")) return "Content Pack";
            return string.Empty;
        }

        private static string ToContentRelativePath(string contentRoot, string assetPath)
        {
            string relative = assetPath.Substring(contentRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace(Path.DirectorySeparatorChar, '/');
        }

        private static string ReadMetaGuid(string metaPath)
        {
            Assert.IsTrue(FileExists(metaPath), "Expected meta file to exist: " + metaPath);
            string[] lines = ReadAllLines(metaPath);
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith("guid:", StringComparison.Ordinal))
                    return lines[i].Substring("guid:".Length).Trim();
            Assert.Fail("Expected meta file to contain a guid: " + metaPath);
            return string.Empty;
        }

        private static string[] GetFiles(string fullPath, string searchPattern)
        {
            string[] files = Directory.GetFiles(ToLongPath(fullPath), searchPattern, SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
                files[i] = FromLongPath(files[i]);
            return files;
        }

        private static bool DirectoryExists(string fullPath)
        {
            return Directory.Exists(ToLongPath(fullPath));
        }

        private static bool FileExists(string fullPath)
        {
            return File.Exists(ToLongPath(fullPath));
        }

        private static string ReadAllText(string fullPath)
        {
            return File.ReadAllText(ToLongPath(fullPath));
        }

        private static string[] ReadAllLines(string fullPath)
        {
            return File.ReadAllLines(ToLongPath(fullPath));
        }

        private static string ToLongPath(string fullPath)
        {
#if UNITY_EDITOR_WIN
            if (string.IsNullOrWhiteSpace(fullPath)) return fullPath;
            string normalized = Path.GetFullPath(fullPath);
            if (normalized.StartsWith(@"\\?\", StringComparison.Ordinal)) return normalized;
            if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
                return @"\\?\UNC\" + normalized.Substring(2);
            return @"\\?\" + normalized;
#else
            return fullPath;
#endif
        }

        private static string FromLongPath(string fullPath)
        {
#if UNITY_EDITOR_WIN
            if (string.IsNullOrWhiteSpace(fullPath)) return fullPath;
            if (fullPath.StartsWith(@"\\?\UNC\", StringComparison.Ordinal))
                return @"\\" + fullPath.Substring(8);
            if (fullPath.StartsWith(@"\\?\", StringComparison.Ordinal))
                return fullPath.Substring(4);
#endif
            return fullPath;
        }

        private static string AssetPathToFullPath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void AssertGeneratedContentIsDiscoverableInGameContentLibrary(string contentRoot)
        {
            GameContentLibraryReport report = GameContentLibraryService.Scan("Assets/GameContent");
            Assert.AreEqual(0, CountLibraryErrors(report, contentRoot), FormatLibraryIssues(report, contentRoot));
            Assert.AreEqual(4, CountLibraryItems(report, contentRoot, GameContentLibraryKind.Attack));
            Assert.AreEqual(4, CountLibraryItems(report, contentRoot, GameContentLibraryKind.Enemy));
            Assert.AreEqual(5, CountLibraryItems(report, contentRoot, GameContentLibraryKind.Wave));
            Assert.AreEqual(4, CountLibraryItems(report, contentRoot, GameContentLibraryKind.Weapon));
            Assert.AreEqual(6, CountLibraryItems(report, contentRoot, GameContentLibraryKind.Upgrade));
            Assert.AreEqual(1, CountLibraryItems(report, contentRoot, GameContentLibraryKind.ContentSet));
            Assert.AreEqual(1, CountLibraryItems(report, contentRoot, GameContentLibraryKind.ContentPack));

            GameContentLibraryItem contentSet = FindLibraryItem(report, contentRoot, GameContentLibraryKind.ContentSet, "contentset.template.basic-idle-auto-defense");
            GameContentLibraryItem contentPack = FindLibraryItem(report, contentRoot, GameContentLibraryKind.ContentPack, "contentpack.template.basic-idle-auto-defense");
            Assert.NotNull(contentSet);
            Assert.NotNull(contentPack);

            GameContentLibraryContentSetSummary contentSetSummary = report.GetContentSetSummary(contentSet);
            GameContentLibraryContentPackSummary contentPackSummary = report.GetContentPackSummary(contentPack);
            Assert.NotNull(contentSetSummary);
            Assert.NotNull(contentPackSummary);
            Assert.IsTrue(contentSetSummary.Ready, contentSetSummary.Message + "\n" + FormatLibraryIssues(report, contentRoot));
            Assert.IsTrue(contentPackSummary.Ready, contentPackSummary.Message + "\n" + FormatLibraryIssues(report, contentRoot));
            Assert.AreEqual(4, contentSetSummary.WeaponCount);
            Assert.AreEqual(4, contentSetSummary.EnemyCount);
            Assert.AreEqual(5, contentSetSummary.WaveCount);
            Assert.AreEqual(6, contentSetSummary.UpgradeCount);
            Assert.AreEqual(1, contentPackSummary.ContentSetCount);
            Assert.AreEqual(4, contentPackSummary.WeaponCount);
            Assert.AreEqual(4, contentPackSummary.EnemyCount);
            Assert.AreEqual(5, contentPackSummary.WaveCount);
            Assert.AreEqual(6, contentPackSummary.UpgradeCount);
        }

        private static int CountLibraryItems(GameContentLibraryReport report, string contentRoot, GameContentLibraryKind kind)
        {
            int count = 0;
            for (int i = 0; i < report.Items.Count; i++)
            {
                GameContentLibraryItem item = report.Items[i];
                if (item.Kind == kind && IsPathUnderAssetRoot(item.Path, contentRoot))
                    count++;
            }

            return count;
        }

        private static GameContentLibraryItem FindLibraryItem(GameContentLibraryReport report, string contentRoot, GameContentLibraryKind kind, string id)
        {
            for (int i = 0; i < report.Items.Count; i++)
            {
                GameContentLibraryItem item = report.Items[i];
                if (item.Kind == kind &&
                    string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase) &&
                    IsPathUnderAssetRoot(item.Path, contentRoot))
                {
                    return item;
                }
            }

            return null;
        }

        private static int CountLibraryErrors(GameContentLibraryReport report, string contentRoot)
        {
            int count = 0;
            for (int i = 0; i < report.Items.Count; i++)
            {
                GameContentLibraryItem item = report.Items[i];
                if (!IsPathUnderAssetRoot(item.Path, contentRoot)) continue;
                for (int j = 0; j < item.Issues.Count; j++)
                    if (item.Issues[j].Severity == GameContentAuthoringValidationSeverity.Error)
                        count++;
            }

            return count;
        }

        private static bool IsPathUnderAssetRoot(string assetPath, string root)
        {
            return !string.IsNullOrWhiteSpace(assetPath) &&
                !string.IsNullOrWhiteSpace(root) &&
                assetPath.StartsWith(root.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatLibraryIssues(GameContentLibraryReport report, string contentRoot)
        {
            var lines = new List<string>();
            for (int i = 0; i < report.Items.Count; i++)
            {
                GameContentLibraryItem item = report.Items[i];
                if (!IsPathUnderAssetRoot(item.Path, contentRoot)) continue;
                for (int j = 0; j < item.Issues.Count; j++)
                {
                    GameContentLibraryIssue issue = item.Issues[j];
                    lines.Add(issue.Severity + " " + item.Path + " " + issue.Path + ": " + issue.Message);
                }
            }

            return string.Join("\n", lines);
        }

        private static void AssertCreatedPathsStayUnderAllowedRoots(IdleAutoDefenseTemplateSetupResult result, string targetRoot, string contentRoot)
        {
            for (int i = 0; i < result.CreatedFiles.Count; i++)
            {
                Assert.IsTrue(
                    IsPathUnderAssetRoot(result.CreatedFiles[i], targetRoot) || IsPathUnderAssetRoot(result.CreatedFiles[i], contentRoot),
                    "Created file should stay under target or content root: " + result.CreatedFiles[i]);
                StringAssert.DoesNotContain("Packages/", result.CreatedFiles[i]);
                StringAssert.DoesNotContain("/Runtime/", result.CreatedFiles[i]);
                StringAssert.DoesNotContain("/Editor/", result.CreatedFiles[i]);
            }

            for (int i = 0; i < result.CreatedDirectories.Count; i++)
            {
                Assert.IsTrue(
                    string.Equals(result.CreatedDirectories[i], targetRoot, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(result.CreatedDirectories[i], contentRoot, StringComparison.OrdinalIgnoreCase) ||
                    IsPathUnderAssetRoot(result.CreatedDirectories[i], targetRoot) ||
                    IsPathUnderAssetRoot(result.CreatedDirectories[i], contentRoot),
                    "Created directory should stay under target or content root: " + result.CreatedDirectories[i]);
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

        private static EncounterDefinition CreateFailCapablePressureEncounterDefinition()
        {
            return new EncounterDefinition(
                new EncounterId("encounter.template.fail-pressure"),
                null,
                new[]
                {
                    new WaveDefinition(new WaveId("wave.template.fail-pressure.overrun"), 0, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.fail-pressure.runner-north"), new SpawnableId(BasicIdleAutoDefenseGame.RunnerEnemySpawnableId.Value), 24, 6, 0, 4, new SpawnChannelId("perimeter-north")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.fail-pressure.runner-east"), new SpawnableId(BasicIdleAutoDefenseGame.RunnerEnemySpawnableId.Value), 24, 6, 0, 4, new SpawnChannelId("perimeter-east")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.fail-pressure.swarm-south"), new SpawnableId(BasicIdleAutoDefenseGame.SwarmEnemySpawnableId.Value), 30, 6, 4, 4, new SpawnChannelId("perimeter-south")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.template.fail-pressure.tank-west"), new SpawnableId(BasicIdleAutoDefenseGame.TankEnemySpawnableId.Value), 8, 2, 8, 8, new SpawnChannelId("perimeter-west"))
                    })
                },
                new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves-emitted")) },
                seed: 20260701);
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
