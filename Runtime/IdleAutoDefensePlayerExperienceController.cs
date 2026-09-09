using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Serialization and source compatibility facade; player behavior is composed in IdleAutoDefensePlayerExperience.</summary>
    public class IdleAutoDefensePlayerExperienceController : IdleAutoDefenseTemplateController
    {
        [SerializeField] private IdleAutoDefensePlayerExperienceAsset _playerExperience;

        private IdleAutoDefensePlayerExperience _experience;
        private string _persistenceRootOverride;

        protected virtual void ConfigurePlayerExperienceBeforeBuild() { }

        protected void ConfigurePlayerExperience(IdleAutoDefensePlayerExperienceAsset playerExperience)
        {
            _playerExperience = playerExperience;
        }

        public void ConfigurePersistenceRoot(string rootPath)
        {
            if (_experience != null) throw new InvalidOperationException("Persistence root must be configured before Awake.");
            _persistenceRootOverride = rootPath ?? string.Empty;
        }

        protected override void Awake()
        {
            ConfigurePlayerExperienceBeforeBuild();
            base.Awake();
            var run = new IdleAutoDefenseRunSession(this);
            var profiles = new IdleAutoDefenseProfileSession(
                new IdleAutoDefensePlayerProfileStore(_persistenceRootOverride, ActiveContentPackId), run);
            var output = new IdleAutoDefenseUnityAudioOutput(gameObject);
            try { _experience = new IdleAutoDefensePlayerExperience(run, profiles, output, _playerExperience, () => RuntimeUiRoot); }
            catch
            {
                try { profiles.Dispose(); } finally { output.Dispose(); }
                throw;
            }
        }

        protected override void Update() => _experience?.Tick(Time.deltaTime);
        private void LateUpdate() => _experience?.Present();
        private void OnApplicationPause(bool paused) { if (paused) _experience?.PersistProfile(DateTimeOffset.UtcNow); }
        protected override void OnApplicationQuit()
        {
            try { _experience?.Dispose(); } finally { base.OnApplicationQuit(); }
        }
        protected override void OnDestroy()
        {
            try { _experience?.Dispose(); } finally { base.OnDestroy(); }
        }

        public IdleAutoDefensePlayerFlowState PlayerFlowState => _experience == null ? IdleAutoDefensePlayerFlowState.MainMenu : _experience.PlayerFlowState;

        public IdleAutoDefensePlayerFlowState CurrentFlowState => _experience == null ? IdleAutoDefensePlayerFlowState.MainMenu : _experience.CurrentFlowState;

        public bool PlayerExperienceValid => _experience == null ? false : _experience.PlayerExperienceValid;

        public bool MainMenuVisible => _experience == null ? false : _experience.MainMenuVisible;

        public bool NormalHudVisible => _experience == null ? false : _experience.NormalHudVisible;

        public bool PauseMenuVisible => _experience == null ? false : _experience.PauseMenuVisible;

        public bool RewardDraftVisible => _experience == null ? false : _experience.RewardDraftVisible;

        public bool RunSummaryVisible => _experience == null ? false : _experience.RunSummaryVisible;

        public bool TutorialVisible => _experience == null ? false : _experience.TutorialVisible;

        public bool OfflineClaimVisible => _experience == null ? false : _experience.OfflineClaimVisible;

        public bool DebugUiVisible => _experience == null ? false : _experience.DebugUiVisible;

        public bool PortraitMessageVisible => _experience == null ? false : _experience.PortraitMessageVisible;

        public string ActiveThemeId => _experience == null ? string.Empty : _experience.ActiveThemeId;

        public bool ThemeFallbackVisible => _experience == null ? false : _experience.ThemeFallbackVisible;

        public string PlayerFacingError => _experience == null ? string.Empty : _experience.PlayerFacingError;

        public IdleAutoDefensePlayerProfile PlayerProfile => _experience == null ? null : _experience.PlayerProfile;

        public IdleAutoDefensePlayerExperienceAsset PlayerExperience => _playerExperience;

        public int TutorialStepIndex => _experience == null ? 0 : _experience.TutorialStepIndex;

        public int PlayerUiButtonCount => _experience == null ? 0 : _experience.PlayerUiButtonCount;

        public bool RunActive => _experience == null ? false : _experience.RunActive;

        public string PersistenceScopeId => _experience == null ? string.Empty : _experience.PersistenceScopeId;

        public string PersistenceDocumentName => _experience == null ? string.Empty : _experience.PersistenceDocumentName;

        public void StartFreshRun() { _experience.RefreshRun(); _experience.StartFreshRun(); }

        public void RestartCurrentRun() { _experience.RefreshRun(); _experience.StartFreshRun(); }

        public void ReturnToMainMenu() { _experience.RefreshRun(); _experience.ReturnToMainMenu(); }

        public void ShowMainMenu() { _experience.RefreshRun(); _experience.ShowMainMenu(); }

        public void TogglePause() { _experience.RefreshRun(); _experience.TogglePause(); }

        public void ResumeRun() { _experience.RefreshRun(); _experience.ResumeRun(); }

        public void OpenBuildView() { _experience.RefreshRun(); _experience.OpenBuildView(); }

        public void OpenSettings(bool fromMainMenu = false) { _experience.RefreshRun(); _experience.OpenSettings(fromMainMenu); }

        public void CloseSettingsOrHelp() { _experience.RefreshRun(); _experience.CloseSettingsOrHelp(); }

        public void OpenTutorial(bool firstRun = false) { _experience.RefreshRun(); _experience.OpenTutorial(firstRun); }

        public void AdvanceTutorial() { _experience.RefreshRun(); _experience.AdvanceTutorial(); }

        public void CompleteTutorial() { _experience.RefreshRun(); _experience.CompleteTutorial(); }

        public void ReplayTutorial() { _experience.RefreshRun(); _experience.ReplayTutorial(); }

        public void RequestResetProgress() { _experience.RefreshRun(); _experience.RequestResetProgress(); }

        public void CancelResetProgress() { _experience.RefreshRun(); _experience.CancelResetProgress(); }

        public void ConfirmResetProgress() { _experience.RefreshRun(); _experience.ConfirmResetProgress(); }

        public void SelectTheme(string themeId) { _experience.RefreshRun(); _experience.SelectTheme(themeId); }

        public void SetMasterVolume(float value) { _experience.RefreshRun(); _experience.SetMasterVolume(value); }

        public void SetUiVolume(float value) { _experience.RefreshRun(); _experience.SetUiVolume(value); }

        public void SetCombatVolume(float value) { _experience.RefreshRun(); _experience.SetCombatVolume(value); }

        public void SetRewardWarningVolume(float value) { _experience.RefreshRun(); _experience.SetRewardWarningVolume(value); }

        public void SetMotionIntensity(float value) { _experience.RefreshRun(); _experience.SetMotionIntensity(value); }

        public void ToggleDebugUi() { _experience.RefreshRun(); _experience.ToggleDebugUi(); }

        public bool TryActivateOverdriveFromUi() { _experience.RefreshRun(); return _experience.TryActivateOverdriveFromUi(); }

        public bool TryUseModuleAction(IdleAutoDefenseModuleRole role) { _experience.RefreshRun(); return _experience.TryUseModuleAction(role); }

        public bool TryPurchasePersistentUpgradeFromUi(string nodeId) { _experience.RefreshRun(); return _experience.TryPurchasePersistentUpgradeFromUi(nodeId); }

        public bool ChooseRewardCard(int index) { _experience.RefreshRun(); return _experience.ChooseRewardCard(index); }

        public bool HasOfflineRewardPreview => _experience == null ? false : _experience.HasOfflineRewardPreview;

        public bool RefreshOfflinePreview(DateTimeOffset nowUtc) { _experience.RefreshRun(); return _experience.RefreshOfflinePreview(nowUtc); }

        public void OpenOfflineClaim() { _experience.RefreshRun(); _experience.OpenOfflineClaim(); }

        public void ClaimOfflineReward() { _experience.RefreshRun(); _experience.ClaimOfflineReward(); }

        public void ShowRunSummary() { _experience.RefreshRun(); _experience.ShowRunSummary(); }

        public static Vector4 CalculateSafeAreaInsets(Rect screen, Rect safeArea) => IdleAutoDefensePlayerView.CalculateSafeAreaInsets(screen, safeArea);

        public static bool ShouldUseCompactLayout(float width, float height, IdleAutoDefenseUiSettingsAsset settings) => IdleAutoDefensePlayerView.ShouldUseCompactLayout(width, height, settings);

        public static bool ShouldShowPortraitMessage(float width, float height, IdleAutoDefenseUiSettingsAsset settings) => IdleAutoDefensePlayerView.ShouldShowPortraitMessage(width, height, settings);
    }
}
