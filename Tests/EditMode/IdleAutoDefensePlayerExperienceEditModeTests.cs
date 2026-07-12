using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefensePlayerExperienceEditModeTests
    {
        [Test]
        public void AuthoredPlayerExperienceHasUniqueThemesAudioEventsTutorialAndMobileSettings()
        {
            IdleAutoDefensePlayerExperienceAsset experience = IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            try
            {
                Assert.That(experience.Validate(), Is.Empty);
                Assert.That(experience.Themes.Count, Is.EqualTo(2));
                Assert.That(experience.Themes.Select(theme => theme.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(2));
                Assert.That(experience.Themes.All(theme => theme.IsValid(out _)), Is.True);
                Assert.That(experience.AudioPalette.IsValid(out string audioIssue), Is.True, audioIssue);
                Assert.That(experience.AudioPalette.Events.Count, Is.EqualTo(IdleAutoDefenseAudioPaletteAsset.RequiredEventIds.Length));
                Assert.That(experience.AudioPalette.Events.Select(value => value.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(IdleAutoDefenseAudioPaletteAsset.RequiredEventIds.Length));
                Assert.That(experience.Tutorial.IsValid(out string tutorialIssue), Is.True, tutorialIssue);
                Assert.That(experience.Tutorial.Steps.Count, Is.EqualTo(10));
                Assert.That(experience.Tutorial.Steps.Select(value => value.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(10));
                Assert.That(experience.UiSettings.IsValid(out string uiIssue), Is.True, uiIssue);
                Assert.That(experience.UiSettings.ModuleTokens.Count, Is.EqualTo(4));
                Assert.That(experience.UiSettings.MinimumTouchTarget, Is.GreaterThanOrEqualTo(44f));
                Assert.That(experience.UiSettings.RespectSafeArea, Is.True);
            }
            finally
            {
                DestroyTransientExperience(experience);
            }
        }

        [Test]
        public void MissingThemeSelectionFallsBackToAuthoredDefaultWithoutAffectingGameplayContent()
        {
            IdleAutoDefensePlayerExperienceAsset experience = IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            try
            {
                IdleAutoDefenseThemeAsset theme = experience.ResolveTheme("theme.missing", out bool usedFallback);
                Assert.That(usedFallback, Is.True);
                Assert.That(theme, Is.Not.Null);
                Assert.That(theme.Id, Is.EqualTo(experience.DefaultThemeId));
                Assert.That(theme.IsValid(out string issue), Is.True, issue);
            }
            finally
            {
                DestroyTransientExperience(experience);
            }
        }

        [TestCase(1920, 1080, false)]
        [TestCase(1280, 720, false)]
        [TestCase(960, 540, true)]
        [TestCase(844, 390, true)]
        [TestCase(1024, 768, false)]
        public void LandscapeTargetsUseSafeAreaAndResponsiveLayoutPolicy(int width, int height, bool expectedCompact)
        {
            IdleAutoDefensePlayerExperienceAsset experience = IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            try
            {
                Rect screen = new Rect(0, 0, width, height);
                Rect safe = new Rect(24, 10, width - 48, height - 20);
                Vector4 insets = IdleAutoDefensePlayerExperienceController.CalculateSafeAreaInsets(screen, safe);
                Assert.That(insets, Is.EqualTo(new Vector4(24, 10, 24, 10)));
                Assert.That(width, Is.GreaterThan(height));
                Assert.That(
                    IdleAutoDefensePlayerExperienceController.ShouldUseCompactLayout(width, height, experience.UiSettings),
                    Is.EqualTo(expectedCompact));
                Assert.That(
                    IdleAutoDefensePlayerExperienceController.ShouldShowPortraitMessage(width, height, experience.UiSettings),
                    Is.False);
            }
            finally
            {
                DestroyTransientExperience(experience);
            }
        }

        [Test]
        public void PortraitTargetShowsRotateDeviceMessage()
        {
            IdleAutoDefensePlayerExperienceAsset experience = IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            try
            {
                Assert.That(
                    IdleAutoDefensePlayerExperienceController.ShouldShowPortraitMessage(390, 844, experience.UiSettings),
                    Is.True);
            }
            finally
            {
                DestroyTransientExperience(experience);
            }
        }

        [Test]
        public void PlayerProfilePersistsSettingsRecoversBackupAndResetsToDefaults()
        {
            string root = Path.Combine(Path.GetTempPath(), "IdleAutoDefenseProfileTests", Guid.NewGuid().ToString("N"));
            try
            {
                var first = new IdleAutoDefensePlayerProfileStore(root);
                IdleAutoDefensePlayerProfile profile = IdleAutoDefensePlayerProfile.CreateDefault();
                profile.MasterVolume = 0.4f;
                profile.SelectedThemeId = "theme.idle-auto-defense.neon-bastion";
                profile.TutorialSeen = true;
                profile.LifetimeCredits = 123;
                Assert.That(first.Save(profile), Is.True, first.LastStatus);
                profile.MasterVolume = 0.55f;
                profile.LifetimeCredits = 456;
                Assert.That(first.Save(profile), Is.True, first.LastStatus);
                first.Dispose();

                string primary = Path.Combine(root, IdleAutoDefensePlayerProfileStore.DocumentName + "__default.json");
                File.WriteAllText(primary, "{ corrupted");
                var recoveredStore = new IdleAutoDefensePlayerProfileStore(root);
                IdleAutoDefensePlayerProfile recovered = recoveredStore.Load();
                Assert.That(recoveredStore.RecoveredFromBackup, Is.True, recoveredStore.LastStatus);
                Assert.That(recovered.MasterVolume, Is.EqualTo(0.4f));
                Assert.That(recovered.SelectedThemeId, Is.EqualTo("theme.idle-auto-defense.neon-bastion"));
                Assert.That(recoveredStore.Reset(), Is.True, recoveredStore.LastStatus);
                recoveredStore.Dispose();

                var resetStore = new IdleAutoDefensePlayerProfileStore(root);
                IdleAutoDefensePlayerProfile reset = resetStore.Load();
                Assert.That(reset.TutorialSeen, Is.False);
                Assert.That(reset.LifetimeCredits, Is.Zero);
                Assert.That(reset.MasterVolume, Is.EqualTo(1f));
                resetStore.Dispose();
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static void DestroyTransientExperience(IdleAutoDefensePlayerExperienceAsset experience)
        {
            if (experience == null) return;
            for (int i = 0; i < experience.Themes.Count; i++)
                if (experience.Themes[i] != null) UnityEngine.Object.DestroyImmediate(experience.Themes[i]);
            if (experience.UiSettings != null) UnityEngine.Object.DestroyImmediate(experience.UiSettings);
            if (experience.Tutorial != null) UnityEngine.Object.DestroyImmediate(experience.Tutorial);
            if (experience.AudioPalette != null) UnityEngine.Object.DestroyImmediate(experience.AudioPalette);
            UnityEngine.Object.DestroyImmediate(experience);
        }
    }
}
