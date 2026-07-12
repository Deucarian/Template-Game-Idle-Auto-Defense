using System;
using System.Collections.Generic;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public enum IdleAutoDefensePlayerFlowState
    {
        MainMenu = 0,
        Running = 1,
        Paused = 2,
        Tutorial = 3,
        RunSummary = 4,
        OfflineClaim = 5
    }

    public partial class IdleAutoDefensePlayerExperienceController : IdleAutoDefenseTemplateController
    {
        [SerializeField] private IdleAutoDefensePlayerExperienceAsset _playerExperience;

        private IdleAutoDefensePlayerExperienceAsset _effectiveExperience;
        private IdleAutoDefensePlayerProfileStore _profileStore;
        private IdleAutoDefensePlayerProfile _profile;
        private IdleAutoDefenseThemeAsset _activeTheme;
        private IdleProgressionResult _offlinePreview;
        private DateTimeOffset _offlinePreviewNowUtc;
        private string _persistenceRootOverride;
        private string _playerFacingError = string.Empty;
        private string _toastText = string.Empty;
        private float _toastUntil;
        private bool _runActive;
        private bool _debugVisible;
        private bool _summaryRecorded;
        private bool _themeFallbackVisible;
        private int _tutorialStepIndex;
        private IdleAutoDefensePlayerFlowState _flowBeforeModal;
        private int _lastKillCount;
        private int _lastObjectiveDamageEvents;
        private int _lastRewardDraftCount;
        private int _lastWaveNumber;
        private bool _lastOverdriveActive;
        private bool _overdriveWasReady;
        private long _lastThreatId;
        private readonly Dictionary<string, float> _lastAudioEventTimes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private AudioSource _playerAudioSource;

        public IdleAutoDefensePlayerFlowState PlayerFlowState => CurrentFlowState;
        public IdleAutoDefensePlayerFlowState CurrentFlowState { get; private set; } = IdleAutoDefensePlayerFlowState.MainMenu;
        public bool PlayerExperienceValid { get; private set; }
        public bool MainMenuVisible => IsVisible(_mainMenuOverlay);
        public bool NormalHudVisible => IsVisible(_hudLayer);
        public bool PauseMenuVisible => IsVisible(_pauseOverlay);
        public bool RewardDraftVisible => IsVisible(_rewardOverlay);
        public bool RunSummaryVisible => IsVisible(_summaryOverlay);
        public bool TutorialVisible => IsVisible(_tutorialOverlay);
        public bool OfflineClaimVisible => IsVisible(_offlineOverlay);
        public bool DebugUiVisible => IsVisible(_debugPanel);
        public bool PortraitMessageVisible => IsVisible(_portraitOverlay);
        public string ActiveThemeId => _activeTheme == null ? string.Empty : _activeTheme.Id;
        public bool ThemeFallbackVisible => _themeFallbackVisible;
        public string PlayerFacingError => _playerFacingError;
        public IdleAutoDefensePlayerProfile PlayerProfile => _profile;
        public IdleAutoDefensePlayerExperienceAsset PlayerExperience => _playerExperience;
        public int TutorialStepIndex => _tutorialStepIndex;
        public int PlayerUiButtonCount => _playerUiRoot == null ? 0 : _playerUiRoot.Query<Button>().ToList().Count;
        public bool RunActive => _runActive;

        protected virtual void ConfigurePlayerExperienceBeforeBuild() { }

        protected void ConfigurePlayerExperience(IdleAutoDefensePlayerExperienceAsset playerExperience)
        {
            _playerExperience = playerExperience;
        }

        public void ConfigurePersistenceRoot(string rootPath)
        {
            if (_profileStore != null) throw new InvalidOperationException("Persistence root must be configured before Awake.");
            _persistenceRootOverride = rootPath ?? string.Empty;
        }

        protected override void Awake()
        {
            ConfigurePlayerExperienceBeforeBuild();
            base.Awake();
            InitializePlayerExperience();
        }

        protected override void Update()
        {
            HandlePlayerInput();
            if (_runActive && CurrentFlowState == IdleAutoDefensePlayerFlowState.Running && !PortraitMessageVisible)
                base.Update();
        }

        private void LateUpdate()
        {
            if (_playerUiRoot == null) return;
            RefreshPlayerUi();
            ObserveFeedbackEvents();
            if (_runActive && !_summaryRecorded && (EncounterCompleted || EncounterFailed))
                ShowRunSummary();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            PersistProfile(DateTimeOffset.UtcNow);
        }

        protected override void OnApplicationQuit()
        {
            PersistProfile(DateTimeOffset.UtcNow);
            _profileStore?.Dispose();
            _profileStore = null;
            base.OnApplicationQuit();
        }

        protected override void OnDestroy()
        {
            PersistProfile(DateTimeOffset.UtcNow);
            _profileStore?.Dispose();
            _profileStore = null;
            base.OnDestroy();
        }

        private void InitializePlayerExperience()
        {
            PlayerExperienceValid = _playerExperience != null && _playerExperience.Validate().Count == 0;
            _effectiveExperience = PlayerExperienceValid ? _playerExperience : IdleAutoDefensePlayerExperienceAsset.CreateTransient();
            if (!PlayerExperienceValid)
            {
                _playerFacingError = _playerExperience == null
                    ? "Player experience content is missing. Regenerate the scene from the Idle Auto Defense setup wizard."
                    : "Player experience content is invalid. Open the named content pack validation for details.";
            }

            _profileStore = new IdleAutoDefensePlayerProfileStore(_persistenceRootOverride);
            _profile = _profileStore.Load() ?? IdleAutoDefensePlayerProfile.CreateDefault();
            if (_profile.Progression != null && _profile.Progression.HasData && !RestorePersistentProgression(_profile.Progression))
                _playerFacingError = "Persistent progression could not be restored. Defaults remain active; reset progress or inspect storage diagnostics.";
            ResolveActiveTheme(_profile.SelectedThemeId);
            _playerAudioSource = gameObject.AddComponent<AudioSource>();
            _playerAudioSource.playOnAwake = false;
            _playerAudioSource.spatialBlend = 0f;
            BuildPlayerUi();
            PrepareOfflinePreview(DateTimeOffset.UtcNow);
            ShowMainMenu();
            if (HasOfflineRewardPreview)
                OpenOfflineClaim();
        }

        public void StartFreshRun()
        {
            if (!PlayerExperienceValid || StartupBlocked)
            {
                ShowToast(string.IsNullOrWhiteSpace(_playerFacingError) ? StartupError : _playerFacingError, true);
                PlayAudioEvent("ui.error");
                return;
            }

            RestartRun();
            _runActive = true;
            _summaryRecorded = false;
            _debugVisible = false;
            CurrentFlowState = IdleAutoDefensePlayerFlowState.Running;
            _flowBeforeModal = CurrentFlowState;
            ResetObservedFeedback();
            HideAllModalOverlays();
            SetVisible(_hudLayer, true);
            SetVisible(_moduleBar, true);
            SetVisible(_debugPanel, false);
            PlayAudioEvent("ui.select");
            if (!_profile.TutorialSeen)
                OpenTutorial(true);
        }

        public void RestartCurrentRun()
        {
            StartFreshRun();
        }

        public void ReturnToMainMenu()
        {
            if (_runActive) RestartRun();
            _runActive = false;
            _summaryRecorded = false;
            _debugVisible = false;
            ShowMainMenu();
            PersistProfile(DateTimeOffset.UtcNow);
            PlayAudioEvent("ui.back");
        }

        public void ShowMainMenu()
        {
            CurrentFlowState = IdleAutoDefensePlayerFlowState.MainMenu;
            _flowBeforeModal = CurrentFlowState;
            HideAllModalOverlays();
            SetVisible(_mainMenuOverlay, true);
            SetVisible(_hudLayer, false);
            SetVisible(_moduleBar, false);
            SetVisible(_debugPanel, false);
            RefreshMainMenu();
        }

        public void TogglePause()
        {
            if (!_runActive || RewardDraftActive || TutorialVisible || RunSummaryVisible) return;
            if (CurrentFlowState == IdleAutoDefensePlayerFlowState.Paused)
            {
                ResumeRun();
                return;
            }
            CurrentFlowState = IdleAutoDefensePlayerFlowState.Paused;
            _flowBeforeModal = CurrentFlowState;
            SetVisible(_pauseOverlay, true);
            ShowPauseSection(PauseSection.Main);
            PlayAudioEvent("ui.select");
        }

        public void ResumeRun()
        {
            if (!_runActive) return;
            CurrentFlowState = IdleAutoDefensePlayerFlowState.Running;
            _flowBeforeModal = CurrentFlowState;
            SetVisible(_pauseOverlay, false);
            PlayAudioEvent("ui.back");
        }

        public void OpenBuildView()
        {
            if (!_runActive || RewardDraftActive) return;
            CurrentFlowState = IdleAutoDefensePlayerFlowState.Paused;
            _flowBeforeModal = CurrentFlowState;
            SetVisible(_pauseOverlay, true);
            ShowPauseSection(PauseSection.Build);
            PlayAudioEvent("ui.select");
        }

        public void OpenSettings(bool fromMainMenu = false)
        {
            _flowBeforeModal = fromMainMenu ? IdleAutoDefensePlayerFlowState.MainMenu : CurrentFlowState;
            if (!fromMainMenu) CurrentFlowState = IdleAutoDefensePlayerFlowState.Paused;
            SetVisible(_pauseOverlay, true);
            ShowPauseSection(PauseSection.Settings);
            PlayAudioEvent("ui.select");
        }

        public void CloseSettingsOrHelp()
        {
            if (_flowBeforeModal == IdleAutoDefensePlayerFlowState.MainMenu)
            {
                ShowMainMenu();
                return;
            }
            if (_runActive)
            {
                CurrentFlowState = IdleAutoDefensePlayerFlowState.Paused;
                SetVisible(_pauseOverlay, true);
                ShowPauseSection(PauseSection.Main);
            }
        }

        public void OpenTutorial(bool firstRun = false)
        {
            if (_effectiveExperience.Tutorial == null) return;
            _flowBeforeModal = firstRun ? IdleAutoDefensePlayerFlowState.Running : CurrentFlowState;
            CurrentFlowState = IdleAutoDefensePlayerFlowState.Tutorial;
            _tutorialStepIndex = 0;
            SetVisible(_tutorialOverlay, true);
            RefreshTutorial();
            PlayAudioEvent("ui.select");
        }

        public void AdvanceTutorial()
        {
            if (_effectiveExperience.Tutorial == null) return;
            _tutorialStepIndex++;
            if (_tutorialStepIndex >= _effectiveExperience.Tutorial.Steps.Count)
            {
                CompleteTutorial();
                return;
            }
            RefreshTutorial();
            PlayAudioEvent("ui.select");
        }

        public void CompleteTutorial()
        {
            _profile.TutorialSeen = true;
            PersistProfile(DateTimeOffset.UtcNow);
            SetVisible(_tutorialOverlay, false);
            CurrentFlowState = _runActive ? IdleAutoDefensePlayerFlowState.Running : IdleAutoDefensePlayerFlowState.MainMenu;
            if (!_runActive) ShowMainMenu();
            PlayAudioEvent("ui.back");
        }

        public void ReplayTutorial()
        {
            OpenTutorial(false);
        }

        public void RequestResetProgress()
        {
            SetVisible(_resetConfirmationOverlay, true);
            PlayAudioEvent("ui.select");
        }

        public void CancelResetProgress()
        {
            SetVisible(_resetConfirmationOverlay, false);
            PlayAudioEvent("ui.back");
        }

        public void ConfirmResetProgress()
        {
            bool reset = _profileStore.Reset();
            _profile = IdleAutoDefensePlayerProfile.CreateDefault();
            ResetPersistentProgression();
            RestartRun();
            ResolveActiveTheme(_effectiveExperience.DefaultThemeId);
            SetVisible(_resetConfirmationOverlay, false);
            PersistProfile(DateTimeOffset.UtcNow);
            RefreshSettingsControls();
            ShowToast(reset ? "Progress reset." : "Progress reset locally; persistent storage reported an error.", !reset);
            PlayAudioEvent(reset ? "ui.select" : "ui.error");
        }

        public void SelectTheme(string themeId)
        {
            ResolveActiveTheme(themeId);
            _profile.SelectedThemeId = ActiveThemeId;
            ApplyTheme();
            RefreshSettingsControls();
            PersistProfile(DateTimeOffset.UtcNow);
            PlayAudioEvent("ui.select");
        }

        public void SetMasterVolume(float value) { _profile.MasterVolume = Mathf.Clamp01(value); PersistProfile(DateTimeOffset.UtcNow); }
        public void SetUiVolume(float value) { _profile.UiVolume = Mathf.Clamp01(value); PersistProfile(DateTimeOffset.UtcNow); }
        public void SetCombatVolume(float value) { _profile.CombatVolume = Mathf.Clamp01(value); PersistProfile(DateTimeOffset.UtcNow); }
        public void SetRewardWarningVolume(float value) { _profile.RewardWarningVolume = Mathf.Clamp01(value); PersistProfile(DateTimeOffset.UtcNow); }
        public void SetMotionIntensity(float value) { _profile.MotionIntensity = Mathf.Clamp01(value); PersistProfile(DateTimeOffset.UtcNow); }

        public void ToggleDebugUi()
        {
            _debugVisible = !_debugVisible;
            SetVisible(_debugPanel, _debugVisible && _runActive);
        }

        public bool TryActivateOverdriveFromUi()
        {
            bool succeeded = TryPurchaseOverdrive();
            ShowToast(succeeded ? "Overdrive engaged." : ResolveOverdriveUnavailableReason(), !succeeded);
            PlayAudioEvent(succeeded ? "gameplay.overdrive-activate" : "ui.insufficient-funds");
            return succeeded;
        }

        public bool TryUseModuleAction(IdleAutoDefenseModuleRole role)
        {
            bool succeeded;
            switch (role)
            {
                case IdleAutoDefenseModuleRole.PrecisionBeam:
                    succeeded = PulseBeamUnlocked ? TryPurchaseDamageUpgrade() : TryPurchasePulseBeamModule();
                    break;
                case IdleAutoDefenseModuleRole.AreaBurst:
                    succeeded = ArcBurstUnlocked ? TryPurchaseRangeUpgrade() : TryPurchaseArcBurstModule();
                    break;
                case IdleAutoDefenseModuleRole.HomingProjectile:
                    succeeded = HomingPulseUnlocked ? TryPurchaseAttackSpeedUpgrade() : TryPurchaseHomingPulseModule();
                    break;
                default:
                    succeeded = TryPurchaseDamageUpgrade();
                    break;
            }
            ShowToast(succeeded ? "Defense module updated." : "Not enough credits for that module action.", !succeeded);
            PlayAudioEvent(succeeded ? "ui.purchase" : "ui.insufficient-funds");
            return succeeded;
        }

        public bool TryPurchasePersistentUpgradeFromUi(string nodeId)
        {
            bool succeeded = TryPurchasePersistentUpgrade(nodeId);
            if (succeeded) PersistProfile(DateTimeOffset.UtcNow);
            ShowToast(succeeded ? "Persistent research acquired." : "Research requirements or resources are not met.", !succeeded);
            PlayAudioEvent(succeeded ? "ui.purchase" : "ui.insufficient-funds");
            return succeeded;
        }

        public bool ChooseRewardCard(int index)
        {
            IdleAutoDefenseRewardDraftChoice choice = RewardDraftActive && index >= 0 && index < RewardDraftChoices.Count
                ? RewardDraftChoices[index]
                : null;
            bool succeeded = TryChooseRewardDraftChoice(index);
            if (!succeeded) return false;
            PlayAudioEvent("reward.card-select");
            if (choice != null && choice.Rarity == IdleAutoDefenseRewardRarity.Epic) PlayAudioEvent("reward.epic");
            if (choice != null && choice.Rarity == IdleAutoDefenseRewardRarity.Legendary) PlayAudioEvent("reward.legendary");
            ShowToast(choice == null ? "Reward acquired." : choice.DisplayName + " acquired.", false);
            return true;
        }

        public bool HasOfflineRewardPreview => _offlinePreview != null &&
            (_offlinePreview.Code == IdleProgressionResultCode.Success || _offlinePreview.Code == IdleProgressionResultCode.Capped) &&
            _offlinePreview.Reward.CurrencyLines.Count > 0;

        public bool RefreshOfflinePreview(DateTimeOffset nowUtc)
        {
            PrepareOfflinePreview(nowUtc);
            if (HasOfflineRewardPreview) OpenOfflineClaim();
            return HasOfflineRewardPreview;
        }

        public void OpenOfflineClaim()
        {
            if (!HasOfflineRewardPreview) return;
            _flowBeforeModal = CurrentFlowState;
            CurrentFlowState = IdleAutoDefensePlayerFlowState.OfflineClaim;
            SetVisible(_offlineOverlay, true);
            RefreshOfflineClaim();
        }

        public void ClaimOfflineReward()
        {
            if (!HasOfflineRewardPreview) return;
            DateTimeOffset lastClaim = SafeUtc(_profile.LastOfflineClaimUtcTicks, _offlinePreviewNowUtc);
            SimulateOfflineReward(lastClaim, _offlinePreviewNowUtc);
            long credits = GetRewardAmount(_offlinePreview, ActiveOfflineProgression == null ? string.Empty : ActiveOfflineProgression.ProductionCurrencyId);
            long parts = GetRewardAmount(_offlinePreview, ActiveOfflineProgression == null ? string.Empty : ActiveOfflineProgression.CycleCurrencyId);
            _profile.LifetimeCredits += Math.Max(0L, credits);
            _profile.LifetimeParts += Math.Max(0L, parts);
            _profile.LastOfflineClaimUtcTicks = _offlinePreviewNowUtc.UtcTicks;
            _profile.LastSeenUtcTicks = _offlinePreviewNowUtc.UtcTicks;
            _offlinePreview = null;
            PersistProfile(_offlinePreviewNowUtc);
            SetVisible(_offlineOverlay, false);
            CurrentFlowState = _flowBeforeModal;
            ShowToast("Offline resources claimed.", false);
            PlayAudioEvent("flow.offline-claim");
        }

        public void ShowRunSummary()
        {
            if (_summaryRecorded) return;
            _summaryRecorded = true;
            _runActive = false;
            CurrentFlowState = IdleAutoDefensePlayerFlowState.RunSummary;
            if (EncounterCompleted) _profile.CompletedRuns++;
            else _profile.FailedRuns++;
            _profile.LifetimeCredits += Math.Max(0L, EncounterRewardCredits);
            _profile.LifetimeParts += Math.Max(0L, EncounterRewardParts);
            PersistProfile(DateTimeOffset.UtcNow);
            HideAllModalOverlays();
            SetVisible(_summaryOverlay, true);
            SetVisible(_hudLayer, false);
            SetVisible(_moduleBar, false);
            RefreshRunSummary();
            PlayAudioEvent(EncounterCompleted ? "flow.victory" : "flow.defeat");
            PlayAudioEvent("flow.run-summary");
        }

        private void HandlePlayerInput()
        {
            if (Input.GetKeyDown(KeyCode.F1)) ToggleDebugUi();
            if (PortraitMessageVisible) return;

            if (RewardDraftActive && CurrentFlowState == IdleAutoDefensePlayerFlowState.Running)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseRewardCard(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) ChooseRewardCard(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) ChooseRewardCard(2);
                return;
            }

            if (TutorialVisible)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) CompleteTutorial();
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) AdvanceTutorial();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (CurrentFlowState == IdleAutoDefensePlayerFlowState.Running &&
                (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.B))) OpenBuildView();
            if (CurrentFlowState == IdleAutoDefensePlayerFlowState.Running && Input.GetKeyDown(KeyCode.Space)) TryActivateOverdriveFromUi();
        }

        private void PrepareOfflinePreview(DateTimeOffset nowUtc)
        {
            _offlinePreviewNowUtc = nowUtc;
            if (ActiveOfflineProgression == null || !ActiveOfflineProgression.Enabled)
            {
                _offlinePreview = null;
                return;
            }
            DateTimeOffset lastClaim = SafeUtc(Math.Max(_profile.LastSeenUtcTicks, _profile.LastOfflineClaimUtcTicks), nowUtc);
            _offlinePreview = ActiveOfflineProgression.Calculate(lastClaim, nowUtc);
        }

        private void ResolveActiveTheme(string requestedThemeId)
        {
            _activeTheme = _effectiveExperience.ResolveTheme(requestedThemeId, out _themeFallbackVisible);
            if (_activeTheme == null)
            {
                _activeTheme = IdleAutoDefenseThemeAsset.CreateTransient();
                _themeFallbackVisible = true;
            }
            if (_profile != null) _profile.SelectedThemeId = _activeTheme.Id;
        }

        private void PersistProfile(DateTimeOffset nowUtc)
        {
            if (_profileStore == null || _profile == null) return;
            _profile.LastSeenUtcTicks = nowUtc.UtcTicks;
            _profile.Progression = CapturePersistentProgression();
            if (!_profileStore.Save(_profile))
                _playerFacingError = "Progress could not be saved. Check storage permissions and retry.";
        }

        private void ObserveFeedbackEvents()
        {
            int kills = DirectOrCombatKillCount + ProjectileAdapterKillCount;
            if (kills > _lastKillCount) PlayAudioEvent("gameplay.enemy-death");
            if (ObjectiveDamageEvents > _lastObjectiveDamageEvents)
            {
                PlayAudioEvent("gameplay.base-damage");
                if (ObjectiveMaximumHealth > 0d && ObjectiveHealth / ObjectiveMaximumHealth <= 0.25d)
                    PlayAudioEvent("gameplay.low-base-health");
            }
            if (RewardDraftOpenedCount > _lastRewardDraftCount) PlayAudioEvent("reward.draft-open");
            if (CurrentWaveNumber > _lastWaveNumber && CurrentWaveNumber > 0) PlayAudioEvent("gameplay.wave-start");
            if (OverdriveActive && !_lastOverdriveActive) PlayAudioEvent("gameplay.overdrive-activate");
            if (!OverdriveActive && _lastOverdriveActive) PlayAudioEvent("gameplay.overdrive-end");
            bool ready = CanPurchaseOverdrive;
            if (ready && !_overdriveWasReady) PlayAudioEvent("gameplay.overdrive-ready");
            if (TryGetPrimaryMajorThreat(out IdleAutoDefenseMajorThreatSnapshot threat) && threat.InstanceId != _lastThreatId)
            {
                _lastThreatId = threat.InstanceId;
                PlayAudioEvent(threat.Boss ? "gameplay.boss-warning" : "gameplay.elite-warning");
            }

            _lastKillCount = kills;
            _lastObjectiveDamageEvents = ObjectiveDamageEvents;
            _lastRewardDraftCount = RewardDraftOpenedCount;
            _lastWaveNumber = CurrentWaveNumber;
            _lastOverdriveActive = OverdriveActive;
            _overdriveWasReady = ready;
        }

        private void ResetObservedFeedback()
        {
            _lastKillCount = 0;
            _lastObjectiveDamageEvents = 0;
            _lastRewardDraftCount = 0;
            _lastWaveNumber = 0;
            _lastOverdriveActive = false;
            _overdriveWasReady = false;
            _lastThreatId = 0;
        }

        private void PlayAudioEvent(string eventId)
        {
            if (_playerAudioSource == null || _effectiveExperience == null || _effectiveExperience.AudioPalette == null || _profile == null) return;
            IdleAutoDefenseAudioEventRecord record = _effectiveExperience.AudioPalette.Find(eventId);
            if (record == null || record.Clip == null) return;
            float now = Time.unscaledTime;
            if (_lastAudioEventTimes.TryGetValue(record.Id, out float last) && now - last < record.MinimumIntervalSeconds) return;
            _lastAudioEventTimes[record.Id] = now;
            float categoryVolume = record.Category == IdleAutoDefenseAudioCategory.Ui
                ? _profile.UiVolume
                : record.Category == IdleAutoDefenseAudioCategory.Combat
                    ? _profile.CombatVolume
                    : _profile.RewardWarningVolume;
            _playerAudioSource.PlayOneShot(record.Clip, record.Volume * categoryVolume * _profile.MasterVolume);
        }

        private string ResolveOverdriveUnavailableReason()
        {
            if (!EncounterRunning) return "Overdrive is available during an active run.";
            if (OverdriveActive) return "Overdrive is already active.";
            if (OverdriveCooldownSecondsRemaining > 0f) return "Overdrive is cooling down.";
            return "Not enough credits for Overdrive.";
        }

        private void ShowToast(string text, bool error)
        {
            _toastText = text ?? string.Empty;
            _toastUntil = Time.unscaledTime + 3.25f;
            if (_toastLabel != null) _toastLabel.style.color = error ? _activeTheme.Danger : _activeTheme.Success;
        }

        private static DateTimeOffset SafeUtc(long ticks, DateTimeOffset fallback)
        {
            if (ticks <= DateTimeOffset.MinValue.UtcTicks || ticks >= DateTimeOffset.MaxValue.UtcTicks) return fallback;
            try { return new DateTimeOffset(ticks, TimeSpan.Zero); }
            catch (ArgumentOutOfRangeException) { return fallback; }
        }

        private static long GetRewardAmount(IdleProgressionResult result, string currencyId)
        {
            if (result == null || string.IsNullOrWhiteSpace(currencyId)) return 0L;
            for (int i = 0; i < result.Reward.CurrencyLines.Count; i++)
            {
                CurrencyLine line = result.Reward.CurrencyLines[i];
                if (string.Equals(line.CurrencyId.Value, currencyId, StringComparison.OrdinalIgnoreCase))
                    return line.Amount.Value;
            }
            return 0L;
        }

        private static bool IsVisible(VisualElement element)
        {
            return element != null && element.style.display != DisplayStyle.None;
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
