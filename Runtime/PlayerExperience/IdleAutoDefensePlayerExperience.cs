using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefensePlayerExperience : IDisposable
    {
        private readonly IIdleAutoDefenseRunSession _run;
        private readonly IdleAutoDefenseProfileSession _profiles;
        private readonly IdleAutoDefensePlayerFlow _flow = new IdleAutoDefensePlayerFlow();
        private readonly IdleAutoDefenseAudioPresenter _audio;
        private readonly IdleAutoDefensePlayerInput _input;
        private readonly IdleAutoDefensePlayerExperienceAsset _assignedExperience;
        private readonly IdleAutoDefensePlayerView _view;
        private bool _disposed;
        internal IdleAutoDefenseRunSnapshot RunState => _run.Snapshot;
        internal void RefreshRun() => _run.Refresh();
        public IdleAutoDefensePlayerFlowState CurrentFlowState { get => _flow.State; private set => _flow.State = value; }
        internal IdleAutoDefensePlayerProfile _profile => _profiles.Profile;
        internal IdleProgressionResult _offlinePreview => _profiles.OfflinePreview;
        internal int _tutorialStepIndex => _flow.TutorialStepIndex;
        internal IdleAutoDefensePlayerExperience(
            IIdleAutoDefenseRunSession run, IdleAutoDefenseProfileSession profiles, IIdleAutoDefenseAudioOutput output,
            IdleAutoDefensePlayerExperienceAsset assignedExperience, VisualElement runtimeRoot)
        {
            _run = run;
            _profiles = profiles;
            _assignedExperience = assignedExperience;
            PlayerExperienceValid = assignedExperience != null && assignedExperience.Validate().Count == 0;
            _effectiveExperience = PlayerExperienceValid ? assignedExperience : IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            if (!PlayerExperienceValid)
                _playerFacingError = assignedExperience == null
                    ? "Player experience content is missing. Regenerate the scene from the Idle Auto Defense setup wizard."
                    : "Player experience content is invalid. Open the named content pack validation for details.";
            try
            {
                _profiles.Initialize();
                if (!string.IsNullOrEmpty(_profiles.Error)) _playerFacingError = _profiles.Error;
                ResolveActiveTheme(_profiles.Profile.SelectedThemeId);
                _audio = new IdleAutoDefenseAudioPresenter(output, _effectiveExperience.AudioPalette, () => _profiles.Profile);
                _input = new IdleAutoDefensePlayerInput(this);
                _view = new IdleAutoDefensePlayerView(this, runtimeRoot);
                _view.BuildPlayerUi();
                _profiles.PrepareOfflinePreview(DateTimeOffset.UtcNow);
                ShowMainMenu();
                if (HasOfflineRewardPreview) OpenOfflineClaim();
            }
            catch
            {
                try { _view?.Dispose(); }
                finally { try { _profiles.Dispose(); } finally { output.Dispose(); } }
                throw;
            }
        }
        internal void Tick(float deltaSeconds)
        {
            if (_disposed) return;
            _run.Refresh();
            _input.Handle();
            _flow.Advance(_run, PortraitMessageVisible, deltaSeconds);
        }
        internal void Present()
        {
            if (_disposed) return;
            _run.Refresh();
            _view.Hud.RefreshPlayerUi();
            _audio.Observe(RunState);
            if (_flow.RunActive && !_flow.SummaryRecorded && (RunState.EncounterCompleted || RunState.EncounterFailed)) ShowRunSummary();
        }
        internal void PersistProfile(DateTimeOffset nowUtc)
        {
            _profiles.Persist(nowUtc);
            if (!string.IsNullOrEmpty(_profiles.Error)) _playerFacingError = _profiles.Error;
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { PersistProfile(DateTimeOffset.UtcNow); }
            finally
            {
                try { _profiles.Dispose(); }
                finally { try { _audio.Dispose(); } finally { _view.Dispose(); } }
            }
        }

        internal IdleAutoDefensePlayerExperienceAsset _effectiveExperience;

        internal IdleAutoDefenseThemeAsset _activeTheme;

        internal string _playerFacingError = string.Empty;

        internal string _toastText = string.Empty;

        internal float _toastUntil;

        internal bool _debugVisible;

        internal bool _themeFallbackVisible;

        public IdleAutoDefensePlayerFlowState PlayerFlowState => CurrentFlowState;

        public bool PlayerExperienceValid { get; private set; }

        public bool MainMenuVisible => IdleAutoDefensePlayerView.IsVisible(_view._mainMenuOverlay);

        public bool NormalHudVisible => IdleAutoDefensePlayerView.IsVisible(_view._hudLayer);

        public bool PauseMenuVisible => IdleAutoDefensePlayerView.IsVisible(_view._pauseOverlay);

        public bool RewardDraftVisible => IdleAutoDefensePlayerView.IsVisible(_view._rewardOverlay);

        public bool RunSummaryVisible => IdleAutoDefensePlayerView.IsVisible(_view._summaryOverlay);

        public bool TutorialVisible => IdleAutoDefensePlayerView.IsVisible(_view._tutorialOverlay);

        public bool OfflineClaimVisible => IdleAutoDefensePlayerView.IsVisible(_view._offlineOverlay);

        public bool DebugUiVisible => IdleAutoDefensePlayerView.IsVisible(_view._debugPanel);

        public bool PortraitMessageVisible => IdleAutoDefensePlayerView.IsVisible(_view._portraitOverlay);

        public string ActiveThemeId => _activeTheme == null ? string.Empty : _activeTheme.Id;

        public bool ThemeFallbackVisible => _themeFallbackVisible;

        public string PlayerFacingError => _playerFacingError;

        public IdleAutoDefensePlayerProfile PlayerProfile => _profiles.Profile;

        public IdleAutoDefensePlayerExperienceAsset PlayerExperience => _assignedExperience;

        public int TutorialStepIndex => _flow.TutorialStepIndex;

        public int PlayerUiButtonCount => _view._playerUiRoot == null ? 0 : _view._playerUiRoot.Query<Button>().ToList().Count;

        public bool RunActive => _flow.RunActive;

        public string PersistenceScopeId => _profiles.ProfileScopeId;

        public string PersistenceDocumentName => _profiles.ProfileDocumentName;

        public void StartFreshRun()
        {
            if (!PlayerExperienceValid || RunState.StartupBlocked)
            {
                ShowToast(string.IsNullOrWhiteSpace(_playerFacingError) ? RunState.StartupError : _playerFacingError, true);
                _audio.Play("ui.error");
                return;
            }

            _run.RestartRun();
            _flow.StartRun();
            _debugVisible = false;
            _audio.Reset();
            _view.HideAllModalOverlays();
            IdleAutoDefensePlayerView.SetVisible(_view._hudLayer, true);
            IdleAutoDefensePlayerView.SetVisible(_view._moduleBar, true);
            IdleAutoDefensePlayerView.SetVisible(_view._debugPanel, false);
            _audio.Play("ui.select");
            if (!_profiles.Profile.TutorialSeen)
                OpenTutorial(true);
        }

        public void RestartCurrentRun()
        {
            StartFreshRun();
        }

        public void ReturnToMainMenu()
        {
            if (_flow.RunActive) _run.RestartRun();
            _flow.ReturnToMenu();
            _debugVisible = false;
            ShowMainMenu();
            PersistProfile(DateTimeOffset.UtcNow);
            _audio.Play("ui.back");
        }

        public void ShowMainMenu()
        {
            _flow.ShowMenu();
            _view.HideAllModalOverlays();
            IdleAutoDefensePlayerView.SetVisible(_view._mainMenuOverlay, true);
            IdleAutoDefensePlayerView.SetVisible(_view._hudLayer, false);
            IdleAutoDefensePlayerView.SetVisible(_view._moduleBar, false);
            IdleAutoDefensePlayerView.SetVisible(_view._debugPanel, false);
            _view.Menu.RefreshMainMenu();
        }

        public void TogglePause()
        {
            if (!_flow.RunActive || RunState.RewardDraftActive || TutorialVisible || RunSummaryVisible) return;
            if (CurrentFlowState == IdleAutoDefensePlayerFlowState.Paused)
            {
                ResumeRun();
                return;
            }
            CurrentFlowState = IdleAutoDefensePlayerFlowState.Paused;
            _flow.BeforeModal = CurrentFlowState;
            IdleAutoDefensePlayerView.SetVisible(_view._pauseOverlay, true);
            _view.Menu.ShowPauseSection(IdleAutoDefensePauseSection.Main);
            _audio.Play("ui.select");
        }

        public void ResumeRun()
        {
            if (!_flow.RunActive) return;
            CurrentFlowState = IdleAutoDefensePlayerFlowState.Running;
            _flow.BeforeModal = CurrentFlowState;
            IdleAutoDefensePlayerView.SetVisible(_view._pauseOverlay, false);
            _audio.Play("ui.back");
        }

        public void OpenBuildView()
        {
            if (!_flow.RunActive || RunState.RewardDraftActive) return;
            CurrentFlowState = IdleAutoDefensePlayerFlowState.Paused;
            _flow.BeforeModal = CurrentFlowState;
            IdleAutoDefensePlayerView.SetVisible(_view._pauseOverlay, true);
            _view.Menu.ShowPauseSection(IdleAutoDefensePauseSection.Build);
            _audio.Play("ui.select");
        }

        public void OpenSettings(bool fromMainMenu = false)
        {
            _flow.BeforeModal = fromMainMenu ? IdleAutoDefensePlayerFlowState.MainMenu : CurrentFlowState;
            if (!fromMainMenu) CurrentFlowState = IdleAutoDefensePlayerFlowState.Paused;
            IdleAutoDefensePlayerView.SetVisible(_view._pauseOverlay, true);
            _view.Menu.ShowPauseSection(IdleAutoDefensePauseSection.Settings);
            _audio.Play("ui.select");
        }

        public void CloseSettingsOrHelp()
        {
            if (_flow.BeforeModal == IdleAutoDefensePlayerFlowState.MainMenu)
            {
                ShowMainMenu();
                return;
            }
            if (_flow.RunActive)
            {
                CurrentFlowState = IdleAutoDefensePlayerFlowState.Paused;
                IdleAutoDefensePlayerView.SetVisible(_view._pauseOverlay, true);
                _view.Menu.ShowPauseSection(IdleAutoDefensePauseSection.Main);
            }
        }

        public void OpenTutorial(bool firstRun = false)
        {
            if (_effectiveExperience.Tutorial == null) return;
            _flow.OpenTutorial(firstRun);
            IdleAutoDefensePlayerView.SetVisible(_view._tutorialOverlay, true);
            _view.Modal.RefreshTutorial();
            _audio.Play("ui.select");
        }

        public void AdvanceTutorial()
        {
            if (_effectiveExperience.Tutorial == null) return;
            _flow.TutorialStepIndex++;
            if (_flow.TutorialStepIndex >= _effectiveExperience.Tutorial.Steps.Count)
            {
                CompleteTutorial();
                return;
            }
            _view.Modal.RefreshTutorial();
            _audio.Play("ui.select");
        }

        public void CompleteTutorial()
        {
            _profiles.Profile.TutorialSeen = true;
            PersistProfile(DateTimeOffset.UtcNow);
            IdleAutoDefensePlayerView.SetVisible(_view._tutorialOverlay, false);
            _flow.CompleteTutorial();
            if (!_flow.RunActive) ShowMainMenu();
            _audio.Play("ui.back");
        }

        public void ReplayTutorial()
        {
            OpenTutorial(false);
        }

        public void RequestResetProgress()
        {
            IdleAutoDefensePlayerView.SetVisible(_view._resetConfirmationOverlay, true);
            _audio.Play("ui.select");
        }

        public void CancelResetProgress()
        {
            IdleAutoDefensePlayerView.SetVisible(_view._resetConfirmationOverlay, false);
            _audio.Play("ui.back");
        }

        public void ConfirmResetProgress()
        {
            bool reset = _profiles.Reset();
            _run.ResetPersistentProgression();
            _run.RestartRun();
            ResolveActiveTheme(_effectiveExperience.DefaultThemeId);
            IdleAutoDefensePlayerView.SetVisible(_view._resetConfirmationOverlay, false);
            PersistProfile(DateTimeOffset.UtcNow);
            _view.Menu.RefreshSettingsControls();
            ShowToast(reset ? "Progress reset." : "Progress reset locally; persistent storage reported an error.", !reset);
            _audio.Play(reset ? "ui.select" : "ui.error");
        }

        public void SelectTheme(string themeId)
        {
            ResolveActiveTheme(themeId);
            _profiles.Profile.SelectedThemeId = ActiveThemeId;
            _view.ApplyTheme();
            _view.Menu.RefreshSettingsControls();
            PersistProfile(DateTimeOffset.UtcNow);
            _audio.Play("ui.select");
        }

        public void SetMasterVolume(float value) { _profiles.Profile.MasterVolume = Mathf.Clamp01(value); PersistProfile(DateTimeOffset.UtcNow); }

        public void SetUiVolume(float value) { _profiles.Profile.UiVolume = Mathf.Clamp01(value); PersistProfile(DateTimeOffset.UtcNow); }

        public void SetCombatVolume(float value) { _profiles.Profile.CombatVolume = Mathf.Clamp01(value); PersistProfile(DateTimeOffset.UtcNow); }

        public void SetRewardWarningVolume(float value) { _profiles.Profile.RewardWarningVolume = Mathf.Clamp01(value); PersistProfile(DateTimeOffset.UtcNow); }

        public void SetMotionIntensity(float value) { _profiles.Profile.MotionIntensity = Mathf.Clamp01(value); PersistProfile(DateTimeOffset.UtcNow); }

        public void ToggleDebugUi()
        {
            _debugVisible = !_debugVisible;
            IdleAutoDefensePlayerView.SetVisible(_view._debugPanel, _debugVisible && _flow.RunActive);
        }

        public bool TryActivateOverdriveFromUi()
        {
            bool succeeded = _run.TryPurchaseOverdrive();
            ShowToast(succeeded ? _effectiveExperience.UiSettings.OverdriveName + " engaged." : ResolveOverdriveUnavailableReason(), !succeeded);
            _audio.Play(succeeded ? "gameplay.overdrive-activate" : "ui.insufficient-funds");
            return succeeded;
        }

        public bool TryUseModuleAction(IdleAutoDefenseModuleRole role)
        {
            bool succeeded;
            switch (role)
            {
                case IdleAutoDefenseModuleRole.PrecisionBeam:
                    succeeded = RunState.PulseBeamUnlocked ? _run.TryPurchaseDamageUpgrade() : _run.TryPurchasePulseBeamModule();
                    break;
                case IdleAutoDefenseModuleRole.AreaBurst:
                    succeeded = RunState.ArcBurstUnlocked ? _run.TryPurchaseRangeUpgrade() : _run.TryPurchaseArcBurstModule();
                    break;
                case IdleAutoDefenseModuleRole.HomingProjectile:
                    succeeded = RunState.HomingPulseUnlocked ? _run.TryPurchaseAttackSpeedUpgrade() : _run.TryPurchaseHomingPulseModule();
                    break;
                default:
                    succeeded = _run.TryPurchaseDamageUpgrade();
                    break;
            }
            ShowToast(succeeded ? "Defense module updated." : "Not enough credits for that module action.", !succeeded);
            _audio.Play(succeeded ? "ui.purchase" : "ui.insufficient-funds");
            return succeeded;
        }

        public bool TryPurchasePersistentUpgradeFromUi(string nodeId)
        {
            bool succeeded = _run.TryPurchasePersistentUpgrade(nodeId);
            if (succeeded) PersistProfile(DateTimeOffset.UtcNow);
            ShowToast(succeeded ? "Persistent research acquired." : "Research requirements or resources are not met.", !succeeded);
            _audio.Play(succeeded ? "ui.purchase" : "ui.insufficient-funds");
            return succeeded;
        }

        public bool ChooseRewardCard(int index)
        {
            IdleAutoDefenseRewardDraftChoice choice = RunState.RewardDraftActive && index >= 0 && index < RunState.RewardDraftChoices.Count
                ? RunState.RewardDraftChoices[index]
                : null;
            bool succeeded = _run.TryChooseRewardDraftChoice(index);
            if (!succeeded) return false;
            _audio.Play("reward.card-select");
            if (choice != null && choice.Rarity == IdleAutoDefenseRewardRarity.Epic) _audio.Play("reward.epic");
            if (choice != null && choice.Rarity == IdleAutoDefenseRewardRarity.Legendary) _audio.Play("reward.legendary");
            ShowToast(choice == null ? "Reward acquired." : choice.DisplayName + " acquired.", false);
            return true;
        }

        public bool HasOfflineRewardPreview => _profiles.OfflinePreview != null &&
            (_profiles.OfflinePreview.Code == IdleProgressionResultCode.Success || _profiles.OfflinePreview.Code == IdleProgressionResultCode.Capped) &&
            _profiles.OfflinePreview.Reward.CurrencyLines.Count > 0;

        public bool RefreshOfflinePreview(DateTimeOffset nowUtc)
        {
            _profiles.PrepareOfflinePreview(nowUtc);
            if (HasOfflineRewardPreview) OpenOfflineClaim();
            return HasOfflineRewardPreview;
        }

        public void OpenOfflineClaim()
        {
            if (!HasOfflineRewardPreview) return;
            _flow.BeforeModal = CurrentFlowState;
            CurrentFlowState = IdleAutoDefensePlayerFlowState.OfflineClaim;
            IdleAutoDefensePlayerView.SetVisible(_view._offlineOverlay, true);
            _view.Modal.RefreshOfflineClaim();
        }

        public void ClaimOfflineReward()
        {
            if (!HasOfflineRewardPreview) return;
            DateTimeOffset lastClaim = IdleAutoDefenseProfileSession.SafeUtc(_profiles.Profile.LastOfflineClaimUtcTicks, _profiles.OfflinePreviewNowUtc);
            _run.SimulateOfflineReward(lastClaim, _profiles.OfflinePreviewNowUtc);
            long credits = IdleAutoDefenseProfileSession.GetRewardAmount(_profiles.OfflinePreview, RunState.ActiveOfflineProgression == null ? string.Empty : RunState.ActiveOfflineProgression.ProductionCurrencyId);
            long parts = IdleAutoDefenseProfileSession.GetRewardAmount(_profiles.OfflinePreview, RunState.ActiveOfflineProgression == null ? string.Empty : RunState.ActiveOfflineProgression.CycleCurrencyId);
            _profiles.Profile.LifetimeCredits += Math.Max(0L, credits);
            _profiles.Profile.LifetimeParts += Math.Max(0L, parts);
            _profiles.Profile.LastOfflineClaimUtcTicks = _profiles.OfflinePreviewNowUtc.UtcTicks;
            _profiles.Profile.LastSeenUtcTicks = _profiles.OfflinePreviewNowUtc.UtcTicks;
            _profiles.OfflinePreview = null;
            PersistProfile(_profiles.OfflinePreviewNowUtc);
            IdleAutoDefensePlayerView.SetVisible(_view._offlineOverlay, false);
            CurrentFlowState = _flow.BeforeModal;
            ShowToast("Offline resources claimed.", false);
            _audio.Play("flow.offline-claim");
        }

        public void ShowRunSummary()
        {
            if (!_flow.TryRecordSummary()) return;
            if (RunState.EncounterCompleted) _profiles.Profile.CompletedRuns++;
            else _profiles.Profile.FailedRuns++;
            _profiles.Profile.LifetimeCredits += Math.Max(0L, RunState.EncounterRewardCredits);
            _profiles.Profile.LifetimeParts += Math.Max(0L, RunState.EncounterRewardParts);
            PersistProfile(DateTimeOffset.UtcNow);
            _view.HideAllModalOverlays();
            IdleAutoDefensePlayerView.SetVisible(_view._summaryOverlay, true);
            IdleAutoDefensePlayerView.SetVisible(_view._hudLayer, false);
            IdleAutoDefensePlayerView.SetVisible(_view._moduleBar, false);
            _view.Modal.RefreshRunSummary();
            _audio.Play(RunState.EncounterCompleted ? "flow.victory" : "flow.defeat");
            _audio.Play("flow.run-summary");
        }

        private void ResolveActiveTheme(string requestedThemeId)
        {
            _activeTheme = _effectiveExperience.ResolveTheme(requestedThemeId, out _themeFallbackVisible);
            if (_activeTheme == null)
            {
                _activeTheme = IdleAutoDefenseThemeAsset.CreateTransient();
                _themeFallbackVisible = true;
            }
            if (_profiles.Profile != null) _profiles.Profile.SelectedThemeId = _activeTheme.Id;
        }

        private string ResolveOverdriveUnavailableReason()
        {
            string ability = _effectiveExperience == null ? "Overdrive" : _effectiveExperience.UiSettings.OverdriveName;
            if (!RunState.EncounterRunning) return ability + " is available during an active run.";
            if (RunState.OverdriveActive) return ability + " is already active.";
            if (RunState.OverdriveCooldownSecondsRemaining > 0f) return ability + " is cooling down.";
            return "Not enough " + PrimaryCurrencyDisplayName.ToLowerInvariant() + " for " + ability + ".";
        }

        internal string PrimaryCurrencyDisplayName => RunState.ActiveEconomy == null || RunState.ActiveEconomy.GetCurrency(RunState.ActiveEconomy.PrimaryCurrencyId) == null
            ? "Credits"
            : RunState.ActiveEconomy.GetCurrency(RunState.ActiveEconomy.PrimaryCurrencyId).DisplayName;

        internal string PrimaryCurrencyToken => string.IsNullOrWhiteSpace(PrimaryCurrencyDisplayName)
            ? "C"
            : char.ToUpperInvariant(PrimaryCurrencyDisplayName[0]).ToString();

        internal string SecondaryCurrencyDisplayName => RunState.ActiveEconomy == null || RunState.ActiveEconomy.GetCurrency(RunState.ActiveEconomy.SecondaryCurrencyId) == null
            ? "Parts"
            : RunState.ActiveEconomy.GetCurrency(RunState.ActiveEconomy.SecondaryCurrencyId).DisplayName;

        internal void ShowToast(string text, bool error)
        {
            _toastText = text ?? string.Empty;
            _toastUntil = Time.unscaledTime + 3.25f;
            if (_view._toastLabel != null) _view._toastLabel.style.color = error ? _activeTheme.Danger : _activeTheme.Success;
        }
    }
}
