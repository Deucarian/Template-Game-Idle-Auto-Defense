using System;
using System.Collections;
using Deucarian.IdleProgression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

            for (int i = 0; i < 240; i++)
            {
                controller.Step(1, 0.05f);
                if (i % 30 == 0) yield return null;
            }

            Assert.That(controller.SpawnedCount, Is.GreaterThanOrEqualTo(4));
            Assert.That(controller.ProjectileLaunchCount, Is.GreaterThan(0));
            Assert.That(controller.DirectOrCombatKillCount + controller.ProjectileAdapterKillCount, Is.GreaterThan(0));
            Assert.That(controller.SelectedUpgradeCount, Is.GreaterThan(0));
            Assert.True(controller.EncounterCompleted || controller.EncounterFailed, "Template run should reach a terminal state in deterministic smoke.");

            controller.SimulateOfflineReward(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddHours(1));
            Assert.AreEqual(IdleProgressionResultCode.Success, controller.LastOfflineRewardCode);
            Assert.That(controller.OfflineRewardCredits, Is.GreaterThanOrEqualTo(900));
            Assert.That(controller.OfflineRewardParts, Is.GreaterThanOrEqualTo(12));

            Assert.That(controller.EncounterRewardCredits, Is.GreaterThanOrEqualTo(25));
            Assert.That(controller.EncounterRewardParts, Is.GreaterThanOrEqualTo(1));

            UnityEngine.Object.Destroy(host);
        }
    }
}
