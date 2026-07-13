using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public partial class IdleAutoDefensePlayerExperienceController
    {
        private enum PauseSection { Main, Build, Settings }

        private VisualElement _playerUiRoot;
        private VisualElement _safeAreaRoot;
        private VisualElement _portraitOverlay;
        private VisualElement _hudLayer;
        private VisualElement _moduleBar;
        private VisualElement _mainMenuOverlay;
        private VisualElement _pauseOverlay;
        private VisualElement _pauseBody;
        private VisualElement _rewardOverlay;
        private VisualElement _rewardCardRow;
        private VisualElement _tutorialOverlay;
        private VisualElement _offlineOverlay;
        private VisualElement _summaryOverlay;
        private VisualElement _resetConfirmationOverlay;
        private VisualElement _debugPanel;
        private VisualElement _threatBar;
        private VisualElement _threatFill;
        private Label _threatLabel;
        private Label _threatMarker;
        private Label _timerLabel;
        private Label _waveLabel;
        private Label _profileLabel;
        private Label _healthLabel;
        private VisualElement _healthFill;
        private Label _currencyLabel;
        private Label _levelLabel;
        private VisualElement _xpFill;
        private Label _overdriveStatusLabel;
        private Button _overdriveButton;
        private Label _toastLabel;
        private Label _mainProfileLabel;
        private Label _mainErrorLabel;
        private Label _themeFallbackLabel;
        private Label _tutorialProgressLabel;
        private Label _tutorialTitleLabel;
        private Label _tutorialBodyLabel;
        private Label _offlineTitleLabel;
        private Label _offlineBodyLabel;
        private Label _summaryTitleLabel;
        private Label _summaryBodyLabel;
        private Label _debugLabel;
        private Label _buildStatsLabel;
        private Slider _masterSlider;
        private Slider _uiSlider;
        private Slider _combatSlider;
        private Slider _rewardSlider;
        private Slider _motionSlider;
        private readonly Button[] _rewardButtons = new Button[3];
        private readonly Button[] _moduleButtons = new Button[4];
        private readonly Label[] _moduleLabels = new Label[4];
        private bool _compactLayout;

        private void BuildPlayerUi()
        {
            VisualElement runtimeRoot = RuntimeUiRoot;
            _playerUiRoot?.RemoveFromHierarchy();
            _playerUiRoot = new VisualElement { name = "idle-player-experience" };
            FillAbsolute(_playerUiRoot);
            _playerUiRoot.pickingMode = PickingMode.Ignore;
            runtimeRoot.Add(_playerUiRoot);

            _safeAreaRoot = new VisualElement { name = "safe-area" };
            FillAbsolute(_safeAreaRoot);
            _safeAreaRoot.pickingMode = PickingMode.Ignore;
            _playerUiRoot.Add(_safeAreaRoot);

            BuildHud();
            BuildMainMenu();
            BuildPauseMenu();
            BuildRewardDraft();
            BuildTutorial();
            BuildOfflineClaim();
            BuildRunSummary();
            BuildResetConfirmation();
            BuildPortraitMessage();
            BuildDebugPanel();

            _playerUiRoot.RegisterCallback<GeometryChangedEvent>(OnPlayerUiGeometryChanged);
            ApplyTheme();
            ApplyResponsiveLayout(_playerUiRoot.resolvedStyle.width, _playerUiRoot.resolvedStyle.height);
        }

        private void BuildHud()
        {
            _hudLayer = new VisualElement { name = "player-hud" };
            FillAbsolute(_hudLayer);
            _hudLayer.pickingMode = PickingMode.Ignore;
            _safeAreaRoot.Add(_hudLayer);

            VisualElement top = new VisualElement { name = "hud-top" };
            top.style.position = Position.Absolute;
            top.style.left = 12;
            top.style.right = 12;
            top.style.top = 8;
            top.style.height = 68;
            top.style.flexDirection = FlexDirection.Row;
            top.style.justifyContent = Justify.SpaceBetween;
            top.style.alignItems = Align.FlexStart;
            top.pickingMode = PickingMode.Ignore;
            _hudLayer.Add(top);

            VisualElement left = HudBlock(250);
            _profileLabel = AddLabel(left, string.Empty, 13, FontStyle.Bold);
            _waveLabel = AddLabel(left, string.Empty, 15, FontStyle.Bold);
            top.Add(left);

            VisualElement center = HudBlock(330);
            center.style.alignItems = Align.Center;
            _timerLabel = AddLabel(center, "04:40", 31, FontStyle.Bold);
            _timerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _threatBar = new VisualElement { name = "major-threat-bar" };
            _threatBar.style.display = DisplayStyle.None;
            _threatBar.style.width = Length.Percent(100);
            _threatBar.style.height = 18;
            _threatBar.style.marginTop = 2;
            _threatBar.style.backgroundColor = new Color(0f, 0f, 0f, 0.65f);
            SetBorder(_threatBar, 1, Color.white, 4);
            _threatFill = new VisualElement();
            _threatFill.style.position = Position.Absolute;
            _threatFill.style.left = 0;
            _threatFill.style.top = 0;
            _threatFill.style.bottom = 0;
            _threatFill.style.width = Length.Percent(100);
            _threatBar.Add(_threatFill);
            _threatLabel = AddLabel(_threatBar, string.Empty, 11, FontStyle.Bold);
            _threatLabel.style.position = Position.Absolute;
            _threatLabel.style.left = 5;
            _threatLabel.style.right = 5;
            _threatLabel.style.top = 1;
            _threatLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            center.Add(_threatBar);
            top.Add(center);

            VisualElement right = HudBlock(250);
            right.style.alignItems = Align.FlexEnd;
            _currencyLabel = AddLabel(right, string.Empty, 16, FontStyle.Bold);
            _levelLabel = AddLabel(right, string.Empty, 13, FontStyle.Bold);
            VisualElement xpTrack = BarTrack(220, 7);
            _xpFill = BarFill(xpTrack);
            right.Add(xpTrack);
            top.Add(right);

            VisualElement objective = new VisualElement { name = "objective-health" };
            objective.style.position = Position.Absolute;
            objective.style.left = 12;
            objective.style.top = 82;
            objective.style.width = 250;
            objective.style.paddingLeft = 10;
            objective.style.paddingRight = 10;
            objective.style.paddingTop = 7;
            objective.style.paddingBottom = 8;
            SetBorder(objective, 1, Color.white, 6);
            _healthLabel = AddLabel(objective, string.Empty, 13, FontStyle.Bold);
            VisualElement healthTrack = BarTrack(228, 10);
            _healthFill = BarFill(healthTrack);
            objective.Add(healthTrack);
            _hudLayer.Add(objective);

            Button menuButton = AddButton(_hudLayer, "Menu", TogglePause, 92, 46);
            menuButton.name = "touch-menu-button";
            menuButton.style.position = Position.Absolute;
            menuButton.style.right = 12;
            menuButton.style.top = 82;

            _overdriveStatusLabel = AddLabel(_hudLayer, string.Empty, 13, FontStyle.Bold);
            _overdriveStatusLabel.style.position = Position.Absolute;
            _overdriveStatusLabel.style.right = 116;
            _overdriveStatusLabel.style.top = 90;
            _overdriveStatusLabel.style.unityTextAlign = TextAnchor.MiddleRight;

            _threatMarker = AddLabel(_hudLayer, string.Empty, 14, FontStyle.Bold);
            _threatMarker.name = "major-threat-offscreen-marker";
            _threatMarker.style.display = DisplayStyle.None;
            _threatMarker.style.position = Position.Absolute;
            _threatMarker.style.paddingLeft = 8;
            _threatMarker.style.paddingRight = 8;
            _threatMarker.style.paddingTop = 5;
            _threatMarker.style.paddingBottom = 5;
            SetBorder(_threatMarker, 2, Color.white, 5);

            _toastLabel = AddLabel(_hudLayer, string.Empty, 15, FontStyle.Bold);
            _toastLabel.name = "player-toast";
            _toastLabel.style.position = Position.Absolute;
            _toastLabel.style.left = Length.Percent(30);
            _toastLabel.style.right = Length.Percent(30);
            _toastLabel.style.bottom = 160;
            _toastLabel.style.paddingTop = 7;
            _toastLabel.style.paddingBottom = 7;
            _toastLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _toastLabel.style.display = DisplayStyle.None;

            _themeFallbackLabel = AddLabel(_hudLayer, "Theme fallback active", 11, FontStyle.Bold);
            _themeFallbackLabel.style.position = Position.Absolute;
            _themeFallbackLabel.style.left = 12;
            _themeFallbackLabel.style.bottom = 156;

            BuildModuleBar();
        }

        private void BuildModuleBar()
        {
            _moduleBar = new VisualElement { name = "module-bar" };
            _moduleBar.style.position = Position.Absolute;
            _moduleBar.style.left = 10;
            _moduleBar.style.right = 10;
            _moduleBar.style.bottom = 8;
            _moduleBar.style.height = 142;
            _moduleBar.style.flexDirection = FlexDirection.Row;
            _moduleBar.style.alignItems = Align.Stretch;
            _moduleBar.pickingMode = PickingMode.Position;
            _safeAreaRoot.Add(_moduleBar);

            IdleAutoDefenseModuleRole[] roles =
            {
                IdleAutoDefenseModuleRole.StartingProjectile,
                IdleAutoDefenseModuleRole.PrecisionBeam,
                IdleAutoDefenseModuleRole.AreaBurst,
                IdleAutoDefenseModuleRole.HomingProjectile
            };
            for (int i = 0; i < roles.Length; i++)
            {
                int index = i;
                IdleAutoDefenseModuleRole role = roles[i];
                Button card = AddButton(_moduleBar, string.Empty, () => TryUseModuleAction(role), 150, 132);
                card.name = "module-card-" + role.ToString().ToLowerInvariant();
                card.style.flexGrow = 1;
                card.style.flexBasis = 0;
                card.style.marginLeft = 3;
                card.style.marginRight = 3;
                card.style.whiteSpace = WhiteSpace.Normal;
                card.style.unityTextAlign = TextAnchor.UpperLeft;
                card.style.paddingLeft = 10;
                card.style.paddingRight = 10;
                card.style.paddingTop = 8;
                card.style.paddingBottom = 6;
                _moduleButtons[index] = card;
                _moduleLabels[index] = null;
            }

            VisualElement overdrive = new VisualElement { name = "overdrive-panel" };
            overdrive.style.width = 184;
            overdrive.style.minWidth = 156;
            overdrive.style.marginLeft = 4;
            overdrive.style.paddingLeft = 7;
            overdrive.style.paddingRight = 7;
            overdrive.style.paddingTop = 7;
            overdrive.style.paddingBottom = 7;
            SetBorder(overdrive, 2, Color.white, 6);
            Label ability = AddLabel(overdrive, _effectiveExperience.UiSettings.OverdriveName, 16, FontStyle.Bold);
            ability.style.unityTextAlign = TextAnchor.MiddleCenter;
            Label description = AddLabel(overdrive, _effectiveExperience.UiSettings.OverdriveDescription, 11);
            description.style.flexGrow = 1;
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            _overdriveButton = AddButton(overdrive, "Activate", () => TryActivateOverdriveFromUi(), 150, 46);
            _overdriveButton.name = "overdrive-button";
            _moduleBar.Add(overdrive);
        }

        private void BuildMainMenu()
        {
            _mainMenuOverlay = Overlay("main-menu-overlay", 0.86f);
            _safeAreaRoot.Add(_mainMenuOverlay);
            VisualElement panel = ModalPanel(_mainMenuOverlay, 560, 660);
            Label title = AddLabel(panel, _effectiveExperience.UiSettings.GameTitle, 38, FontStyle.Bold);
            title.name = "main-menu-title";
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            Label description = AddLabel(panel, _effectiveExperience.UiSettings.GameDescription, 17);
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            description.style.marginBottom = 12;
            _mainProfileLabel = AddLabel(panel, string.Empty, 15, FontStyle.Bold);
            _mainProfileLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _mainProfileLabel.style.marginBottom = 16;
            Button start = AddButton(panel, "Start Run", StartFreshRun, 300, 54);
            start.name = "start-run-button";
            start.SetEnabled(PlayerExperienceValid && !StartupBlocked);
            AddButton(panel, "How to Play", () => OpenTutorial(false), 300, 48);
            AddButton(panel, "Settings", () => OpenSettings(true), 300, 48);
            AddButton(panel, "Reset Progress", RequestResetProgress, 300, 48);
            if (!Application.isEditor && Application.platform != RuntimePlatform.WebGLPlayer)
                AddButton(panel, "Quit", Application.Quit, 300, 48);
            _mainErrorLabel = AddLabel(panel, string.Empty, 13, FontStyle.Bold);
            _mainErrorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _mainErrorLabel.style.marginTop = 10;
        }

        private void BuildPauseMenu()
        {
            _pauseOverlay = Overlay("pause-overlay", 0.78f);
            _pauseOverlay.style.display = DisplayStyle.None;
            _safeAreaRoot.Add(_pauseOverlay);
            VisualElement panel = ModalPanel(_pauseOverlay, 820, 650);
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            panel.Add(header);
            Label title = AddLabel(header, _effectiveExperience.UiSettings.PauseMenuTitle, 27, FontStyle.Bold);
            title.style.flexGrow = 1;
            AddButton(header, "Resume", ResumeRun, 112, 46);
            _pauseBody = new ScrollView(ScrollViewMode.Vertical) { name = "pause-scroll" };
            _pauseBody.style.flexGrow = 1;
            _pauseBody.style.marginTop = 10;
            panel.Add(_pauseBody);
        }

        private void BuildRewardDraft()
        {
            _rewardOverlay = Overlay("reward-draft-overlay", 0.87f);
            _rewardOverlay.style.display = DisplayStyle.None;
            _safeAreaRoot.Add(_rewardOverlay);
            Label title = AddLabel(_rewardOverlay, "Choose an Authored Reward", 31, FontStyle.Bold);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 4;
            Label hint = AddLabel(_rewardOverlay, "Combat paused  |  Tap a card or press 1, 2, 3", 14, FontStyle.Bold);
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            hint.style.marginBottom = 12;
            _rewardCardRow = new VisualElement { name = "reward-card-row" };
            _rewardCardRow.style.width = Length.Percent(94);
            _rewardCardRow.style.maxWidth = 1180;
            _rewardCardRow.style.flexDirection = FlexDirection.Row;
            _rewardCardRow.style.flexGrow = 1;
            _rewardCardRow.style.maxHeight = 430;
            _rewardOverlay.Add(_rewardCardRow);
            for (int i = 0; i < _rewardButtons.Length; i++)
            {
                int index = i;
                Button card = AddButton(_rewardCardRow, string.Empty, () => ChooseRewardCard(index), 250, 300);
                card.name = "reward-card-" + (i + 1).ToString(CultureInfo.InvariantCulture);
                card.style.flexGrow = 1;
                card.style.flexBasis = 0;
                card.style.marginLeft = 7;
                card.style.marginRight = 7;
                card.style.whiteSpace = WhiteSpace.Normal;
                card.style.unityTextAlign = TextAnchor.UpperLeft;
                card.style.paddingLeft = 18;
                card.style.paddingRight = 18;
                card.style.paddingTop = 16;
                card.style.paddingBottom = 14;
                _rewardButtons[i] = card;
            }
        }

        private void BuildTutorial()
        {
            _tutorialOverlay = Overlay("tutorial-overlay", 0.84f);
            _tutorialOverlay.style.display = DisplayStyle.None;
            _safeAreaRoot.Add(_tutorialOverlay);
            VisualElement panel = ModalPanel(_tutorialOverlay, 680, 500);
            _tutorialProgressLabel = AddLabel(panel, string.Empty, 13, FontStyle.Bold);
            _tutorialTitleLabel = AddLabel(panel, string.Empty, 29, FontStyle.Bold);
            _tutorialTitleLabel.style.marginTop = 10;
            _tutorialBodyLabel = AddLabel(panel, string.Empty, 18);
            _tutorialBodyLabel.style.flexGrow = 1;
            _tutorialBodyLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            VisualElement row = ButtonRow(panel);
            AddButton(row, "Skip", CompleteTutorial, 120, 48);
            AddButton(row, "Next", AdvanceTutorial, 170, 48);
        }

        private void BuildOfflineClaim()
        {
            _offlineOverlay = Overlay("offline-claim-overlay", 0.86f);
            _offlineOverlay.style.display = DisplayStyle.None;
            _safeAreaRoot.Add(_offlineOverlay);
            VisualElement panel = ModalPanel(_offlineOverlay, 620, 520);
            _offlineTitleLabel = AddLabel(panel, "Welcome Back", 30, FontStyle.Bold);
            _offlineTitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _offlineBodyLabel = AddLabel(panel, string.Empty, 17);
            _offlineBodyLabel.style.flexGrow = 1;
            _offlineBodyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            Button claim = AddButton(panel, "Claim Resources", ClaimOfflineReward, 260, 52);
            claim.name = "offline-claim-button";
        }

        private void BuildRunSummary()
        {
            _summaryOverlay = Overlay("run-summary-overlay", 0.9f);
            _summaryOverlay.style.display = DisplayStyle.None;
            _safeAreaRoot.Add(_summaryOverlay);
            VisualElement panel = ModalPanel(_summaryOverlay, 760, 680);
            _summaryTitleLabel = AddLabel(panel, string.Empty, 32, FontStyle.Bold);
            _summaryTitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            _summaryBodyLabel = AddLabel(scroll, string.Empty, 15);
            panel.Add(scroll);
            VisualElement row = ButtonRow(panel);
            AddButton(row, "Restart Run", RestartCurrentRun, 180, 50);
            AddButton(row, "Main Menu", ReturnToMainMenu, 180, 50);
        }

        private void BuildResetConfirmation()
        {
            _resetConfirmationOverlay = Overlay("reset-confirmation-overlay", 0.9f);
            _resetConfirmationOverlay.style.display = DisplayStyle.None;
            _safeAreaRoot.Add(_resetConfirmationOverlay);
            VisualElement panel = ModalPanel(_resetConfirmationOverlay, 500, 320);
            Label title = AddLabel(panel, "Reset Progress?", 27, FontStyle.Bold);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            Label body = AddLabel(panel, "This clears tutorial, settings, theme choice, offline timestamp, and template progression totals.", 16);
            body.style.flexGrow = 1;
            body.style.unityTextAlign = TextAnchor.MiddleCenter;
            VisualElement row = ButtonRow(panel);
            AddButton(row, "Cancel", CancelResetProgress, 140, 48);
            AddButton(row, "Reset", ConfirmResetProgress, 140, 48);
        }

        private void BuildPortraitMessage()
        {
            _portraitOverlay = Overlay("portrait-rotate-overlay", 0.96f);
            _portraitOverlay.style.display = DisplayStyle.None;
            _playerUiRoot.Add(_portraitOverlay);
            Label title = AddLabel(_portraitOverlay, "Rotate Device", 31, FontStyle.Bold);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            Label body = AddLabel(_portraitOverlay, _effectiveExperience.UiSettings.PortraitMessage, 17);
            body.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        private void BuildDebugPanel()
        {
            _debugPanel = new VisualElement { name = "debug-panel" };
            _debugPanel.style.display = DisplayStyle.None;
            _debugPanel.style.position = Position.Absolute;
            _debugPanel.style.left = 12;
            _debugPanel.style.top = 132;
            _debugPanel.style.width = 390;
            _debugPanel.style.maxHeight = 310;
            _debugPanel.style.paddingLeft = 9;
            _debugPanel.style.paddingRight = 9;
            _debugPanel.style.paddingTop = 8;
            _debugPanel.style.paddingBottom = 8;
            SetBorder(_debugPanel, 1, Color.white, 5);
            _debugLabel = AddLabel(_debugPanel, string.Empty, 11);
            _safeAreaRoot.Add(_debugPanel);
        }

        private void ShowPauseSection(PauseSection section)
        {
            if (_pauseBody == null) return;
            _pauseBody.Clear();
            VisualElement nav = ButtonRow(_pauseBody);
            AddButton(nav, "Pause", () => ShowPauseSection(PauseSection.Main), 120, 44);
            AddButton(nav, "Build", () => ShowPauseSection(PauseSection.Build), 120, 44);
            AddButton(nav, "Settings", () => ShowPauseSection(PauseSection.Settings), 120, 44);

            if (section == PauseSection.Build)
            {
                AddSectionHeading(_pauseBody, "Current Build");
                _buildStatsLabel = AddLabel(_pauseBody, string.Empty, 14);
                RefreshBuildStats();
                BuildPersistentResearchControls(_pauseBody);
                return;
            }

            if (section == PauseSection.Settings)
            {
                BuildSettingsSection(_pauseBody);
                return;
            }

            AddSectionHeading(_pauseBody, "Run Paused");
            AddButton(_pauseBody, "Resume", ResumeRun, 260, 48);
            AddButton(_pauseBody, "Current Build", OpenBuildView, 260, 48);
            AddButton(_pauseBody, "How to Play", () => OpenTutorial(false), 260, 48);
            AddButton(_pauseBody, "Restart Run", RestartCurrentRun, 260, 48);
            AddButton(_pauseBody, "Return to Main Menu", ReturnToMainMenu, 260, 48);
        }

        private void BuildSettingsSection(VisualElement parent)
        {
            AddSectionHeading(parent, "Settings");
            _masterSlider = AddSlider(parent, "Master Volume", _profile.MasterVolume, SetMasterVolume);
            _uiSlider = AddSlider(parent, "UI Volume", _profile.UiVolume, SetUiVolume);
            _combatSlider = AddSlider(parent, "Combat Volume", _profile.CombatVolume, SetCombatVolume);
            _rewardSlider = AddSlider(parent, "Reward / Warning Volume", _profile.RewardWarningVolume, SetRewardWarningVolume);
            _motionSlider = AddSlider(parent, "Motion Intensity", _profile.MotionIntensity, SetMotionIntensity);
            AddSectionHeading(parent, "Theme");
            VisualElement themeRow = ButtonRow(parent);
            for (int i = 0; i < _effectiveExperience.Themes.Count; i++)
            {
                IdleAutoDefenseThemeAsset theme = _effectiveExperience.Themes[i];
                if (theme == null) continue;
                string themeId = theme.Id;
                AddButton(themeRow, theme.DisplayName, () => SelectTheme(themeId), 170, 46);
            }
            AddButton(parent, "Replay Tutorial", ReplayTutorial, 240, 46);
            RefreshSettingsControls();
        }

        private void BuildPersistentResearchControls(VisualElement parent)
        {
            if (ActiveProgression == null || ActiveProgression.ResearchNodes.Count == 0) return;
            AddSectionHeading(parent, "Persistent Research");
            for (int i = 0; i < ActiveProgression.ResearchNodes.Count; i++)
            {
                IdleAutoDefenseResearchNodeRecord node = ActiveProgression.ResearchNodes[i];
                if (node == null) continue;
                int rank = GetPersistentResearchRank(node.Id);
                bool maxed = rank >= node.MaxRank;
                long cost = maxed || rank >= node.RankCosts.Count ? 0L : node.RankCosts[rank];
                long balance = GetPersistentCurrencyBalance(node.CostCurrencyId);
                string nodeId = node.Id;
                Button button = AddButton(
                    parent,
                    node.DisplayName + "  Rank " + rank.ToString(CultureInfo.InvariantCulture) + " / " + node.MaxRank.ToString(CultureInfo.InvariantCulture) +
                    (maxed ? "  MAX" : "  |  " + cost.ToString(CultureInfo.InvariantCulture) + " " + Nicify(node.CostCurrencyId.Split('.')[node.CostCurrencyId.Split('.').Length - 1])) +
                    "\n" + Nicify(node.EffectKind.ToString()) + "  |  Bank " + balance.ToString(CultureInfo.InvariantCulture),
                    () =>
                    {
                        TryPurchasePersistentUpgradeFromUi(nodeId);
                        ShowPauseSection(PauseSection.Build);
                    },
                    320,
                    58);
                button.style.whiteSpace = WhiteSpace.Normal;
                button.SetEnabled(!maxed);
            }
        }

        private Slider AddSlider(VisualElement parent, string label, float value, Action<float> changed)
        {
            var slider = new Slider(label, 0f, 1f) { value = value };
            ApplyRuntimeUiFont(slider);
            slider.style.minHeight = 42;
            slider.style.marginTop = 3;
            slider.style.marginBottom = 3;
            slider.RegisterValueChangedCallback(evt => changed(evt.newValue));
            parent.Add(slider);
            return slider;
        }

        private void RefreshPlayerUi()
        {
            if (_timerLabel == null) return;
            double remaining = Math.Max(0d, SessionLengthSeconds - SurvivalSeconds);
            _timerLabel.text = FormatDuration(remaining);
            _profileLabel.text = ActiveRunProfile == null ? "Authored Run" : ActiveRunProfile.DisplayName;
            _waveLabel.text = "Wave " + CurrentWaveNumber.ToString(CultureInfo.InvariantCulture) + " / " + TotalWaveCount.ToString(CultureInfo.InvariantCulture) +
                (CurrentWaveNumber > 0 ? "  " + CurrentSpawnProfileName : string.Empty);
            _healthLabel.text = _effectiveExperience.UiSettings.ObjectiveLabel.ToUpperInvariant() + "  " + ObjectiveHealth.ToString("0", CultureInfo.InvariantCulture) + " / " + ObjectiveMaximumHealth.ToString("0", CultureInfo.InvariantCulture);
            SetPercentWidth(_healthFill, ObjectiveMaximumHealth <= 0d ? 0f : (float)(ObjectiveHealth / ObjectiveMaximumHealth));
            _healthFill.style.backgroundColor = ObjectiveMaximumHealth > 0d && ObjectiveHealth / ObjectiveMaximumHealth <= 0.25d ? _activeTheme.Danger : _activeTheme.Success;
            _currencyLabel.text = RuntimeCurrency.ToString(CultureInfo.InvariantCulture) + " " + PrimaryCurrencyDisplayName.ToUpperInvariant();
            _currencyLabel.style.color = _activeTheme.Currency;
            _levelLabel.text = _effectiveExperience.UiSettings.PlayerRankLabel + " " + CommanderLevel.ToString(CultureInfo.InvariantCulture) + "  XP " + CommanderExperience.ToString(CultureInfo.InvariantCulture) + " / " + ExperienceToNextLevel.ToString(CultureInfo.InvariantCulture);
            SetPercentWidth(_xpFill, ExperienceToNextLevel <= 0 ? 0f : (float)CommanderExperience / ExperienceToNextLevel);
            _xpFill.style.backgroundColor = _activeTheme.Accent;
            string overdriveName = _effectiveExperience.UiSettings.OverdriveName;
            _overdriveStatusLabel.text = OverdriveActive
                ? overdriveName.ToUpperInvariant() + " " + OverdriveSecondsRemaining.ToString("0.0", CultureInfo.InvariantCulture) + "s"
                : OverdriveCooldownSecondsRemaining > 0f
                    ? "Cooldown " + OverdriveCooldownSecondsRemaining.ToString("0.0", CultureInfo.InvariantCulture) + "s"
                    : overdriveName + " ready";
            _overdriveStatusLabel.style.color = OverdriveActive ? _activeTheme.Warning : _activeTheme.PrimaryText;
            _overdriveButton.text = OverdriveActive
                ? OverdriveSecondsRemaining.ToString("0.0", CultureInfo.InvariantCulture) + "s ACTIVE"
                : OverdriveCooldownSecondsRemaining > 0f
                    ? OverdriveCooldownSecondsRemaining.ToString("0.0", CultureInfo.InvariantCulture) + "s"
                    : "Activate  " + OverdriveCost.ToString(CultureInfo.InvariantCulture) + " " + PrimaryCurrencyToken;
            _overdriveButton.SetEnabled(CanPurchaseOverdrive);
            RefreshModuleCards();
            RefreshRewardCards();
            RefreshThreatUi();
            RefreshBuildStats();
            _themeFallbackLabel.style.display = _themeFallbackVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _toastLabel.style.display = !string.IsNullOrWhiteSpace(_toastText) && Time.unscaledTime < _toastUntil ? DisplayStyle.Flex : DisplayStyle.None;
            _toastLabel.text = _toastText;
            if (_debugVisible)
                _debugLabel.text = StatusSummary + "\nCore=" + UsingAuthoredCore + "  Fallback=" + FallbackModeActive + "  Pack=" + UsingAssignedContentPack;
        }

        private void RefreshMainMenu()
        {
            if (_mainProfileLabel == null) return;
            string profile = ActiveRunProfile == null ? "Authored run profile" : ActiveRunProfile.DisplayName;
            _mainProfileLabel.text = profile + "  |  " + FormatDuration(SessionLengthSeconds) + " defense";
            _mainErrorLabel.text = string.IsNullOrWhiteSpace(_playerFacingError) ? string.Empty : _playerFacingError;
            _mainErrorLabel.style.display = string.IsNullOrWhiteSpace(_playerFacingError) ? DisplayStyle.None : DisplayStyle.Flex;
            _mainErrorLabel.style.color = _activeTheme.Danger;
        }

        private void RefreshModuleCards()
        {
            IdleAutoDefenseModuleRole[] roles =
            {
                IdleAutoDefenseModuleRole.StartingProjectile,
                IdleAutoDefenseModuleRole.PrecisionBeam,
                IdleAutoDefenseModuleRole.AreaBurst,
                IdleAutoDefenseModuleRole.HomingProjectile
            };
            for (int i = 0; i < roles.Length; i++)
            {
                IdleAutoDefenseModuleRole role = roles[i];
                IdleAutoDefenseModuleRule module = ActiveGameRules == null ? null : ActiveGameRules.GetModule(role);
                Button button = _moduleButtons[i];
                if (button == null) continue;
                bool unlocked = IsModuleUnlocked(role);
                string name = module == null || module.Weapon == null || string.IsNullOrWhiteSpace(module.Weapon.DisplayName)
                    ? Nicify(role.ToString())
                    : module.Weapon.DisplayName;
                IdleAutoDefenseModulePresentationToken token = _effectiveExperience.UiSettings.GetModuleToken(role);
                string icon = token == null ? "MODULE" : token.IconToken;
                string description = token == null ? string.Empty : token.PlayerDescription;
                int rank = ResolveModuleRank(role);
                int cost = ResolveModuleActionCost(role, unlocked);
                double damage = module == null ? 0d : module.BaseDamage + DamageUpgradeRank * module.DamagePerDamageRank + RangeUpgradeRank * module.DamagePerRangeRank;
                double range = module == null ? 0d : module.BaseRange + RangeUpgradeRank * (ActiveGameRules == null ? 0d : ActiveGameRules.ModuleRangeRankBonus);
                int cooldownTicks = module == null ? 0 : Math.Max(module.MinimumCooldownTicks, module.BaseCooldownTicks - AttackSpeedUpgradeRank * module.CooldownReductionPerFireRateRank);
                double cadence = SimulationTicksPerSecond <= 0 ? 0d : (double)cooldownTicks / SimulationTicksPerSecond;
                string action = unlocked ? ResolveModuleUpgradeLabel(role) : "UNLOCK";
                button.text = icon + "  " + name + "\n" + (unlocked ? "ONLINE" : "LOCKED") + "  |  " + Nicify(role.ToString()) +
                    "\nDMG " + damage.ToString("0.#", CultureInfo.InvariantCulture) + "  CAD " + cadence.ToString("0.00", CultureInfo.InvariantCulture) + "s  RNG " + range.ToString("0.#", CultureInfo.InvariantCulture) +
                    "\nRank " + rank.ToString(CultureInfo.InvariantCulture) + " -> " + (rank + 1).ToString(CultureInfo.InvariantCulture) + "  |  " + action + " " + cost.ToString(CultureInfo.InvariantCulture) + " " + PrimaryCurrencyToken +
                    (_compactLayout ? string.Empty : "\n" + description);
                bool affordable = EncounterRunning && RuntimeCurrency >= cost;
                button.SetEnabled(unlocked || cost > 0);
                button.style.borderTopColor = affordable ? _activeTheme.Success : unlocked ? _activeTheme.Accent : _activeTheme.Warning;
                button.style.borderBottomColor = button.style.borderTopColor;
                button.style.borderLeftColor = button.style.borderTopColor;
                button.style.borderRightColor = button.style.borderTopColor;
                button.style.opacity = unlocked ? 1f : 0.88f;
            }
        }

        private void RefreshRewardCards()
        {
            bool visible = RewardDraftActive && CurrentFlowState == IdleAutoDefensePlayerFlowState.Running;
            SetVisible(_rewardOverlay, visible);
            if (!visible) return;
            for (int i = 0; i < _rewardButtons.Length; i++)
            {
                Button button = _rewardButtons[i];
                if (i >= RewardDraftChoices.Count)
                {
                    button.style.display = DisplayStyle.None;
                    continue;
                }
                IdleAutoDefenseRewardDraftChoice choice = RewardDraftChoices[i];
                int rank = GetRewardDraftChoiceCurrentRank(choice);
                button.style.display = DisplayStyle.Flex;
                button.text = (i + 1).ToString(CultureInfo.InvariantCulture) + "  " + choice.RarityName.ToUpperInvariant() + "\n" +
                    choice.TypeName + "  |  " + choice.TargetName + "\n\n" +
                    choice.DisplayName + "\n\n" + choice.EffectDescription + "\n\n" +
                    (choice.IsUnlock ? "UNLOCK  |  Eligible now" : "RANK " + rank.ToString(CultureInfo.InvariantCulture) + " -> " + (rank + 1).ToString(CultureInfo.InvariantCulture) + "  |  Eligible now");
                Color rarity = _activeTheme.GetRarityColor(choice.Rarity);
                button.style.backgroundColor = Color.Lerp(_activeTheme.Panel, rarity, 0.18f);
                SetBorder(button, 3, rarity, 6);
            }
        }

        private void RefreshThreatUi()
        {
            if (!TryGetPrimaryMajorThreat(out IdleAutoDefenseMajorThreatSnapshot threat))
            {
                SetVisible(_threatBar, false);
                SetVisible(_threatMarker, false);
                return;
            }
            SetVisible(_threatBar, true);
            _threatLabel.text = (threat.Boss ? "BOSS  " : "ELITE  ") + threat.DisplayName + "  " + threat.Health.ToString("0", CultureInfo.InvariantCulture) + " / " + threat.MaximumHealth.ToString("0", CultureInfo.InvariantCulture);
            SetPercentWidth(_threatFill, threat.HealthNormalized);
            Color color = threat.Boss ? _activeTheme.Danger : _activeTheme.Warning;
            _threatFill.style.backgroundColor = color;
            SetBorder(_threatBar, 1, color, 4);

            Camera camera = Camera.main;
            if (camera == null)
            {
                SetVisible(_threatMarker, false);
                return;
            }
            Vector3 screen = camera.WorldToScreenPoint(threat.WorldPosition);
            bool offscreen = screen.z <= 0f || screen.x < 40f || screen.x > Screen.width - 40f || screen.y < 40f || screen.y > Screen.height - 40f;
            SetVisible(_threatMarker, offscreen);
            if (!offscreen) return;
            float panelWidth = Mathf.Max(1f, _safeAreaRoot.resolvedStyle.width);
            float panelHeight = Mathf.Max(1f, _safeAreaRoot.resolvedStyle.height);
            float x = screen.z <= 0f ? panelWidth - screen.x / Mathf.Max(1f, Screen.width) * panelWidth : screen.x / Mathf.Max(1f, Screen.width) * panelWidth;
            float y = panelHeight - screen.y / Mathf.Max(1f, Screen.height) * panelHeight;
            _threatMarker.style.left = Mathf.Clamp(x - 55f, 12f, panelWidth - 122f);
            _threatMarker.style.top = Mathf.Clamp(y - 20f, 132f, panelHeight - 190f);
            _threatMarker.text = (threat.Boss ? "BOSS" : "ELITE") + "  >";
            _threatMarker.style.color = color;
            SetBorder(_threatMarker, 2, color, 5);
        }

        private void RefreshBuildStats()
        {
            if (_buildStatsLabel == null) return;
            _buildStatsLabel.text =
                "Mounted modules: " + UnlockedModuleCount.ToString(CultureInfo.InvariantCulture) + " / 4\n" +
                "Damage rank: " + DamageUpgradeRank.ToString(CultureInfo.InvariantCulture) + "   Cadence rank: " + AttackSpeedUpgradeRank.ToString(CultureInfo.InvariantCulture) + "   Range rank: " + RangeUpgradeRank.ToString(CultureInfo.InvariantCulture) + "   Repair rank: " + RepairUpgradeRank.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Rewards acquired: " + RewardDraftSelectionCount.ToString(CultureInfo.InvariantCulture) + "   Epic: " + EpicRewardSelectionCount.ToString(CultureInfo.InvariantCulture) + "   Legendary: " + LegendaryRewardSelectionCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                PrimaryCurrencyDisplayName + " earned: " + RuntimeCurrencyEarned.ToString(CultureInfo.InvariantCulture) + "   Spent: " + RuntimeCurrencySpent.ToString(CultureInfo.InvariantCulture) + "   Current: " + RuntimeCurrency.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Reward multiplier bonus: " + RewardCreditMultiplierBonus.ToString("0.##", CultureInfo.InvariantCulture) + "   Projectile speed: x" + ProjectileSpeedMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "\n" +
                _effectiveExperience.UiSettings.OverdriveName + ": " + (OverdriveActive ? "Active" : OverdriveCooldownSecondsRemaining > 0f ? "Cooling down" : "Ready") + "   Cost: " + OverdriveCost.ToString(CultureInfo.InvariantCulture) + " " + PrimaryCurrencyDisplayName + "\n" +
                "Persistent totals: " + _profile.LifetimeCredits.ToString(CultureInfo.InvariantCulture) + " " + PrimaryCurrencyDisplayName.ToLowerInvariant() + ", " + _profile.LifetimeParts.ToString(CultureInfo.InvariantCulture) + " " + SecondaryCurrencyDisplayName.ToLowerInvariant();
        }

        private void RefreshTutorial()
        {
            if (_effectiveExperience.Tutorial == null || _effectiveExperience.Tutorial.Steps.Count == 0) return;
            _tutorialStepIndex = Mathf.Clamp(_tutorialStepIndex, 0, _effectiveExperience.Tutorial.Steps.Count - 1);
            IdleAutoDefenseTutorialStep step = _effectiveExperience.Tutorial.Steps[_tutorialStepIndex];
            _tutorialProgressLabel.text = "BRIEFING " + (_tutorialStepIndex + 1).ToString(CultureInfo.InvariantCulture) + " / " + _effectiveExperience.Tutorial.Steps.Count.ToString(CultureInfo.InvariantCulture);
            _tutorialTitleLabel.text = step.Title;
            _tutorialBodyLabel.text = step.Body;
        }

        private void RefreshOfflineClaim()
        {
            if (!HasOfflineRewardPreview || ActiveOfflineProgression == null) return;
            long credits = GetRewardAmount(_offlinePreview, ActiveOfflineProgression.ProductionCurrencyId);
            long parts = GetRewardAmount(_offlinePreview, ActiveOfflineProgression.CycleCurrencyId);
            _offlineTitleLabel.text = "Welcome Back";
            _offlineBodyLabel.text =
                "Time away: " + FormatDuration(_offlinePreview.RawElapsed.TotalSeconds) + "\n\n" +
                credits.ToString(CultureInfo.InvariantCulture) + " " + PrimaryCurrencyDisplayName.ToLowerInvariant() + "\n" + parts.ToString(CultureInfo.InvariantCulture) + " " + SecondaryCurrencyDisplayName.ToLowerInvariant() + "\n\n" +
                "Authored rate: " + ActiveOfflineProgression.ProductionAmountPerSecond.ToString("0.##", CultureInfo.InvariantCulture) + " " + PrimaryCurrencyDisplayName.ToLowerInvariant() + " / second\n" +
                "Effective time: " + FormatDuration(_offlinePreview.EffectiveElapsed.TotalSeconds) + (_offlinePreview.Capped ? " (cap applied)" : string.Empty) + "\n" +
                "Rounding: " + ActiveOfflineProgression.Rounding + "\n\n" +
                "The optional multiplier is omitted because no rewarded placement is currently available.";
        }

        private void RefreshRunSummary()
        {
            _summaryTitleLabel.text = EncounterCompleted ? _effectiveExperience.UiSettings.VictoryTitle : _effectiveExperience.UiSettings.DefeatTitle;
            _summaryTitleLabel.style.color = EncounterCompleted ? _activeTheme.Success : _activeTheme.Danger;
            _summaryBodyLabel.text =
                "Run profile: " + (ActiveRunProfile == null ? "Authored Run" : ActiveRunProfile.DisplayName) + "\n" +
                "Run time: " + FormatDuration(SurvivalSeconds) + "\n" +
                "Wave reached: " + CurrentWaveNumber.ToString(CultureInfo.InvariantCulture) + " / " + TotalWaveCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Enemies defeated: " + (DirectOrCombatKillCount + ProjectileAdapterKillCount).ToString(CultureInfo.InvariantCulture) + "\n" +
                "Elites defeated: " + EliteDefeatCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Boss defeated: " + (BossDefeatCount > 0 ? "Yes" : "No") + "\n\n" +
                PrimaryCurrencyDisplayName + " earned / spent: " + RuntimeCurrencyEarned.ToString(CultureInfo.InvariantCulture) + " / " + RuntimeCurrencySpent.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Run reward: " + EncounterRewardCredits.ToString(CultureInfo.InvariantCulture) + " " + PrimaryCurrencyDisplayName.ToLowerInvariant() + ", " + EncounterRewardParts.ToString(CultureInfo.InvariantCulture) + " " + SecondaryCurrencyDisplayName.ToLowerInvariant() + "\n" +
                "Upgrades acquired: " + SelectedUpgradeCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Epic / Legendary: " + EpicRewardSelectionCount.ToString(CultureInfo.InvariantCulture) + " / " + LegendaryRewardSelectionCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Module ranks: Damage " + DamageUpgradeRank.ToString(CultureInfo.InvariantCulture) + ", Cadence " + AttackSpeedUpgradeRank.ToString(CultureInfo.InvariantCulture) + ", Range " + RangeUpgradeRank.ToString(CultureInfo.InvariantCulture) + ", Repair " + RepairUpgradeRank.ToString(CultureInfo.InvariantCulture) + "\n" +
                _effectiveExperience.UiSettings.ObjectiveLabel + " damage taken: " + Math.Max(0d, ObjectiveMaximumHealth - ObjectiveHealth).ToString("0", CultureInfo.InvariantCulture) + "\n\n" +
                "Per-module damage attribution is not exposed by the current combat runtime, so no fabricated top-module statistic is shown.";
        }

        private void RefreshSettingsControls()
        {
            if (_masterSlider == null) return;
            _masterSlider.SetValueWithoutNotify(_profile.MasterVolume);
            _uiSlider.SetValueWithoutNotify(_profile.UiVolume);
            _combatSlider.SetValueWithoutNotify(_profile.CombatVolume);
            _rewardSlider.SetValueWithoutNotify(_profile.RewardWarningVolume);
            _motionSlider.SetValueWithoutNotify(_profile.MotionIntensity);
        }

        private void ApplyTheme()
        {
            if (_playerUiRoot == null || _activeTheme == null) return;
            ApplyThemeRecursive(_playerUiRoot);
            if (_mainMenuOverlay != null) _mainMenuOverlay.style.backgroundColor = WithAlpha(_activeTheme.Background, 0.94f);
            if (_timerLabel != null) _timerLabel.style.color = _activeTheme.PrimaryText;
            if (_healthFill != null) _healthFill.style.backgroundColor = _activeTheme.Success;
            if (_xpFill != null) _xpFill.style.backgroundColor = _activeTheme.Accent;
            if (_themeFallbackLabel != null) _themeFallbackLabel.style.color = _activeTheme.Warning;
            RefreshPlayerUi();
        }

        private void ApplyThemeRecursive(VisualElement element)
        {
            if (element is Label label) label.style.color = _activeTheme.PrimaryText;
            if (element is Button button)
            {
                button.style.backgroundColor = _activeTheme.PanelRaised;
                button.style.color = _activeTheme.PrimaryText;
                SetBorder(button, 1, _activeTheme.Accent, 6);
            }
            if (element.name != null && (element.name.Contains("panel") || element.name.Contains("objective-health")))
                element.style.backgroundColor = _activeTheme.Panel;
            for (int i = 0; i < element.childCount; i++) ApplyThemeRecursive(element[i]);
        }

        private void OnPlayerUiGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveLayout(evt.newRect.width, evt.newRect.height);
        }

        private void ApplyResponsiveLayout(float width, float height)
        {
            if (_playerUiRoot == null || width <= 0f || height <= 0f) return;
            IdleAutoDefenseUiSettingsAsset settings = _effectiveExperience.UiSettings;
            _compactLayout = ShouldUseCompactLayout(width, height, settings);
            bool portrait = ShouldShowPortraitMessage(width, height, settings);
            SetVisible(_portraitOverlay, portrait);
            ApplySafeArea(width, height);
            if (_moduleBar != null) _moduleBar.style.height = _compactLayout ? 118 : 142;
            if (_toastLabel != null) _toastLabel.style.bottom = _compactLayout ? 132 : 160;
            for (int i = 0; i < _moduleButtons.Length; i++)
                if (_moduleButtons[i] != null) _moduleButtons[i].style.fontSize = _compactLayout ? 10 : 11;
            for (int i = 0; i < _rewardButtons.Length; i++)
                if (_rewardButtons[i] != null) _rewardButtons[i].style.fontSize = _compactLayout ? 12 : 15;
        }

        private void ApplySafeArea(float panelWidth, float panelHeight)
        {
            if (_safeAreaRoot == null) return;
            if (!_effectiveExperience.UiSettings.RespectSafeArea || Screen.width <= 0 || Screen.height <= 0)
            {
                _safeAreaRoot.style.paddingLeft = 0;
                _safeAreaRoot.style.paddingRight = 0;
                _safeAreaRoot.style.paddingTop = 0;
                _safeAreaRoot.style.paddingBottom = 0;
                return;
            }
            Vector4 insets = CalculateSafeAreaInsets(new Rect(0, 0, Screen.width, Screen.height), Screen.safeArea);
            _safeAreaRoot.style.paddingLeft = insets.x * panelWidth / Screen.width;
            _safeAreaRoot.style.paddingTop = insets.y * panelHeight / Screen.height;
            _safeAreaRoot.style.paddingRight = insets.z * panelWidth / Screen.width;
            _safeAreaRoot.style.paddingBottom = insets.w * panelHeight / Screen.height;
        }

        public static Vector4 CalculateSafeAreaInsets(Rect screen, Rect safeArea)
        {
            return new Vector4(
                Mathf.Max(0f, safeArea.xMin - screen.xMin),
                Mathf.Max(0f, screen.yMax - safeArea.yMax),
                Mathf.Max(0f, screen.xMax - safeArea.xMax),
                Mathf.Max(0f, safeArea.yMin - screen.yMin));
        }

        public static bool ShouldUseCompactLayout(float width, float height, IdleAutoDefenseUiSettingsAsset settings)
        {
            if (settings == null) return width < 1000f || height < 560f;
            return width < settings.CompactWidthThreshold || height < settings.CompactHeightThreshold;
        }

        public static bool ShouldShowPortraitMessage(float width, float height, IdleAutoDefenseUiSettingsAsset settings)
        {
            float threshold = settings == null ? 0.95f : settings.PortraitAspectThreshold;
            return height / Mathf.Max(1f, width) > threshold;
        }

        private bool IsModuleUnlocked(IdleAutoDefenseModuleRole role)
        {
            if (role == IdleAutoDefenseModuleRole.PrecisionBeam) return PulseBeamUnlocked;
            if (role == IdleAutoDefenseModuleRole.AreaBurst) return ArcBurstUnlocked;
            if (role == IdleAutoDefenseModuleRole.HomingProjectile) return HomingPulseUnlocked;
            return true;
        }

        private int ResolveModuleRank(IdleAutoDefenseModuleRole role)
        {
            if (role == IdleAutoDefenseModuleRole.AreaBurst) return RangeUpgradeRank;
            if (role == IdleAutoDefenseModuleRole.HomingProjectile) return AttackSpeedUpgradeRank;
            return DamageUpgradeRank;
        }

        private int ResolveModuleActionCost(IdleAutoDefenseModuleRole role, bool unlocked)
        {
            if (!unlocked)
            {
                if (role == IdleAutoDefenseModuleRole.PrecisionBeam) return PulseBeamUnlockCost;
                if (role == IdleAutoDefenseModuleRole.AreaBurst) return ArcBurstUnlockCost;
                if (role == IdleAutoDefenseModuleRole.HomingProjectile) return HomingPulseUnlockCost;
            }
            if (role == IdleAutoDefenseModuleRole.AreaBurst) return RangeUpgradeCost;
            if (role == IdleAutoDefenseModuleRole.HomingProjectile) return AttackSpeedUpgradeCost;
            return DamageUpgradeCost;
        }

        private static string ResolveModuleUpgradeLabel(IdleAutoDefenseModuleRole role)
        {
            if (role == IdleAutoDefenseModuleRole.AreaBurst) return "RANGE";
            if (role == IdleAutoDefenseModuleRole.HomingProjectile) return "CADENCE";
            return "DAMAGE";
        }

        private void HideAllModalOverlays()
        {
            SetVisible(_mainMenuOverlay, false);
            SetVisible(_pauseOverlay, false);
            SetVisible(_rewardOverlay, false);
            SetVisible(_tutorialOverlay, false);
            SetVisible(_offlineOverlay, false);
            SetVisible(_summaryOverlay, false);
            SetVisible(_resetConfirmationOverlay, false);
        }

        private VisualElement Overlay(string name, float alpha)
        {
            var overlay = new VisualElement { name = name };
            FillAbsolute(overlay);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            overlay.style.paddingLeft = 18;
            overlay.style.paddingRight = 18;
            overlay.style.paddingTop = 14;
            overlay.style.paddingBottom = 14;
            overlay.style.backgroundColor = WithAlpha(_activeTheme == null ? Color.black : _activeTheme.Background, alpha);
            overlay.pickingMode = PickingMode.Position;
            return overlay;
        }

        private VisualElement ModalPanel(VisualElement parent, float maxWidth, float maxHeight)
        {
            var panel = new VisualElement { name = "modal-panel" };
            panel.style.width = Length.Percent(92);
            panel.style.maxWidth = maxWidth;
            panel.style.maxHeight = maxHeight;
            panel.style.flexGrow = 1;
            panel.style.paddingLeft = 22;
            panel.style.paddingRight = 22;
            panel.style.paddingTop = 20;
            panel.style.paddingBottom = 20;
            panel.style.backgroundColor = _activeTheme == null ? new Color(0.04f, 0.06f, 0.08f, 0.98f) : _activeTheme.Panel;
            SetBorder(panel, 2, _activeTheme == null ? Color.cyan : _activeTheme.Accent, 7);
            parent.Add(panel);
            return panel;
        }

        private VisualElement HudBlock(float width)
        {
            var block = new VisualElement { name = "hud-block" };
            block.style.width = width;
            block.style.minHeight = 58;
            block.style.paddingLeft = 9;
            block.style.paddingRight = 9;
            block.style.paddingTop = 6;
            block.style.paddingBottom = 6;
            block.style.backgroundColor = _activeTheme == null ? new Color(0.02f, 0.03f, 0.04f, 0.82f) : WithAlpha(_activeTheme.Panel, 0.9f);
            SetBorder(block, 1, _activeTheme == null ? Color.cyan : _activeTheme.Accent, 5);
            return block;
        }

        private static VisualElement ButtonRow(VisualElement parent)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.justifyContent = Justify.Center;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 8;
            row.style.marginBottom = 8;
            parent.Add(row);
            return row;
        }

        private void AddSectionHeading(VisualElement parent, string text)
        {
            Label label = AddLabel(parent, text, 21, FontStyle.Bold);
            label.style.marginTop = 12;
            label.style.marginBottom = 8;
            label.style.color = _activeTheme.Accent;
        }

        private Label AddLabel(VisualElement parent, string text, int fontSize, FontStyle style = FontStyle.Normal)
        {
            var label = new Label(text);
            ApplyRuntimeUiFont(label);
            label.style.fontSize = fontSize;
            label.style.unityFontStyleAndWeight = style;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = _activeTheme == null ? Color.white : _activeTheme.PrimaryText;
            label.style.minHeight = fontSize + 4;
            parent.Add(label);
            return label;
        }

        private Button AddButton(VisualElement parent, string text, Action clicked, float minWidth, float height)
        {
            var button = new Button(clicked) { text = text };
            ApplyRuntimeUiFont(button);
            button.style.minWidth = minWidth;
            button.style.height = Mathf.Max(_effectiveExperience.UiSettings.MinimumTouchTarget, height);
            button.style.minHeight = Mathf.Max(_effectiveExperience.UiSettings.MinimumTouchTarget, height);
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            button.style.marginTop = 4;
            button.style.marginBottom = 4;
            button.style.fontSize = 14;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.backgroundColor = _activeTheme == null ? new Color(0.08f, 0.12f, 0.16f, 1f) : _activeTheme.PanelRaised;
            button.style.color = _activeTheme == null ? Color.white : _activeTheme.PrimaryText;
            SetBorder(button, 1, _activeTheme == null ? Color.cyan : _activeTheme.Accent, 6);
            button.RegisterCallback<PointerEnterEvent>(_ => button.style.borderTopWidth = 3);
            button.RegisterCallback<PointerLeaveEvent>(_ => button.style.borderTopWidth = 1);
            parent.Add(button);
            return button;
        }

        private static VisualElement BarTrack(float width, float height)
        {
            var track = new VisualElement();
            track.style.width = width;
            track.style.height = height;
            track.style.backgroundColor = new Color(0f, 0f, 0f, 0.65f);
            track.style.overflow = Overflow.Hidden;
            SetBorder(track, 1, new Color(1f, 1f, 1f, 0.55f), 3);
            return track;
        }

        private static VisualElement BarFill(VisualElement track)
        {
            var fill = new VisualElement();
            fill.style.position = Position.Absolute;
            fill.style.left = 0;
            fill.style.top = 0;
            fill.style.bottom = 0;
            fill.style.width = Length.Percent(100);
            track.Add(fill);
            return fill;
        }

        private static void FillAbsolute(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.right = 0;
            element.style.top = 0;
            element.style.bottom = 0;
            element.style.width = Length.Percent(100);
            element.style.height = Length.Percent(100);
        }

        private static void SetBorder(VisualElement element, float width, Color color, float radius)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        private static void SetPercentWidth(VisualElement element, float normalized)
        {
            if (element != null) element.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static string FormatDuration(double seconds)
        {
            int total = Math.Max(0, (int)Math.Ceiling(seconds));
            int minutes = total / 60;
            int remainder = total % 60;
            return minutes.ToString("00", CultureInfo.InvariantCulture) + ":" + remainder.ToString("00", CultureInfo.InvariantCulture);
        }

        private static string Nicify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var result = new System.Text.StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                if (i > 0 && char.IsUpper(value[i]) && !char.IsUpper(value[i - 1])) result.Append(' ');
                result.Append(value[i]);
            }
            return result.ToString();
        }
    }
}
