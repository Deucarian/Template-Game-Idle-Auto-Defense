using System;
using Deucarian.AutoDefense;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefensePersistentProgressionTests
    {
        [Test]
        public void RepeatedOfflineClaimAndRunRebindingPreserveOnePersistentBalance()
        {
            WithProgression((owner, progression, economy, offline) =>
            {
                var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
                owner.SimulateOfflineReward(now.AddMinutes(-30), now, 0.25d);
                long credits = owner.OfflineRewardCredits;
                Assert.That(credits, Is.GreaterThan(0));
                owner.SimulateOfflineReward(now.AddMinutes(-30), now, 0.25d);
                Assert.That(owner.OfflineRewardCredits, Is.EqualTo(credits));
                owner.ResetRunRewards();
                owner.Bind(progression, economy, offline, null, false);
                Assert.That(owner.GetPersistentCurrencyBalance(economy.PrimaryCurrencyId), Is.EqualTo(credits));
                owner.ApplyEncounterRewardIfTerminal(AutoDefenseRuntimeState.Completed, 1, 0d);
                long afterRun = owner.EncounterRewardCredits;
                Assert.That(afterRun, Is.GreaterThan(credits));
                owner.ApplyEncounterRewardIfTerminal(AutoDefenseRuntimeState.Completed, 1, 0d);
                Assert.That(owner.EncounterRewardCredits, Is.EqualTo(afterRun));
                owner.ResetRunRewards();
                owner.ApplyEncounterRewardIfTerminal(AutoDefenseRuntimeState.Completed, 2, 0d);
                Assert.That(owner.EncounterRewardCredits, Is.GreaterThan(afterRun));
            });
        }

        [Test]
        public void FailedResearchRestoreLeavesExistingStateIntactAndValidCaptureRoundTrips()
        {
            WithProgression((owner, progression, economy, offline) =>
            {
                owner.ApplyEncounterRewardIfTerminal(AutoDefenseRuntimeState.Completed, 1, 0d);
                long credits = owner.GetPersistentCurrencyBalance(economy.PrimaryCurrencyId);
                IdleAutoDefensePersistentProgressionData saved = owner.CapturePersistentProgression();
                var invalid = new IdleAutoDefensePersistentProgressionData();
                invalid.ResearchRanks.Add(new IdleAutoDefensePersistentIntValue { Id = "missing.research", Value = 1 });
                Assert.That(owner.RestorePersistentProgression(invalid), Is.False);
                Assert.That(owner.GetPersistentCurrencyBalance(economy.PrimaryCurrencyId), Is.EqualTo(credits));
                var restored = new IdleAutoDefensePersistentProgression(_ => { });
                restored.Bind(progression, economy, offline, null, false);
                Assert.That(restored.RestorePersistentProgression(saved), Is.True);
                Assert.That(restored.GetPersistentCurrencyBalance(economy.PrimaryCurrencyId), Is.EqualTo(credits));
                Assert.That(restored.CapturePersistentProgression().Tracks.Count, Is.EqualTo(saved.Tracks.Count));
            });
        }

        private static void WithProgression(Action<IdleAutoDefensePersistentProgression, IdleAutoDefenseProgressionAsset,
            IdleAutoDefenseEconomyAsset, IdleAutoDefenseOfflineProgressionAsset> action)
        {
            var progression = IdleAutoDefenseProgressionAsset.CreateTransient();
            var economy = IdleAutoDefenseEconomyAsset.CreateTransient(10, 0);
            var offline = IdleAutoDefenseOfflineProgressionAsset.CreateTransient();
            try
            {
                var owner = new IdleAutoDefensePersistentProgression(_ => { });
                owner.Bind(progression, economy, offline, null, false);
                action(owner, progression, economy, offline);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(progression);
                UnityEngine.Object.DestroyImmediate(economy);
                UnityEngine.Object.DestroyImmediate(offline);
            }
        }
    }
}
