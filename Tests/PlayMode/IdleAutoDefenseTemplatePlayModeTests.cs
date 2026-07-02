using System;
using System.Collections;
using Deucarian.IdleProgression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense.PlayModeTests
{
    public sealed class IdleAutoDefenseTemplatePlayModeTests
    {
        [UnityTest]
        public IEnumerator BasicIdleAutoDefenseControllerRunsDeterministicSmoke()
        {
            GameObject host = new GameObject("idle-auto-defense-template-smoke");
            var controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            controller.enabled = false;

            for (int i = 0; i < 2200; i++)
            {
                BuyAvailableLivePurchases(controller);
                controller.Step(1, 0.05f);
                if (controller.EncounterCompleted || controller.EncounterFailed)
                    break;
                if (i % 30 == 0) yield return null;
            }

            Assert.That(controller.SpawnedCount, Is.GreaterThanOrEqualTo(4));
            Assert.That(controller.ProjectileLaunchCount, Is.GreaterThan(0));
            Assert.That(controller.ProjectileVisualSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ProjectileMotionObservedCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ProjectileDamageAppliedCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.DamageNumberSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.AttackVfxSpawnCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.AttackAudioPlayCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.EnemyPresentationEventCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.DirectOrCombatKillCount + controller.ProjectileAdapterKillCount, Is.GreaterThan(0));
            Assert.That(controller.SelectedUpgradeCount, Is.GreaterThanOrEqualTo(4));
            Assert.That(controller.RewardDraftOpenedCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.RewardDraftSelectionCount, Is.GreaterThan(0), controller.StatusSummary);
            Assert.That(controller.ModuleActivationCount, Is.GreaterThan(0));
            Assert.True(controller.PulseBeamUnlocked, "Smoke should unlock Pulse Beam.");
            Assert.True(controller.ArcBurstUnlocked, "Smoke should unlock Arc Burst.");
            Assert.True(controller.HomingPulseUnlocked, "Smoke should unlock Homing Pulse.");
            Assert.AreEqual(0, controller.DraftTickCount, "Sample upgrades should be explicit live purchases only.");
            Assert.True(controller.EncounterCompleted, "Assisted sample run should complete. " + controller.StatusSummary);
            Assert.That(controller.EncounterRewardCredits, Is.GreaterThanOrEqualTo(60));
            Assert.That(controller.EncounterRewardParts, Is.GreaterThanOrEqualTo(3));

            controller.RestartRun(BasicIdleAutoDefenseGame.CreateBossPulseEncounterDefinition());
            for (int i = 0; i < 720; i++)
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
        public IEnumerator BasicIdleAutoDefenseControllerCanFailWithoutLivePurchases()
        {
            GameObject host = new GameObject("idle-auto-defense-template-no-upgrade-smoke");
            var controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            controller.enabled = false;
            controller.RewardDraftPausesCombat = false;

            for (int i = 0; i < 2200; i++)
            {
                controller.Step(1, 0.05f);
                if (controller.EncounterCompleted || controller.EncounterFailed)
                    break;
                if (i % 30 == 0) yield return null;
            }

            Assert.AreEqual(0, controller.SelectedUpgradeCount);
            Assert.IsFalse(controller.PulseBeamUnlocked);
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

            for (int i = 0; i < 300; i++)
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

        private static void BuyAvailableLivePurchases(IdleAutoDefenseTemplateController controller)
        {
            if (controller.RewardDraftActive)
                controller.TryChooseRewardDraftChoice(0);
            if (controller.CanPurchasePulseBeamModule) controller.TryPurchasePulseBeamModule();
            if (controller.CanPurchaseArcBurstModule) controller.TryPurchaseArcBurstModule();
            if (controller.CanPurchaseHomingPulseModule) controller.TryPurchaseHomingPulseModule();

            if (!controller.PulseBeamUnlocked || !controller.ArcBurstUnlocked || !controller.HomingPulseUnlocked)
            {
                if (controller.ObjectiveHealth < controller.ObjectiveMaximumHealth * 0.55d && controller.CanPurchaseRepairUpgrade)
                    controller.TryPurchaseRepairUpgrade();
                return;
            }

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
    }
}
