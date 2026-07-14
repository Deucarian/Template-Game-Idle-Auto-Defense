using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Deucarian.Attacks.Authoring;
using Deucarian.Attacks.Editor;
using Deucarian.AutoDefense;
using Deucarian.Editor;
using Deucarian.Encounters;
using Deucarian.GameContentAuthoring.Editor;
using Deucarian.IdleProgression;
using Deucarian.Monetization;
using Deucarian.Progression;
using Deucarian.RunUpgrades;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.RunUpgrades.Editor;
using Deucarian.TemplateGameIdleAutoDefense.Editor;
using Deucarian.WeaponSystems;
using Deucarian.WeaponSystems.Authoring;
using Deucarian.WeaponSystems.Editor;
using Deucarian.WorldSpawning;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseTemplateEditModeTests
    {
        [Test]
        public void DefinitionHasCentralObjectivePerimeterEnemiesAndFourWeaponModes()
        {
            AutoDefenseDefinition definition = BasicIdleAutoDefenseGame.CreateDefinition();

            Assert.AreEqual("objective.idle-auto-defense.core", definition.Objective.Id.Value);
            Assert.AreEqual(8, definition.SpawnRing.Channels.Count);
            Assert.That(definition.SpawnRing.Radius, Is.GreaterThanOrEqualTo(18f));
            Assert.AreEqual(6, definition.Enemies.Count);
            Assert.AreEqual(4, definition.Mounts.Count);
            Assert.AreEqual(4, definition.WeaponModules.Count);
            Assert.IsTrue(definition.Mounts[0].HasWeapon);
            Assert.IsTrue(definition.Mounts[1].HasWeapon);
            Assert.IsFalse(definition.Mounts[0].Enabled);
            Assert.IsFalse(definition.Mounts[1].Enabled);
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

            Assert.AreEqual(7, waves.Length);
            Assert.AreEqual("wave.idle-auto-defense.opening", waves[0].Id);
            Assert.AreEqual("wave.idle-auto-defense.runner-pressure", waves[1].Id);
            Assert.AreEqual(7, BasicIdleAutoDefenseGame.CreateEncounterWaves(waves).Length);
            Assert.AreEqual(2, waves[1].Entries.Entries.Count);
        }

        [Test]
        public void WaveEntryIdsPreserveLegacyGroupIdsAndDeterministicSpawnPoses()
        {
            WaveDefinitionAsset[] authored = BasicIdleAutoDefenseGame.CreateWaveDefinitions();
            WaveDefinition[] runtime = BasicIdleAutoDefenseGame.CreateEncounterWaves(authored);
            for (int i = 0; i < authored.Length; i++)
            {
                Assert.That(runtime[i].SpawnGroups.Count, Is.EqualTo(authored[i].Entries.Entries.Count));
                for (int j = 0; j < authored[i].Entries.Entries.Count; j++)
                {
                    string legacyGroupId = authored[i].Id + ".group." + j;
                    Assert.That(authored[i].Entries.Entries[j].EntryId.Value, Is.EqualTo(j.ToString()));
                    Assert.That(runtime[i].SpawnGroups[j].Id.Value, Is.EqualTo(legacyGroupId));
                }
            }

            AutoDefenseDefinition definition = BasicIdleAutoDefenseGame.CreateDefinition();
            Type resolverType = typeof(IdleAutoDefenseTemplateController).GetNestedType(
                "TemplateJitteredPerimeterPoseResolver",
                BindingFlags.NonPublic);
            Assert.That(resolverType, Is.Not.Null);
            var resolver = (ISpawnPoseResolver)Activator.CreateInstance(
                resolverType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { definition.Objective, definition.SpawnRing },
                null);
            WaveEntryRecipe firstEntry = authored[0].Entries.Entries[0];
            string legacyId = authored[0].Id + ".group.0";
            string stableId = runtime[0].SpawnGroups[0].Id.Value;
            var legacyRequest = new WorldSpawnRequest(
                new WorldSpawnableId(firstEntry.Enemy.Id),
                new WorldSpawnChannelId(firstEntry.SpawnChannelId),
                7,
                new WorldSpawnRequestContext("test", "encounter.test", authored[0].Id, legacyId, 0, 23));
            var stableRequest = new WorldSpawnRequest(
                new WorldSpawnableId(firstEntry.Enemy.Id),
                new WorldSpawnChannelId(firstEntry.SpawnChannelId),
                7,
                new WorldSpawnRequestContext("test", "encounter.test", authored[0].Id, stableId, 0, 23));

            SpawnPoseResult legacyPose = resolver.TryResolvePose(legacyRequest);
            SpawnPoseResult stablePose = resolver.TryResolvePose(stableRequest);

            Assert.That(stableId, Is.EqualTo(legacyId));
            Assert.That(legacyPose.Succeeded, Is.True, legacyPose.Message);
            Assert.That(stablePose.Succeeded, Is.True, stablePose.Message);
            Assert.That(stablePose.Pose.Position, Is.EqualTo(legacyPose.Pose.Position));
            Assert.That(stablePose.Pose.Rotation, Is.EqualTo(legacyPose.Pose.Rotation));
        }

        [Test]
        public void StableWaveEntryIdsSurviveReorderInsertRemoveAndSnapshotRestore()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient(
                "enemy.entry-identity",
                "Entry Identity Enemy",
                EnemyRole.Basic,
                8f,
                2f,
                1,
                3f,
                BasicIdleAutoDefenseGame.DamageType.Value);
            WaveDefinitionAsset original = WaveDefinitionAsset.CreateTransient(
                "wave.entry-identity",
                "Entry Identity",
                0,
                new[]
                {
                    new WaveEntryRecipe("alpha", enemy, 2, 1, 0, 10, "perimeter-north"),
                    new WaveEntryRecipe("beta", enemy, 2, 1, 0, 10, "perimeter-east")
                });
            WaveDefinitionAsset reorderedAndInserted = WaveDefinitionAsset.CreateTransient(
                "wave.entry-identity",
                "Entry Identity",
                0,
                new[]
                {
                    new WaveEntryRecipe("beta", enemy, 2, 1, 0, 10, "perimeter-east"),
                    new WaveEntryRecipe("gamma", enemy, 2, 1, 0, 10, "perimeter-south"),
                    new WaveEntryRecipe("alpha", enemy, 2, 1, 0, 10, "perimeter-north")
                });
            WaveDefinitionAsset removed = WaveDefinitionAsset.CreateTransient(
                "wave.entry-identity",
                "Entry Identity",
                0,
                new[] { new WaveEntryRecipe("beta", enemy, 2, 1, 0, 10, "perimeter-east") });
            try
            {
                WaveDefinition originalRuntimeWave = BasicIdleAutoDefenseGame.CreateEncounterWaves(new[] { original })[0];
                WaveDefinition changedRuntimeWave = BasicIdleAutoDefenseGame.CreateEncounterWaves(new[] { reorderedAndInserted })[0];
                WaveDefinition removedRuntimeWave = BasicIdleAutoDefenseGame.CreateEncounterWaves(new[] { removed })[0];
                Assert.That(changedRuntimeWave.SpawnGroups.Select(group => group.Id.Value), Is.EqualTo(new[]
                {
                    "wave.entry-identity.group.beta",
                    "wave.entry-identity.group.gamma",
                    "wave.entry-identity.group.alpha"
                }));
                Assert.That(removedRuntimeWave.SpawnGroups.Single().Id.Value, Is.EqualTo("wave.entry-identity.group.beta"));

                var originalDefinition = new EncounterDefinition(
                    new EncounterId("encounter.entry-identity"),
                    null,
                    new[] { originalRuntimeWave },
                    new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves")) });
                var runtime = new EncounterRuntime(originalDefinition);
                runtime.Start();
                var request = new SpawnRequest[1];
                EncounterDrainResult drain = runtime.DrainSpawnRequests(request);
                Assert.That(drain.Written, Is.EqualTo(1));
                Assert.That(request[0].GroupId.Value, Is.EqualTo("wave.entry-identity.group.alpha"));
                EncounterSnapshot snapshot = runtime.CreateSnapshot();

                var changedDefinition = new EncounterDefinition(
                    new EncounterId("encounter.entry-identity"),
                    null,
                    new[] { changedRuntimeWave },
                    new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("all-waves")) });
                EncounterRuntime restored = EncounterRuntime.FromSnapshot(changedDefinition, snapshot);
                EncounterSnapshot restoredSnapshot = restored.CreateSnapshot();

                Assert.That(restoredSnapshot.Groups.Single(group => group.GroupId.Value.EndsWith(".alpha", StringComparison.Ordinal)).EmittedCount, Is.EqualTo(1));
                Assert.That(restoredSnapshot.Groups.Single(group => group.GroupId.Value.EndsWith(".beta", StringComparison.Ordinal)).EmittedCount, Is.EqualTo(0));
                Assert.That(restoredSnapshot.Groups.Single(group => group.GroupId.Value.EndsWith(".gamma", StringComparison.Ordinal)).EmittedCount, Is.EqualTo(0));
            }
            finally
            {
                DestroyTransientWave(original);
                DestroyTransientWave(reorderedAndInserted);
                DestroyTransientWave(removed);
                DestroyTransientEnemy(enemy);
            }
        }

        [Test]
        public void AttackRecipesCreateRuntimeDefinitionsAndProjectiles()
        {
            AttackDefinitionAsset[] recipes = BasicIdleAutoDefenseGame.CreateAttackRecipes();

            Assert.AreEqual(4, recipes.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.PulseAttackId.Value, recipes[0].Id);
            Assert.AreEqual(AttackRecipeDeliveryMode.Hitscan, recipes[0].Delivery.Mode);
            Assert.AreEqual(BasicIdleAutoDefenseGame.ShardAttackId.Value, recipes[1].Id);
            Assert.AreEqual(AttackRecipeDeliveryMode.Projectile, recipes[1].Delivery.Mode);
            Assert.AreEqual(BasicIdleAutoDefenseGame.ArcBurstAttackId.Value, recipes[2].Id);
            Assert.AreEqual(AttackRecipeDeliveryMode.Area, recipes[2].Delivery.Mode);
            Assert.AreEqual(BasicIdleAutoDefenseGame.HomingPulseAttackId.Value, recipes[3].Id);
            Assert.AreEqual(AttackRecipeDeliveryMode.Projectile, recipes[3].Delivery.Mode);

            Assert.AreEqual(4, BasicIdleAutoDefenseGame.CreateAttackDefinitions(recipes).Length);
            Assert.AreEqual(2, BasicIdleAutoDefenseGame.CreateProjectileDefinitions(recipes).Length);
            Assert.AreEqual(0, recipes[1].CreateStatusDefinitions().Length);
            Assert.That(recipes[1].Mechanics.Range, Is.LessThan(5.5f));
            Assert.That(recipes[1].Mechanics.DamageAmount, Is.LessThan(4f));
            Assert.That(recipes[1].Delivery.ProjectileSpeed, Is.InRange(4f, 4.5f));
        }

        [Test]
        public void WeaponAndUpgradeRecipesCreateRuntimeDefinitions()
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
            RunUpgradeDefinitionAsset[] upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);

            Assert.AreEqual(4, weapons.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, weapons[0].Id);
            Assert.AreEqual(WeaponFireMode.Projectile, weapons[0].Stats.FireMode);
            Assert.AreEqual(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, weapons[1].Id);
            Assert.AreEqual(WeaponFireMode.DirectAttack, weapons[1].Stats.FireMode);
            Assert.AreEqual(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, weapons[2].Id);
            Assert.AreEqual(WeaponFireMode.DirectAttack, weapons[2].Stats.FireMode);
            Assert.AreEqual(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, weapons[3].Id);
            Assert.AreEqual(WeaponFireMode.Projectile, weapons[3].Stats.FireMode);
            Assert.AreEqual(4, BasicIdleAutoDefenseGame.CreateWeaponDefinitions(weapons).Length);
            Assert.AreEqual(4, BasicIdleAutoDefenseGame.CreateDefinition(null, weapons).WeaponModules.Count);
            Assert.That(weapons[0].Stats.CooldownTicks, Is.EqualTo(34));
            Assert.That(weapons[0].Stats.Range, Is.LessThan(5.5f));
            Assert.That(weapons[3].Stats.Range, Is.GreaterThan(weapons[2].Stats.Range));

            Assert.AreEqual(6, upgrades.Length);
            Assert.AreEqual("upgrade.idle-auto-defense.damage-up", upgrades[0].Id);
            Assert.AreEqual(6, BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitions(upgrades).Length);
            Assert.IsTrue(BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog(upgrades).TryGet(new RunUpgradeId("upgrade.idle-auto-defense.projectile-speed-up"), out _));
        }

        [Test]
        public void RewardDraftCatalogHasAssetFlipReadableWeaponUpgradeTracks()
        {
            IdleAutoDefenseRewardDraftCatalog catalog = IdleAutoDefenseRewardDraftCatalog.CreateDefault();
            string[] weaponIds =
            {
                BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value,
                BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value,
                BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value,
                BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value
            };

            Assert.That(catalog.WeaponUnlocks.Count, Is.EqualTo(3));
            Assert.That(catalog.BaseRewards.Count, Is.GreaterThanOrEqualTo(6));
            for (int i = 0; i < weaponIds.Length; i++)
            {
                Assert.That(catalog.CountWeaponRewards(weaponIds[i], IdleAutoDefenseRewardRarity.Common), Is.EqualTo(1), weaponIds[i]);
                Assert.That(catalog.CountWeaponRewards(weaponIds[i], IdleAutoDefenseRewardRarity.Uncommon), Is.EqualTo(1), weaponIds[i]);
                Assert.That(catalog.CountWeaponRewards(weaponIds[i], IdleAutoDefenseRewardRarity.Rare), Is.EqualTo(1), weaponIds[i]);
                Assert.That(catalog.CountWeaponRewards(weaponIds[i], IdleAutoDefenseRewardRarity.Epic), Is.EqualTo(3), weaponIds[i]);
                Assert.That(catalog.CountWeaponRewards(weaponIds[i], IdleAutoDefenseRewardRarity.Legendary), Is.EqualTo(1), weaponIds[i]);
                Assert.That(catalog.GetNormalWeaponReward(weaponIds[i], 0).DisplayName, Is.Not.Empty);
                Assert.That(catalog.GetNormalWeaponReward(weaponIds[i], 0).Amount, Is.GreaterThanOrEqualTo(2d), "First damage reward should be a visible power spike.");
                Assert.That(catalog.GetNormalWeaponReward(weaponIds[i], 1).Amount, Is.GreaterThanOrEqualTo(2d), "First fire-rate reward should be a visible power spike.");
                Assert.That(catalog.GetEpicWeaponReward(weaponIds[i], 2).EffectDescription, Is.Not.Empty);
                Assert.That(catalog.GetLegendaryWeaponReward(weaponIds[i]).Rarity, Is.EqualTo(IdleAutoDefenseRewardRarity.Legendary));
            }

            Assert.That(catalog.GetNormalWeaponReward(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, 2).DisplayName, Is.EqualTo("Split Tip"));
            Assert.That(catalog.GetNormalWeaponReward(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, 2).EffectKind, Is.EqualTo(IdleAutoDefenseRewardEffectKind.PulsePower));
            Assert.That(catalog.GetNormalWeaponReward(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, 2).EffectKind, Is.EqualTo(IdleAutoDefenseRewardEffectKind.ArcPower));
            Assert.That(catalog.GetNormalWeaponReward(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, 2).EffectKind, Is.EqualTo(IdleAutoDefenseRewardEffectKind.HomingPower));
            Assert.That(catalog.GetEpicWeaponReward(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, 0).DisplayName, Is.EqualTo("Fracture Burst"));
            Assert.That(catalog.GetEpicWeaponReward(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, 0).DisplayName, Is.EqualTo("Refracting Beam"));
            Assert.That(catalog.GetEpicWeaponReward(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, 0).DisplayName, Is.EqualTo("Cluster Shells"));
            Assert.That(catalog.GetEpicWeaponReward(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, 1).DisplayName, Is.EqualTo("Target Painter"));
            Assert.That(catalog.GetLegendaryWeaponReward(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value).DisplayName, Is.EqualTo("Crystal Tempest"));
            Assert.That(catalog.GetLegendaryWeaponReward(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value).DisplayName, Is.EqualTo("Orbital Lance"));
            Assert.That(catalog.GetLegendaryWeaponReward(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value).DisplayName, Is.EqualTo("Siege Barrage"));
            Assert.That(catalog.GetLegendaryWeaponReward(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value).DisplayName, Is.EqualTo("Carrier Hive"));
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
            Assert.AreEqual(4, resolved.Length);
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
            Assert.AreEqual(7, resolved.Length);
            Assert.AreEqual("wave.idle-auto-defense.opening", resolved[0].Id);
            Assert.AreEqual("wave.idle-auto-defense.runner-pressure", resolved[1].Id);
        }

        [Test]
        public void AssignedWaveDefinitionsRejectMissingInvalidAndDuplicateEntryIds()
        {
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            var cases = new[]
            {
                new[] { new WaveEntryRecipe(string.Empty, enemies[0], 2, 1, 0, 10, "perimeter-north") },
                new[] { new WaveEntryRecipe("Invalid ID", enemies[0], 2, 1, 0, 10, "perimeter-north") },
                new[]
                {
                    new WaveEntryRecipe("same", enemies[0], 2, 1, 0, 10, "perimeter-north"),
                    new WaveEntryRecipe("same", enemies[0], 2, 1, 0, 10, "perimeter-east")
                }
            };

            for (int i = 0; i < cases.Length; i++)
            {
                WaveDefinitionAsset wave = WaveDefinitionAsset.CreateTransient(
                    "wave.invalid-entry-id." + i,
                    "Invalid Entry Identity",
                    0,
                    cases[i]);
                try
                {
                    ContentAuthoringValidationReport report = WaveDefinitionValidator.Validate(wave);
                    WaveDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveWaveDefinitionsForTemplate(
                        new[] { wave },
                        enemies,
                        out int rejectedDefinitionCount);

                    Assert.That(report.IsValid, Is.False);
                    Assert.That(report.Issues.Any(issue => issue.Path.EndsWith(".EntryId", StringComparison.Ordinal)), Is.True);
                    Assert.That(rejectedDefinitionCount, Is.EqualTo(1));
                    Assert.That(resolved[0].Id, Is.EqualTo("wave.idle-auto-defense.opening"));
                    Assert.Throws<ArgumentException>(() => BasicIdleAutoDefenseGame.CreateEncounterWaves(new[] { wave }));
                }
                finally
                {
                    DestroyTransientWave(wave);
                }
            }
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
                Assert.AreEqual(4, resolvedAttacks.Length);
                Assert.AreEqual(6, resolvedEnemies.Length);
                Assert.AreEqual(7, resolvedWaves.Length);
                Assert.AreEqual(4, resolvedWeapons.Length);
                Assert.AreEqual(6, resolvedUpgrades.Length);
                Assert.AreSame(enemies[5], resolvedEnemies[5]);
                Assert.AreSame(waves[6], resolvedWaves[6]);
                Assert.AreSame(weapons[3], resolvedWeapons[3]);
                Assert.AreSame(upgrades[5], resolvedUpgrades[5]);
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
            Assert.AreEqual(4, resolution.AttackRecipes.Count);
            Assert.AreEqual(6, resolution.Enemies.Count);
            Assert.AreEqual(7, resolution.Waves.Count);
            Assert.AreEqual(4, resolution.Weapons.Count);
            Assert.AreEqual(6, resolution.Upgrades.Count);
            Assert.AreSame(contentSet.StartingWeapon, resolution.Weapons[0]);
            Assert.NotNull(contentSet.RuntimeSettings);
            Assert.AreEqual(3, contentSet.RewardCatalog.Catalog.WeaponUnlocks.Count);
            Assert.NotNull(contentSet.RuntimeSettings.ObjectivePresentation);
            Assert.AreEqual(3, contentSet.RuntimeSettings.ObjectivePresentation.Models.Count);
            Assert.AreEqual(4, contentSet.RuntimeSettings.ModuleSlotPresentationBindings.Count);
            Assert.AreEqual(4, contentSet.RuntimeSettings.WeaponPresentationBindings.Count);
            Assert.IsFalse(contentSet.RuntimeSettings.PresentationDebug.AnyEnabled);
        }

        [Test]
        public void GameContentSetValidationBlocksMissingAuthoredRuntimePresentation()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            FieldInfo objectiveField = typeof(IdleAutoDefenseContentSetRuntimeSettings).GetField("_objectivePresentation", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo slotsField = typeof(IdleAutoDefenseContentSetRuntimeSettings).GetField("_moduleSlotPresentationBindings", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(objectiveField);
            Assert.NotNull(slotsField);
            objectiveField.SetValue(contentSet.RuntimeSettings, null);
            slotsField.SetValue(contentSet.RuntimeSettings, Array.Empty<IdleAutoDefenseModuleSlotPresentationBinding>());

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsFalse(report.IsValid);
            AssertHasIssue(report, "RuntimeSettings.ObjectivePresentation");
            AssertHasIssue(report, "RuntimeSettings.ModuleSlotPresentationBindings");
        }

        [Test]
        public void GameContentSetValidationBlocksMissingRuntimePresentationBinding()
        {
            GameContentSetAsset contentSet = CreateValidContentSet();
            IdleAutoDefenseContentSetRuntimeSettings incomplete = IdleAutoDefenseContentSetRuntimeSettings.CreateDefault();
            FieldInfo bindingsField = typeof(IdleAutoDefenseContentSetRuntimeSettings).GetField("_weaponPresentationBindings", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(bindingsField);
            bindingsField.SetValue(incomplete, new[] { IdleAutoDefenseWeaponPresentationBinding.CreateDefaultBindings()[0] });
            contentSet.Configure(
                contentSet.Id,
                contentSet.DisplayName,
                contentSet.Description,
                contentSet.Icon,
                contentSet.Banner,
                contentSet.StartingWeapon,
                contentSet.AvailableWeapons,
                contentSet.EnemyPool,
                contentSet.WaveSet,
                contentSet.UpgradePool,
                contentSet.StartingCredits,
                contentSet.StartingParts,
                contentSet.RewardMultiplier,
                contentSet.DifficultyMultiplier,
                contentSet.SessionLengthTicks,
                contentSet.Endless,
                contentSet.Tags,
                incomplete);

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsFalse(report.IsValid);
            AssertHasIssue(report, "RuntimeSettings.WeaponPresentationBindings");
        }

        [Test]
        public void GameContentSetValidationBlocksPulseBeamPresentationOnNonBeamWeapons()
        {
            GameObject pulseBeamVfx = new GameObject("PulseBeamVfx");
            GameObject impactVfx = new GameObject("ImpactVfx");
            try
            {
                AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
                attacks[1].Presentation.Configure(new[]
                {
                    new AttackPresentationEventRecipe(AttackPresentationEventKind.OnFire, vfxPrefab: pulseBeamVfx, spawnPointRole: AttackPresentationSpawnPointRole.Muzzle),
                    new AttackPresentationEventRecipe(AttackPresentationEventKind.OnImpact, vfxPrefab: impactVfx, spawnPointRole: AttackPresentationSpawnPointRole.ImpactPoint)
                });
                WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
                GameContentSetAsset contentSet = CreateValidContentSet(weaponsOverride: weapons);

                GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

                Assert.IsFalse(report.IsValid);
                AssertHasIssue(report, "Presentation.OnFire");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pulseBeamVfx);
                UnityEngine.Object.DestroyImmediate(impactVfx);
            }
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
            Assert.AreEqual(4, dependencies.WeaponCount);
            Assert.AreEqual(4, dependencies.AttackCount);
            Assert.AreEqual(6, dependencies.EnemyCount);
            Assert.AreEqual(7, dependencies.WaveCount);
            Assert.AreEqual(6, dependencies.UpgradeCount);
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
            AssertHasIssue(report, "AuthoredCore.Economy.Currencies[0].StartingAmount");
            AssertHasIssue(report, "AuthoredCore.Economy.Currencies[1].StartingAmount");
            AssertHasIssue(report, "AuthoredCore.RunProfile.RewardMultiplier");
            AssertHasIssue(report, "AuthoredCore.RunProfile.DifficultyMultiplier");
            AssertHasIssue(report, "AuthoredCore.RunProfile.SessionLengthTicks");
        }

        [Test]
        public void GameContentSetValidationWarnsWhenEndlessSessionLengthIsOnlyPreviewHint()
        {
            GameContentSetAsset contentSet = CreateValidContentSet(sessionLengthTicks: 0, endless: true);

            GameContentSetValidationReport report = GameContentSetValidator.Validate(contentSet);

            Assert.IsTrue(report.IsValid, FormatIssues(report));
            AssertHasIssue(report, "AuthoredCore.RunProfile.SessionLengthTicks");
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
                Assert.IsTrue(controller.UsingContentSetRuntimeSettings);
                Assert.That(controller.AuthoredWeaponPresentationBindingCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.AreEqual(0, controller.FallbackWeaponPresentationBindingCount, controller.StatusSummary);
                Assert.That(controller.AuthoredObjectivePresentationBindingCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.AreEqual(0, controller.FallbackObjectivePresentationBindingCount, controller.StatusSummary);
                Assert.That(controller.AuthoredModuleSlotPresentationBindingCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.AreEqual(0, controller.FallbackModuleSlotPresentationBindingCount, controller.StatusSummary);
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
                Assert.IsTrue(controller.UsingContentSetRuntimeSettings);
                Assert.That(controller.AuthoredWeaponPresentationBindingCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.AreEqual(0, controller.FallbackWeaponPresentationBindingCount, controller.StatusSummary);
                Assert.That(controller.AuthoredObjectivePresentationBindingCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.AreEqual(0, controller.FallbackObjectivePresentationBindingCount, controller.StatusSummary);
                Assert.That(controller.AuthoredModuleSlotPresentationBindingCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.AreEqual(0, controller.FallbackModuleSlotPresentationBindingCount, controller.StatusSummary);
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
            IdleAutoDefenseTemplateController controller = null;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                controller = CreateControllerWithContentSet(contentSet);
                Assert.IsFalse(controller.UsingAssignedContentSet);
                Assert.That(controller.InvalidAssignedContentSetIssueCount, Is.GreaterThan(0));
                Assert.IsNotNull(controller.Runtime);
                Assert.AreEqual("Running", controller.RuntimeStateName);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                DestroyController(controller);
            }
        }

        [Test]
        public void ControllerFallsBackSafelyWhenAssignedGameContentPackIsInvalid()
        {
            GameContentSetAsset contentSet = CreateValidContentSet(omitStartingWeapon: true);
            GameContentPackAsset pack = CreateValidContentPack(contentSet);
            IdleAutoDefenseTemplateController controller = null;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                controller = CreateControllerWithContentPack(pack, null);
                Assert.IsFalse(controller.UsingAssignedContentPack);
                Assert.IsFalse(controller.UsingAssignedContentSet);
                Assert.That(controller.InvalidAssignedContentPackIssueCount, Is.GreaterThan(0));
                Assert.IsNotNull(controller.Runtime);
                Assert.AreEqual("Running", controller.RuntimeStateName);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
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
            Assert.That(GameContentSetProviderV2View.BuildWeaponAttackSummary(state.StartingWeapon), Does.Contain("Mode"));
            Assert.That(GameContentSetProviderV2View.BuildWeaponAttackSummary(state.StartingWeapon), Does.Contain("Projectile"));
            Assert.That(GameContentSetProviderV2View.BuildWeaponAttackSummary(state.StartingWeapon), Does.Contain("Impact"));
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
            Assert.AreEqual(4, missingRequired.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, missingRequired[0].Id);

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
            Assert.AreEqual(4, missingAttack.Length);
            Assert.AreEqual(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, missingAttack[1].Id);
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
            Assert.AreEqual(6, resolved.Length);
            Assert.AreEqual("upgrade.idle-auto-defense.damage-up", resolved[0].Id);
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
        public void RewardDraftSettingsExposeTunableRarityExperienceAndThresholds()
        {
            IdleAutoDefenseRewardDraftSettings settings = IdleAutoDefenseRewardDraftSettings.CreateDefault();

            Assert.AreEqual(3, settings.ChoiceCount);
            Assert.AreEqual(3, settings.NormalInvestmentsForEpic);
            Assert.AreEqual(3, settings.EpicInvestmentsForLegendary);
            Assert.That(settings.EliteEnemyExperience, Is.GreaterThan(settings.NormalEnemyExperience));
            Assert.That(settings.BossEnemyExperience, Is.GreaterThan(settings.EliteEnemyExperience));
            Assert.That(settings.GetRarityWeight(IdleAutoDefenseRewardDraftKind.BossDefeated, IdleAutoDefenseRewardRarity.Epic),
                Is.GreaterThan(settings.GetRarityWeight(IdleAutoDefenseRewardDraftKind.LevelUp, IdleAutoDefenseRewardRarity.Epic)));

            settings.BossRarityWeights.Legendary = 999d;
            settings.NormalEnemyExperience = 100;
            settings.ProjectileRetargetRadius = 5.5f;

            Assert.AreEqual(999d, settings.GetRarityWeight(IdleAutoDefenseRewardDraftKind.BossDefeated, IdleAutoDefenseRewardRarity.Legendary));
            Assert.AreEqual(100, settings.NormalEnemyExperience);
            Assert.AreEqual(5.5f, settings.ProjectileRetargetRadius);
        }

        [Test]
        public void RewardDraftOffersThreeUniqueReadableChoicesAndHotkeySelection()
        {
            IdleAutoDefenseTemplateController controller = CreateController();
            try
            {
                controller.Build();
                controller.RequestRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);

                Assert.IsTrue(controller.RewardDraftActive);
                Assert.AreEqual(3, controller.RewardDraftChoiceCount);
                CollectionAssert.AreEqual(
                    new[]
                    {
                        "reward.unlock.weapon.idle-auto-defense.pulse-beam",
                        "base.damage",
                        "base.fire-rate"
                    },
                    RewardChoiceIds(controller));
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var dedupeGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool offeredUnlock = false;
                bool offeredExcitingChoice = false;
                for (int i = 0; i < controller.RewardDraftChoices.Count; i++)
                {
                    IdleAutoDefenseRewardDraftChoice choice = controller.RewardDraftChoices[i];
                    Assert.IsTrue(ids.Add(choice.Id), "Duplicate reward choice: " + choice.Id);
                    Assert.IsTrue(dedupeGroups.Add(GetRewardChoiceDedupeKey(choice)), "Duplicate reward dedupe group: " + GetRewardChoiceDedupeKey(choice));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(choice.DisplayName));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(choice.RarityName));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(choice.TypeName));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(choice.TargetName));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(choice.EffectDescription));
                    Assert.AreEqual((i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), choice.HotkeyLabel);
                    offeredUnlock |= choice.IsUnlock;
                    offeredExcitingChoice |= IsExcitingRewardChoice(choice);
                }

                Assert.IsTrue(offeredUnlock, "The first level-up draft should offer at least one module unlock.");
                Assert.IsTrue(offeredExcitingChoice, "Every draft should try to include a visible behavior-changing or high-rarity option.");
                float pausedSurvivalSeconds = controller.SurvivalSeconds;
                controller.Step(1, 0.05f);
                Assert.AreEqual(pausedSurvivalSeconds, controller.SurvivalSeconds, "An active reward draft should continue to pause combat.");
                Assert.IsFalse(controller.PulseBeamUnlocked);
                Assert.IsTrue(controller.TryChooseRewardDraftHotkey(1));
                Assert.AreEqual(1, controller.RewardDraftSelectionCount);
                Assert.IsTrue(controller.PulseBeamUnlocked, "The preferred authored unlock should apply its existing effect.");
                controller.Step(1, 0.05f);
                Assert.That(controller.SurvivalSeconds, Is.GreaterThan(pausedSurvivalSeconds), "Combat should resume after the draft closes.");
            }
            finally
            {
                DestroyController(controller);
            }
        }

        [Test]
        public void RewardDraftSelectionAndQueueAreDeterministicAndAdvanceSeedWhenOpened()
        {
            IdleAutoDefenseTemplateController first = CreateController();
            IdleAutoDefenseTemplateController second = CreateController();
            try
            {
                Assert.AreEqual(0, GetRewardDraftSeed(first));
                Assert.AreEqual(0, GetRewardDraftSeed(second));

                first.RequestRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
                second.RequestRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
                CollectionAssert.AreEqual(RewardChoiceIds(first), RewardChoiceIds(second));
                Assert.AreEqual(1, GetRewardDraftSeed(first));
                Assert.AreEqual(1, GetRewardDraftSeed(second));

                first.RequestRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
                second.RequestRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
                Assert.AreEqual(1, GetRewardDraftSeed(first), "Queued drafts must not consume a seed before opening.");
                Assert.AreEqual(1, GetRewardDraftSeed(second), "Queued drafts must not consume a seed before opening.");

                Assert.IsTrue(first.TryChooseRewardDraftChoice(0));
                Assert.IsTrue(second.TryChooseRewardDraftChoice(0));
                Assert.AreEqual(2, GetRewardDraftSeed(first));
                Assert.AreEqual(2, GetRewardDraftSeed(second));
                Assert.AreEqual(IdleAutoDefenseRewardDraftKind.LevelUp.ToString(), first.ActiveRewardDraftKindName);
                CollectionAssert.AreEqual(
                    new[]
                    {
                        "reward.unlock.weapon.idle-auto-defense.arc-burst",
                        "reward.unlock.weapon.idle-auto-defense.homing-pulse",
                        "reward.weapon.idle-auto-defense.shard-launcher.normal.0"
                    },
                    RewardChoiceIds(first));
                CollectionAssert.AreEqual(RewardChoiceIds(first), RewardChoiceIds(second));
            }
            finally
            {
                DestroyController(first);
                DestroyController(second);
            }
        }

        [Test]
        public void PreferredUnlockExcludesCaseInsensitiveDedupeGroupConflict()
        {
            IdleAutoDefenseRewardDraftCatalog catalog = IdleAutoDefenseRewardDraftCatalog.CreateDefault();
            IdleAutoDefenseWeaponUnlockReward preferredUnlock = catalog.WeaponUnlocks[0];
            FieldInfo weaponIdField = typeof(IdleAutoDefenseWeaponUnlockReward).GetField("_weaponId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(weaponIdField);
            weaponIdField.SetValue(preferredUnlock, BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value.ToUpperInvariant());

            var conflictingReward = new IdleAutoDefenseBaseRewardDefinition(
                "unlock.weapon.idle-auto-defense.pulse-beam",
                "Conflicting Pulse Reward",
                IdleAutoDefenseRewardRarity.Legendary,
                "Base Upgrade",
                "Tower",
                "Must remain excluded by the preferred unlock group.",
                IdleAutoDefenseRewardEffectKind.GlobalDamageMultiplier,
                5d,
                1)
            {
                Weight = 1_000_000_000d
            };
            IdleAutoDefenseBaseRewardDefinition[] baseRewards = catalog.BaseRewards.Concat(new[] { conflictingReward }).ToArray();
            FieldInfo baseRewardsField = typeof(IdleAutoDefenseRewardDraftCatalog).GetField("_baseRewards", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(baseRewardsField);
            baseRewardsField.SetValue(catalog, baseRewards);

            IdleAutoDefenseTemplateController controller = CreateController();
            try
            {
                controller.ConfigureRewardDraftCatalog(catalog);
                controller.RequestRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);

                Assert.AreEqual("reward.unlock.weapon.idle-auto-defense.pulse-beam", controller.RewardDraftChoices[0].Id);
                Assert.IsFalse(controller.RewardDraftChoices.Any(choice => choice.Id == conflictingReward.Id));
                Assert.AreEqual(
                    controller.RewardDraftChoiceCount,
                    controller.RewardDraftChoices.Select(GetRewardChoiceDedupeKey).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            }
            finally
            {
                DestroyController(controller);
            }
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
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "idle-auto-defense-content-source-audit.md"), "Shard Launcher");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "idle-auto-defense-content-source-audit.md"), "PulseBeamVfx is restricted");
            AssertFileContains(Path.Combine(packageRoot, "Documentation~", "idle-auto-defense-authored-content-validation-report.md"), "Weapon / Attack Visual Matrix");
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
            AssertFileContains(Path.Combine(packageRoot, "Editor", "IdleAutoDefenseAuthoredContentValidationMenu.cs"), "Validate Authored Content");

            string contentRoot = Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame", "Content");
            AssertDirectoryExists(Path.Combine(contentRoot, "Attacks"));
            AssertDirectoryExists(Path.Combine(contentRoot, "Enemies"));
            AssertDirectoryExists(Path.Combine(contentRoot, "Weapons"));
            AssertDirectoryExists(Path.Combine(contentRoot, "Waves"));
            AssertDirectoryExists(Path.Combine(contentRoot, "Upgrades"));
            AssertDirectoryExists(Path.Combine(contentRoot, "Rewards"));
            AssertDirectoryExists(Path.Combine(contentRoot, "Economy"));
            AssertDirectoryExists(Path.Combine(contentRoot, "RunProfiles"));
            AssertDirectoryExists(Path.Combine(contentRoot, "Progression"));
            AssertDirectoryExists(Path.Combine(contentRoot, "OfflineProgression"));
            AssertDirectoryExists(Path.Combine(contentRoot, "GameRules"));
            AssertDirectoryExists(Path.Combine(contentRoot, "ContentSets"));
            AssertDirectoryExists(Path.Combine(contentRoot, "ContentPacks"));

            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.swarm", "enemy.idle-auto-defense.swarm_EnemyDefinition.asset"), "_id: enemy.idle-auto-defense.swarm");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.runner", "enemy.idle-auto-defense.runner_EnemyDefinition.asset"), "_id: enemy.idle-auto-defense.runner");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.shielded", "enemy.idle-auto-defense.shielded_EnemyDefinition.asset"), "_id: enemy.idle-auto-defense.shielded");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.elite", "enemy.idle-auto-defense.elite_EnemyDefinition.asset"), "_id: enemy.idle-auto-defense.elite");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.boss", "enemy.idle-auto-defense.boss_EnemyDefinition.asset"), "_id: enemy.idle-auto-defense.boss");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.pulse-beam", "attack.idle-auto-defense.pulse-beam_AttackDefinition.asset"), "_id: attack.idle-auto-defense.pulse-beam");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.shard-projectile", "attack.idle-auto-defense.shard-projectile_AttackDefinition.asset"), "_id: attack.idle-auto-defense.shard-projectile");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.arc-burst", "attack.idle-auto-defense.arc-burst_AttackDefinition.asset"), "_id: attack.idle-auto-defense.arc-burst");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.homing-pulse", "attack.idle-auto-defense.homing-pulse_AttackDefinition.asset"), "_id: attack.idle-auto-defense.homing-pulse");
            AssertFileContains(Path.Combine(contentRoot, "Waves", "wave.idle-auto-defense.opening", "wave.idle-auto-defense.opening_WaveDefinition.asset"), "Opening Wave");
            AssertFileContains(Path.Combine(contentRoot, "Waves", "wave.idle-auto-defense.runner-pressure", "wave.idle-auto-defense.runner-pressure_WaveDefinition.asset"), "Runner Pressure");
            AssertFileContains(Path.Combine(contentRoot, "Waves", "wave.idle-auto-defense.elite", "wave.idle-auto-defense.elite_WaveDefinition.asset"), "Elite Pressure");
            AssertFileContains(Path.Combine(contentRoot, "Waves", "wave.idle-auto-defense.final", "wave.idle-auto-defense.final_WaveDefinition.asset"), "Final Surge");
            AssertFileContains(Path.Combine(contentRoot, "Waves", "wave.idle-auto-defense.boss", "wave.idle-auto-defense.boss_WaveDefinition.asset"), "Boss Push");
            AssertFileContains(Path.Combine(contentRoot, "Upgrades", "upgrade.idle-auto-defense.projectile-speed-up", "upgrade.idle-auto-defense.projectile-speed-up_RunUpgradeDefinition.asset"), "Projectile Speed");
            AssertFileContains(Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame", "Prefabs", "Enemies", "README.md"), "Swarm");
            AssertFileContains(Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame", "Prefabs", "Weapons", "README.md"), "Pulse Beam");
            AssertFileContains(Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame", "Prefabs", "Projectiles", "README.md"), "projectile");
            AssertFileContains(Path.Combine(contentRoot, "ContentPacks", "contentpack.idle-auto-defense.playable", "contentpack.idle-auto-defense.playable_ContentPack.asset"), "contentpack.idle-auto-defense.playable");
            string contentSetAsset = Path.Combine(contentRoot, "ContentSets", "contentset.idle-auto-defense.playable", "contentset.idle-auto-defense.playable_GameContentSet.asset");
            string rewardCatalogAsset = Path.Combine(contentRoot, "Rewards", "reward-catalog.idle-auto-defense.playable.asset");
            AssertFileContains(contentSetAsset, "_runtimeSettings:");
            AssertFileContains(contentSetAsset, "_rewardCatalog:");
            AssertFileContains(contentSetAsset, "_economy:");
            AssertFileContains(contentSetAsset, "_runProfile:");
            AssertFileContains(contentSetAsset, "_progression:");
            AssertFileContains(contentSetAsset, "_offlineProgression:");
            AssertFileContains(contentSetAsset, "_gameRules:");
            AssertFileDoesNotContain(contentSetAsset, "_rewardDraftSettings:");
            AssertFileDoesNotContain(contentSetAsset, "_rewardDraftCatalog:");
            AssertFileContains(rewardCatalogAsset, "Crystal Tempest");
            AssertFileContains(rewardCatalogAsset, "Orbital Lance");
            AssertFileContains(rewardCatalogAsset, "Siege Barrage");
            AssertFileContains(rewardCatalogAsset, "Carrier Hive");
            Assert.IsFalse(File.Exists(Path.Combine(contentRoot, "starter-content.json")));
            AssertFileContains(contentSetAsset, "_presentationDebug:");
            AssertFileContains(contentSetAsset, "_showDebugAimLines: 0");
            AssertFileContains(contentSetAsset, "_objectivePresentation:");
            AssertFileContains(contentSetAsset, "_contentId: objective.idle-auto-defense.core");
            AssertFileContains(contentSetAsset, "_moduleSlotPresentationBindings:");
            AssertFileContains(contentSetAsset, "module-slot.shard-launcher");
            AssertFileContains(contentSetAsset, "module-slot.pulse-beam");
            AssertFileContains(contentSetAsset, "_weaponPresentationBindings:");
            AssertFileContains(contentSetAsset, "weapon-ballista");
        }

        [Test]
        public void ProductionEditorMenusUseToolsDeucarianRoot()
        {
            string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BasicIdleAutoDefenseGame).Assembly).resolvedPath;
            string editorRoot = Path.Combine(packageRoot, "Editor");
            string[] editorFiles = Directory.GetFiles(editorRoot, "*.cs", SearchOption.AllDirectories);
            var offenders = new List<string>();
            string attributeDoubleQuote = "[MenuItem(\"" + "Deucarian/";
            string attributeSingleQuote = "[MenuItem('" + "Deucarian/";
            string invocationDoubleQuote = "MenuItem(\"" + "Deucarian/";
            for (int i = 0; i < editorFiles.Length; i++)
            {
                string text = File.ReadAllText(editorFiles[i]);
                if (text.Contains(attributeDoubleQuote) ||
                    text.Contains(attributeSingleQuote) ||
                    text.Contains(invocationDoubleQuote))
                {
                    offenders.Add(editorFiles[i]);
                }
            }

            Assert.That(offenders, Is.Empty, "Root-level Deucarian editor menus are forbidden. Use Tools/Deucarian/...");
            Assert.AreEqual("Tools/Deucarian/Idle Auto Defense/Validate Playable Content", IdleAutoDefensePlayableContentAuditMenu.ValidateMenuPath);
            Assert.AreEqual("Tools/Deucarian/Idle Auto Defense/Open Main Content Set", IdleAutoDefensePlayableContentAuditMenu.OpenContentSetMenuPath);
            Assert.AreEqual("Tools/Deucarian/Idle Auto Defense/Generate Runtime Content Audit", IdleAutoDefensePlayableContentAuditMenu.RuntimeAuditMenuPath);
            Assert.AreEqual("Tools/Deucarian/Idle Auto Defense/Validate Authored Content", IdleAutoDefenseAuthoredContentValidationMenu.MenuPath);
        }

        [Test]
        public void TemplateSourceAuthoredAssetIdsAreUniqueForContentLibrary()
        {
            string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BasicIdleAutoDefenseGame).Assembly).resolvedPath;
            string contentRoot = Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame", "Content");

            AssertSampleAuthoredDefinitionIdsAreUnique(contentRoot);
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.swarm", "enemy.idle-auto-defense.swarm_EnemyDefinition.asset"), "_id: enemy.idle-auto-defense.swarm");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.shielded", "enemy.idle-auto-defense.shielded_EnemyDefinition.asset"), "_id: enemy.idle-auto-defense.shielded");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.elite", "enemy.idle-auto-defense.elite_EnemyDefinition.asset"), "_id: enemy.idle-auto-defense.elite");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.boss", "enemy.idle-auto-defense.boss_EnemyDefinition.asset"), "_id: enemy.idle-auto-defense.boss");
            AssertFileContains(Path.Combine(contentRoot, "Upgrades", "upgrade.idle-auto-defense.projectile-speed-up", "upgrade.idle-auto-defense.projectile-speed-up_RunUpgradeDefinition.asset"), "_id: upgrade.idle-auto-defense.projectile-speed-up");
            AssertFileContains(Path.Combine(contentRoot, "Upgrades", "upgrade.idle-auto-defense.core-reinforcement", "upgrade.idle-auto-defense.core-reinforcement_RunUpgradeDefinition.asset"), "_id: upgrade.idle-auto-defense.core-reinforcement");
            AssertFileContains(Path.Combine(contentRoot, "Upgrades", "upgrade.idle-auto-defense.credit-reward", "upgrade.idle-auto-defense.credit-reward_RunUpgradeDefinition.asset"), "_id: upgrade.idle-auto-defense.credit-reward");
            AssertFileContains(Path.Combine(contentRoot, "README.md"), "six generic enemy definitions");
        }

        [Test]
        public void TemplateSourcePlayableStarterRoundIsCompleteAndPresentable()
        {
            string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BasicIdleAutoDefenseGame).Assembly).resolvedPath;
            string templateSourceRoot = Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame");
            string contentRoot = Path.Combine(templateSourceRoot, "Content");

            Assert.AreEqual(4, CountAuthoredDefinitionFiles(contentRoot, "*_AttackDefinition.asset", "AttackDefinitionAsset"));
            Assert.AreEqual(6, CountAuthoredDefinitionFiles(Path.Combine(contentRoot, "Enemies"), "*_EnemyDefinition.asset", "EnemyDefinitionAsset"));
            Assert.AreEqual(7, CountAuthoredDefinitionFiles(Path.Combine(contentRoot, "Waves"), "*_WaveDefinition.asset", "WaveDefinitionAsset"));
            Assert.AreEqual(4, CountAuthoredDefinitionFiles(contentRoot, "*_WeaponDefinition.asset", "WeaponDefinitionAsset"));
            Assert.AreEqual(6, CountAuthoredDefinitionFiles(contentRoot, "*_RunUpgradeDefinition.asset", "RunUpgradeDefinitionAsset"));

            AssertFileContains(Path.Combine(contentRoot, "ContentPacks", "contentpack.idle-auto-defense.playable", "contentpack.idle-auto-defense.playable_ContentPack.asset"), "contentpack.idle-auto-defense.playable");
            AssertFileContains(Path.Combine(contentRoot, "ContentSets", "contentset.idle-auto-defense.playable", "contentset.idle-auto-defense.playable_GameContentSet.asset"), "seven spawn profiles");
            string runProfileAsset = Path.Combine(contentRoot, "RunProfiles", "run-profile.idle-auto-defense.playable.asset");
            AssertFileContains(Path.Combine(contentRoot, "ContentSets", "contentset.idle-auto-defense.playable", "contentset.idle-auto-defense.playable_GameContentSet.asset"), "77799b15bea84afd8daeab37a1503011");
            AssertFileContains(runProfileAsset, "139ea81c2ca6259408bcf0527b568e74");
            AssertFileContains(runProfileAsset, "9bfe6c935b0d4599b65986b90fca9e3a");
            AssertFileContains(runProfileAsset, "03d0c000098e49699eb86ed1176892d4");
            AssertFileContains(runProfileAsset, "d0b884e8b4a74e4eac54796e5003f919");
            AssertFileContains(runProfileAsset, "7df0e750dfab40b28145ffcbd4951cdb");
            AssertFileContains(Path.Combine(contentRoot, "ContentSets", "contentset.idle-auto-defense.playable", "contentset.idle-auto-defense.playable_GameContentSet.asset"), "02b74debbe2246c4b09d6d043c80536b");
            AssertFileContains(Path.Combine(contentRoot, "ContentSets", "contentset.idle-auto-defense.playable", "contentset.idle-auto-defense.playable_GameContentSet.asset"), "9832cf788c584c0a9c8cd160b57f84a2");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.shard-projectile", "attack.idle-auto-defense.shard-projectile_Delivery.asset"), "_mode: 0");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.shard-projectile", "attack.idle-auto-defense.shard-projectile_Delivery.asset"), "projectile.idle-auto-defense.shard");
            AssertFileDoesNotContain(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.shard-projectile", "attack.idle-auto-defense.shard-projectile_Delivery.asset"), "_projectilePrefab: {fileID: 0");
            AssertFileDoesNotContain(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.homing-pulse", "attack.idle-auto-defense.homing-pulse_Delivery.asset"), "_projectilePrefab: {fileID: 0");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.shard-projectile", "attack.idle-auto-defense.shard-projectile_Delivery.asset"), "_projectileSpeed: 4.2");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.homing-pulse", "attack.idle-auto-defense.homing-pulse_Delivery.asset"), "_projectileSpeed: 4.4");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.pulse-beam", "attack.idle-auto-defense.pulse-beam_Delivery.asset"), "_mode: 1");
            AssertFileDoesNotContain(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.pulse-beam", "attack.idle-auto-defense.pulse-beam_Delivery.asset"), "_beamVfxPrefab: {fileID: 0");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.pulse-beam", "attack.idle-auto-defense.pulse-beam_Delivery.asset"), "cf9e006673d1663419fc7abacc16e5a6");
            AssertFileDoesNotContain(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.pulse-beam", "attack.idle-auto-defense.pulse-beam_Delivery.asset"), "_impactVfxPrefab: {fileID: 0");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.shard-projectile", "attack.idle-auto-defense.shard-projectile_Presentation.asset"), "_audioClip: {fileID: 8300000");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.shard-projectile", "attack.idle-auto-defense.shard-projectile_Presentation.asset"), "_vfxPrefab: {fileID:");
            AssertFileDoesNotContain(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.shard-projectile", "attack.idle-auto-defense.shard-projectile_Presentation.asset"), "cf9e006673d1663419fc7abacc16e5a6");
            AssertFileDoesNotContain(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.arc-burst", "attack.idle-auto-defense.arc-burst_Presentation.asset"), "cf9e006673d1663419fc7abacc16e5a6");
            AssertFileDoesNotContain(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.homing-pulse", "attack.idle-auto-defense.homing-pulse_Presentation.asset"), "cf9e006673d1663419fc7abacc16e5a6");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.pulse-beam", "attack.idle-auto-defense.pulse-beam_Presentation.asset"), "cf9e006673d1663419fc7abacc16e5a6");
            AssertFileContains(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.shard-projectile", "attack.idle-auto-defense.shard-projectile_Presentation.asset"), "a74521512239d7e48ab7287d657f16e8");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.swarm", "enemy.idle-auto-defense.swarm_Presentation.asset"), "_audioClip: {fileID: 8300000");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.swarm", "enemy.idle-auto-defense.swarm_Presentation.asset"), "_vfxPrefab: {fileID:");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.elite", "enemy.idle-auto-defense.elite_Presentation.asset"), "_audioClip: {fileID: 8300000");
            AssertFileContains(Path.Combine(contentRoot, "Enemies", "enemy.idle-auto-defense.boss", "enemy.idle-auto-defense.boss_Presentation.asset"), "_vfxPrefab: {fileID:");
            AssertFileDoesNotContain(Path.Combine(contentRoot, "Attacks", "attack.idle-auto-defense.shard-projectile", "attack.idle-auto-defense.shard-projectile_Delivery.asset"), "projectile.idle-auto-defense.fire-orb");
            AssertFileContains(Path.Combine(contentRoot, "Weapons", "weapon.idle-auto-defense.shard-launcher", "weapon.idle-auto-defense.shard-launcher_Stats.asset"), "_cooldownTicks: 34");
            AssertFileContains(Path.Combine(contentRoot, "Weapons", "weapon.idle-auto-defense.pulse-beam", "weapon.idle-auto-defense.pulse-beam_Stats.asset"), "_cooldownTicks: 72");
            AssertFileContains(Path.Combine(contentRoot, "Weapons", "weapon.idle-auto-defense.arc-burst", "weapon.idle-auto-defense.arc-burst_Stats.asset"), "_cooldownTicks: 108");
            AssertFileContains(Path.Combine(contentRoot, "Weapons", "weapon.idle-auto-defense.homing-pulse", "weapon.idle-auto-defense.homing-pulse_Stats.asset"), "_cooldownTicks: 92");

            string bootstrapPath = Path.Combine(templateSourceRoot, "Scripts", "BasicIdleAutoDefenseGameBootstrap.cs");
            AssertFileContains(bootstrapPath, "IdleAutoDefensePlayerExperienceController");
            AssertFileContains(bootstrapPath, "_templatePlayerExperience");
            AssertFileContains(bootstrapPath, "RequireAuthoredContentOnStartup");
            AssertFileContains(bootstrapPath, "ConfigurePlayerExperience");
            AssertFileDoesNotContain(bootstrapPath, "TryPurchaseDamageUpgrade");
            AssertFileDoesNotContain(bootstrapPath, "TryPurchaseAttackSpeedUpgrade");
            AssertFileDoesNotContain(bootstrapPath, "TryPurchaseRangeUpgrade");
            AssertFileDoesNotContain(bootstrapPath, "TryPurchaseRepairUpgrade");
            AssertFileDoesNotContain(bootstrapPath, "TryPurchaseOverdrive");
            AssertFileDoesNotContain(bootstrapPath, "Test Rewards");
            AssertFileDoesNotContain(bootstrapPath, "Reward Now");
            AssertFileDoesNotContain(bootstrapPath, "OnGUI");
            AssertFileDoesNotContain(bootstrapPath, "GUILayout");

            string runtimePath = Path.Combine(packageRoot, "Runtime", "IdleAutoDefenseTemplate.cs");
            string playerUiPath = Path.Combine(packageRoot, "Runtime", "IdleAutoDefensePlayerExperienceController.Ui.cs");
            AssertFileContains(playerUiPath, "BuildMainMenu");
            AssertFileContains(playerUiPath, "BuildModuleBar");
            AssertFileContains(playerUiPath, "BuildRewardDraft");
            AssertFileContains(playerUiPath, "ApplySafeArea");
            AssertFileContains(playerUiPath, "BuildRunSummary");
            AssertFileContains(runtimePath, "RuntimeUiDocumentReady");
            AssertFileContains(runtimePath, "RuntimeUiThemeAssigned");
            AssertFileContains(runtimePath, "RuntimeUiDirectStylesApplied");
            AssertFileContains(runtimePath, "RuntimeUiRootResolvedWidth");
            AssertFileContains(runtimePath, "RuntimeUiRootResolvedHeight");
            AssertFileContains(runtimePath, "ApplyRuntimeUiRootStyles");
            AssertFileContains(runtimePath, "ApplyRuntimeUiFont");
            AssertFileContains(runtimePath, "Resources.Load<ThemeStyleSheet>(\"IdleAutoDefenseRuntimeTheme\")");
            AssertFileContains(runtimePath, "PanelScaleMode.ScaleWithScreenSize");
            AssertFileContains(runtimePath, "settings.sortingOrder = 32767");
            AssertFileContains(runtimePath, "settings.clearColor = false");
            AssertFileContains(runtimePath, "CreateRuntimeVisualPrefab");
            AssertFileContains(runtimePath, "Kenney3DResourceRoot");
            AssertFileContains(runtimePath, "IdleAutoDefenseWeaponVisualBinding");
            AssertFileContains(runtimePath, "TemplateProjectileMuzzlePoseResolver");
            AssertFileContains(runtimePath, "ResolveTowerMuzzlePosition");
            AssertFileContains(runtimePath, "CreateWeaponPresentation");
            AssertFileContains(runtimePath, "ShowDebugAimLines");
            AssertFileContains(runtimePath, "DebugAimTracerSpawnCount");
            AssertFileContains(runtimePath, "AuthoredWeaponPresentationSpawnCount");
            AssertFileContains(runtimePath, "AuthoredVisibleInstanceStampCount");
            AssertFileContains(runtimePath, "FallbackVisibleGameplaySpawnCount");
            AssertFileContains(runtimePath, "AuthoredObjectivePresentationBindingCount");
            AssertFileContains(runtimePath, "FallbackObjectivePresentationBindingCount");
            AssertFileContains(runtimePath, "AuthoredModuleSlotPresentationBindingCount");
            AssertFileContains(runtimePath, "FallbackModuleSlotPresentationBindingCount");
            AssertFileContains(runtimePath, "FindWeaponDefinitionForPresentation");
            AssertFileContains(runtimePath, "ProjectileImpactCallbackCount");
            AssertFileContains(runtimePath, "ProjectileDamageResolvedFromImpactCount");
            AssertFileContains(runtimePath, "ProjectileImpactRejectedCount");
            AssertFileContains(runtimePath, "_projectiles.ReportImpact(new ProjectileImpactRequest");
            AssertFileContains(runtimePath, "TryEmitBeamVfx");
            AssertFileContains(runtimePath, "BeamVisualSpawnCount");
            AssertFileContains(runtimePath, "Runtime Beam");
            AssertFileContains(runtimePath, "ConfigureBeamLineRenderer");
            AssertFileContains(runtimePath, "LineRenderer");
            AssertFileContains(runtimePath, "UpdateActiveBeamVisuals");
            AssertFileDoesNotContain(runtimePath, "CreateTransientPrimitiveVfxPrefab");
            AssertFileDoesNotContain(runtimePath, "_projectiles?.Cleanup(pending.ProjectileId, ProjectileExpiryReason.HitLimitReached)");
            AssertFileContains(runtimePath, "CreateEnemyModelPrefab");
            AssertFileContains(runtimePath, "CreateProjectileModelPrefab");
            AssertFileContains(runtimePath, "AttachKenneySprite");
            string runtimeSettingsPath = Path.Combine(packageRoot, "Runtime", "IdleAutoDefenseContentSetRuntimeSettings.cs");
            AssertFileContains(runtimeSettingsPath, "Pulse Beam Locked Pad");
            AssertFileContains(runtimeSettingsPath, "Arc Burst Locked Pad");
            AssertFileContains(runtimeSettingsPath, "Homing Pulse Locked Pad");
            AssertFileContains(runtimePath, "TintSpriteRenderers");
            AssertFileContains(runtimePath, "HideMeshRenderers");
            AssertFileContains(runtimePath, "AddProjectileTrail");
            AssertFileContains(runtimePath, "EmitKenneySpriteBurst");
            AssertFileContains(runtimePath, "TriggerCameraShake");
            AssertFileContains(runtimePath, "Art/impact_flame");
            AssertFileContains(runtimePath, "Art/currency_coin_gold");
            AssertFileDoesNotContain(runtimePath, "ScriptableObject.CreateInstance<ThemeStyleSheet>()");
            AssertFileDoesNotContain(runtimePath, "private sealed class IdleAutoDefenseWeaponVisualBinding");
            AssertFileDoesNotContain(runtimePath, "private sealed class IdleAutoDefenseEnemyModelPresentation");
            AssertFileDoesNotContain(runtimePath, "private sealed class TemplateProjectileMuzzlePoseResolver");
            string presentationRoot = Path.Combine(packageRoot, "Runtime", "Presentation");
            AssertFileContains(Path.Combine(presentationRoot, "IdleAutoDefenseWeaponVisualBinding.cs"), "internal sealed class IdleAutoDefenseWeaponVisualBinding");
            AssertFileContains(Path.Combine(presentationRoot, "IdleAutoDefenseWeaponVisualBinding.cs"), "EmitMuzzleFlash");
            AssertFileContains(Path.Combine(presentationRoot, "IdleAutoDefenseEnemyModelPresentation.cs"), "internal sealed class IdleAutoDefenseEnemyModelPresentation");
            AssertFileContains(Path.Combine(presentationRoot, "TemplateProjectileMuzzlePoseResolver.cs"), "internal sealed class TemplateProjectileMuzzlePoseResolver");
            AssertFileContains(Path.Combine(presentationRoot, "IdleAutoDefenseKenneyPresentationEffects.cs"), "internal sealed class KenneySpriteBurstVisual");
            AssertFileExistsAtFullPath(Path.Combine(packageRoot, "Runtime", "IdleAutoDefenseKenneyModelPrefab.cs"));
            AssertFileContains(Path.Combine(packageRoot, "Runtime", "IdleAutoDefenseKenneyModelPrefab.cs"), "Resources.Load<GameObject>(DefaultResourceRoot + modelName)");
            AssertFileContains(Path.Combine(packageRoot, "Runtime", "AuthoredContentInstance.cs"), "public sealed class AuthoredContentInstance");
            AssertFileContains(Path.Combine(packageRoot, "Runtime", "AuthoredContentInstance.cs"), "public string SourceAssetGuid");
            AssertFileContains(Path.Combine(packageRoot, "Runtime", "AuthoredContentInstance.cs"), "public string OriginSocketId");
            AssertFileContains(Path.Combine(packageRoot, "Runtime", "AuthoredContentInstance.cs"), "public bool FallbackUsed");
            string playableAuditMenuPath = Path.Combine(packageRoot, "Editor", "IdleAutoDefensePlayableContentAuditMenu.cs");
            AssertFileContains(playableAuditMenuPath, "Validate Playable Content");
            AssertFileContains(playableAuditMenuPath, "Generate Runtime Content Audit");
            AssertFileContains(playableAuditMenuPath, "Generate Authoring Runtime Parity Report");
            AssertFileContains(playableAuditMenuPath, "GenerateFreshSampleReportsForBatch");
            AssertFileContains(playableAuditMenuPath, "FindVisibleObjectsWithoutAuthoredStamp");
            AssertFileContains(runtimePath, "EnvironmentPresentation");
            AssertFileContains(runtimePath, "environment.idle-auto-defense.arena");
            AssertFileExistsAtFullPath(Path.Combine(packageRoot, "Runtime", "Resources", "IdleAutoDefenseRuntimeTheme.tss"));
            AssertFileContains(Path.Combine(packageRoot, "Runtime", "Resources", "IdleAutoDefenseRuntimeTheme.tss"), "unity-theme://default");

            AssertDirectoryExists(Path.Combine(templateSourceRoot, "Audio"));
            AssertDirectoryExists(Path.Combine(templateSourceRoot, "Visuals", "Prefabs"));
            AssertDirectoryExists(Path.Combine(templateSourceRoot, "Visuals", "Textures"));
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Textures", "idle-auto-defense-playable-icon.png.meta"), "TextureImporter");
            AssertFileExistsAtFullPath(Path.Combine(templateSourceRoot, "Visuals", "Textures", "idle-auto-defense-playable-banner.png"));
            AssertFileExistsAtFullPath(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "KenneyShardProjectile.prefab"));
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "KenneyShardLauncherWeapon.prefab"), "weapon-ballista");
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "KenneyPulseBeamWeapon.prefab"), "weapon-turret");
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "KenneyArcBurstWeapon.prefab"), "weapon-catapult");
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "KenneyHomingSpireWeapon.prefab"), "weapon-cannon");
            AssertFileExistsAtFullPath(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "PulseBeamVfx.prefab"));
            AssertFileExistsAtFullPath(Path.Combine(templateSourceRoot, "Visuals", "Materials", "PulseBeamEnergy.mat"));
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "PulseBeamVfx.prefab"), "m_Name: PulseBeamVfx");
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "PulseBeamVfx.prefab"), "m_LocalScale: {x: 0.11, y: 0.11, z: 1}");
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Materials", "PulseBeamEnergy.mat"), "m_Name: PulseBeamEnergy");
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "KenneySwarmEnemy.prefab"), "enemy-ufo-a");
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "KenneyRunnerEnemy.prefab"), "enemy-ufo-b");
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "KenneyTankEnemy.prefab"), "enemy-ufo-d");
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "KenneyShardProjectile.prefab"), "weapon-ammo-arrow");
            AssertFileContains(Path.Combine(templateSourceRoot, "Visuals", "Prefabs", "KenneyHomingProjectile.prefab"), "weapon-ammo-bullet");
            AssertFileExistsAtFullPath(Path.Combine(templateSourceRoot, "Audio", "idle-auto-defense-fire.wav"));
            AssertFileExistsAtFullPath(Path.Combine(templateSourceRoot, "Audio", "idle-auto-defense-impact.wav"));
            string kenney3dRoot = Path.Combine(templateSourceRoot, "Resources", "Kenney", "IdleAutoDefense", "Models", "TowerDefenseKit");
            AssertFileExistsAtFullPath(Path.Combine(kenney3dRoot, "License.txt"));
            AssertFileExistsAtFullPath(Path.Combine(kenney3dRoot, "FBX", "tower-round-base.fbx"));
            AssertFileExistsAtFullPath(Path.Combine(kenney3dRoot, "FBX", "weapon-ballista.fbx"));
            AssertFileExistsAtFullPath(Path.Combine(kenney3dRoot, "FBX", "weapon-cannon.fbx"));
            AssertFileExistsAtFullPath(Path.Combine(kenney3dRoot, "FBX", "enemy-ufo-a.fbx"));
            AssertFileExistsAtFullPath(Path.Combine(kenney3dRoot, "FBX", "enemy-ufo-d.fbx"));
            AssertFileExistsAtFullPath(Path.Combine(kenney3dRoot, "FBX", "weapon-ammo-arrow.fbx"));
            AssertFileExistsAtFullPath(Path.Combine(kenney3dRoot, "FBX", "tile.fbx"));
            AssertFileExistsAtFullPath(Path.Combine(kenney3dRoot, "Textures", "colormap.png"));
            string runtimeKenney3dRoot = Path.Combine(packageRoot, "Runtime", "Resources", "Kenney", "IdleAutoDefense", "Models", "TowerDefenseKit");
            AssertFileExistsAtFullPath(Path.Combine(runtimeKenney3dRoot, "License.txt"));
            AssertFileExistsAtFullPath(Path.Combine(runtimeKenney3dRoot, "FBX", "tower-round-base.fbx"));
            AssertFileExistsAtFullPath(Path.Combine(runtimeKenney3dRoot, "FBX", "weapon-ballista.fbx"));
            AssertFileExistsAtFullPath(Path.Combine(runtimeKenney3dRoot, "FBX", "enemy-ufo-a.fbx"));
            AssertFileExistsAtFullPath(Path.Combine(runtimeKenney3dRoot, "FBX", "weapon-ammo-arrow.fbx"));
            AssertFileExistsAtFullPath(Path.Combine(runtimeKenney3dRoot, "Textures", "colormap.png"));
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
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.damage-up");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.fire-rate-up");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.projectile-count-up");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.projectile-speed-up");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.objective-max-health-up");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.objective-repair");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.shield-restore-intent");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.enemy-reward-up");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.offline-gain-up");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.reroll-bonus");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.crit-chance-intent");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.crit-damage-intent");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.direct-specialization");
            AssertUpgradeExists(catalog, "upgrade.idle-auto-defense.projectile-specialization");

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
                controller.RestartRun(BasicIdleAutoDefenseGame.CreateEncounterDefinition());
                StepUntilTerminalWithLivePurchases(controller, 6400);
                Assert.IsTrue(controller.EncounterCompleted, controller.StatusSummary);
                Assert.That(controller.ProjectileVisualSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.That(controller.ProjectileMotionObservedCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.That(controller.ProjectileImpactCallbackCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.That(controller.ProjectileDamageResolvedFromImpactCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.AreEqual(controller.ProjectileDamageResolvedFromImpactCount, controller.ProjectileDamageAppliedCount, controller.StatusSummary);
                Assert.That(controller.DamageNumberSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.That(controller.AttackVfxSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.AreEqual(0, controller.ProjectileImpactRejectedCount, controller.StatusSummary);
                Assert.That(controller.AttackAudioPlayCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.That(controller.EnemyPresentationEventCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.That(controller.SelectedUpgradeCount, Is.GreaterThanOrEqualTo(3), controller.StatusSummary);
                Assert.That(controller.ModuleActivationCount, Is.GreaterThan(0));
                Assert.That(controller.OverdriveActivationCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.That(controller.UpgradeFeedbackSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
                Assert.AreEqual(0, controller.DraftTickCount);
                Assert.That(controller.EncounterRewardCredits, Is.GreaterThanOrEqualTo(60));
                Assert.That(controller.EncounterRewardParts, Is.GreaterThanOrEqualTo(3));

                controller.RestartRun(CreateFailCapablePressureEncounterDefinition());
                controller.RewardDraftPausesCombat = false;
                StepUntilTerminal(controller, 1400, chooseRewardDrafts: false);
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
        public void IdleNamedPackProviderRegistersOnceAndReportsMissingGeneratedContent()
        {
            string root = "Assets/GameContent/IdlePackMissing_" + Guid.NewGuid().ToString("N");
            if (!AssetDatabase.IsValidFolder("Assets/GameContent")) AssetDatabase.CreateFolder("Assets", "GameContent");
            AssetDatabase.CreateFolder("Assets/GameContent", Path.GetFileName(root));
            try
            {
                IdleAutoDefenseContentPackIndex index = IdleAutoDefenseContentPackIndex.Discover(root);
                var provider = new GameContentPackAuthoringProvider();
                GameContentPackDescriptor descriptor = index.BuildDescriptor(provider.ProviderId);

                Assert.That(GameContentAuthoringProviderRegistry.Providers.Count(value =>
                    value is IGameContentPackProvider &&
                    string.Equals(value.ProviderId, GameContentPackAuthoringProvider.ContentPackProviderId, StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1));
                Assert.That(descriptor.PackId, Is.EqualTo(IdleAutoDefenseContentPackIndex.PackId));
                Assert.That(descriptor.SourceState, Is.EqualTo(GameContentPackSourceState.MissingSource));
                Assert.That(descriptor.RecordCount, Is.Zero);
                Assert.That(descriptor.Actions.Any(action => action.ActionId == IdleAutoDefenseContentPackIndex.OpenSetupActionId && action.Enabled), Is.True);
                Assert.That(descriptor.Validation.ErrorCount, Is.GreaterThan(0));
            }
            finally
            {
                AssetDatabase.DeleteAsset(root);
            }
        }

        [Test]
        public void IdleNamedPackDiscoveryReportsAmbiguousGeneratedRoots()
        {
            string root = "Assets/GameContent/IdlePackAmbiguous_" + Guid.NewGuid().ToString("N");
            if (!AssetDatabase.IsValidFolder("Assets/GameContent")) AssetDatabase.CreateFolder("Assets", "GameContent");
            AssetDatabase.CreateFolder("Assets/GameContent", Path.GetFileName(root));
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    var pack = ScriptableObject.CreateInstance<GameContentPackAsset>();
                    pack.Configure(
                        IdleAutoDefenseContentPackIndex.PackId,
                        "Candidate " + i,
                        string.Empty,
                        "1",
                        "Tests",
                        null,
                        null,
                        Array.Empty<GameContentSetAsset>(),
                        null,
                        Array.Empty<string>(),
                        Array.Empty<string>(),
                        string.Empty,
                        Array.Empty<string>());
                    AssetDatabase.CreateAsset(pack, root + "/Candidate" + i + ".asset");
                }
                AssetDatabase.SaveAssets();

                IdleAutoDefenseContentPackIndex index = IdleAutoDefenseContentPackIndex.Discover(root);

                Assert.That(index.SourceState, Is.EqualTo(GameContentPackSourceState.DuplicateConflict));
                Assert.That(index.Records, Is.Empty);
                Assert.That(index.SourceClaims, Is.Empty);
                Assert.That(index.Validation.Issues.Any(issue => issue.Message.Contains("Multiple generated")), Is.True);
            }
            finally
            {
                AssetDatabase.DeleteAsset(root);
            }
        }

        [Test]
        public void BasicTemplateSourceBaselineAndScrapSourceReferencesStayIsolated()
        {
            string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BasicIdleAutoDefenseGame).Assembly).resolvedPath;
            string basicRoot = Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame");
            string scrapRoot = Path.Combine(packageRoot, "TemplateSource~", "ScrapFrontierGame");
            AssertTemplateSourceWaveEntryIds(basicRoot);
            AssertTemplateSourceWaveEntryIds(scrapRoot);
            Assert.That(Directory.GetFiles(basicRoot, "*", SearchOption.AllDirectories).Length, Is.EqualTo(494));
            Assert.That(Directory.GetFiles(scrapRoot, "*", SearchOption.AllDirectories).Length, Is.EqualTo(369));
            AssertFileSha256(
                Path.Combine(basicRoot, "Content", "ContentPacks", "contentpack.idle-auto-defense.playable", "contentpack.idle-auto-defense.playable_ContentPack.asset"),
                "40507e230897fb5a30b419ec4e0016d0fecf7560975e9e5d6eb1376023fac4c1");
            AssertFileSha256(
                Path.Combine(basicRoot, "Content", "ContentSets", "contentset.idle-auto-defense.playable", "contentset.idle-auto-defense.playable_GameContentSet.asset"),
                "3bfde7c23d88eb58ac6f760ca59475da40fa5bd34fab488add47436182e821ad");
            AssertFileSha256(
                Path.Combine(basicRoot, "Content", "Rewards", "reward-catalog.idle-auto-defense.playable.asset"),
                "b9e279a11492d4b351f8efc84c2860be44c3c7f373bdfdcaf05688d2e07c345f");
            AssertFileSha256(
                Path.Combine(basicRoot, "Content", "Economy", "economy.idle-auto-defense.playable.asset"),
                "6da54d4a6098f5cd52703d8f85e95b20d1a0a6587bb48b0ad0d277a5b7f80ab5");
            AssertFileSha256(
                Path.Combine(basicRoot, "Content", "RunProfiles", "run-profile.idle-auto-defense.playable.asset"),
                "a247605112ffa373d85d529025b1f786427ca4293e3b7277e4c691487dd3aa6f");
            AssertFileSha256(
                Path.Combine(basicRoot, "Content", "Progression", "progression.idle-auto-defense.playable.asset"),
                "549267e56cc3b5c25fe4928d0541c942285b0446e41c08b9c596334ad862f06f");
            AssertFileSha256(
                Path.Combine(basicRoot, "Content", "OfflineProgression", "offline-progression.idle-auto-defense.playable.asset"),
                "c100bb2e11ce440bb222c068e8e4e5e56b2a443c7fc68bb46d5a36a1b770870f");
            AssertFileSha256(
                Path.Combine(basicRoot, "Content", "GameRules", "game-rules.idle-auto-defense.playable.asset"),
                "993ff863fc0e9448acb221e1358e5cbe946890d4a1add82a10e32c9c72b6f8a9");
            AssertFileSha256(
                Path.Combine(basicRoot, "Content", "Presentation", "player-experience.idle-auto-defense.playable.asset"),
                "2df262f9167a769adaa5ea0a41a583688a4b4314e36a2f5bbc4ca983e6196638");
            AssertFileSha256(
                Path.Combine(basicRoot, "Scenes", "OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity"),
                "ac1ec2b38705e9d1710eedd51af30228db62f6e3b8b9726f350b55aa201ccfc0");

            Dictionary<string, string> basicGuids = Directory.GetFiles(basicRoot, "*.meta", SearchOption.AllDirectories)
                .Select(path => new { Path = path, Guid = ReadMetaGuid(path) })
                .Where(value => !string.IsNullOrWhiteSpace(value.Guid))
                .ToDictionary(value => value.Guid, value => value.Path, StringComparer.OrdinalIgnoreCase);
            string sharedBootstrapGuid = ReadMetaGuid(Path.Combine(basicRoot, "Scripts", "BasicIdleAutoDefenseGameBootstrap.cs.meta"));
            var leaked = new List<string>();
            foreach (string scrapFile in Directory.GetFiles(scrapRoot, "*", SearchOption.AllDirectories)
                         .Where(CanContainGuidReference))
            {
                string text = File.ReadAllText(scrapFile);
                foreach (KeyValuePair<string, string> pair in basicGuids)
                {
                    if (!text.Contains(pair.Key) || string.Equals(pair.Key, sharedBootstrapGuid, StringComparison.OrdinalIgnoreCase)) continue;
                    leaked.Add(scrapFile + " -> " + pair.Value);
                }
            }

            Assert.That(leaked, Is.Empty, string.Join("\n", leaked));
            Dictionary<string, string> scrapGuids = Directory.GetFiles(scrapRoot, "*.meta", SearchOption.AllDirectories)
                .Select(path => new { Path = path, Guid = ReadMetaGuid(path) })
                .Where(value => !string.IsNullOrWhiteSpace(value.Guid))
                .ToDictionary(value => value.Guid, value => value.Path, StringComparer.OrdinalIgnoreCase);
            var reverseLeaks = new List<string>();
            foreach (string basicFile in Directory.GetFiles(basicRoot, "*", SearchOption.AllDirectories)
                         .Where(CanContainGuidReference))
            {
                string text = File.ReadAllText(basicFile);
                foreach (KeyValuePair<string, string> pair in scrapGuids)
                    if (text.Contains(pair.Key)) reverseLeaks.Add(basicFile + " -> " + pair.Value);
            }
            Assert.That(reverseLeaks, Is.Empty, string.Join("\n", reverseLeaks));
            string[] duplicateGuids = Directory.GetFiles(Path.Combine(packageRoot, "TemplateSource~"), "*.meta", SearchOption.AllDirectories)
                .Select(ReadMetaGuid)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            Assert.That(duplicateGuids, Is.Empty, "Duplicate source GUIDs: " + string.Join(", ", duplicateGuids));
        }

        [Test]
        public void SetupWizardGeneratesScrapFrontierOnlyWithDirectStrictPackWiring()
        {
            string targetRoot = "Assets/T/S" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string contentRoot = "Assets/GameContent/S" + Guid.NewGuid().ToString("N").Substring(0, 8);
            const string sceneRoot = "Assets/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame";
            string backup = BackupAssetDirectory(sceneRoot);
            var request = new IdleAutoDefenseTemplateSetupRequest
            {
                TargetRootAssetPath = targetRoot,
                ContentRootAssetPath = contentRoot,
                GameNamespace = "ScrapOnlySmoke.IdleAutoDefense",
                GamePrefix = "Scrap Only Smoke",
                PackSelection = IdleAutoDefenseTemplatePackSelection.ScrapFrontierOnly,
                OpenCreatedScene = false,
                RefreshAssetDatabase = false
            };

            try
            {
                IdleAutoDefenseTemplateSetupResult setup = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
                Assert.That(setup.Succeeded, Is.True, setup.CreateSummary());
                string scenePath = sceneRoot + "/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame.unity";
                Assert.That(setup.CreatedSceneAssetPaths, Is.EqualTo(new[] { scenePath }));
                AssertFileExists(targetRoot + "/README.md");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/README.md"), "# Scrap Frontier");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/README.md"), "product-owned output");
                Assert.That(Directory.Exists(AssetPathToFullPath(targetRoot + "/ScrapFrontier")), Is.False);
                Assert.That(Directory.Exists(AssetPathToFullPath(contentRoot + "/ScrapFrontier")), Is.False);

                string packPath = contentRoot + "/ContentPacks/contentpack.idle-auto-defense.scrap-frontier/contentpack.idle-auto-defense.scrap-frontier_ContentPack.asset";
                string setPath = contentRoot + "/ContentSets/contentset.idle-auto-defense.scrap-frontier.playable/contentset.idle-auto-defense.scrap-frontier.playable_GameContentSet.asset";
                string experiencePath = contentRoot + "/Presentation/player-experience.idle-auto-defense.scrap-frontier.playable.asset";
                AssertFileExists(packPath);
                AssertFileExists(setPath);
                AssertFileExists(experiencePath);
                Assert.That(File.Exists(AssetPathToFullPath(
                    contentRoot + "/ContentPacks/contentpack.idle-auto-defense.playable/contentpack.idle-auto-defense.playable_ContentPack.asset")), Is.False);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                GameContentPackAsset pack = AssetDatabase.LoadAssetAtPath<GameContentPackAsset>(packPath);
                GameContentSetAsset contentSet = AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(setPath);
                IdleAutoDefensePlayerExperienceAsset experience = AssetDatabase.LoadAssetAtPath<IdleAutoDefensePlayerExperienceAsset>(experiencePath);
                Assert.That(pack, Is.Not.Null);
                Assert.That(contentSet, Is.Not.Null);
                Assert.That(experience, Is.Not.Null);
                Assert.That(pack.Id, Is.EqualTo(IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId));
                Assert.That(pack.DefaultContentSet, Is.SameAs(contentSet));
                Assert.That(GameContentSetValidator.Validate(contentSet).IsValid, Is.True, FormatIssues(GameContentSetValidator.Validate(contentSet)));
                Assert.That(experience.Validate(), Is.Empty);
                Assert.That(AssetDatabase.GetDependencies(scenePath, true), Does.Contain(packPath));
                Assert.That(AssetDatabase.GetDependencies(scenePath, true), Does.Contain(setPath));
                Assert.That(AssetDatabase.GetDependencies(scenePath, true), Does.Contain(experiencePath));

                var provider = new GameContentPackAuthoringProvider(contentRoot);
                GameContentPackDescriptor[] descriptors = provider.GetContentPacks().ToArray();
                GameContentPackDescriptor scrap = descriptors.Single(value =>
                    string.Equals(value.PackId, IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId, StringComparison.OrdinalIgnoreCase));
                GameContentPackDescriptor basic = descriptors.Single(value =>
                    string.Equals(value.PackId, IdleAutoDefenseNamedPackDefinition.Basic.PackId, StringComparison.OrdinalIgnoreCase));
                Assert.That(scrap.SourceState, Is.EqualTo(GameContentPackSourceState.Available), FormatValidation(scrap.Validation));
                Assert.That(basic.SourceState, Is.EqualTo(GameContentPackSourceState.MissingSource));
                AssertNamedPackAuthoringSurface(provider, scrap, contentRoot, scenePath);
                AssertGeneratedMetaGuidsAreUnique(targetRoot, contentRoot, sceneRoot);
                AssertGeneratedPackMenuFirstStrictBoot(pack, contentSet, experience);
                AssertResponsiveLayoutPolicy(experience.UiSettings);
            }
            finally
            {
                AssetDatabase.DeleteAsset(targetRoot);
                AssetDatabase.DeleteAsset(contentRoot);
                RestoreAssetDirectory(sceneRoot, backup);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        [Test]
        public void SetupWizardGeneratesBothIndependentPacksWithParityIsolationStrictBindingAndRepair()
        {
            string targetRoot = "Assets/T/A" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string contentRoot = "Assets/GameContent/A" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string[] sceneRoots =
            {
                "Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame",
                "Assets/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame"
            };
            string[] backups = sceneRoots.Select(BackupAssetDirectory).ToArray();
            var request = new IdleAutoDefenseTemplateSetupRequest
            {
                TargetRootAssetPath = targetRoot,
                ContentRootAssetPath = contentRoot,
                GameNamespace = "AssetFlipSmoke.IdleAutoDefense",
                GamePrefix = "Asset Flip Smoke",
                PackSelection = IdleAutoDefenseTemplatePackSelection.Both,
                AllowOverwrite = false,
                OpenCreatedScene = false,
                RefreshAssetDatabase = false
            };

            try
            {
                IdleAutoDefenseTemplateSetupResult setup = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
                Assert.That(setup.Succeeded, Is.True, setup.CreateSummary());
                Assert.That(setup.CreatedSceneAssetPaths.Count, Is.EqualTo(2));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                string basicContentRoot = contentRoot + "/Basic";
                string scrapContentRoot = contentRoot + "/ScrapFrontier";
                string basicPackPath = basicContentRoot + "/ContentPacks/contentpack.idle-auto-defense.playable/contentpack.idle-auto-defense.playable_ContentPack.asset";
                string scrapPackPath = scrapContentRoot + "/ContentPacks/contentpack.idle-auto-defense.scrap-frontier/contentpack.idle-auto-defense.scrap-frontier_ContentPack.asset";
                string basicSetPath = basicContentRoot + "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset";
                string scrapSetPath = scrapContentRoot + "/ContentSets/contentset.idle-auto-defense.scrap-frontier.playable/contentset.idle-auto-defense.scrap-frontier.playable_GameContentSet.asset";
                string basicExperiencePath = basicContentRoot + "/Presentation/player-experience.idle-auto-defense.playable.asset";
                string scrapExperiencePath = scrapContentRoot + "/Presentation/player-experience.idle-auto-defense.scrap-frontier.playable.asset";
                GameContentPackAsset basicPack = AssetDatabase.LoadAssetAtPath<GameContentPackAsset>(basicPackPath);
                GameContentPackAsset scrapPack = AssetDatabase.LoadAssetAtPath<GameContentPackAsset>(scrapPackPath);
                GameContentSetAsset basicSet = AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(basicSetPath);
                GameContentSetAsset scrapSet = AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(scrapSetPath);
                Assert.That(basicPack, Is.Not.Null);
                Assert.That(scrapPack, Is.Not.Null);
                Assert.That(scrapPack, Is.Not.SameAs(basicPack));
                Assert.That(basicPack.Id, Is.EqualTo(IdleAutoDefenseNamedPackDefinition.Basic.PackId));
                Assert.That(scrapPack.Id, Is.EqualTo(IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId));
                Assert.That(GameContentSetValidator.Validate(basicSet).IsValid, Is.True, FormatIssues(GameContentSetValidator.Validate(basicSet)));
                Assert.That(GameContentSetValidator.Validate(scrapSet).IsValid, Is.True, FormatIssues(GameContentSetValidator.Validate(scrapSet)));

                AssertSequentialWaveEntryIds(basicSet.WaveSet);
                AssertSequentialWaveEntryIds(scrapSet.WaveSet);
                AssertWaveGameplayParity(BasicIdleAutoDefenseGame.CreateWaveDefinitions(), basicSet.WaveSet);

                AssertPackNumericParity(basicSet, scrapSet);
                Assert.That(scrapSet.AvailableWeapons.All(value => value.Id.Contains("scrap-frontier")), Is.True);
                Assert.That(scrapSet.EnemyPool.All(value => value.Id.Contains("scrap-frontier")), Is.True);
                Assert.That(scrapSet.WaveSet.All(value => value.Id.Contains("scrap-frontier")), Is.True);
                Assert.That(scrapSet.UpgradePool.All(value => value.Id.Contains("scrap-frontier")), Is.True);

                IdleAutoDefensePlayerExperienceAsset basicExperience = AssetDatabase.LoadAssetAtPath<IdleAutoDefensePlayerExperienceAsset>(
                    basicExperiencePath);
                IdleAutoDefensePlayerExperienceAsset scrapExperience = AssetDatabase.LoadAssetAtPath<IdleAutoDefensePlayerExperienceAsset>(
                    scrapExperiencePath);
                Assert.That(basicExperience.Validate(), Is.Empty);
                Assert.That(scrapExperience.Validate(), Is.Empty);
                Assert.That(scrapExperience.UiSettings.GameTitle, Is.EqualTo("Scrap Frontier"));
                Assert.That(scrapExperience.UiSettings.OverdriveName, Is.EqualTo("Redline"));
                Assert.That(scrapExperience.Themes.Count, Is.EqualTo(2));
                Assert.That(scrapExperience.AudioPalette.Events.Count, Is.EqualTo(26));
                Assert.That(scrapExperience.Tutorial.Steps.Count, Is.EqualTo(10));
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Basic/README.md"), "# Basic Idle Auto Defense");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/ScrapFrontier/README.md"), "# Scrap Frontier");

                string authoredValidation = IdleAutoDefenseAuthoredContentValidationMenu.BuildReport();
                Assert.That(authoredValidation, Does.StartWith("Idle Auto Defense authored content validation: PASS"), authoredValidation);
                Assert.That(authoredValidation, Does.Contain("PASS contentset.idle-auto-defense.playable"), authoredValidation);
                Assert.That(authoredValidation, Does.Contain("PASS contentset.idle-auto-defense.scrap-frontier.playable"), authoredValidation);
                Assert.That(authoredValidation, Does.Contain("PASS player-experience.idle-auto-defense.playable"), authoredValidation);
                Assert.That(authoredValidation, Does.Contain("PASS player-experience.idle-auto-defense.scrap-frontier.playable"), authoredValidation);

                var provider = new GameContentPackAuthoringProvider(contentRoot);
                GameContentPackDescriptor[] descriptors = provider.GetContentPacks().ToArray();
                Assert.That(descriptors.Length, Is.EqualTo(2));
                Assert.That(descriptors.All(value => value.SourceState == GameContentPackSourceState.Available), Is.True,
                    string.Join("\n", descriptors.Select(value => FormatValidation(value.Validation))));
                foreach (GameContentPackDescriptor descriptor in descriptors)
                {
                    IReadOnlyList<GameContentRecordDescriptor> records = provider.GetRecords(descriptor.PackId);
                    Assert.That(records.Count, Is.EqualTo(124), descriptor.DisplayName);
                    Assert.That(records.All(value => value.CanonicalKey.PackId == descriptor.PackId), Is.True);
                    Assert.That(provider.ValidatePack(descriptor.PackId).IsValid, Is.True, FormatValidation(provider.ValidatePack(descriptor.PackId)));
                }

                string basicScene = sceneRoots[0] + "/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity";
                string scrapScene = sceneRoots[1] + "/OPEN_THIS_TO_TEST_ScrapFrontier_PlayableGame.unity";
                GameContentPackDescriptor basicDescriptor = descriptors.Single(value =>
                    string.Equals(value.PackId, IdleAutoDefenseNamedPackDefinition.Basic.PackId, StringComparison.OrdinalIgnoreCase));
                GameContentPackDescriptor scrapDescriptor = descriptors.Single(value =>
                    string.Equals(value.PackId, IdleAutoDefenseNamedPackDefinition.ScrapFrontier.PackId, StringComparison.OrdinalIgnoreCase));
                AssertNamedPackAuthoringSurface(provider, basicDescriptor, basicContentRoot, basicScene);
                AssertNamedPackAuthoringSurface(provider, scrapDescriptor, scrapContentRoot, scrapScene);
                GameContentRecordDescriptor basicHover = provider.GetRecords(basicDescriptor.PackId)
                    .Single(value => value.IsInCategory("audio-events") && value.PlayerFacingMetadata.Any(metadata =>
                        metadata.Label == "Event ID" && metadata.Value == "ui.hover"));
                GameContentRecordDescriptor scrapHover = provider.GetRecords(scrapDescriptor.PackId)
                    .Single(value => value.IsInCategory("audio-events") && value.PlayerFacingMetadata.Any(metadata =>
                        metadata.Label == "Event ID" && metadata.Value == "ui.hover"));
                Assert.That(scrapHover.CanonicalKey, Is.Not.EqualTo(basicHover.CanonicalKey));

                var projectProvider = new GameContentLibraryProvider();
                GameContentPackCatalog catalog = GameContentPackCatalog.Build(new IGameContentAuthoringProvider[] { projectProvider, provider });
                Assert.That(catalog.SourceClaimConflicts, Is.Empty);
                GameContentPackCatalogEntry projectEntry = catalog.Find(GameContentPackDescriptor.BuildStableKey(
                    "com.deucarian.game-content-authoring.project", "project-content"));
                Assert.That(projectEntry.Records.Any(value => IsPathUnderAssetRoot(value.SourcePath, contentRoot)), Is.False);

                Assert.That(AssetDatabase.GetDependencies(basicScene, true), Does.Contain(basicPackPath));
                Assert.That(AssetDatabase.GetDependencies(basicScene, true), Does.Not.Contain(scrapPackPath));
                Assert.That(AssetDatabase.GetDependencies(scrapScene, true), Does.Contain(scrapPackPath));
                Assert.That(AssetDatabase.GetDependencies(scrapScene, true), Does.Not.Contain(basicPackPath));
                AssertGeneratedMetaGuidsAreUnique(targetRoot, contentRoot, sceneRoots[0], sceneRoots[1]);
                AssertGeneratedPackMenuFirstStrictBoot(basicPack, basicSet, basicExperience);
                AssertGeneratedPackMenuFirstStrictBoot(scrapPack, scrapSet, scrapExperience);
                AssertResponsiveLayoutPolicy(basicExperience.UiSettings);
                AssertResponsiveLayoutPolicy(scrapExperience.UiSettings);

                AssertMutationIsolation(basicSet, scrapSet, basicExperience, scrapExperience);
                AssertStrictMissingOwnerFailures(scrapSet, scrapExperience);

                string missingRewardPath = scrapContentRoot + "/Rewards/reward-catalog.idle-auto-defense.scrap-frontier.playable.asset";
                string missingRewardMetaPath = missingRewardPath + ".meta";
                string rewardGuid = ReadMetaGuid(AssetPathToFullPath(missingRewardMetaPath));
                string reportPath = AssetPathToFullPath(targetRoot + "/Docs/setup-report.md");
                byte[] originalReport = File.ReadAllBytes(reportPath);
                File.WriteAllText(reportPath, "repair conflict");
                request.RepairMissingContent = true;
                IdleAutoDefenseTemplateSetupResult conflict = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
                Assert.That(conflict.Status, Is.EqualTo(IdleAutoDefenseTemplateSetupStatus.BlockedByExistingFiles));
                Assert.That(conflict.BlockedFiles, Does.Contain(targetRoot + "/Docs/setup-report.md"));
                Assert.That(File.ReadAllText(reportPath), Is.EqualTo("repair conflict"));
                File.WriteAllBytes(reportPath, originalReport);

                File.Delete(AssetPathToFullPath(missingRewardPath));
                IdleAutoDefenseTemplateSetupResult repaired = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
                Assert.That(repaired.Succeeded, Is.True, repaired.CreateSummary());
                AssertFileExists(missingRewardPath);
                Assert.That(ReadMetaGuid(AssetPathToFullPath(missingRewardMetaPath)), Is.EqualTo(rewardGuid));
                Assert.That(repaired.BlockedFiles, Is.Empty);

                IdleAutoDefenseTemplateSetupResult noOpRepair = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);
                Assert.That(noOpRepair.Succeeded, Is.True, noOpRepair.CreateSummary());
                Assert.That(noOpRepair.CreatedFiles, Is.Empty, "A deterministic repair rerun should not rewrite valid output.");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                ClearWaveEntryIds(basicSet.WaveSet[0]);
                AssetDatabase.SaveAssets();
                WaveEntryIdMigrationReport migration = WaveEntryIdMigration.MigrateProjectOwnedWaveAssets(basicContentRoot);
                WaveEntryIdMigrationReport repeatedMigration = WaveEntryIdMigration.MigrateProjectOwnedWaveAssets(basicContentRoot);
                Assert.That(migration.Succeeded, Is.True, migration.CreateSummary());
                Assert.That(migration.MigratedAssetCount, Is.EqualTo(1));
                Assert.That(migration.UnchangedAssetCount, Is.EqualTo(6));
                Assert.That(repeatedMigration.MigratedAssetCount, Is.EqualTo(0));
                Assert.That(repeatedMigration.UnchangedAssetCount, Is.EqualTo(7));
                AssertSequentialWaveEntryIds(basicSet.WaveSet);
                AssertSequentialWaveEntryIds(scrapSet.WaveSet);

                Assert.That(AssetDatabase.FindAssets("t:GameContentPackAsset", new[] { contentRoot }).Length, Is.EqualTo(2));
                Assert.That(File.Exists(AssetPathToFullPath(basicScene)), Is.True);
                Assert.That(File.Exists(AssetPathToFullPath(scrapScene)), Is.True);
            }
            finally
            {
                Undo.ClearAll();
                AssetDatabase.DeleteAsset(targetRoot);
                AssetDatabase.DeleteAsset(contentRoot);
                for (int i = 0; i < sceneRoots.Length; i++) RestoreAssetDirectory(sceneRoots[i], backups[i]);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        [Test]
        public void SetupWizardCopiesStarterToProjectOwnedFolderAndBlocksOverwrite()
        {
            string tempRoot = "Assets/T";
            string targetRoot = tempRoot + "/W" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string contentRoot = "Assets/GameContent/W" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string secondContentRoot = "Assets/GameContent/W" + Guid.NewGuid().ToString("N").Substring(0, 8);
            const string visibleSceneRootAssetPath = "Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame";
            string visibleSceneRootFullPath = AssetPathToFullPath(visibleSceneRootAssetPath);
            string visibleSceneBackupPath = Path.Combine(Path.GetTempPath(), "IdleAutoDefenseVisibleSceneBackup_" + Guid.NewGuid().ToString("N"));
            bool hadVisibleScene = Directory.Exists(visibleSceneRootFullPath) || File.Exists(visibleSceneRootFullPath + ".meta");
            var request = new IdleAutoDefenseTemplateSetupRequest
            {
                TargetRootAssetPath = targetRoot,
                ContentRootAssetPath = contentRoot,
                GameNamespace = "WizardSmoke.IdleAutoDefense",
                GamePrefix = "Wizard Smoke",
                PackSelection = IdleAutoDefenseTemplatePackSelection.BasicOnly,
                AllowOverwrite = false,
                OpenCreatedScene = false,
                RefreshAssetDatabase = false
            };

            try
            {
                if (hadVisibleScene)
                {
                    if (Directory.Exists(visibleSceneRootFullPath))
                        CopyDirectory(visibleSceneRootFullPath, Path.Combine(visibleSceneBackupPath, "SceneRoot"));
                    if (File.Exists(visibleSceneRootFullPath + ".meta"))
                    {
                        Directory.CreateDirectory(visibleSceneBackupPath);
                        File.Copy(visibleSceneRootFullPath + ".meta", Path.Combine(visibleSceneBackupPath, "SceneRoot.meta"), true);
                    }

                    DeleteDirectoryIfExists(visibleSceneRootFullPath);
                }

                IdleAutoDefenseTemplateSetupResult result = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(request);

                Assert.IsTrue(result.Succeeded, result.CreateSummary());
                Assert.AreEqual(IdleAutoDefenseTemplateSetupStatus.Succeeded, result.Status);
                AssertCreatedPathsStayUnderAllowedRoots(result, targetRoot, contentRoot);
                const string generatedSceneAssetPath = "Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity";
                AssertFileExists(generatedSceneAssetPath);
                AssertFileExists(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs");
                AssertFileExists(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs.meta");
                AssertFileExists(targetRoot + "/WizardSmoke.IdleAutoDefense.asmdef");
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Attacks"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Enemies"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Weapons"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Waves"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Upgrades"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Rewards"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Economy"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/RunProfiles"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Progression"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/OfflineProgression"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/GameRules"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/ContentSets"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/ContentPacks"));
                AssertDirectoryExists(AssetPathToFullPath(contentRoot + "/Presentation"));
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Visuals/Prefabs"));
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Audio"));
                AssertDirectoryExists(AssetPathToFullPath(targetRoot + "/Resources/Kenney/IdleAutoDefense"));
                AssertFileExists(targetRoot + "/Resources/Kenney/IdleAutoDefense/Art/enemy_basic_green.png");
                AssertFileExists(targetRoot + "/Resources/Kenney/IdleAutoDefense/Art/projectile_rocket.png");
                AssertFileExists(targetRoot + "/Resources/Kenney/IdleAutoDefense/Audio/laserSmall_000.ogg");
                AssertFileExists(targetRoot + "/Resources/Kenney/README.md");
                AssertFileExists(targetRoot + "/Docs/ThirdPartyNotices.md");
                AssertFileExists(contentRoot + "/Enemies/enemy.idle-auto-defense.swarm/enemy.idle-auto-defense.swarm_EnemyDefinition.asset");
                AssertFileExists(contentRoot + "/Waves/wave.idle-auto-defense.runner-pressure/wave.idle-auto-defense.runner-pressure_WaveDefinition.asset");
                AssertFileExists(contentRoot + "/Upgrades/upgrade.idle-auto-defense.projectile-speed-up/upgrade.idle-auto-defense.projectile-speed-up_RunUpgradeDefinition.asset");
                AssertFileExists(contentRoot + "/Upgrades/upgrade.idle-auto-defense.core-reinforcement/upgrade.idle-auto-defense.core-reinforcement_RunUpgradeDefinition.asset");
                AssertFileExists(contentRoot + "/Upgrades/upgrade.idle-auto-defense.credit-reward/upgrade.idle-auto-defense.credit-reward_RunUpgradeDefinition.asset");
                AssertFileExists(contentRoot + "/Rewards/reward-catalog.idle-auto-defense.playable.asset");
                AssertFileExists(contentRoot + "/Economy/economy.idle-auto-defense.playable.asset");
                AssertFileExists(contentRoot + "/RunProfiles/run-profile.idle-auto-defense.playable.asset");
                AssertFileExists(contentRoot + "/Progression/progression.idle-auto-defense.playable.asset");
                AssertFileExists(contentRoot + "/OfflineProgression/offline-progression.idle-auto-defense.playable.asset");
                AssertFileExists(contentRoot + "/GameRules/game-rules.idle-auto-defense.playable.asset");
                AssertFileExists(contentRoot + "/Presentation/player-experience.idle-auto-defense.playable.asset");
                AssertFileExists(contentRoot + "/Presentation/ui-settings.idle-auto-defense.mobile-landscape.asset");
                AssertFileExists(contentRoot + "/Presentation/tutorial.idle-auto-defense.first-run.asset");
                AssertFileExists(contentRoot + "/Presentation/audio-palette.idle-auto-defense.playable.asset");
                AssertFileExists(contentRoot + "/Presentation/Themes/theme.idle-auto-defense.default.asset");
                AssertFileExists(contentRoot + "/Presentation/Themes/theme.idle-auto-defense.neon-bastion.asset");
                Assert.IsFalse(File.Exists(AssetPathToFullPath(contentRoot + "/starter-content.json")), "The unconsumed JSON mirror must not be generated.");
                AssertFileExists(contentRoot + "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset");
                AssertFileExists(contentRoot + "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset.meta");
                AssertFileExists(contentRoot + "/ContentPacks/contentpack.idle-auto-defense.playable/contentpack.idle-auto-defense.playable_ContentPack.asset");
                AssertFileExists(contentRoot + "/ContentPacks/contentpack.idle-auto-defense.playable/contentpack.idle-auto-defense.playable_ContentPack.asset.meta");
                string contentSetGuid = ReadMetaGuid(AssetPathToFullPath(contentRoot + "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset.meta"));
                string contentPackGuid = ReadMetaGuid(AssetPathToFullPath(contentRoot + "/ContentPacks/contentpack.idle-auto-defense.playable/contentpack.idle-auto-defense.playable_ContentPack.asset.meta"));
                string playerExperienceGuid = ReadMetaGuid(AssetPathToFullPath(contentRoot + "/Presentation/player-experience.idle-auto-defense.playable.asset.meta"));
                string generatedPulseWeaponGuid = ReadMetaGuid(AssetPathToFullPath(contentRoot + "/Weapons/weapon.idle-auto-defense.pulse-beam/weapon.idle-auto-defense.pulse-beam_WeaponDefinition.asset.meta"));
                string generatedRewardCatalogGuid = ReadMetaGuid(AssetPathToFullPath(contentRoot + "/Rewards/reward-catalog.idle-auto-defense.playable.asset.meta"));
                string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BasicIdleAutoDefenseGame).Assembly).resolvedPath;
                string templateSourceRoot = Path.Combine(packageRoot, "TemplateSource~", "BasicIdleAutoDefenseGame");
                string sourceContentSetGuid = ReadMetaGuid(Path.Combine(templateSourceRoot, "Content", "ContentSets", "contentset.idle-auto-defense.playable", "contentset.idle-auto-defense.playable_GameContentSet.asset.meta"));
                string sourceContentPackGuid = ReadMetaGuid(Path.Combine(templateSourceRoot, "Content", "ContentPacks", "contentpack.idle-auto-defense.playable", "contentpack.idle-auto-defense.playable_ContentPack.asset.meta"));
                string sourcePlayerExperienceGuid = ReadMetaGuid(Path.Combine(templateSourceRoot, "Content", "Presentation", "player-experience.idle-auto-defense.playable.asset.meta"));
                string sourcePulseWeaponGuid = ReadMetaGuid(Path.Combine(templateSourceRoot, "Content", "Weapons", "weapon.idle-auto-defense.pulse-beam", "weapon.idle-auto-defense.pulse-beam_WeaponDefinition.asset.meta"));
                string sourceRewardCatalogGuid = ReadMetaGuid(Path.Combine(templateSourceRoot, "Content", "Rewards", "reward-catalog.idle-auto-defense.playable.asset.meta"));
                Assert.AreNotEqual(sourceContentSetGuid, contentSetGuid);
                Assert.AreNotEqual(sourceContentPackGuid, contentPackGuid);
                Assert.AreNotEqual(sourcePlayerExperienceGuid, playerExperienceGuid);
                Assert.AreNotEqual(sourcePulseWeaponGuid, generatedPulseWeaponGuid);
                Assert.AreNotEqual(sourceRewardCatalogGuid, generatedRewardCatalogGuid);
                AssertFileContains(AssetPathToFullPath(generatedSceneAssetPath), contentSetGuid);
                AssertFileContains(AssetPathToFullPath(generatedSceneAssetPath), contentPackGuid);
                AssertFileContains(AssetPathToFullPath(generatedSceneAssetPath), playerExperienceGuid);
                AssertFileDoesNotContain(AssetPathToFullPath(generatedSceneAssetPath), sourceContentSetGuid);
                AssertFileDoesNotContain(AssetPathToFullPath(generatedSceneAssetPath), sourceContentPackGuid);
                AssertFileDoesNotContain(AssetPathToFullPath(generatedSceneAssetPath), sourcePlayerExperienceGuid);
                AssertFileContains(AssetPathToFullPath(contentRoot + "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset"), generatedPulseWeaponGuid);
                AssertFileContains(AssetPathToFullPath(contentRoot + "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset"), generatedRewardCatalogGuid);
                AssertFileDoesNotContain(AssetPathToFullPath(contentRoot + "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset"), sourcePulseWeaponGuid);
                AssertFileDoesNotContain(AssetPathToFullPath(contentRoot + "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset"), sourceRewardCatalogGuid);
                AssertFileDoesNotContain(AssetPathToFullPath(contentRoot + "/Attacks/attack.idle-auto-defense.shard-projectile/attack.idle-auto-defense.shard-projectile_Delivery.asset"), "_projectilePrefab: {fileID: 0");
                AssertFileDoesNotContain(AssetPathToFullPath(contentRoot + "/Attacks/attack.idle-auto-defense.homing-pulse/attack.idle-auto-defense.homing-pulse_Delivery.asset"), "_projectilePrefab: {fileID: 0");
                AssertFileDoesNotContain(AssetPathToFullPath(contentRoot + "/Attacks/attack.idle-auto-defense.pulse-beam/attack.idle-auto-defense.pulse-beam_Delivery.asset"), "_beamVfxPrefab: {fileID: 0");
                AssertFileContains(AssetPathToFullPath(contentRoot + "/Attacks/attack.idle-auto-defense.shard-projectile/attack.idle-auto-defense.shard-projectile_Presentation.asset"), "_audioClip: {fileID: 8300000");
                AssertFileContains(AssetPathToFullPath(contentRoot + "/Attacks/attack.idle-auto-defense.shard-projectile/attack.idle-auto-defense.shard-projectile_Presentation.asset"), "_vfxPrefab: {fileID:");
                AssertFileContains(AssetPathToFullPath(contentRoot + "/Enemies/enemy.idle-auto-defense.swarm/enemy.idle-auto-defense.swarm_Presentation.asset"), "_audioClip: {fileID: 8300000");
                AssertFileContains(AssetPathToFullPath(contentRoot + "/Enemies/enemy.idle-auto-defense.swarm/enemy.idle-auto-defense.swarm_Presentation.asset"), "_vfxPrefab: {fileID:");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Docs/asset-flip-checklist.md"), "product-owned");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Docs/asset-flip-checklist.md"), contentRoot);
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Docs/setup-report.md"), "Deucarian.TemplateGameIdleAutoDefense");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Docs/setup-report.md"), contentRoot);
                AssertFileContains(AssetPathToFullPath(targetRoot + "/README.md"), contentRoot);
                AssertFileContains(AssetPathToFullPath(targetRoot + "/README.md"), "# Wizard Smoke Idle Auto Defense");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/README.md"), "product-owned output");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs"), "namespace WizardSmoke.IdleAutoDefense");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs"), "WizardSmokeIdleAutoDefenseGameBootstrap");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs"), "IdleAutoDefensePlayerExperienceController");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs"), "_templatePlayerExperience");
                AssertFileDoesNotContain(AssetPathToFullPath(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs"), "OnGUI");
                AssertFileDoesNotContain(AssetPathToFullPath(targetRoot + "/Scripts/WizardSmokeIdleAutoDefenseGameBootstrap.cs"), "GUILayout");
                AssertFileContains(AssetPathToFullPath(targetRoot + "/WizardSmoke.IdleAutoDefense.asmdef"), "Deucarian.TemplateGameIdleAutoDefense");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                GameContentSetAsset generatedContentSet = AssetDatabase.LoadAssetAtPath<GameContentSetAsset>(
                    contentRoot + "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset");
                Assert.That(generatedContentSet, Is.Not.Null);
                Assert.That(generatedContentSet.RewardCatalog, Is.Not.Null);
                Assert.That(generatedContentSet.Economy, Is.Not.Null);
                Assert.That(generatedContentSet.RunProfile, Is.Not.Null);
                Assert.That(generatedContentSet.Progression, Is.Not.Null);
                Assert.That(generatedContentSet.OfflineProgression, Is.Not.Null);
                Assert.That(generatedContentSet.GameRules, Is.Not.Null);
                Assert.That(GameContentSetValidator.Validate(generatedContentSet).IsValid, Is.True, FormatIssues(GameContentSetValidator.Validate(generatedContentSet)));
                string authoredValidation = IdleAutoDefenseAuthoredContentValidationMenu.BuildReport();
                Assert.That(authoredValidation, Does.StartWith("Idle Auto Defense authored content validation: PASS"), authoredValidation);
                Assert.That(authoredValidation, Does.Contain("player-experiences=1"), authoredValidation);
                Assert.That(authoredValidation, Does.Contain("PASS contentset.idle-auto-defense.playable"), authoredValidation);
                AssertGeneratedContentIsDiscoverableInGameContentLibrary(contentRoot);
                AssertGeneratedContentPackAppearsInGameContentAuthoring(contentRoot, generatedSceneAssetPath);

                string secondTargetRoot = tempRoot + "/W" + Guid.NewGuid().ToString("N").Substring(0, 8);
                var secondRequest = new IdleAutoDefenseTemplateSetupRequest
                {
                    TargetRootAssetPath = secondTargetRoot,
                    ContentRootAssetPath = secondContentRoot,
                    GameNamespace = "WizardSmoke.SecondIdleAutoDefense",
                    GamePrefix = "Wizard Second",
                    AllowOverwrite = true,
                    OpenCreatedScene = false,
                    RefreshAssetDatabase = false
                };
                IdleAutoDefenseTemplateSetupResult second = IdleAutoDefenseTemplateSetupService.CreateGameFromTemplate(secondRequest);
                Assert.IsTrue(second.Succeeded, second.CreateSummary());
                string secondContentSetGuid = ReadMetaGuid(AssetPathToFullPath(secondContentRoot + "/ContentSets/contentset.idle-auto-defense.playable/contentset.idle-auto-defense.playable_GameContentSet.asset.meta"));
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
                DeleteDirectoryIfExists(visibleSceneRootFullPath);
                DeleteDirectoryIfExists(AssetPathToFullPath(tempRoot));
                DeleteDirectoryIfExists(AssetPathToFullPath(contentRoot));
                DeleteDirectoryIfExists(AssetPathToFullPath(secondContentRoot));
                if (hadVisibleScene)
                {
                    string sceneRootBackupPath = Path.Combine(visibleSceneBackupPath, "SceneRoot");
                    string sceneRootMetaBackupPath = Path.Combine(visibleSceneBackupPath, "SceneRoot.meta");
                    if (Directory.Exists(sceneRootBackupPath))
                        CopyDirectory(sceneRootBackupPath, visibleSceneRootFullPath);
                    if (File.Exists(sceneRootMetaBackupPath))
                        File.Copy(sceneRootMetaBackupPath, visibleSceneRootFullPath + ".meta", true);
                    DeleteDirectoryIfExists(visibleSceneBackupPath);
                }
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
                        new WaveEntryRecipe(enemies[0], 4, 1, 0, 28, "perimeter-north"),
                        new WaveEntryRecipe(enemies[1], 2, 1, 35, 42, "perimeter-east"),
                        new WaveEntryRecipe(enemies[2], 1, 1, 110, 0, "perimeter-west")
                    }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.assigned.runner-pressure",
                    "Assigned Runner Pressure",
                    130,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[1], 4, 1, 0, 32, "perimeter-east"),
                        new WaveEntryRecipe(enemies[0], 4, 1, 24, 30, "perimeter-north")
                    }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.assigned.mixed-pressure",
                    "Assigned Mixed Pressure",
                    230,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[3], 2, 1, 0, 42, "perimeter-south"),
                        new WaveEntryRecipe(enemies[2], 2, 1, 30, 36, "perimeter-east"),
                        new WaveEntryRecipe(enemies[1], 4, 1, 55, 28, "perimeter-north")
                    }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.assigned.tank-break",
                    "Assigned Tank Break",
                    330,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[0], 5, 1, 0, 26, "perimeter-north"),
                        new WaveEntryRecipe(enemies[1], 3, 1, 32, 36, "perimeter-south"),
                        new WaveEntryRecipe(enemies[3], 2, 1, 68, 42, "perimeter-west")
                    }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.assigned.final-surge",
                    "Assigned Final Surge",
                    450,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[2], 2, 1, 0, 52, "perimeter-north"),
                        new WaveEntryRecipe(enemies[3], 3, 1, 28, 42, "perimeter-east"),
                        new WaveEntryRecipe(enemies[1], 4, 1, 70, 30, "perimeter-south")
                    }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.assigned.elite-pressure",
                    "Assigned Elite Pressure",
                    560,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[4], 1, 1, 0, 0, "perimeter-northwest", 3),
                        new WaveEntryRecipe(enemies[1], 4, 1, 42, 42, "perimeter-east", 2)
                    }),
                WaveDefinitionAsset.CreateTransient(
                    "wave.assigned.boss-push",
                    "Assigned Boss Push",
                    820,
                    new[]
                    {
                        new WaveEntryRecipe(enemies[5], 1, 1, 0, 0, "perimeter-south", 4),
                        new WaveEntryRecipe(enemies[4], 1, 1, 70, 0, "perimeter-northeast", 3),
                        new WaveEntryRecipe(enemies[1], 6, 1, 95, 34, "perimeter-northwest", 3)
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
                Endless = contentSet.Endless,
                RuntimeSettings = contentSet.RuntimeSettings.Clone()
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

        private static bool IsExcitingRewardChoice(IdleAutoDefenseRewardDraftChoice choice)
        {
            if (choice == null) return false;
            if (choice.IsUnlock) return true;
            if (choice.Rarity >= IdleAutoDefenseRewardRarity.Epic) return true;

            string description = choice.EffectDescription ?? string.Empty;
            return description.IndexOf("extra", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   description.IndexOf("becomes", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   description.IndexOf("projectile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   description.IndexOf("visible", StringComparison.OrdinalIgnoreCase) >= 0;
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
            GameContentLibraryReport report = GameContentLibraryService.Scan(contentRoot);
            Assert.AreEqual(0, CountLibraryErrors(report, contentRoot), FormatLibraryIssues(report, contentRoot));
            Assert.AreEqual(4, CountLibraryItems(report, contentRoot, GameContentLibraryKind.Attack));
            Assert.AreEqual(6, CountLibraryItems(report, contentRoot, GameContentLibraryKind.Enemy));
            Assert.AreEqual(7, CountLibraryItems(report, contentRoot, GameContentLibraryKind.Wave));
            Assert.AreEqual(4, CountLibraryItems(report, contentRoot, GameContentLibraryKind.Weapon));
            Assert.AreEqual(6, CountLibraryItems(report, contentRoot, GameContentLibraryKind.Upgrade));
            Assert.AreEqual(1, CountLibraryItems(report, contentRoot, GameContentLibraryKind.ContentSet));
            Assert.AreEqual(1, CountLibraryItems(report, contentRoot, GameContentLibraryKind.ContentPack));

            GameContentLibraryItem contentSet = FindLibraryItem(report, contentRoot, GameContentLibraryKind.ContentSet, "contentset.idle-auto-defense.playable");
            GameContentLibraryItem contentPack = FindLibraryItem(report, contentRoot, GameContentLibraryKind.ContentPack, "contentpack.idle-auto-defense.playable");
            Assert.NotNull(contentSet);
            Assert.NotNull(contentPack);

            GameContentLibraryContentSetSummary contentSetSummary = report.GetContentSetSummary(contentSet);
            GameContentLibraryContentPackSummary contentPackSummary = report.GetContentPackSummary(contentPack);
            Assert.NotNull(contentSetSummary);
            Assert.NotNull(contentPackSummary);
            Assert.AreEqual(1, contentPackSummary.ContentSetCount);
            Assert.That(contentSetSummary.Message, Is.Not.Empty);
            Assert.That(contentPackSummary.Message, Is.Not.Empty);
        }

        private static void AssertGeneratedContentPackAppearsInGameContentAuthoring(
            string contentRoot,
            string generatedSceneAssetPath)
        {
            IdleAutoDefenseContentLensAdapters.EnsureRegistered();
            var idleProvider = new GameContentPackAuthoringProvider(contentRoot);
            GameContentPackDescriptor pack = idleProvider.GetContentPacks().Single(value =>
                string.Equals(value.PackId, IdleAutoDefenseContentPackIndex.PackId, StringComparison.OrdinalIgnoreCase));
            IReadOnlyList<GameContentRecordDescriptor> records = idleProvider.GetRecords(pack.PackId);

            Assert.That(pack.PackId, Is.EqualTo(IdleAutoDefenseContentPackIndex.PackId));
            Assert.That(pack.OwningPackageId, Is.EqualTo(IdleAutoDefenseContentPackIndex.OwningPackageId));
            Assert.That(pack.DisplayName, Is.EqualTo(IdleAutoDefenseContentPackIndex.DisplayName));
            Assert.That(pack.SourceState, Is.EqualTo(GameContentPackSourceState.Available), FormatValidation(pack.Validation));
            Assert.That(pack.Access.CanEditExisting, Is.True);
            Assert.That(pack.Access.PersistenceLabel, Is.EqualTo("Staged project-owned ScriptableObject field editing"));
            Assert.That(pack.Manifest, Is.Null);
            Assert.That(pack.PlayableScene, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(pack.PlayableScene), Is.EqualTo(generatedSceneAssetPath));
            Assert.That(pack.Metadata.Single(value => value.Label == "GameContentPackAsset").Value, Is.Not.EqualTo("Missing"));
            Assert.That(pack.Metadata.Single(value => value.Label == "GameContentSetAsset").Value, Is.Not.EqualTo("Missing"));
            Assert.That(pack.Actions.Any(action => action.ActionId == IdleAutoDefenseContentPackIndex.ValidateActionId && action.Enabled), Is.True);
            Assert.That(pack.Actions.Any(action => action.ActionId == IdleAutoDefenseContentPackIndex.RevealActionId && action.Enabled), Is.True);
            Assert.That(pack.Actions.Any(action => action.ActionId == IdleAutoDefenseContentPackIndex.OpenSceneActionId && action.Enabled), Is.True);
            Assert.That(pack.Actions.Any(action => action.ActionId == IdleAutoDefenseContentPackIndex.OpenSetupActionId), Is.False);

            Assert.That(records.Count(record => record.HasCapability(GameContentRecordCapabilities.Attack)), Is.EqualTo(4));
            Assert.That(records.Count(record => record.HasCapability(GameContentRecordCapabilities.Enemy)), Is.EqualTo(6));
            Assert.That(records.Count(record => record.HasCapability(GameContentRecordCapabilities.Wave)), Is.EqualTo(7));
            Assert.That(records.Count(record => record.HasCapability(GameContentRecordCapabilities.Weapon)), Is.EqualTo(4));
            Assert.That(records.Count(record => record.HasCapability(GameContentRecordCapabilities.Tower)), Is.EqualTo(4));
            Assert.That(records.Count(record => record.HasCapability(GameContentRecordCapabilities.Upgrade)), Is.EqualTo(6));
            Assert.That(records.Count(record => record.IsInCategory("reward-choices")), Is.EqualTo(37));
            Assert.That(records.Count(record => record.IsInCategory("reward-tables")), Is.EqualTo(1));
            Assert.That(records.Count(record => record.IsInCategory("normal-upgrades")), Is.EqualTo(12));
            Assert.That(records.Count(record => record.IsInCategory("epic-upgrades")), Is.EqualTo(12));
            Assert.That(records.Count(record => record.IsInCategory("legendary-upgrades")), Is.EqualTo(4));
            Assert.That(records.Count(record => record.IsInCategory("economy")), Is.EqualTo(8));
            Assert.That(records.Count(record => record.IsInCategory("currencies")), Is.EqualTo(2));
            Assert.That(records.Count(record => record.IsInCategory("run-profiles")), Is.EqualTo(1));
            Assert.That(records.Count(record => record.IsInCategory("persistent-progression")), Is.EqualTo(6));
            Assert.That(records.Count(record => record.IsInCategory("offline-progression")), Is.EqualTo(1));
            Assert.That(records.Count(record => record.IsInCategory("game-rules")), Is.EqualTo(1));
            Assert.That(records.Count(record => record.IsInCategory("player-experience")), Is.EqualTo(1));
            Assert.That(records.Count(record => record.IsInCategory("themes")), Is.EqualTo(2));
            Assert.That(records.Count(record => record.IsInCategory("audio-palettes")), Is.EqualTo(1));
            Assert.That(records.Count(record => record.IsInCategory("audio-events")), Is.EqualTo(26));
            Assert.That(records.Count(record => record.IsInCategory("tutorial-definitions")), Is.EqualTo(1));
            Assert.That(records.Count(record => record.IsInCategory("tutorials")), Is.EqualTo(10));
            Assert.That(records.Count(record => record.IsInCategory("ui-settings")), Is.EqualTo(1));
            Assert.That(records.Count, Is.EqualTo(124));
            Assert.That(records.Select(record => record.CanonicalKey).Distinct().Count(), Is.EqualTo(124));
            Assert.That(records.All(record => record.CanonicalKey.OwningPackageId == IdleAutoDefenseContentPackIndex.OwningPackageId), Is.True);
            Assert.That(records.All(record => record.CanonicalKey.PackId == IdleAutoDefenseContentPackIndex.PackId), Is.True);
            Assert.That(records.All(record => record.CanonicalKey.SourceId.StartsWith(GameContentSourceIdentity.UnityAssetGuidKind + "::", StringComparison.Ordinal)), Is.True);

            var projectProvider = new GameContentLibraryProvider();
            GameContentPackCatalog catalog = GameContentPackCatalog.Build(new IGameContentAuthoringProvider[] { projectProvider, idleProvider });
            GameContentPackCatalogEntry idleEntry = catalog.Find(pack.StableKey);
            string projectKey = GameContentPackDescriptor.BuildStableKey(
                "com.deucarian.game-content-authoring.project",
                "project-content");
            GameContentPackCatalogEntry projectEntry = catalog.Find(projectKey);
            Assert.That(idleEntry, Is.Not.Null);
            Assert.That(projectEntry, Is.Not.Null);
            Assert.That(idleEntry.Records.Count, Is.EqualTo(124));
            Assert.That(projectEntry.Records.Any(record => IsPathUnderAssetRoot(record.SourcePath, contentRoot)), Is.False);
            Assert.That(catalog.SourceClaimConflicts, Is.Empty);
            Assert.That(catalog.AllRecords.Count(record => IsPathUnderAssetRoot(record.SourcePath, contentRoot)), Is.EqualTo(124));

            GameContentPackContext context = new GameContentPackSelectionState().Select(catalog, pack.StableKey);
            foreach (GameContentRecordDescriptor record in context.Records)
            {
                foreach (GameContentRecordReferenceDescriptor reference in record.OutboundReferences.Where(value => value.TargetRecordKey != null))
                    Assert.That(context.ResolveReference(record, reference), Is.Not.Null, record.SourceRecordId + " -> " + reference.TargetRecordId);
            }

            GameContentRecordDescriptor attack = records.First(record => record.HasCapability(GameContentRecordCapabilities.Attack));
            GameContentRecordDescriptor enemy = records.First(record => record.HasCapability(GameContentRecordCapabilities.Enemy));
            GameContentRecordDescriptor wave = records.First(record => record.HasCapability(GameContentRecordCapabilities.Wave));
            GameContentRecordDescriptor weapon = records.First(record => record.HasCapability(GameContentRecordCapabilities.Weapon));
            GameContentRecordDescriptor upgrade = records.First(record => record.HasCapability(GameContentRecordCapabilities.Upgrade));
            Assert.That(GameContentRecordProjectionRegistry<AttackContentRecordProjection>.TryProject(attack, out AttackContentRecordProjection attackProjection), Is.True);
            Assert.That(attackProjection.StatusSummary, Does.Contain("simulation ticks"));
            Assert.That(GameContentRecordProjectionRegistry<EnemyContentRecordProjection>.TryProject(enemy, out _), Is.True);
            Assert.That(GameContentRecordProjectionRegistry<EncounterContentRecordProjection>.TryProject(wave, out EncounterContentRecordProjection waveProjection), Is.True);
            Assert.That(waveProjection.EncounterKind, Does.Contain("tick"));
            Assert.That(GameContentRecordProjectionRegistry<WeaponContentRecordProjection>.TryProject(weapon, out WeaponContentRecordProjection weaponProjection), Is.True);
            Assert.That(weaponProjection.IsTower, Is.True);
            Assert.That(weaponProjection.RankPathSummary, Does.Contain("simulation ticks"));
            Assert.That(GameContentRecordProjectionRegistry<UpgradeContentRecordProjection>.TryProject(upgrade, out _), Is.True);

            UnityEngine.Object[] sourceAssets = records.Select(record => record.SourceAsset).Where(value => value != null).ToArray();
            Assert.That(sourceAssets.Any(EditorUtility.IsDirty), Is.False);
            Assert.That(idleProvider.GetSourceClaims(pack.PackId).Count, Is.GreaterThanOrEqualTo(35));
            Assert.That(idleProvider.ValidatePack(pack.PackId).IsValid, Is.True, FormatValidation(idleProvider.ValidatePack(pack.PackId)));
            Assert.That(idleProvider.ExecuteAction(pack.PackId, IdleAutoDefenseContentPackIndex.ValidateActionId).Succeeded, Is.True);
            Assert.That(idleProvider.ExecuteAction(pack.PackId, IdleAutoDefenseContentPackIndex.RevealActionId).Succeeded, Is.True);
            Assert.That(sourceAssets.Any(EditorUtility.IsDirty), Is.False);
        }

        private static string FormatValidation(GameContentAuthoringValidationResult validation)
        {
            return validation == null
                ? "No validation result."
                : string.Join("\n", validation.Issues.Select(issue => issue.Severity + " " + issue.Path + ": " + issue.Message));
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

        private static void AssertNamedPackAuthoringSurface(
            GameContentPackAuthoringProvider provider,
            GameContentPackDescriptor pack,
            string expectedContentRoot,
            string expectedScenePath)
        {
            IReadOnlyList<GameContentRecordDescriptor> records = provider.GetRecords(pack.PackId);
            Assert.That(pack.SourceState, Is.EqualTo(GameContentPackSourceState.Available), FormatValidation(pack.Validation));
            Assert.That(pack.Access.CanEditExisting, Is.True);
            Assert.That(pack.PlayableScene, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(pack.PlayableScene), Is.EqualTo(expectedScenePath));
            Assert.That(pack.Actions.Any(value => value.ActionId == IdleAutoDefenseContentPackIndex.ValidateActionId && value.Enabled), Is.True);
            Assert.That(pack.Actions.Any(value => value.ActionId == IdleAutoDefenseContentPackIndex.RevealActionId && value.Enabled), Is.True);
            Assert.That(pack.Actions.Any(value => value.ActionId == IdleAutoDefenseContentPackIndex.OpenSceneActionId && value.Enabled), Is.True);
            Assert.That(records.Count, Is.EqualTo(124));
            Assert.That(records.All(value => value.CanonicalKey.PackId == pack.PackId), Is.True);
            Assert.That(records.All(value => IsPathUnderAssetRoot(value.SourcePath, expectedContentRoot)), Is.True);
            Assert.That(records.Count(value => value.HasCapability(GameContentRecordCapabilities.Attack)), Is.EqualTo(4));
            Assert.That(records.Count(value => value.HasCapability(GameContentRecordCapabilities.Enemy)), Is.EqualTo(6));
            Assert.That(records.Count(value => value.HasCapability(GameContentRecordCapabilities.Wave)), Is.EqualTo(7));
            Assert.That(records.Count(value => value.HasCapability(GameContentRecordCapabilities.Weapon)), Is.EqualTo(4));
            Assert.That(records.Count(value => value.HasCapability(GameContentRecordCapabilities.Upgrade)), Is.EqualTo(6));
            Assert.That(records.Count(value => value.IsInCategory("reward-choices")), Is.EqualTo(37));
            Assert.That(records.Count(value => value.IsInCategory("normal-upgrades")), Is.EqualTo(12));
            Assert.That(records.Count(value => value.IsInCategory("epic-upgrades")), Is.EqualTo(12));
            Assert.That(records.Count(value => value.IsInCategory("legendary-upgrades")), Is.EqualTo(4));
            Assert.That(records.Count(value => value.IsInCategory("themes")), Is.EqualTo(2));
            Assert.That(records.Count(value => value.IsInCategory("audio-events")), Is.EqualTo(26));
            Assert.That(records.Count(value => value.IsInCategory("tutorials")), Is.EqualTo(10));
            Assert.That(records.Select(value => value.CanonicalKey).Distinct().Count(), Is.EqualTo(124));
            Assert.That(records.SelectMany(value => value.OutboundReferences)
                .Where(value => !string.IsNullOrWhiteSpace(value.TargetPackId))
                .All(value => string.Equals(value.TargetPackId, pack.PackId, StringComparison.OrdinalIgnoreCase)), Is.True);

            GameContentPackCatalog catalog = GameContentPackCatalog.Build(new IGameContentAuthoringProvider[] { provider });
            GameContentPackContext context = new GameContentPackSelectionState().Select(catalog, pack.StableKey);
            foreach (GameContentRecordDescriptor record in context.Records)
            {
                foreach (GameContentRecordReferenceDescriptor reference in record.OutboundReferences.Where(value => value.TargetRecordKey != null))
                    Assert.That(context.ResolveReference(record, reference), Is.Not.Null, record.SourceRecordId + " -> " + reference.TargetRecordId);
            }

            UnityEngine.Object[] sourceAssets = records.Select(value => value.SourceAsset)
                .Where(value => value != null)
                .Distinct()
                .ToArray();
            Assert.That(sourceAssets.Any(EditorUtility.IsDirty), Is.False);
            Assert.That(provider.GetSourceClaims(pack.PackId).Count, Is.GreaterThanOrEqualTo(35));
            Assert.That(provider.ExecuteAction(pack.PackId, IdleAutoDefenseContentPackIndex.ValidateActionId).Succeeded, Is.True);
            UnityEngine.Object previousSelection = Selection.activeObject;
            try
            {
                Assert.That(provider.ExecuteAction(pack.PackId, IdleAutoDefenseContentPackIndex.RevealActionId).Succeeded, Is.True);
            }
            finally
            {
                Selection.activeObject = previousSelection;
            }
            Assert.That(sourceAssets.Any(EditorUtility.IsDirty), Is.False);
        }

        private static void AssertGeneratedMetaGuidsAreUnique(params string[] assetRoots)
        {
            var metaPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < assetRoots.Length; i++)
            {
                string fullRoot = AssetPathToFullPath(assetRoots[i]);
                if (Directory.Exists(fullRoot))
                {
                    foreach (string path in Directory.GetFiles(fullRoot, "*.meta", SearchOption.AllDirectories))
                        metaPaths.Add(path);
                }
                if (File.Exists(fullRoot + ".meta")) metaPaths.Add(fullRoot + ".meta");
            }

            string[] duplicates = metaPaths
                .Select(ReadMetaGuid)
                .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
                .Where(value => value.Count() > 1)
                .Select(value => value.Key)
                .ToArray();
            Assert.That(duplicates, Is.Empty, "Duplicate generated GUIDs: " + string.Join(", ", duplicates));
        }

        private static void AssertResponsiveLayoutPolicy(IdleAutoDefenseUiSettingsAsset settings)
        {
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.RespectSafeArea, Is.True);
            Assert.That(settings.MinimumTouchTarget, Is.GreaterThanOrEqualTo(44f));
            var targets = new[]
            {
                new { Width = 1920, Height = 1080, Compact = false },
                new { Width = 1280, Height = 720, Compact = false },
                new { Width = 960, Height = 540, Compact = true },
                new { Width = 844, Height = 390, Compact = true },
                new { Width = 1024, Height = 768, Compact = false }
            };
            foreach (var target in targets)
            {
                Assert.That(IdleAutoDefensePlayerExperienceController.ShouldUseCompactLayout(target.Width, target.Height, settings),
                    Is.EqualTo(target.Compact), target.Width + "x" + target.Height);
                Assert.That(IdleAutoDefensePlayerExperienceController.ShouldShowPortraitMessage(target.Width, target.Height, settings),
                    Is.False, target.Width + "x" + target.Height);
                Vector4 insets = IdleAutoDefensePlayerExperienceController.CalculateSafeAreaInsets(
                    new Rect(0, 0, target.Width, target.Height),
                    new Rect(24, 10, target.Width - 48, target.Height - 20));
                Assert.That(insets, Is.EqualTo(new Vector4(24, 10, 24, 10)));
            }
        }

        private static void AssertGeneratedPackMenuFirstStrictBoot(
            GameContentPackAsset pack,
            GameContentSetAsset contentSet,
            IdleAutoDefensePlayerExperienceAsset experience)
        {
            string persistenceRoot = Path.Combine(Path.GetTempPath(), "IdleGeneratedPackBoot", Guid.NewGuid().ToString("N"));
            GameObject host = new GameObject("generated-pack-menu-first-smoke");
            host.SetActive(false);
            try
            {
                var controller = host.AddComponent<GeneratedPackBootProbeController>();
                controller.Pack = pack;
                controller.ContentSet = contentSet;
                controller.Experience = experience;
                controller.ConfigurePersistenceRoot(persistenceRoot);
                controller.InitializeForEditMode();
                Assert.That(controller.StartupBlocked, Is.False, controller.StartupError);
                Assert.That(controller.UsingAssignedContentPack, Is.True);
                Assert.That(controller.UsingAssignedContentSet, Is.True);
                Assert.That(controller.UsingAuthoredCore, Is.True);
                Assert.That(controller.FallbackModeActive, Is.False);
                Assert.That(controller.PlayerExperienceValid, Is.True, controller.PlayerFacingError);
                Assert.That(controller.ActiveContentPackId, Is.EqualTo(pack.Id));
                Assert.That(controller.ActiveContentSetId, Is.EqualTo(contentSet.Id));
                Assert.That(controller.PersistenceScopeId, Is.EqualTo(pack.Id));
                Assert.That(controller.PersistenceDocumentName, Is.EqualTo(IdleAutoDefensePlayerProfileStore.BuildDocumentName(pack.Id)));
                Assert.That(controller.ActiveThemeId, Is.EqualTo(experience.DefaultThemeId));
                Assert.That(controller.MainMenuVisible, Is.True);
                Assert.That(controller.RunActive, Is.False);
                Assert.That(controller.NormalHudVisible, Is.False);
                Assert.That(controller.SurvivalSeconds, Is.Zero);
                Assert.That(controller.PlayerUiButtonCount, Is.GreaterThanOrEqualTo(16));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                if (Directory.Exists(persistenceRoot)) Directory.Delete(persistenceRoot, true);
            }
        }

        private sealed class GeneratedPackBootProbeController : IdleAutoDefensePlayerExperienceController
        {
            public GameContentPackAsset Pack { get; set; }
            public GameContentSetAsset ContentSet { get; set; }
            public IdleAutoDefensePlayerExperienceAsset Experience { get; set; }

            protected override void ConfigurePlayerExperienceBeforeBuild()
            {
                RequireAuthoredContentOnStartup();
                ConfigureContentPack(Pack, ContentSet);
                ConfigurePlayerExperience(Experience);
            }

            public void InitializeForEditMode()
            {
                base.Awake();
            }
        }

        private static void AssertPackNumericParity(GameContentSetAsset basic, GameContentSetAsset scrap)
        {
            Assert.That(scrap.AvailableWeapons.Count, Is.EqualTo(basic.AvailableWeapons.Count));
            for (int i = 0; i < basic.AvailableWeapons.Count; i++)
            {
                WeaponStatsDefinitionAsset left = basic.AvailableWeapons[i].Stats;
                WeaponStatsDefinitionAsset right = scrap.AvailableWeapons[i].Stats;
                Assert.That(right, Is.Not.SameAs(left));
                Assert.That(right.FireMode, Is.EqualTo(left.FireMode));
                Assert.That(right.CooldownTicks, Is.EqualTo(left.CooldownTicks));
                Assert.That(right.Range, Is.EqualTo(left.Range));
                Assert.That(right.BuildCost, Is.EqualTo(left.BuildCost));
                Assert.That(right.Attack.Mechanics.DamageAmount, Is.EqualTo(left.Attack.Mechanics.DamageAmount));
                Assert.That(right.Attack.Mechanics.CooldownTicks, Is.EqualTo(left.Attack.Mechanics.CooldownTicks));
                Assert.That(right.Attack.Mechanics.Range, Is.EqualTo(left.Attack.Mechanics.Range));
                Assert.That(right.Attack.Delivery.Mode, Is.EqualTo(left.Attack.Delivery.Mode));
                Assert.That(right.Attack.Delivery.ProjectileSpeed, Is.EqualTo(left.Attack.Delivery.ProjectileSpeed));
                Assert.That(right.Attack.Delivery.Radius, Is.EqualTo(left.Attack.Delivery.Radius));
            }

            Assert.That(scrap.EnemyPool.Count, Is.EqualTo(basic.EnemyPool.Count));
            for (int i = 0; i < basic.EnemyPool.Count; i++)
            {
                EnemyStatsDefinitionAsset left = basic.EnemyPool[i].Stats;
                EnemyStatsDefinitionAsset right = scrap.EnemyPool[i].Stats;
                Assert.That(right, Is.Not.SameAs(left));
                Assert.That(right.MaximumHealth, Is.EqualTo(left.MaximumHealth));
                Assert.That(right.MoveSpeed, Is.EqualTo(left.MoveSpeed));
                Assert.That(right.RewardValue, Is.EqualTo(left.RewardValue));
                Assert.That(right.ContactDamage, Is.EqualTo(left.ContactDamage));
                Assert.That(right.CollisionRadius, Is.EqualTo(left.CollisionRadius));
            }

            Assert.That(scrap.WaveSet.Count, Is.EqualTo(basic.WaveSet.Count));
            for (int i = 0; i < basic.WaveSet.Count; i++)
            {
                Assert.That(scrap.WaveSet[i], Is.Not.SameAs(basic.WaveSet[i]));
                Assert.That(scrap.WaveSet[i].Schedule.StartTick, Is.EqualTo(basic.WaveSet[i].Schedule.StartTick));
                Assert.That(scrap.WaveSet[i].Entries.Entries.Count, Is.EqualTo(basic.WaveSet[i].Entries.Entries.Count));
                for (int j = 0; j < basic.WaveSet[i].Entries.Entries.Count; j++)
                {
                    WaveEntryRecipe left = basic.WaveSet[i].Entries.Entries[j];
                    WaveEntryRecipe right = scrap.WaveSet[i].Entries.Entries[j];
                    Assert.That(right.EntryId, Is.EqualTo(left.EntryId));
                    Assert.That(right.Count, Is.EqualTo(left.Count));
                    Assert.That(right.BatchSize, Is.EqualTo(left.BatchSize));
                    Assert.That(right.InitialDelayTicks, Is.EqualTo(left.InitialDelayTicks));
                    Assert.That(right.IntervalTicks, Is.EqualTo(left.IntervalTicks));
                    Assert.That(right.ScalingTier, Is.EqualTo(left.ScalingTier));
                }
            }

            Assert.That(scrap.RewardCatalog.FirstDraftSeconds, Is.EqualTo(basic.RewardCatalog.FirstDraftSeconds));
            Assert.That(scrap.RewardCatalog.Settings.NormalEnemyExperience, Is.EqualTo(basic.RewardCatalog.Settings.NormalEnemyExperience));
            Assert.That(scrap.RewardCatalog.Settings.BaseExperienceToNextLevel, Is.EqualTo(basic.RewardCatalog.Settings.BaseExperienceToNextLevel));
            Assert.That(scrap.RewardCatalog.Catalog.WeaponUnlocks.Count, Is.EqualTo(basic.RewardCatalog.Catalog.WeaponUnlocks.Count));
            Assert.That(scrap.RewardCatalog.Catalog.NormalWeaponRewards.Count, Is.EqualTo(basic.RewardCatalog.Catalog.NormalWeaponRewards.Count));
            Assert.That(scrap.RewardCatalog.Catalog.EpicWeaponRewards.Count, Is.EqualTo(basic.RewardCatalog.Catalog.EpicWeaponRewards.Count));
            Assert.That(scrap.RewardCatalog.Catalog.LegendaryWeaponRewards.Count, Is.EqualTo(basic.RewardCatalog.Catalog.LegendaryWeaponRewards.Count));
            Assert.That(scrap.RewardCatalog.Catalog.BaseRewards.Count, Is.EqualTo(basic.RewardCatalog.Catalog.BaseRewards.Count));
            Assert.That(scrap.Economy.StartingCredits, Is.EqualTo(basic.Economy.StartingCredits));
            Assert.That(scrap.Economy.StartingParts, Is.EqualTo(basic.Economy.StartingParts));
            Assert.That(scrap.Economy.PassiveIncomeAmount, Is.EqualTo(basic.Economy.PassiveIncomeAmount));
            Assert.That(scrap.Economy.PassiveIncomeIntervalTicks, Is.EqualTo(basic.Economy.PassiveIncomeIntervalTicks));
            Assert.That(scrap.RunProfile.SessionLengthTicks, Is.EqualTo(basic.RunProfile.SessionLengthTicks));
            Assert.That(scrap.RunProfile.SimulationTicksPerSecond, Is.EqualTo(basic.RunProfile.SimulationTicksPerSecond));
            Assert.That(scrap.RunProfile.RewardMultiplier, Is.EqualTo(basic.RunProfile.RewardMultiplier));
            Assert.That(scrap.OfflineProgression.MaximumOfflineSeconds, Is.EqualTo(basic.OfflineProgression.MaximumOfflineSeconds));
            Assert.That(scrap.OfflineProgression.ProductionAmountPerSecond, Is.EqualTo(basic.OfflineProgression.ProductionAmountPerSecond));
            Assert.That(scrap.GameRules.ObjectiveMaximumHealth, Is.EqualTo(basic.GameRules.ObjectiveMaximumHealth));
            Assert.That(scrap.GameRules.SpawnRingRadius, Is.EqualTo(basic.GameRules.SpawnRingRadius));
            Assert.That(scrap.Progression.Tracks.Count, Is.EqualTo(basic.Progression.Tracks.Count));
            Assert.That(scrap.Progression.ResearchNodes.Count, Is.EqualTo(basic.Progression.ResearchNodes.Count));
        }

        private static void AssertTemplateSourceWaveEntryIds(string templateSourceRoot)
        {
            string wavesRoot = Path.Combine(templateSourceRoot, "Content", "Waves");
            string[] files = Directory.GetFiles(wavesRoot, "*_Entries.asset", SearchOption.AllDirectories);
            Assert.That(files.Length, Is.EqualTo(7));
            int total = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string[] ids = File.ReadAllLines(files[i])
                    .Select(line => line.Trim())
                    .Where(line => line.StartsWith("- _entryId:", StringComparison.Ordinal))
                    .Select(line => line.Substring(line.IndexOf(':') + 1).Trim())
                    .ToArray();
                total += ids.Length;
                for (int j = 0; j < ids.Length; j++)
                    Assert.That(ids[j], Is.EqualTo(j.ToString()), files[i]);
            }

            Assert.That(total, Is.EqualTo(20));
        }

        private static void AssertSequentialWaveEntryIds(IReadOnlyList<WaveDefinitionAsset> waves)
        {
            Assert.That(waves.Count, Is.EqualTo(7));
            for (int i = 0; i < waves.Count; i++)
            {
                IReadOnlyList<WaveEntryRecipe> entries = waves[i].Entries.Entries;
                for (int j = 0; j < entries.Count; j++)
                    Assert.That(entries[j].EntryId.Value, Is.EqualTo(j.ToString()), waves[i].Id);
            }
        }

        private static void AssertWaveGameplayParity(IReadOnlyList<WaveDefinitionAsset> expected, IReadOnlyList<WaveDefinitionAsset> actual)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.That(actual[i].Id, Is.EqualTo(expected[i].Id));
                Assert.That(actual[i].Schedule.StartTick, Is.EqualTo(expected[i].Schedule.StartTick));
                Assert.That(actual[i].Entries.Entries.Count, Is.EqualTo(expected[i].Entries.Entries.Count));
                for (int j = 0; j < expected[i].Entries.Entries.Count; j++)
                {
                    WaveEntryRecipe left = expected[i].Entries.Entries[j];
                    WaveEntryRecipe right = actual[i].Entries.Entries[j];
                    Assert.That(right.EntryId, Is.EqualTo(left.EntryId));
                    Assert.That(right.Enemy.Id, Is.EqualTo(left.Enemy.Id));
                    Assert.That(right.Count, Is.EqualTo(left.Count));
                    Assert.That(right.BatchSize, Is.EqualTo(left.BatchSize));
                    Assert.That(right.InitialDelayTicks, Is.EqualTo(left.InitialDelayTicks));
                    Assert.That(right.IntervalTicks, Is.EqualTo(left.IntervalTicks));
                    Assert.That(right.SpawnChannelId, Is.EqualTo(left.SpawnChannelId));
                    Assert.That(right.ScalingTier, Is.EqualTo(left.ScalingTier));
                }
            }
        }

        private static void ClearWaveEntryIds(WaveDefinitionAsset wave)
        {
            var serialized = new SerializedObject(wave.Entries);
            SerializedProperty entries = serialized.FindProperty("_entries");
            Assert.That(entries, Is.Not.Null);
            for (int i = 0; i < entries.arraySize; i++)
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("_entryId").stringValue = string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wave.Entries);
        }

        private static void DestroyTransientWave(WaveDefinitionAsset wave)
        {
            if (wave == null) return;
            if (wave.Schedule != null) UnityEngine.Object.DestroyImmediate(wave.Schedule);
            if (wave.Entries != null) UnityEngine.Object.DestroyImmediate(wave.Entries);
            UnityEngine.Object.DestroyImmediate(wave);
        }

        private static void DestroyTransientEnemy(EnemyDefinitionAsset enemy)
        {
            if (enemy == null) return;
            if (enemy.Stats != null) UnityEngine.Object.DestroyImmediate(enemy.Stats);
            if (enemy.Presentation != null) UnityEngine.Object.DestroyImmediate(enemy.Presentation);
            UnityEngine.Object.DestroyImmediate(enemy);
        }

        private static void AssertMutationIsolation(
            GameContentSetAsset basic,
            GameContentSetAsset scrap,
            IdleAutoDefensePlayerExperienceAsset basicExperience,
            IdleAutoDefensePlayerExperienceAsset scrapExperience)
        {
            float basicDamage = basic.StartingWeapon.Stats.Attack.Mechanics.DamageAmount;
            float scrapDamage = scrap.StartingWeapon.Stats.Attack.Mechanics.DamageAmount;
            AssertSerializedFloatMutation(scrap.StartingWeapon.Stats.Attack.Mechanics, "_damageAmount", scrapDamage + 7f,
                () => Assert.That(basic.StartingWeapon.Stats.Attack.Mechanics.DamageAmount, Is.EqualTo(basicDamage)));

            float basicHealth = basic.EnemyPool[0].Stats.MaximumHealth;
            float scrapHealth = scrap.EnemyPool[0].Stats.MaximumHealth;
            AssertSerializedFloatMutation(scrap.EnemyPool[0].Stats, "_maximumHealth", scrapHealth + 11f,
                () => Assert.That(basic.EnemyPool[0].Stats.MaximumHealth, Is.EqualTo(basicHealth)));

            int basicWaveCount = basic.WaveSet[0].Entries.Entries[0].Count;
            int scrapWaveCount = scrap.WaveSet[0].Entries.Entries[0].Count;
            AssertSerializedIntMutation(scrap.WaveSet[0].Entries, "_entries.Array.data[0]._count", scrapWaveCount + 2,
                () => Assert.That(basic.WaveSet[0].Entries.Entries[0].Count, Is.EqualTo(basicWaveCount)));

            int basicCost = basic.StartingWeapon.Stats.BuildCost;
            int scrapCost = scrap.StartingWeapon.Stats.BuildCost;
            AssertSerializedIntMutation(scrap.StartingWeapon.Stats, "_buildCost", scrapCost + 9,
                () => Assert.That(basic.StartingWeapon.Stats.BuildCost, Is.EqualTo(basicCost)));

            double basicReward = basic.RewardCatalog.Catalog.BaseRewards[0].Amount;
            double scrapReward = scrap.RewardCatalog.Catalog.BaseRewards[0].Amount;
            AssertSerializedDoubleMutation(scrap.RewardCatalog, "_catalog._baseRewards.Array.data[0]._amount", scrapReward + 0.5d,
                () => Assert.That(basic.RewardCatalog.Catalog.BaseRewards[0].Amount, Is.EqualTo(basicReward)));

            Color basicAccent = basicExperience.Themes[0].Accent;
            AssertSerializedColorMutation(scrapExperience.Themes[0], "_accent", Color.magenta,
                () => Assert.That(basicExperience.Themes[0].Accent, Is.EqualTo(basicAccent)));

            string basicTutorialTitle = basicExperience.Tutorial.Steps[0].Title;
            AssertSerializedStringMutation(scrapExperience.Tutorial, "_steps.Array.data[0]._title", "Scrap mutation proof",
                () => Assert.That(basicExperience.Tutorial.Steps[0].Title, Is.EqualTo(basicTutorialTitle)));

            float basicAudioVolume = basicExperience.AudioPalette.Events[0].Volume;
            float scrapAudioVolume = scrapExperience.AudioPalette.Events[0].Volume;
            AssertSerializedFloatMutation(scrapExperience.AudioPalette, "_events.Array.data[0]._volume", Mathf.Clamp01(scrapAudioVolume * 0.5f),
                () => Assert.That(basicExperience.AudioPalette.Events[0].Volume, Is.EqualTo(basicAudioVolume)));
        }

        private static void AssertStrictMissingOwnerFailures(
            GameContentSetAsset scrap,
            IdleAutoDefensePlayerExperienceAsset experience)
        {
            AssertMissingObjectReferenceFails(scrap.StartingWeapon.Stats, "_attack", () => !GameContentSetValidator.Validate(scrap).IsValid);
            AssertMissingObjectReferenceFails(scrap, "_enemyPool.Array.data[0]", () => !GameContentSetValidator.Validate(scrap).IsValid);
            AssertMissingObjectReferenceFails(scrap.RunProfile, "_waves.Array.data[0]", () => !GameContentSetValidator.Validate(scrap).IsValid);
            AssertMissingObjectReferenceFails(scrap, "_rewardCatalog", () => !GameContentSetValidator.Validate(scrap).IsValid);
            AssertMissingObjectReferenceFails(scrap, "_economy", () => !GameContentSetValidator.Validate(scrap).IsValid);
            AssertMissingObjectReferenceFails(scrap, "_runProfile", () => !GameContentSetValidator.Validate(scrap).IsValid);
            AssertMissingObjectReferenceFails(scrap, "_progression", () => !GameContentSetValidator.Validate(scrap).IsValid);
            AssertMissingObjectReferenceFails(experience, "_uiSettings", () => experience.Validate().Count > 0);
            AssertMissingObjectReferenceFails(experience, "_themes.Array.data[0]", () => experience.Validate().Count > 0);
            Assert.That(GameContentSetValidator.Validate(scrap).IsValid, Is.True, FormatIssues(GameContentSetValidator.Validate(scrap)));
            Assert.That(experience.Validate(), Is.Empty);
        }

        private static void AssertMissingObjectReferenceFails(UnityEngine.Object owner, string propertyPath, Func<bool> validation)
        {
            var serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            Assert.That(property, Is.Not.Null, propertyPath);
            UnityEngine.Object original = property.objectReferenceValue;
            property.objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                Assert.That(validation(), Is.True, propertyPath + " should be required.");
            }
            finally
            {
                serialized.Update();
                property = serialized.FindProperty(propertyPath);
                property.objectReferenceValue = original;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssertSerializedFloatMutation(UnityEngine.Object owner, string propertyPath, float value, Action assertBasic)
        {
            var serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            Assert.That(property, Is.Not.Null, propertyPath);
            float original = property.floatValue;
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try { assertBasic(); }
            finally
            {
                serialized.Update();
                property = serialized.FindProperty(propertyPath);
                property.floatValue = original;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssertSerializedIntMutation(UnityEngine.Object owner, string propertyPath, int value, Action assertBasic)
        {
            var serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            Assert.That(property, Is.Not.Null, propertyPath);
            int original = property.intValue;
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try { assertBasic(); }
            finally
            {
                serialized.Update();
                property = serialized.FindProperty(propertyPath);
                property.intValue = original;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssertSerializedDoubleMutation(UnityEngine.Object owner, string propertyPath, double value, Action assertBasic)
        {
            var serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            Assert.That(property, Is.Not.Null, propertyPath);
            double original = property.doubleValue;
            property.doubleValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try { assertBasic(); }
            finally
            {
                serialized.Update();
                property = serialized.FindProperty(propertyPath);
                property.doubleValue = original;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssertSerializedColorMutation(UnityEngine.Object owner, string propertyPath, Color value, Action assertBasic)
        {
            var serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            Assert.That(property, Is.Not.Null, propertyPath);
            Color original = property.colorValue;
            property.colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try { assertBasic(); }
            finally
            {
                serialized.Update();
                property = serialized.FindProperty(propertyPath);
                property.colorValue = original;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssertSerializedStringMutation(UnityEngine.Object owner, string propertyPath, string value, Action assertBasic)
        {
            var serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            Assert.That(property, Is.Not.Null, propertyPath);
            string original = property.stringValue;
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try { assertBasic(); }
            finally
            {
                serialized.Update();
                property = serialized.FindProperty(propertyPath);
                property.stringValue = original;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static bool CanContainGuidReference(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".asset", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".prefab", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".unity", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".mat", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".asmdef", StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertFileSha256(string path, string expected)
        {
            using (SHA256 sha = SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty).ToLowerInvariant();
                Assert.That(actual, Is.EqualTo(expected), path);
            }
        }

        private static string BackupAssetDirectory(string assetRoot)
        {
            string fullPath = AssetPathToFullPath(assetRoot);
            bool hasDirectory = Directory.Exists(fullPath);
            bool hasMeta = File.Exists(fullPath + ".meta");
            if (!hasDirectory && !hasMeta) return string.Empty;
            string backup = Path.Combine(Path.GetTempPath(), "IdleAssetFlipBackup_" + Guid.NewGuid().ToString("N"));
            if (hasDirectory) CopyDirectory(fullPath, Path.Combine(backup, "Root"));
            if (hasMeta)
            {
                Directory.CreateDirectory(backup);
                File.Copy(fullPath + ".meta", Path.Combine(backup, "Root.meta"), true);
            }
            DeleteDirectoryIfExists(fullPath);
            return backup;
        }

        private static void RestoreAssetDirectory(string assetRoot, string backup)
        {
            string fullPath = AssetPathToFullPath(assetRoot);
            DeleteDirectoryIfExists(fullPath);
            if (string.IsNullOrWhiteSpace(backup)) return;
            string directory = Path.Combine(backup, "Root");
            string meta = Path.Combine(backup, "Root.meta");
            if (Directory.Exists(directory)) CopyDirectory(directory, fullPath);
            if (File.Exists(meta)) File.Copy(meta, fullPath + ".meta", true);
            DeleteDirectoryIfExists(backup);
        }

        private static void AssertCreatedPathsStayUnderAllowedRoots(IdleAutoDefenseTemplateSetupResult result, string targetRoot, string contentRoot)
        {
            const string visibleSceneRoot = "Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame";
            for (int i = 0; i < result.CreatedFiles.Count; i++)
            {
                Assert.IsTrue(
                    IsPathUnderAssetRoot(result.CreatedFiles[i], targetRoot) ||
                    IsPathUnderAssetRoot(result.CreatedFiles[i], contentRoot) ||
                    IsPathUnderAssetRoot(result.CreatedFiles[i], visibleSceneRoot),
                    "Created file should stay under target, content, or visible scene root: " + result.CreatedFiles[i]);
                StringAssert.DoesNotContain("Packages/", result.CreatedFiles[i]);
                StringAssert.DoesNotContain("/Runtime/", result.CreatedFiles[i]);
                StringAssert.DoesNotContain("/Editor/", result.CreatedFiles[i]);
            }

            for (int i = 0; i < result.CreatedDirectories.Count; i++)
            {
                Assert.IsTrue(
                    string.Equals(result.CreatedDirectories[i], targetRoot, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(result.CreatedDirectories[i], contentRoot, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(result.CreatedDirectories[i], visibleSceneRoot, StringComparison.OrdinalIgnoreCase) ||
                    IsPathUnderAssetRoot(result.CreatedDirectories[i], targetRoot) ||
                    IsPathUnderAssetRoot(result.CreatedDirectories[i], contentRoot) ||
                    IsPathUnderAssetRoot(result.CreatedDirectories[i], visibleSceneRoot),
                    "Created directory should stay under target, content, or visible scene root: " + result.CreatedDirectories[i]);
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

        private static void CopyDirectory(string sourcePath, string destinationPath)
        {
            Directory.CreateDirectory(destinationPath);
            string sourceFullPath = Path.GetFullPath(sourcePath);
            string[] files = Directory.GetFiles(sourceFullPath, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string relativePath = files[i].Substring(sourceFullPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string destinationFile = Path.Combine(destinationPath, relativePath);
                string destinationDirectory = Path.GetDirectoryName(destinationFile);
                if (!string.IsNullOrEmpty(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);
                File.Copy(files[i], destinationFile, true);
            }
        }

        private static IdleAutoDefenseTemplateController CreateController()
        {
            GameObject host = new GameObject("idle-auto-defense-template-editmode");
            return host.AddComponent<IdleAutoDefenseTemplateController>();
        }

        private static string[] RewardChoiceIds(IdleAutoDefenseTemplateController controller)
        {
            return controller.RewardDraftChoices.Select(choice => choice.Id).ToArray();
        }

        private static string GetRewardChoiceDedupeKey(IdleAutoDefenseRewardDraftChoice choice)
        {
            PropertyInfo property = typeof(IdleAutoDefenseRewardDraftChoice).GetProperty("DedupeKey", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(property);
            return (string)property.GetValue(choice);
        }

        private static int GetRewardDraftSeed(IdleAutoDefenseTemplateController controller)
        {
            FieldInfo field = typeof(IdleAutoDefenseTemplateController).GetField("_rewardDraftSeed", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return (int)field.GetValue(controller);
        }

        private static EncounterDefinition CreateFailCapablePressureEncounterDefinition()
        {
            return new EncounterDefinition(
                new EncounterId("encounter.idle-auto-defense.fail-pressure"),
                null,
                new[]
                {
                    new WaveDefinition(new WaveId("wave.idle-auto-defense.fail-pressure.overrun"), 0, new[]
                    {
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.fail-pressure.runner-north"), new SpawnableId(BasicIdleAutoDefenseGame.RunnerEnemySpawnableId.Value), 24, 6, 0, 4, new SpawnChannelId("perimeter-north")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.fail-pressure.runner-east"), new SpawnableId(BasicIdleAutoDefenseGame.RunnerEnemySpawnableId.Value), 24, 6, 0, 4, new SpawnChannelId("perimeter-east")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.fail-pressure.swarm-south"), new SpawnableId(BasicIdleAutoDefenseGame.SwarmEnemySpawnableId.Value), 30, 6, 4, 4, new SpawnChannelId("perimeter-south")),
                        SpawnGroupDefinition.Fixed(new SpawnGroupId("group.idle-auto-defense.fail-pressure.tank-west"), new SpawnableId(BasicIdleAutoDefenseGame.TankEnemySpawnableId.Value), 8, 2, 8, 8, new SpawnChannelId("perimeter-west"))
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
            StepUntilTerminal(controller, 6400);
            if (expectCompletion.HasValue)
            {
                if (expectCompletion.Value) Assert.IsTrue(controller.EncounterCompleted, controller.StatusSummary);
                else Assert.IsTrue(controller.EncounterFailed, controller.StatusSummary);
            }
        }

        private static void StepUntilTerminal(IdleAutoDefenseTemplateController controller, int maxTicks, bool chooseRewardDrafts = true)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                if (chooseRewardDrafts && controller.RewardDraftActive)
                    controller.TryChooseRewardDraftChoice(0);
                controller.Step(1, 0.05f);
                if (controller.EncounterCompleted || controller.EncounterFailed)
                    break;
            }

            Assert.IsTrue(controller.EncounterCompleted || controller.EncounterFailed);
        }

        private static void StepUntilTerminalWithLivePurchases(IdleAutoDefenseTemplateController controller, int maxTicks)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                BuyAvailableLivePurchases(controller);
                controller.Step(1, 0.05f);
                if (controller.EncounterCompleted || controller.EncounterFailed)
                    break;
            }

            Assert.IsTrue(controller.EncounterCompleted || controller.EncounterFailed, controller.StatusSummary);
        }

        private static void BuyAvailableLivePurchases(IdleAutoDefenseTemplateController controller)
        {
            if (controller.RewardDraftActive)
                controller.TryChooseRewardDraftChoice(0);
            if (controller.ObjectiveHealth < controller.ObjectiveMaximumHealth * 0.7d && controller.CanPurchaseRepairUpgrade)
                controller.TryPurchaseRepairUpgrade();
            if (controller.CanPurchasePulseBeamModule) controller.TryPurchasePulseBeamModule();
            if (controller.PulseBeamUnlocked && controller.CanPurchaseDamageUpgrade) controller.TryPurchaseDamageUpgrade();
            if (controller.PulseBeamUnlocked && controller.CanPurchaseAttackSpeedUpgrade) controller.TryPurchaseAttackSpeedUpgrade();
            if (controller.CanPurchaseArcBurstModule) controller.TryPurchaseArcBurstModule();
            if (controller.ArcBurstUnlocked && controller.CanPurchaseRangeUpgrade) controller.TryPurchaseRangeUpgrade();
            if (controller.CanPurchaseHomingPulseModule) controller.TryPurchaseHomingPulseModule();

            if (!controller.PulseBeamUnlocked || !controller.ArcBurstUnlocked || !controller.HomingPulseUnlocked)
                return;

            if (controller.CanPurchaseOverdrive) controller.TryPurchaseOverdrive();
            if (controller.ObjectiveHealth < controller.ObjectiveMaximumHealth * 0.7d && controller.CanPurchaseRepairUpgrade)
                controller.TryPurchaseRepairUpgrade();
            if (controller.CanPurchaseDamageUpgrade) controller.TryPurchaseDamageUpgrade();
            if (controller.CanPurchaseAttackSpeedUpgrade) controller.TryPurchaseAttackSpeedUpgrade();
            if (controller.CanPurchaseRangeUpgrade) controller.TryPurchaseRangeUpgrade();
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
