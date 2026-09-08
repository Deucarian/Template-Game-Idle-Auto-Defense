using System;
using Deucarian.IdleProgression;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseCompositionTests
    {
        [Test]
        public void PlayerFlowTicksOnlyAnActiveUnobstructedRun()
        {
            var flow = new IdleAutoDefensePlayerFlow();
            var clock = new FakeRun();
            flow.Advance(clock, false, 0.2f);
            flow.StartRun();
            flow.Advance(clock, false, 0.2f);
            flow.Advance(clock, true, 0.3f);
            flow.State = IdleAutoDefensePlayerFlowState.Paused;
            flow.Advance(clock, false, 0.4f);
            flow.OpenTutorial(false);
            flow.Advance(clock, false, 0.5f);
            flow.CompleteTutorial();
            flow.Advance(clock, false, 0.6f);
            flow.TryRecordSummary();
            flow.Advance(clock, false, 0.7f);
            Assert.That(clock.TickCount, Is.EqualTo(2));
            Assert.That(clock.Elapsed, Is.EqualTo(0.8f).Within(0.00001f));
        }

        [Test]
        public void TerminalSummaryIsRecordedOnceAndFreshRunResetsTheGuard()
        {
            var flow = new IdleAutoDefensePlayerFlow();
            flow.StartRun();
            Assert.That(flow.TryRecordSummary(), Is.True);
            Assert.That(flow.TryRecordSummary(), Is.False);
            Assert.That(flow.RunActive, Is.False);
            Assert.That(flow.State, Is.EqualTo(IdleAutoDefensePlayerFlowState.RunSummary));
            flow.StartRun();
            Assert.That(flow.TryRecordSummary(), Is.True);
        }

        [Test]
        public void MenuTutorialDoesNotCreateAnActiveRun()
        {
            var flow = new IdleAutoDefensePlayerFlow();
            flow.OpenTutorial(false);
            flow.CompleteTutorial();
            Assert.That(flow.State, Is.EqualTo(IdleAutoDefensePlayerFlowState.MainMenu));
            Assert.That(flow.CanTick(false), Is.False);
        }

        [Test]
        public void ProfileOwnsOneLoadRestoreAndIdempotentStoreDisposal()
        {
            var storage = new FakeStorage();
            storage.Value.Progression.UnlockIds.Add("test.unlock");
            var run = new FakeRun();
            var profiles = new IdleAutoDefenseProfileSession(storage, run);
            profiles.Initialize();
            profiles.Initialize();
            Assert.That(storage.LoadCount, Is.EqualTo(1));
            Assert.That(run.RestoreCount, Is.EqualTo(1));
            var timestamp = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
            profiles.Persist(timestamp);
            Assert.That(storage.Value.LastSeenUtcTicks, Is.EqualTo(timestamp.UtcTicks));
            Assert.That(storage.Value.Progression, Is.SameAs(run.Progression));
            profiles.Dispose();
            profiles.Dispose();
            profiles.Persist(timestamp.AddHours(1));
            Assert.That(storage.DisposeCount, Is.EqualTo(1));
            Assert.That(storage.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void FailedRestoreAndSaveRemainVisibleWithoutReplacingTheProfile()
        {
            var storage = new FakeStorage { SaveResult = false };
            storage.Value.Progression.UnlockIds.Add("test.unlock");
            using (var profiles = new IdleAutoDefenseProfileSession(storage, new FakeRun { RestoreResult = false }))
            {
                profiles.Initialize();
                Assert.That(profiles.Error, Does.Contain("could not be restored"));
                Assert.That(profiles.Profile, Is.SameAs(storage.Value));
                profiles.Persist(DateTimeOffset.UtcNow);
                Assert.That(profiles.Error, Does.Contain("could not be saved"));
            }
        }

        [Test]
        public void AuthoredAudioUsesLiveVolumeAndThrottlesRepeatedEvents()
        {
            var palette = ScriptableObject.CreateInstance<IdleAutoDefenseAudioPaletteAsset>();
            var clip = AudioClip.Create("composition-test", 8, 1, 8000, false);
            var output = new FakeAudioOutput();
            float now = 1f;
            var profile = new IdleAutoDefensePlayerProfile { MasterVolume = 0.5f, UiVolume = 0.25f };
            palette.Configure("test.audio", "Test", new[] {
                new IdleAutoDefenseAudioEventRecord("ui.select", IdleAutoDefenseAudioCategory.Ui, clip, 0.8f, 0.5f) });
            try
            {
                var audio = new IdleAutoDefenseAudioPresenter(output, palette, () => profile, () => now);
                audio.Play("UI.SELECT");
                Assert.That(output.Volume, Is.EqualTo(0.1f).Within(0.00001f));
                now = 1.2f;
                audio.Play("ui.select");
                Assert.That(output.PlayCount, Is.EqualTo(1));
                profile.UiVolume = 0.5f;
                now = 1.5f;
                audio.Play("ui.select");
                Assert.That(output.Volume, Is.EqualTo(0.2f).Within(0.00001f));
                audio.Dispose();
                audio.Dispose();
                now = 3f;
                audio.Play("ui.select");
                Assert.That(output.PlayCount, Is.EqualTo(2));
                Assert.That(output.DisposeCount, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(palette); UnityEngine.Object.DestroyImmediate(clip); }
        }

        [Test]
        public void FailedInitializationReleasesTheOwnedStoreAndAudioOutput()
        {
            var storage = new FakeStorage { ThrowOnLoad = true };
            var run = new FakeRun();
            var output = new FakeAudioOutput();
            var experience = IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            try
            {
                Assert.Throws<InvalidOperationException>(() => new IdleAutoDefensePlayerExperience(
                    run, new IdleAutoDefenseProfileSession(storage, run), output, experience, new UnityEngine.UIElements.VisualElement()));
                Assert.That(storage.DisposeCount, Is.EqualTo(1));
                Assert.That(output.DisposeCount, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(experience); }
        }

        [Test]
        public void OriginalComponentRemainsSubstitutableWhileThePlayerApplicationDoesNotInheritGameplay()
        {
            Assert.That(typeof(IdleAutoDefenseTemplateController).IsAssignableFrom(typeof(IdleAutoDefensePlayerExperienceController)), Is.True);
            Assert.That(typeof(IdleAutoDefensePlayerExperience).BaseType, Is.EqualTo(typeof(object)));
        }

        private sealed class FakeAudioOutput : IIdleAutoDefenseAudioOutput
        {
            internal int PlayCount, DisposeCount;
            internal float Volume;
            public void Play(AudioClip clip, float volume) { PlayCount++; Volume = volume; }
            public void Dispose() => DisposeCount++;
        }

        private sealed class FakeStorage : IIdleAutoDefenseProfileStorage
        {
            internal IdleAutoDefensePlayerProfile Value = new IdleAutoDefensePlayerProfile();
            internal int LoadCount, SaveCount, DisposeCount;
            internal bool SaveResult = true;
            internal bool ThrowOnLoad;
            public string ProfileScopeId => "test.scope";
            public string ProfileDocumentName => "test.document";
            public IdleAutoDefensePlayerProfile Load()
            {
                LoadCount++;
                if (ThrowOnLoad) throw new InvalidOperationException("Expected test storage failure.");
                return Value;
            }
            public bool Save(IdleAutoDefensePlayerProfile value) { SaveCount++; Value = value; return SaveResult; }
            public bool Reset() => true;
            public void Dispose() => DisposeCount++;
        }

        private sealed class FakeRun : IIdleAutoDefenseRunSession
        {
            internal int TickCount, RestoreCount;
            internal float Elapsed;
            internal bool RestoreResult = true;
            internal readonly IdleAutoDefensePersistentProgressionData Progression = new IdleAutoDefensePersistentProgressionData();
            public IdleAutoDefenseRunSnapshot Snapshot => null;
            public void Refresh() { }
            public void Tick(float deltaSeconds) { TickCount++; Elapsed += deltaSeconds; }
            public void RestartRun() { }
            public bool RestorePersistentProgression(IdleAutoDefensePersistentProgressionData data) { RestoreCount++; return RestoreResult; }
            public void ResetPersistentProgression() { }
            public bool TryPurchaseOverdrive() => false;
            public bool TryPurchaseDamageUpgrade() => false;
            public bool TryPurchasePulseBeamModule() => false;
            public bool TryPurchaseRangeUpgrade() => false;
            public bool TryPurchaseArcBurstModule() => false;
            public bool TryPurchaseAttackSpeedUpgrade() => false;
            public bool TryPurchaseHomingPulseModule() => false;
            public bool TryPurchasePersistentUpgrade(string nodeId) => false;
            public bool TryChooseRewardDraftChoice(int choiceIndex) => false;
            public IdleProgressionResult SimulateOfflineReward(DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc) => null;
            public IdleAutoDefensePersistentProgressionData CapturePersistentProgression() => Progression;
        }
    }
}
