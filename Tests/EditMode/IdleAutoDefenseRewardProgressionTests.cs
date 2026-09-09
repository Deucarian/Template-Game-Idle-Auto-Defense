using NUnit.Framework;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseRewardProgressionTests
    {
        [Test]
        public void DefeatIdentityIsRewardedOnceAndQueuedDraftOpensAfterSelection()
        {
            var wallet = new IdleAutoDefenseRunWallet();
            var effects = new IdleAutoDefenseBuildEffects(() => true, () => false, (_, __) => { }, _ => { },
                (_, __, ___) => { }, (_, __, ___) => { });
            var build = new IdleAutoDefenseRunBuild(() => null, () => null, wallet, effects);
            int creditFeedback = 0;
            int choices = 0;
            var rewards = new IdleAutoDefenseRewardProgression(build, IdleAutoDefenseRewardDraftSettings.CreateDefault,
                IdleAutoDefenseRewardDraftCatalog.CreateDefault, () => 12f, id => id,
                _ => choices++, _ => creditFeedback++, () => { });
            rewards.RecordEnemyDefeated(7, 17, false, false, Vector3.zero);
            rewards.RecordEnemyDefeated(7, 17, false, false, Vector3.zero);
            Assert.That(rewards.ConsumeKillCredits(), Is.EqualTo(17));
            Assert.That(rewards.ConsumeKillCredits(), Is.Zero);
            Assert.That(creditFeedback, Is.EqualTo(1));
            Assert.That(rewards.CommanderExperience, Is.EqualTo(IdleAutoDefenseRewardDraftSettings.CreateDefault().NormalEnemyExperience));
            rewards.RequestRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
            rewards.RequestRewardDraft(IdleAutoDefenseRewardDraftKind.BossDefeated);
            Assert.That(rewards.RewardDraftOpenedCount, Is.EqualTo(1));
            Assert.That(rewards.TryChooseRewardDraftHotkey(0), Is.False);
            Assert.That(rewards.TryChooseRewardDraftHotkey(1), Is.True);
            Assert.That(rewards.RewardDraftOpenedCount, Is.EqualTo(2));
            Assert.That(rewards.ActiveRewardDraftKindName, Is.EqualTo("BossDefeated"));
            Assert.That(rewards.FirstRewardDraftSeconds, Is.EqualTo(12f));
            Assert.That(rewards.RewardDraftSelectionCount, Is.EqualTo(1));
            Assert.That(build.SelectedUpgradeCount, Is.EqualTo(1));
            Assert.That(choices, Is.EqualTo(1));
        }

        [Test]
        public void StarterOfferIsOncePerRunAndResetClearsQueuesRanksAndDefeatIdentity()
        {
            var wallet = new IdleAutoDefenseRunWallet();
            var effects = new IdleAutoDefenseBuildEffects(() => true, () => false, (_, __) => { }, _ => { },
                (_, __, ___) => { }, (_, __, ___) => { });
            var build = new IdleAutoDefenseRunBuild(() => null, () => null, wallet, effects);
            float time = 29f;
            int readyFeedback = 0;
            var rewards = new IdleAutoDefenseRewardProgression(build, IdleAutoDefenseRewardDraftSettings.CreateDefault,
                IdleAutoDefenseRewardDraftCatalog.CreateDefault, () => time, id => id, _ => { }, _ => { }, () => readyFeedback++);
            rewards.OfferFirstRewardDraftIfReady(30f);
            Assert.That(rewards.RewardDraftActive, Is.False);
            time = 30f;
            rewards.OfferFirstRewardDraftIfReady(30f);
            rewards.OfferFirstRewardDraftIfReady(30f);
            Assert.That(readyFeedback, Is.EqualTo(1));
            rewards.RecordEnemyDefeated(1, 9, false, false, Vector3.zero);
            rewards.Reset();
            Assert.That(rewards.RewardDraftActive, Is.False);
            Assert.That(rewards.FirstRewardDraftSeconds, Is.EqualTo(-1f));
            Assert.That(rewards.CommanderLevel, Is.EqualTo(1));
            Assert.That(rewards.CommanderExperience, Is.Zero);
            rewards.RecordEnemyDefeated(1, 9, false, false, Vector3.zero);
            Assert.That(rewards.ConsumeKillCredits(), Is.EqualTo(9));
            rewards.OfferFirstRewardDraftIfReady(30f);
            Assert.That(readyFeedback, Is.EqualTo(2));
        }
    }
}
