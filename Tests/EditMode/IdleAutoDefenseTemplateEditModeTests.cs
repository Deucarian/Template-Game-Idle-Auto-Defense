using System;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using Deucarian.RunUpgrades;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseTemplateEditModeTests
    {
        [Test]
        public void DefinitionHasCentralObjectivePerimeterEnemiesAndThreeAuthoredWeaponModes()
        {
            AutoDefenseDefinition definition = BasicIdleAutoDefenseGame.CreateDefinition();

            Assert.AreEqual("template-core", definition.Objective.Id.Value);
            Assert.AreEqual(4, definition.SpawnRing.Channels.Count);
            Assert.AreEqual(3, definition.Enemies.Count);
            Assert.AreEqual(3, definition.Mounts.Count);
            Assert.AreEqual(3, definition.WeaponModules.Count);
            Assert.IsTrue(definition.Mounts[0].HasWeapon);
            Assert.IsTrue(definition.Mounts[1].HasWeapon);
            Assert.IsTrue(definition.Mounts[2].HasWeapon);
        }

        [Test]
        public void EnemyAndWaveRecipesCreateRuntimeDefinitions()
        {
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            WaveDefinitionAsset[] waves = BasicIdleAutoDefenseGame.CreateWaveDefinitions();

            Assert.AreEqual(3, enemies.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.BasicEnemySpawnableId.Value, enemies[0].Id);
            Assert.AreEqual(EnemyRole.Fast, enemies[1].Role);
            Assert.AreEqual(EnemyRole.Tank, enemies[2].Role);
            Assert.AreEqual(3, BasicIdleAutoDefenseGame.CreateAutoDefenseEnemyDefinitions(enemies).Length);

            Assert.AreEqual(2, waves.Length);
            Assert.AreEqual("wave.template.early", waves[0].Id);
            Assert.AreEqual("wave.template.mixed", waves[1].Id);
            Assert.AreEqual(2, BasicIdleAutoDefenseGame.CreateEncounterWaves(waves).Length);
            Assert.AreEqual(3, waves[1].Entries.Entries.Count);
        }

        [Test]
        public void AttackRecipesCreateRuntimeDefinitionsProjectilesAndStatuses()
        {
            AttackDefinitionAsset[] recipes = BasicIdleAutoDefenseGame.CreateAttackRecipes();

            Assert.AreEqual(3, recipes.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.HitscanAttackId.Value, recipes[0].Id);
            Assert.AreEqual(AttackRecipeDeliveryMode.Hitscan, recipes[0].Delivery.Mode);
            Assert.AreEqual(AttackRecipeDeliveryMode.Projectile, recipes[1].Delivery.Mode);
            Assert.IsTrue(recipes[2].Delivery.Homing);

            Assert.AreEqual(3, BasicIdleAutoDefenseGame.CreateAttackDefinitions(recipes).Length);
            Assert.AreEqual(2, BasicIdleAutoDefenseGame.CreateProjectileDefinitions(recipes).Length);
            Assert.AreEqual(1, recipes[2].CreateStatusDefinitions().Length);
            Assert.IsTrue(BasicIdleAutoDefenseGame.CreateCombatCatalog(recipes).TryGetStatus(new Deucarian.Combat.StatusEffectId("status.template.slow"), out _));
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
            Assert.AreEqual(3, resolved.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.HitscanAttackId.Value, resolved[0].Id);
            Assert.AreEqual(BasicIdleAutoDefenseGame.FireOrbAttackId.Value, resolved[1].Id);
            Assert.AreEqual(BasicIdleAutoDefenseGame.HomingPulseAttackId.Value, resolved[2].Id);
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
                prefab: new UnityEngine.GameObject("custom-enemy-prefab"));

            try
            {
                EnemyDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveEnemyDefinitionsForTemplate(new[] { customOnly, null }, out int rejectedDefinitionCount);

                Assert.That(rejectedDefinitionCount, Is.GreaterThan(0));
                Assert.AreEqual(3, resolved.Length);
                Assert.AreEqual(BasicIdleAutoDefenseGame.BasicEnemySpawnableId.Value, resolved[0].Id);
                Assert.AreEqual(BasicIdleAutoDefenseGame.FastEnemySpawnableId.Value, resolved[1].Id);
                Assert.AreEqual(BasicIdleAutoDefenseGame.TankEnemySpawnableId.Value, resolved[2].Id);
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
            Assert.AreEqual("wave.template.early", resolved[0].Id);
            Assert.AreEqual("wave.template.mixed", resolved[1].Id);
        }

        [Test]
        public void ValidAssignedAuthoredContentResolvesWithoutFallback()
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            EnemyDefinitionAsset[] enemies = CreateAssignedTemplateEnemiesWithPrefabs();
            WaveDefinitionAsset[] waves = CreateAssignedTemplateWaves(enemies);

            try
            {
                AttackDefinitionAsset[] resolvedAttacks = BasicIdleAutoDefenseGame.ResolveAttackRecipesForTemplate(attacks, out int rejectedRecipeCount);
                EnemyDefinitionAsset[] resolvedEnemies = BasicIdleAutoDefenseGame.ResolveEnemyDefinitionsForTemplate(enemies, out int rejectedEnemyCount);
                WaveDefinitionAsset[] resolvedWaves = BasicIdleAutoDefenseGame.ResolveWaveDefinitionsForTemplate(waves, resolvedEnemies, out int rejectedWaveCount);

                Assert.AreEqual(0, rejectedRecipeCount);
                Assert.AreEqual(0, rejectedEnemyCount);
                Assert.AreEqual(0, rejectedWaveCount);
                Assert.AreEqual(3, resolvedAttacks.Length);
                Assert.AreEqual(3, resolvedEnemies.Length);
                Assert.AreEqual(2, resolvedWaves.Length);
                Assert.AreEqual(BasicIdleAutoDefenseGame.HomingPulseAttackId.Value, resolvedAttacks[2].Id);
                Assert.AreSame(enemies[2], resolvedEnemies[2]);
                Assert.AreSame(waves[1], resolvedWaves[1]);
            }
            finally
            {
                DestroyEnemyPrefabs(enemies);
            }
        }

        [Test]
        public void AssignedEnemyDefinitionsRejectDuplicatesAndFallbackWhenIncomplete()
        {
            EnemyDefinitionAsset[] enemies = CreateAssignedTemplateEnemiesWithPrefabs();
            try
            {
                enemies[2].Configure(
                    BasicIdleAutoDefenseGame.FastEnemySpawnableId.Value,
                    "Duplicate Fast",
                    null,
                    EnemyRole.Tank,
                    Array.Empty<string>(),
                    enemies[2].Stats,
                    enemies[2].Presentation);

                EnemyDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveEnemyDefinitionsForTemplate(enemies, out int rejectedDefinitionCount);

                Assert.That(rejectedDefinitionCount, Is.GreaterThan(0));
                Assert.AreEqual(3, resolved.Length);
                Assert.AreEqual(BasicIdleAutoDefenseGame.TankEnemySpawnableId.Value, resolved[2].Id);
                Assert.AreNotSame(enemies[2], resolved[2]);
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

        private static EnemyDefinitionAsset[] CreateAssignedTemplateEnemiesWithPrefabs()
        {
            return new[]
            {
                EnemyDefinitionAsset.CreateTransient(
                    BasicIdleAutoDefenseGame.BasicEnemySpawnableId.Value,
                    "Assigned Basic Enemy",
                    EnemyRole.Basic,
                    8f,
                    2.2f,
                    1,
                    3f,
                    BasicIdleAutoDefenseGame.DamageType.Value,
                    prefab: new GameObject("assigned-basic-enemy-prefab")),
                EnemyDefinitionAsset.CreateTransient(
                    BasicIdleAutoDefenseGame.FastEnemySpawnableId.Value,
                    "Assigned Fast Enemy",
                    EnemyRole.Fast,
                    5f,
                    3.6f,
                    1,
                    2f,
                    BasicIdleAutoDefenseGame.DamageType.Value,
                    prefab: new GameObject("assigned-fast-enemy-prefab")),
                EnemyDefinitionAsset.CreateTransient(
                    BasicIdleAutoDefenseGame.TankEnemySpawnableId.Value,
                    "Assigned Tank Enemy",
                    EnemyRole.Tank,
                    22f,
                    1.25f,
                    3,
                    5f,
                    BasicIdleAutoDefenseGame.DamageType.Value,
                    prefab: new GameObject("assigned-tank-enemy-prefab"))
            };
        }

        private static WaveDefinitionAsset[] CreateAssignedTemplateWaves(EnemyDefinitionAsset[] enemies)
        {
            return new[]
            {
                WaveDefinitionAsset.CreateTransient(
                    "wave.assigned.early",
                    "Assigned Early",
                    0,
                    new[] { new WaveEntryRecipe(enemies[0], 2, 1, 0, 10, "perimeter-north") }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.assigned.mixed",
                    "Assigned Mixed",
                    12,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[0], 2, 1, 0, 10, "perimeter-east"),
                        new WaveEntryRecipe(enemies[1], 2, 1, 4, 10, "perimeter-south"),
                        new WaveEntryRecipe(enemies[2], 1, 1, 8, 16, "perimeter-west")
                    })
            };
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
    }
}
