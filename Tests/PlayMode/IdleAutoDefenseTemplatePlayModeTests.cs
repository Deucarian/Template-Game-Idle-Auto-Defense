using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Deucarian.Attacks.Authoring;
using Deucarian.IdleProgression;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense.PlayModeTests
{
    public sealed class IdleAutoDefenseTemplatePlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthoredScalarValueFlowsIntoStrictRuntimeWithoutFallback()
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
            AttackDefinitionAsset authoredAttack = weapons[0].Stats.Attack;
            int authoredAttackIndex = Array.IndexOf(attacks, authoredAttack);
            const float authoredDamage = 37.5f;
            authoredAttack.Mechanics.Configure(
                authoredAttack.Mechanics.CooldownTicks,
                authoredAttack.Mechanics.Range,
                authoredDamage,
                authoredAttack.Mechanics.DamageTypeId);
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            WaveDefinitionAsset[] waves = BasicIdleAutoDefenseGame.CreateWaveDefinitions();
            RunUpgradeDefinitionAsset[] upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);
            GameContentSetAsset contentSet = GameContentSetAsset.CreateTransient(
                "contentset.test.playmode.authored-scalar",
                "Authored Scalar PlayMode",
                weapons[0],
                weapons,
                enemies,
                waves,
                upgrades,
                10,
                0,
                1f,
                1f,
                5600,
                false,
                "Runtime consumption proof for a provider-editable scalar.",
                new[] { "test", "authored-scalar" });
            GameContentPackAsset pack = GameContentPackAsset.CreateTransient(
                "contentpack.test.playmode.authored-scalar",
                "Authored Scalar PlayMode",
                new[] { contentSet },
                contentSet);

            Assert.That(authoredAttackIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(authoredAttack.ToRuntimeDefinition().BaseDamage, Is.EqualTo(authoredDamage));
            Assert.That(
                BasicIdleAutoDefenseGame.CreateAttackDefinitions(attacks)[authoredAttackIndex].BaseDamage,
                Is.EqualTo(authoredDamage));

            GameObject host = new GameObject("idle-auto-defense-authored-scalar-playmode");
            host.SetActive(false);
            var controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            typeof(IdleAutoDefenseTemplateController).GetField("_contentPack", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(controller, pack);
            typeof(IdleAutoDefenseTemplateController).GetField("_contentSet", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(controller, contentSet);
            controller.ConfigureStrictAuthoredStartup(true);
            host.SetActive(true);
            controller.enabled = false;
            yield return null;

            Assert.That(controller.StartupBlocked, Is.False, controller.StartupError);
            Assert.That(controller.FallbackModeActive, Is.False);
            Assert.That(controller.UsingAuthoredCore, Is.True);
            Assert.That(controller.ActiveContentPackId, Is.EqualTo(pack.Id));
            Assert.That(contentSet.StartingWeapon.Stats.Attack.ToRuntimeDefinition().BaseDamage, Is.EqualTo(authoredDamage));

            UnityEngine.Object.Destroy(host);
            UnityEngine.Object.Destroy(pack);
            UnityEngine.Object.Destroy(contentSet);
        }

        [UnityTest]
        public IEnumerator StrictAuthoredControllerRejectsMissingWaveEntryIdentityWithoutFallback()
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            WaveDefinitionAsset[] waves = BasicIdleAutoDefenseGame.CreateWaveDefinitions();
            waves[0] = WaveDefinitionAsset.CreateTransient(
                "wave.invalid-entry-identity.playmode",
                "Invalid Entry Identity",
                0,
                new[] { new WaveEntryRecipe(string.Empty, enemies[0], 2, 1, 0, 10, "perimeter-north") });
            RunUpgradeDefinitionAsset[] upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);
            GameContentSetAsset contentSet = GameContentSetAsset.CreateTransient(
                "contentset.test.playmode.invalid-entry-identity",
                "Invalid Entry Identity PlayMode",
                weapons[0],
                weapons,
                enemies,
                waves,
                upgrades,
                10,
                0,
                1f,
                1f,
                5600,
                false,
                "Strict rejection proof for missing wave entry identity.",
                new[] { "test", "strict-authored", "invalid-entry-identity" });

            GameObject host = new GameObject("idle-auto-defense-invalid-entry-identity-playmode");
            host.SetActive(false);
            var controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            typeof(IdleAutoDefenseTemplateController).GetField("_contentSet", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(controller, contentSet);
            controller.ConfigureStrictAuthoredStartup(true);
            LogAssert.Expect(LogType.Error, new Regex("Strict authored startup blocked:"));
            host.SetActive(true);
            controller.enabled = false;
            yield return null;

            Assert.That(controller.StartupBlocked, Is.True);
            Assert.That(controller.StartupError, Does.Contain("strict").IgnoreCase);
            Assert.That(controller.FallbackModeActive, Is.False);
            Assert.That(controller.UsingAuthoredCore, Is.False);
            Assert.That(controller.SpawnedCount, Is.Zero);

            UnityEngine.Object.Destroy(host);
            UnityEngine.Object.Destroy(contentSet);
        }

        [UnityTest]
        public IEnumerator StrictAuthoredControllerBindsCoreAndRunsGameplayWithoutFallback()
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            WaveDefinitionAsset[] waves = BasicIdleAutoDefenseGame.CreateWaveDefinitions();
            RunUpgradeDefinitionAsset[] upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);
            GameContentSetAsset contentSet = GameContentSetAsset.CreateTransient(
                "contentset.test.playmode.strict-authored",
                "Strict Authored PlayMode",
                weapons[0],
                weapons,
                enemies,
                waves,
                upgrades,
                10,
                0,
                1f,
                1f,
                5600,
                false,
                "Strict generated-scene binding fixture.",
                new[] { "test", "strict-authored" });

            GameObject host = new GameObject("idle-auto-defense-strict-authored-playmode");
            host.SetActive(false);
            var controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            FieldInfo contentSetField = typeof(IdleAutoDefenseTemplateController).GetField("_contentSet", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(contentSetField, Is.Not.Null);
            contentSetField.SetValue(controller, contentSet);
            controller.ConfigureStrictAuthoredStartup(true);
            host.SetActive(true);
            controller.enabled = false;
            controller.RewardDraftPausesCombat = false;
            yield return null;

            for (int i = 0; i < 900; i++)
            {
                controller.Step(1, 0.05f);
                if (controller.RewardDraftActive) controller.TryChooseRewardDraftChoice(0);
                if (i % 60 == 0) yield return null;
            }

            Assert.That(controller.StartupBlocked, Is.False, controller.StartupError);
            Assert.That(controller.FallbackModeActive, Is.False);
            Assert.That(controller.UsingAuthoredCore, Is.True);
            Assert.That(controller.ActiveRewardCatalogId, Is.EqualTo(contentSet.RewardCatalog.Id));
            Assert.That(controller.ActiveEconomyId, Is.EqualTo(contentSet.Economy.Id));
            Assert.That(controller.ActiveRunProfileId, Is.EqualTo(contentSet.RunProfile.Id));
            Assert.That(controller.ActiveProgressionId, Is.EqualTo(contentSet.Progression.Id));
            Assert.That(controller.ActiveOfflineProgressionId, Is.EqualTo(contentSet.OfflineProgression.Id));
            Assert.That(controller.ActiveGameRulesId, Is.EqualTo(contentSet.GameRules.Id));
            Assert.That(controller.SpawnedCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ProjectileLaunchCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.RewardDraftOpenedCount, Is.GreaterThan(0), controller.StatusSummary);

            UnityEngine.Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator SharedControllerRunsScrapFrontierPackIdentityWithoutFallback()
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            WaveDefinitionAsset[] waves = BasicIdleAutoDefenseGame.CreateWaveDefinitions();
            RunUpgradeDefinitionAsset[] upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);
            GameContentSetAsset contentSet = GameContentSetAsset.CreateTransient(
                "contentset.idle-auto-defense.scrap-frontier.playmode",
                "Scrap Frontier PlayMode",
                weapons[0],
                weapons,
                enemies,
                waves,
                upgrades,
                10,
                0,
                1f,
                1f,
                5600,
                false,
                "Shared-runtime Scrap Frontier strict binding fixture.",
                new[] { "test", "scrap-frontier", "strict-authored" });
            GameContentPackAsset pack = GameContentPackAsset.CreateTransient(
                "contentpack.idle-auto-defense.scrap-frontier",
                "Scrap Frontier",
                new[] { contentSet },
                contentSet,
                description: "Asset-flip PlayMode binding proof.");

            GameObject host = new GameObject("scrap-frontier-shared-runtime-playmode");
            host.SetActive(false);
            var controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            typeof(IdleAutoDefenseTemplateController).GetField("_contentPack", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(controller, pack);
            typeof(IdleAutoDefenseTemplateController).GetField("_contentSet", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(controller, contentSet);
            controller.ConfigureStrictAuthoredStartup(true);
            host.SetActive(true);
            controller.enabled = false;
            controller.RewardDraftPausesCombat = false;
            yield return null;

            for (int i = 0; i < 900; i++)
            {
                controller.Step(1, 0.05f);
                if (controller.RewardDraftActive) controller.TryChooseRewardDraftChoice(0);
                if (i % 60 == 0) yield return null;
            }

            Assert.That(controller.StartupBlocked, Is.False, controller.StartupError);
            Assert.That(controller.FallbackModeActive, Is.False);
            Assert.That(controller.UsingAuthoredCore, Is.True);
            Assert.That(controller.ActiveContentPackId, Is.EqualTo("contentpack.idle-auto-defense.scrap-frontier"));
            Assert.That(controller.ActiveContentPackDisplayName, Is.EqualTo("Scrap Frontier"));
            Assert.That(controller.ActiveContentSetId, Is.EqualTo("contentset.idle-auto-defense.scrap-frontier.playmode"));
            Assert.That(controller.SpawnedCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ProjectileLaunchCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.RewardDraftOpenedCount, Is.GreaterThan(0), controller.StatusSummary);

            UnityEngine.Object.Destroy(host);
            UnityEngine.Object.Destroy(pack);
            UnityEngine.Object.Destroy(contentSet);
        }

        [UnityTest]
        public IEnumerator BasicIdleAutoDefenseControllerRunsDeterministicSmoke()
        {
            GameObject host = new GameObject("idle-auto-defense-template-smoke");
            var controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            controller.enabled = false;
            bool observedMajorThreat = false;

            for (int i = 0; i < 6400; i++)
            {
                BuyAvailableLivePurchases(controller);
                controller.Step(1, 0.05f);
                if (controller.TryGetPrimaryMajorThreat(out IdleAutoDefenseMajorThreatSnapshot threat))
                {
                    observedMajorThreat = true;
                    Assert.That(threat.DisplayName, Is.Not.Empty);
                    Assert.That(threat.MaximumHealth, Is.GreaterThan(0d));
                    Assert.That(threat.HealthNormalized, Is.InRange(0f, 1f));
                }
                if (controller.EncounterCompleted || controller.EncounterFailed)
                    break;
                if (i % 30 == 0) yield return null;
            }

            Assert.That(controller.SpawnedCount, Is.GreaterThanOrEqualTo(4));
            Assert.That(controller.EnemiesSpawnedBeyondStartingRangeCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.MinimumEnemySpawnDistance, Is.GreaterThan(17f), controller.StatusSummary);
            Assert.That(controller.RangeRejectedTargetCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.EnemyDamageSurvivedCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ClosestEnemyDistanceToObjective, Is.GreaterThan(0f).And.LessThan(8f), controller.StatusSummary);
            Assert.That(controller.ProjectileLaunchCount, Is.GreaterThan(0));
            Assert.That(controller.ProjectileVisualSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ProjectileMotionObservedCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ProjectileImpactCallbackCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ProjectileDamageResolvedFromImpactCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ProjectileDamageAppliedCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.AreEqual(controller.ProjectileDamageResolvedFromImpactCount, controller.ProjectileDamageAppliedCount, controller.StatusSummary);
            Assert.AreEqual(0, controller.ProjectileImpactRejectedCount, controller.StatusSummary);
            Assert.That(controller.DamageNumberSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.AttackVfxSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.AttackAudioPlayCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.EnemyPresentationEventCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.Kenney3DModelSpawnCount, Is.GreaterThan(12), controller.StatusSummary);
            Assert.That(controller.TurretAimUpdateCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.MuzzleProjectileLaunchCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.MuzzleFlashSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.RecoilEventCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.AuthoredWeaponPresentationSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.AreEqual(0, controller.FallbackWeaponPresentationSpawnCount, controller.StatusSummary);
            Assert.AreEqual(0, controller.DebugAimTracerSpawnCount, controller.StatusSummary);
            Assert.That(controller.EnemyFacingUpdateCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.EnemyHitFlashCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.EnemyDeathPopCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.AuthoredVisibleInstanceStampCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.DirectOrCombatKillCount + controller.ProjectileAdapterKillCount, Is.GreaterThan(0));
            Assert.That(controller.SelectedUpgradeCount, Is.GreaterThanOrEqualTo(3), controller.StatusSummary);
            Assert.That(controller.RewardDraftOpenedCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.RewardDraftSelectionCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.FirstRewardDraftSeconds, Is.GreaterThanOrEqualTo(30f).And.LessThanOrEqualTo(60f), controller.StatusSummary);
            Assert.That(controller.UpgradeFeedbackSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.EliteOrBossSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(observedMajorThreat, Is.True, "Elite/boss health-bar data should be observable while the threat is active.");
            Assert.That(controller.ObjectiveDamageEvents, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ModuleActivationCount, Is.GreaterThan(0));
            Assert.That(controller.OverdriveActivationCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.True(controller.PulseBeamUnlocked, "Smoke should unlock Pulse Beam.");
            Assert.True(controller.ArcBurstUnlocked, "Smoke should unlock Arc Burst.");
            Assert.True(controller.HomingPulseUnlocked, "Smoke should unlock Homing Pulse.");
            Assert.AreEqual(0, controller.DraftTickCount, "Sample upgrades should be explicit live purchases only.");
            Assert.True(controller.EncounterCompleted, "Assisted sample run should complete. " + controller.StatusSummary);
            Assert.That(controller.SurvivalSeconds, Is.GreaterThan(180f), controller.StatusSummary);
            Assert.That(controller.EncounterRewardCredits, Is.GreaterThanOrEqualTo(60));
            Assert.That(controller.EncounterRewardParts, Is.GreaterThanOrEqualTo(3));

            controller.RestartRun(BasicIdleAutoDefenseGame.CreateBossPulseEncounterDefinition());
            for (int i = 0; i < 1400; i++)
            {
                BuyAvailableLivePurchases(controller);
                controller.Step(1, 0.05f);
                if (controller.EncounterCompleted || controller.EncounterFailed)
                    break;
                if (i % 30 == 0) yield return null;
            }

            Assert.True(controller.EncounterCompleted || controller.EncounterFailed, "Pressure run should reach a terminal state in deterministic smoke. " + controller.StatusSummary);

            controller.SimulateOfflineReward(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddHours(1));
            Assert.AreEqual(IdleProgressionResultCode.Success, controller.LastOfflineRewardCode);
            Assert.That(controller.OfflineRewardCredits, Is.GreaterThanOrEqualTo(1260));
            Assert.That(controller.OfflineRewardParts, Is.GreaterThanOrEqualTo(15));

            UnityEngine.Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator ProjectileDamageWaitsForImpactCallback()
        {
            GameObject host = new GameObject("idle-auto-defense-projectile-impact-probe");
            var controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            controller.enabled = false;
            controller.RewardDraftPausesCombat = false;

            for (int i = 0; i < 2600; i++)
            {
                controller.Step(1, 0.05f);
                if (controller.ProjectileLaunchCount > 0)
                    break;
                if (i % 30 == 0) yield return null;
            }

            Assert.That(controller.ProjectileLaunchCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.AreEqual(0, controller.ProjectileImpactCallbackCount, controller.StatusSummary);
            Assert.AreEqual(0, controller.ProjectileDamageAppliedCount, controller.StatusSummary);
            Assert.AreEqual(0, controller.DamageNumberSpawnCount, controller.StatusSummary);

            for (int i = 0; i < 260; i++)
            {
                controller.Step(1, 0.05f);
                if (controller.ProjectileImpactCallbackCount > 0)
                    break;
                if (i % 15 == 0) yield return null;
            }

            Assert.That(controller.ProjectileImpactCallbackCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ProjectileDamageResolvedFromImpactCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.AreEqual(controller.ProjectileDamageResolvedFromImpactCount, controller.ProjectileDamageAppliedCount, controller.StatusSummary);
            Assert.That(controller.DamageNumberSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.AreEqual(0, controller.ProjectileImpactRejectedCount, controller.StatusSummary);

            UnityEngine.Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator BasicIdleAutoDefenseControllerCanFailWithoutLivePurchases()
        {
            GameObject host = new GameObject("idle-auto-defense-template-no-upgrade-smoke");
            var controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            controller.enabled = false;
            controller.RewardDraftPausesCombat = false;

            for (int i = 0; i < 6400; i++)
            {
                controller.Step(1, 0.05f);
                if (controller.EncounterCompleted || controller.EncounterFailed)
                    break;
                if (i % 30 == 0) yield return null;
            }

            Assert.AreEqual(0, controller.SelectedUpgradeCount);
            Assert.IsFalse(controller.PulseBeamUnlocked);
            Assert.That(controller.RangeRejectedTargetCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.EnemyDamageSurvivedCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ClosestEnemyDistanceToObjective, Is.GreaterThan(0f).And.LessThan(2f), controller.StatusSummary);
            Assert.That(controller.EnemyPresentationEventCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ObjectiveDamageEvents, Is.GreaterThan(0), controller.StatusSummary);
            Assert.True(controller.EncounterFailed, "No-upgrade sample run should be able to lose. " + controller.StatusSummary);

            UnityEngine.Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator BasicIdleAutoDefenseRuntimeUiToolkitPanelHasVisibleBoundsAndDamageNumbers()
        {
            GameObject host = new GameObject("idle-auto-defense-template-ui-toolkit-probe");
            var controller = host.AddComponent<RuntimeUiProbeController>();
            controller.enabled = false;

            UIDocument document = controller.RuntimeDocument;
            yield return null;

            Assert.NotNull(document);
            Assert.NotNull(document.panelSettings);
            Assert.NotNull(controller.Root);
            Assert.True(controller.RuntimeUiDocumentReady);
            Assert.True(controller.RuntimeUiThemeAssigned);
            Assert.True(controller.RuntimeUiDirectStylesApplied);
            Assert.AreEqual(32767, document.sortingOrder);
            Assert.AreEqual(32767, document.panelSettings.sortingOrder);
            Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, document.panelSettings.scaleMode);
            Assert.IsFalse(document.panelSettings.clearColor);
            Assert.IsFalse(document.panelSettings.clearDepthStencil);
            Assert.AreEqual(DisplayStyle.Flex, controller.Root.resolvedStyle.display);
            Assert.That(controller.RuntimeUiRootResolvedWidth, Is.GreaterThan(100f));
            Assert.That(controller.RuntimeUiRootResolvedHeight, Is.GreaterThan(100f));

            for (int i = 0; i < 720; i++)
            {
                controller.Step(1, 0.05f);
                if (controller.DamageNumberSpawnCount > 0 && controller.RuntimeDamageNumberVisibleCount > 0)
                    break;
                if (i % 15 == 0) yield return null;
            }

            Assert.That(controller.DamageNumberSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.RuntimeDamageNumberVisibleCount, Is.GreaterThan(0), controller.StatusSummary);

            UnityEngine.Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator PlayerExperienceBootsToMenuAndRoutesRunPauseBuildThemeAndDebugFlow()
        {
            string persistenceRoot = Path.Combine(Path.GetTempPath(), "IdleAutoDefensePlayerFlow", Guid.NewGuid().ToString("N"));
            GameContentSetAsset contentSet = CreatePlayerFlowContentSet(200);
            IdleAutoDefensePlayerExperienceAsset experience = IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            GameObject host = new GameObject("idle-auto-defense-player-flow");
            host.SetActive(false);
            var controller = host.AddComponent<PlayerExperienceProbeController>();
            controller.ContentSet = contentSet;
            controller.Experience = experience;
            controller.ConfigurePersistenceRoot(persistenceRoot);
            host.SetActive(true);
            yield return null;
            yield return null;

            Assert.That(controller.PlayerExperienceValid, Is.True, controller.PlayerFacingError);
            Assert.That(controller.UsingAuthoredCore, Is.True);
            Assert.That(controller.FallbackModeActive, Is.False);
            Assert.That(controller.MainMenuVisible, Is.True);
            Assert.That(controller.RunActive, Is.False);
            Assert.That(controller.NormalHudVisible, Is.False);
            Assert.That(controller.DebugUiVisible, Is.False);
            Assert.That(controller.SurvivalSeconds, Is.Zero, "Menu-first boot must not advance combat.");
            Assert.That(controller.PlayerUiButtonCount, Is.GreaterThanOrEqualTo(16));

            controller.PlayerProfile.LastSeenUtcTicks = DateTimeOffset.UnixEpoch.UtcTicks;
            controller.PlayerProfile.LastOfflineClaimUtcTicks = DateTimeOffset.UnixEpoch.UtcTicks;
            Assert.That(controller.RefreshOfflinePreview(DateTimeOffset.UnixEpoch.AddHours(1)), Is.True);
            Assert.That(controller.OfflineClaimVisible, Is.True);
            controller.ClaimOfflineReward();
            long claimedCredits = controller.PlayerProfile.LifetimeCredits;
            Assert.That(claimedCredits, Is.GreaterThan(0));
            controller.ClaimOfflineReward();
            Assert.That(controller.PlayerProfile.LifetimeCredits, Is.EqualTo(claimedCredits), "Offline claim must be idempotent.");

            controller.StartFreshRun();
            Assert.That(controller.TutorialVisible, Is.True, "First run should show the authored tutorial.");
            controller.CompleteTutorial();
            Assert.That(controller.PlayerFlowState, Is.EqualTo(IdleAutoDefensePlayerFlowState.Running));
            Assert.That(controller.NormalHudVisible, Is.True);
            Assert.That(controller.RunActive, Is.True);

            controller.TogglePause();
            Assert.That(controller.PauseMenuVisible, Is.True);
            Assert.That(controller.PlayerFlowState, Is.EqualTo(IdleAutoDefensePlayerFlowState.Paused));
            controller.OpenBuildView();
            Assert.That(controller.PauseMenuVisible, Is.True);
            controller.ResumeRun();
            Assert.That(controller.PlayerFlowState, Is.EqualTo(IdleAutoDefensePlayerFlowState.Running));

            controller.SelectTheme("theme.idle-auto-defense.neon-bastion");
            Assert.That(controller.ActiveThemeId, Is.EqualTo("theme.idle-auto-defense.neon-bastion"));
            Assert.That(controller.TryPurchasePersistentUpgradeFromUi("research.idle-auto-defense.core-plating"), Is.True);
            Assert.That(controller.GetPersistentResearchRank("research.idle-auto-defense.core-plating"), Is.EqualTo(1));
            controller.ToggleDebugUi();
            Assert.That(controller.DebugUiVisible, Is.True);
            controller.RestartCurrentRun();
            Assert.That(controller.DebugUiVisible, Is.False, "Restart must return debug UI to its hidden default.");
            Assert.That(controller.PlayerFlowState, Is.EqualTo(IdleAutoDefensePlayerFlowState.Running));
            controller.ReturnToMainMenu();
            Assert.That(controller.MainMenuVisible, Is.True);
            Assert.That(controller.RunActive, Is.False);

            UnityEngine.Object.Destroy(host);
            yield return null;

            GameObject restoredHost = new GameObject("idle-auto-defense-restored-player-flow");
            restoredHost.SetActive(false);
            var restored = restoredHost.AddComponent<PlayerExperienceProbeController>();
            restored.ContentSet = contentSet;
            restored.Experience = experience;
            restored.ConfigurePersistenceRoot(persistenceRoot);
            restoredHost.SetActive(true);
            yield return null;
            Assert.That(restored.PlayerProfile.TutorialSeen, Is.True);
            Assert.That(restored.ActiveThemeId, Is.EqualTo("theme.idle-auto-defense.neon-bastion"));
            Assert.That(restored.GetPersistentResearchRank("research.idle-auto-defense.core-plating"), Is.EqualTo(1));
            UnityEngine.Object.Destroy(restoredHost);
            yield return null;
            DestroyPlayerFlowContent(contentSet, experience);
            if (Directory.Exists(persistenceRoot)) Directory.Delete(persistenceRoot, true);
        }

        [UnityTest]
        public IEnumerator PlayerExperienceUsesAuthoredPurchasesRewardsOverdriveAndShowsTerminalSummary()
        {
            string persistenceRoot = Path.Combine(Path.GetTempPath(), "IdleAutoDefensePlayerSummary", Guid.NewGuid().ToString("N"));
            GameContentSetAsset contentSet = CreatePlayerFlowContentSet(500);
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
            IdleAutoDefensePlayerExperienceAsset experience = IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            GameObject host = new GameObject("idle-auto-defense-player-summary");
            host.SetActive(false);
            var controller = host.AddComponent<PlayerExperienceProbeController>();
            controller.ContentSet = contentSet;
            controller.Experience = experience;
            controller.ConfigurePersistenceRoot(persistenceRoot);
            host.SetActive(true);
            yield return null;
            controller.StartFreshRun();
            controller.CompleteTutorial();

            Assert.That(controller.PulseBeamUnlockCost, Is.EqualTo(contentSet.GameRules.GetModule(IdleAutoDefenseModuleRole.PrecisionBeam).BuildCost));
            Assert.That(controller.TryUseModuleAction(IdleAutoDefenseModuleRole.PrecisionBeam), Is.True);
            Assert.That(controller.PulseBeamUnlocked, Is.True);
            Assert.That(controller.RuntimeCurrencySpent, Is.EqualTo(controller.PulseBeamUnlockCost));
            Assert.That(controller.TryActivateOverdriveFromUi(), Is.True);
            Assert.That(controller.OverdriveActive, Is.True);

            controller.RequestRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
            Assert.That(controller.RewardDraftChoiceCount, Is.EqualTo(3));
            IdleAutoDefenseRewardDraftChoice selected = controller.RewardDraftChoices[0];
            int beforeRank = controller.GetRewardDraftChoiceCurrentRank(selected);
            Assert.That(controller.ChooseRewardCard(0), Is.True);
            Assert.That(controller.RewardDraftSelectionCount, Is.EqualTo(1));
            if (!selected.IsUnlock)
                Assert.That(controller.GetRewardDraftChoiceCurrentRank(selected), Is.GreaterThanOrEqualTo(beforeRank + 1));

            for (int i = 0; i < 24 && !controller.EncounterCompleted; i++)
                controller.Step(1, 0.05f);
            for (int i = 0; i < 10 && !controller.RunSummaryVisible; i++)
                yield return null;

            Assert.That(controller.EncounterCompleted, Is.True, controller.StatusSummary);
            Assert.That(controller.RunSummaryVisible, Is.True);
            Assert.That(controller.PlayerFlowState, Is.EqualTo(IdleAutoDefensePlayerFlowState.RunSummary));
            Assert.That(controller.PlayerProfile.CompletedRuns, Is.EqualTo(1));
            controller.RestartCurrentRun();
            Assert.That(controller.PlayerFlowState, Is.EqualTo(IdleAutoDefensePlayerFlowState.Running));
            Assert.That(controller.RunSummaryVisible, Is.False);

            UnityEngine.Object.Destroy(host);
            yield return null;
            DestroyPlayerFlowContent(contentSet, experience);
            if (Directory.Exists(persistenceRoot)) Directory.Delete(persistenceRoot, true);
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

        private sealed class RuntimeUiProbeController : IdleAutoDefenseTemplateController
        {
            public UIDocument RuntimeDocument => EnsureRuntimeUiDocument();
            public VisualElement Root => RuntimeUiRoot;
        }

        [UnityTest]
        public IEnumerator LegacyRunCommandImmediatelyFollowedByPlayerActionUsesCurrentState()
        {
            GameContentSetAsset contentSet = CreatePlayerFlowContentSet(2000);
            var experience = IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            string persistenceRoot = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Temp", "idle-composition-" + Guid.NewGuid().ToString("N"));
            var host = new GameObject("idle-composition-legacy-commands");
            host.SetActive(false);
            var controller = host.AddComponent<PlayerExperienceProbeController>();
            controller.ContentSet = contentSet;
            controller.Experience = experience;
            controller.ConfigurePersistenceRoot(persistenceRoot);
            try
            {
                host.SetActive(true);
                controller.enabled = false;
                yield return null;
                controller.StartFreshRun();
                controller.CompleteTutorial();
                Assert.That(controller.PulseBeamUnlocked, Is.False);
                Assert.That(controller.TryPurchasePulseBeamModule(), Is.True);
                int damageRank = controller.DamageUpgradeRank;
                Assert.That(controller.TryUseModuleAction(IdleAutoDefenseModuleRole.PrecisionBeam), Is.True,
                    "The player command must observe a module unlocked through the legacy public run API without waiting for Update.");
                Assert.That(controller.DamageUpgradeRank, Is.EqualTo(damageRank + 1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                DestroyPlayerFlowContent(contentSet, experience);
                if (Directory.Exists(persistenceRoot)) Directory.Delete(persistenceRoot, true);
            }
        }

        [UnityTest]
        public IEnumerator PlayerUiRemainsAttachedAcrossStartLegacyRestartAndReturnToMenu()
        {
            GameContentSetAsset contentSet = CreatePlayerFlowContentSet(2000);
            var experience = IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            string persistenceRoot = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Temp", "idle-ui-attachment-" + Guid.NewGuid().ToString("N"));
            var host = new GameObject("idle-ui-attachment");
            host.SetActive(false);
            var controller = host.AddComponent<PlayerExperienceProbeController>();
            controller.ContentSet = contentSet;
            controller.Experience = experience;
            controller.ConfigurePersistenceRoot(persistenceRoot);
            try
            {
                host.SetActive(true);
                yield return null;
                VisualElement menuRoot = controller.Root;
                VisualElement playerTree = menuRoot.Q<VisualElement>("idle-player-experience");
                Assert.That(playerTree, Is.Not.Null);
                controller.StartFreshRun();
                Assert.That(controller.Root, Is.Not.SameAs(menuRoot), "Starting rebuilds the run's Unity UI document.");
                Assert.That(playerTree.parent, Is.SameAs(controller.Root), "The existing player tree must move to the live document immediately.");
                Assert.That(controller.TutorialVisible, Is.True);
                controller.CompleteTutorial();
                yield return null;
                Assert.That(playerTree.panel, Is.Not.Null);
                Assert.That(playerTree.panel, Is.SameAs(controller.Root.panel));
                Assert.That(controller.Root.Q<VisualElement>("player-hud").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

                controller.RestartRun();
                yield return null;
                Assert.That(playerTree.parent, Is.SameAs(controller.Root), "External legacy restart must reattach on the next frame.");
                controller.ReturnToMainMenu();
                Assert.That(playerTree.parent, Is.SameAs(controller.Root));
                Assert.That(controller.MainMenuVisible, Is.True);
                Assert.That(controller.Root.Query<VisualElement>("idle-player-experience").ToList().Count, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                DestroyPlayerFlowContent(contentSet, experience);
                if (Directory.Exists(persistenceRoot)) Directory.Delete(persistenceRoot, true);
            }
        }

        [UnityTest]
        public IEnumerator ReplayedTutorialReturnsToVisiblePausedMenuUntilExplicitResume()
        {
            GameContentSetAsset contentSet = CreatePlayerFlowContentSet(2000);
            var experience = IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            string persistenceRoot = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Temp", "idle-tutorial-return-" + Guid.NewGuid().ToString("N"));
            var host = new GameObject("idle-tutorial-return");
            host.SetActive(false);
            var controller = host.AddComponent<PlayerExperienceProbeController>();
            controller.ContentSet = contentSet;
            controller.Experience = experience;
            controller.ConfigurePersistenceRoot(persistenceRoot);
            try
            {
                host.SetActive(true);
                yield return null;
                controller.StartFreshRun();
                controller.CompleteTutorial();
                Assert.That(controller.PlayerFlowState, Is.EqualTo(IdleAutoDefensePlayerFlowState.Running));
                for (int menu = 0; menu < 2; menu++)
                {
                    if (menu == 0) controller.TogglePause();
                    else controller.OpenSettings();
                    int pausedTicks = controller.SessionElapsedTicks;
                    controller.OpenTutorial();
                    Assert.That(controller.TutorialVisible, Is.True);
                    controller.CompleteTutorial();
                    yield return new WaitForSecondsRealtime(0.2f);
                    Assert.That(controller.TutorialVisible, Is.False);
                    Assert.That(controller.PlayerFlowState, Is.EqualTo(IdleAutoDefensePlayerFlowState.Paused));
                    Assert.That(controller.PauseMenuVisible, Is.True);
                    Assert.That(controller.Root.Q("pause-overlay").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                    Assert.That(controller.SessionElapsedTicks, Is.EqualTo(pausedTicks), "A visible paused menu must stop actual Update simulation.");
                    controller.ResumeRun();
                    yield return new WaitForSeconds(0.2f);
                    Assert.That(controller.PauseMenuVisible, Is.False);
                    Assert.That(controller.SessionElapsedTicks, Is.GreaterThan(pausedTicks));
                }
                controller.TogglePause();
                controller.RestartCurrentRun();
                Assert.That(controller.PlayerFlowState, Is.EqualTo(IdleAutoDefensePlayerFlowState.Running));
                Assert.That(controller.PauseMenuVisible, Is.False);
                Assert.That(controller.Root.Q("idle-player-experience").parent, Is.SameAs(controller.Root));
                controller.ReturnToMainMenu();
                controller.OpenTutorial();
                controller.CompleteTutorial();
                Assert.That(controller.MainMenuVisible, Is.True);
                Assert.That(controller.RunActive, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                DestroyPlayerFlowContent(contentSet, experience);
                if (Directory.Exists(persistenceRoot)) Directory.Delete(persistenceRoot, true);
            }
        }

        private sealed class PlayerExperienceProbeController : IdleAutoDefensePlayerExperienceController
        {
            public GameContentSetAsset ContentSet { get; set; }
            public IdleAutoDefensePlayerExperienceAsset Experience { get; set; }
            public VisualElement Root => RuntimeUiRoot;

            protected override void ConfigurePlayerExperienceBeforeBuild()
            {
                ConfigureContentPack(null, ContentSet);
                RequireAuthoredContentOnStartup();
                ConfigurePlayerExperience(Experience);
            }
        }

        private static GameContentSetAsset CreatePlayerFlowContentSet(int startingCredits)
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            WaveDefinitionAsset[] waves = BasicIdleAutoDefenseGame.CreateWaveDefinitions();
            RunUpgradeDefinitionAsset[] upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);
            return GameContentSetAsset.CreateTransient(
                "contentset.test.player-experience",
                "Player Experience Test",
                weapons[0],
                weapons,
                enemies,
                waves,
                upgrades,
                startingCredits,
                0,
                1f,
                1f,
                5600,
                false,
                "Player-facing flow fixture.",
                new[] { "test", "player-experience" });
        }

        private static void DestroyPlayerFlowContent(GameContentSetAsset contentSet, IdleAutoDefensePlayerExperienceAsset experience)
        {
            if (experience != null)
            {
                for (int i = 0; i < experience.Themes.Count; i++)
                    if (experience.Themes[i] != null) UnityEngine.Object.DestroyImmediate(experience.Themes[i]);
                if (experience.UiSettings != null) UnityEngine.Object.DestroyImmediate(experience.UiSettings);
                if (experience.Tutorial != null) UnityEngine.Object.DestroyImmediate(experience.Tutorial);
                if (experience.AudioPalette != null) UnityEngine.Object.DestroyImmediate(experience.AudioPalette);
                UnityEngine.Object.DestroyImmediate(experience);
            }
            if (contentSet != null) UnityEngine.Object.DestroyImmediate(contentSet);
        }
    }
}
