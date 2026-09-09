using System;
using System.Linq;
using Deucarian.AutoDefense;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseRunLoopIntegrationTests
    {
        [Test]
        public void DraftPauseAdvancesSessionTicksUntilSelectionThenResumesAndRestartPreservesPausePolicy()
        {
            var host = new GameObject("idle-run-loop-integration-test");
            IdleAutoDefenseTemplateController controller = host.AddComponent<IdleAutoDefenseTemplateController>();
            try
            {
                controller.Build();
                controller.Step(1, 1f / 60f);
                Assert.That(controller.SpawnedCount, Is.GreaterThan(0), "Exercise an existing live enemy during the pause.");
                controller.RequestRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
                Assert.That(controller.RewardDraftActive, Is.True);
                Assert.That(controller.RewardDraftPausesCombat, Is.True);
                int ticksBefore = controller.SessionElapsedTicks;
                float survivalBefore = controller.SurvivalSeconds;
                int spawnedBefore = controller.SpawnedCount;
                int projectilesBefore = controller.ProjectileLaunchCount;
                AutoDefenseRuntimeSnapshot before = controller.Runtime.CreateSnapshot();

                controller.Step(7, 0.25f);

                Assert.That(controller.SessionElapsedTicks, Is.EqualTo(ticksBefore + 7));
                Assert.That(controller.SurvivalSeconds, Is.EqualTo(survivalBefore));
                Assert.That(controller.SpawnedCount, Is.EqualTo(spawnedBefore));
                Assert.That(controller.ProjectileLaunchCount, Is.EqualTo(projectilesBefore));
                AutoDefenseRuntimeSnapshot paused = controller.Runtime.CreateSnapshot();
                Assert.That(paused.ObjectiveHealth, Is.EqualTo(before.ObjectiveHealth));
                Assert.That(paused.Lives, Is.EqualTo(before.Lives));
                Assert.That(ObserveEnemies(paused), Is.EqualTo(ObserveEnemies(before)));

                Assert.That(controller.TryChooseRewardDraftChoice(0), Is.True);
                Assert.That(controller.RewardDraftActive, Is.False);
                controller.Step(2, 0.125f);

                Assert.That(controller.SessionElapsedTicks, Is.EqualTo(ticksBefore + 9));
                Assert.That(controller.SurvivalSeconds, Is.EqualTo(survivalBefore + 0.125f).Within(0.00001f));
                Assert.That(ObserveEnemies(controller.Runtime.CreateSnapshot()), Is.Not.EqualTo(ObserveEnemies(paused)),
                    "Enemy navigation or combat must resume on the first step after selection.");

                controller.RewardDraftPausesCombat = false;
                controller.RestartRun();

                Assert.That(controller.RewardDraftPausesCombat, Is.False);
                Assert.That(controller.SessionElapsedTicks, Is.Zero);
                Assert.That(controller.SurvivalSeconds, Is.Zero);
                Assert.That(controller.SpawnedCount, Is.Zero);
                Assert.That(controller.ProjectileLaunchCount, Is.Zero);
                Assert.That(controller.RewardDraftSelectionCount, Is.Zero);
                Assert.That(controller.EncounterRunning, Is.True);
            }
            finally { Object.DestroyImmediate(host); }
        }

        private static Tuple<long, Vector3, double, AutoDefenseEnemyLifecycle, float>[] ObserveEnemies(AutoDefenseRuntimeSnapshot snapshot)
            => snapshot.Enemies.OrderBy(enemy => enemy.Id).Select(enemy => Tuple.Create(
                enemy.Id, enemy.Position, enemy.Health, enemy.Lifecycle, enemy.ObjectiveProgress)).ToArray();
    }
}
