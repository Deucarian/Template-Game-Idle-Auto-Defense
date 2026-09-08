using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseMenuPresenter
    {
        private readonly IdleAutoDefensePlayerView _view;
        internal IdleAutoDefenseMenuPresenter(IdleAutoDefensePlayerView view) => _view = view;

        internal void BuildMainMenu()
        {
            _view._mainMenuOverlay = _view.Style.Overlay("main-menu-overlay", 0.86f);
            _view._safeAreaRoot.Add(_view._mainMenuOverlay);
            VisualElement panel = _view.Style.ModalPanel(_view._mainMenuOverlay, 560, 660);
            Label title = _view.Style.AddLabel(panel, _view.App._effectiveExperience.UiSettings.GameTitle, 38, FontStyle.Bold);
            title.name = "main-menu-title";
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            Label description = _view.Style.AddLabel(panel, _view.App._effectiveExperience.UiSettings.GameDescription, 17);
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            description.style.marginBottom = 12;
            _view._mainProfileLabel = _view.Style.AddLabel(panel, string.Empty, 15, FontStyle.Bold);
            _view._mainProfileLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _view._mainProfileLabel.style.marginBottom = 16;
            Button start = _view.Style.AddButton(panel, "Start Run", _view.App.StartFreshRun, 300, 54);
            start.name = "start-run-button";
            start.SetEnabled(_view.App.PlayerExperienceValid && !_view.App.RunState.StartupBlocked);
            _view.Style.AddButton(panel, "How to Play", () => _view.App.OpenTutorial(false), 300, 48);
            _view.Style.AddButton(panel, "Settings", () => _view.App.OpenSettings(true), 300, 48);
            _view.Style.AddButton(panel, "Reset Progress", _view.App.RequestResetProgress, 300, 48);
            if (!Application.isEditor && Application.platform != RuntimePlatform.WebGLPlayer)
                _view.Style.AddButton(panel, "Quit", Application.Quit, 300, 48);
            _view._mainErrorLabel = _view.Style.AddLabel(panel, string.Empty, 13, FontStyle.Bold);
            _view._mainErrorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _view._mainErrorLabel.style.marginTop = 10;
        }

        internal void BuildPauseMenu()
        {
            _view._pauseOverlay = _view.Style.Overlay("pause-overlay", 0.78f);
            _view._pauseOverlay.style.display = DisplayStyle.None;
            _view._safeAreaRoot.Add(_view._pauseOverlay);
            VisualElement panel = _view.Style.ModalPanel(_view._pauseOverlay, 820, 650);
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            panel.Add(header);
            Label title = _view.Style.AddLabel(header, _view.App._effectiveExperience.UiSettings.PauseMenuTitle, 27, FontStyle.Bold);
            title.style.flexGrow = 1;
            _view.Style.AddButton(header, "Resume", _view.App.ResumeRun, 112, 46);
            _view._pauseBody = new ScrollView(ScrollViewMode.Vertical) { name = "pause-scroll" };
            _view._pauseBody.style.flexGrow = 1;
            _view._pauseBody.style.marginTop = 10;
            panel.Add(_view._pauseBody);
        }

        internal void ShowPauseSection(IdleAutoDefensePauseSection section)
        {
            if (_view._pauseBody == null) return;
            _view._pauseBody.Clear();
            VisualElement nav = _view.Style.ButtonRow(_view._pauseBody);
            _view.Style.AddButton(nav, "Pause", () => ShowPauseSection(IdleAutoDefensePauseSection.Main), 120, 44);
            _view.Style.AddButton(nav, "Build", () => ShowPauseSection(IdleAutoDefensePauseSection.Build), 120, 44);
            _view.Style.AddButton(nav, "Settings", () => ShowPauseSection(IdleAutoDefensePauseSection.Settings), 120, 44);

            if (section == IdleAutoDefensePauseSection.Build)
            {
                _view.Style.AddSectionHeading(_view._pauseBody, "Current Build");
                _view._buildStatsLabel = _view.Style.AddLabel(_view._pauseBody, string.Empty, 14);
                RefreshBuildStats();
                BuildPersistentResearchControls(_view._pauseBody);
                return;
            }

            if (section == IdleAutoDefensePauseSection.Settings)
            {
                BuildSettingsSection(_view._pauseBody);
                return;
            }

            _view.Style.AddSectionHeading(_view._pauseBody, "Run Paused");
            _view.Style.AddButton(_view._pauseBody, "Resume", _view.App.ResumeRun, 260, 48);
            _view.Style.AddButton(_view._pauseBody, "Current Build", _view.App.OpenBuildView, 260, 48);
            _view.Style.AddButton(_view._pauseBody, "How to Play", () => _view.App.OpenTutorial(false), 260, 48);
            _view.Style.AddButton(_view._pauseBody, "Restart Run", _view.App.RestartCurrentRun, 260, 48);
            _view.Style.AddButton(_view._pauseBody, "Return to Main Menu", _view.App.ReturnToMainMenu, 260, 48);
        }

        internal void BuildSettingsSection(VisualElement parent)
        {
            _view.Style.AddSectionHeading(parent, "Settings");
            _view._masterSlider = AddSlider(parent, "Master Volume", _view.App._profile.MasterVolume, _view.App.SetMasterVolume);
            _view._uiSlider = AddSlider(parent, "UI Volume", _view.App._profile.UiVolume, _view.App.SetUiVolume);
            _view._combatSlider = AddSlider(parent, "Combat Volume", _view.App._profile.CombatVolume, _view.App.SetCombatVolume);
            _view._rewardSlider = AddSlider(parent, "Reward / Warning Volume", _view.App._profile.RewardWarningVolume, _view.App.SetRewardWarningVolume);
            _view._motionSlider = AddSlider(parent, "Motion Intensity", _view.App._profile.MotionIntensity, _view.App.SetMotionIntensity);
            _view.Style.AddSectionHeading(parent, "Theme");
            VisualElement themeRow = _view.Style.ButtonRow(parent);
            for (int i = 0; i < _view.App._effectiveExperience.Themes.Count; i++)
            {
                IdleAutoDefenseThemeAsset theme = _view.App._effectiveExperience.Themes[i];
                if (theme == null) continue;
                string themeId = theme.Id;
                _view.Style.AddButton(themeRow, theme.DisplayName, () => _view.App.SelectTheme(themeId), 170, 46);
            }
            _view.Style.AddButton(parent, "Replay Tutorial", _view.App.ReplayTutorial, 240, 46);
            RefreshSettingsControls();
        }

        internal void BuildPersistentResearchControls(VisualElement parent)
        {
            if (_view.App.RunState.ActiveProgression == null || _view.App.RunState.ActiveProgression.ResearchNodes.Count == 0) return;
            _view.Style.AddSectionHeading(parent, "Persistent Research");
            for (int i = 0; i < _view.App.RunState.ActiveProgression.ResearchNodes.Count; i++)
            {
                IdleAutoDefenseResearchNodeRecord node = _view.App.RunState.ActiveProgression.ResearchNodes[i];
                if (node == null) continue;
                int rank = _view.App.RunState.GetPersistentResearchRank(node.Id);
                bool maxed = rank >= node.MaxRank;
                long cost = maxed || rank >= node.RankCosts.Count ? 0L : node.RankCosts[rank];
                long balance = _view.App.RunState.GetPersistentCurrencyBalance(node.CostCurrencyId);
                string nodeId = node.Id;
                Button button = _view.Style.AddButton(
                    parent,
                    node.DisplayName + "  Rank " + rank.ToString(CultureInfo.InvariantCulture) + " / " + node.MaxRank.ToString(CultureInfo.InvariantCulture) +
                    (maxed ? "  MAX" : "  |  " + cost.ToString(CultureInfo.InvariantCulture) + " " + _view.Style.Nicify(node.CostCurrencyId.Split('.')[node.CostCurrencyId.Split('.').Length - 1])) +
                    "\n" + _view.Style.Nicify(node.EffectKind.ToString()) + "  |  Bank " + balance.ToString(CultureInfo.InvariantCulture),
                    () =>
                    {
                        _view.App.TryPurchasePersistentUpgradeFromUi(nodeId);
                        ShowPauseSection(IdleAutoDefensePauseSection.Build);
                    },
                    320,
                    58);
                button.style.whiteSpace = WhiteSpace.Normal;
                button.SetEnabled(!maxed);
            }
        }

        internal Slider AddSlider(VisualElement parent, string label, float value, Action<float> changed)
        {
            var slider = new Slider(label, 0f, 1f) { value = value };
            IdleAutoDefenseTemplateController.ApplyRuntimeUiFont(slider);
            slider.style.minHeight = 42;
            slider.style.marginTop = 3;
            slider.style.marginBottom = 3;
            slider.RegisterValueChangedCallback(evt => changed(evt.newValue));
            parent.Add(slider);
            return slider;
        }

        internal void RefreshMainMenu()
        {
            if (_view._mainProfileLabel == null) return;
            string profile = _view.App.RunState.ActiveRunProfile == null ? "Authored run profile" : _view.App.RunState.ActiveRunProfile.DisplayName;
            _view._mainProfileLabel.text = profile + "  |  " + _view.Style.FormatDuration(_view.App.RunState.SessionLengthSeconds) + " defense";
            _view._mainErrorLabel.text = string.IsNullOrWhiteSpace(_view.App._playerFacingError) ? string.Empty : _view.App._playerFacingError;
            _view._mainErrorLabel.style.display = string.IsNullOrWhiteSpace(_view.App._playerFacingError) ? DisplayStyle.None : DisplayStyle.Flex;
            _view._mainErrorLabel.style.color = _view.App._activeTheme.Danger;
        }

        internal void RefreshBuildStats()
        {
            if (_view._buildStatsLabel == null) return;
            _view._buildStatsLabel.text =
                "Mounted modules: " + _view.App.RunState.UnlockedModuleCount.ToString(CultureInfo.InvariantCulture) + " / 4\n" +
                "Damage rank: " + _view.App.RunState.DamageUpgradeRank.ToString(CultureInfo.InvariantCulture) + "   Cadence rank: " + _view.App.RunState.AttackSpeedUpgradeRank.ToString(CultureInfo.InvariantCulture) + "   Range rank: " + _view.App.RunState.RangeUpgradeRank.ToString(CultureInfo.InvariantCulture) + "   Repair rank: " + _view.App.RunState.RepairUpgradeRank.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Rewards acquired: " + _view.App.RunState.RewardDraftSelectionCount.ToString(CultureInfo.InvariantCulture) + "   Epic: " + _view.App.RunState.EpicRewardSelectionCount.ToString(CultureInfo.InvariantCulture) + "   Legendary: " + _view.App.RunState.LegendaryRewardSelectionCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                _view.App.PrimaryCurrencyDisplayName + " earned: " + _view.App.RunState.RuntimeCurrencyEarned.ToString(CultureInfo.InvariantCulture) + "   Spent: " + _view.App.RunState.RuntimeCurrencySpent.ToString(CultureInfo.InvariantCulture) + "   Current: " + _view.App.RunState.RuntimeCurrency.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Reward multiplier bonus: " + _view.App.RunState.RewardCreditMultiplierBonus.ToString("0.##", CultureInfo.InvariantCulture) + "   Projectile speed: x" + _view.App.RunState.ProjectileSpeedMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "\n" +
                _view.App._effectiveExperience.UiSettings.OverdriveName + ": " + (_view.App.RunState.OverdriveActive ? "Active" : _view.App.RunState.OverdriveCooldownSecondsRemaining > 0f ? "Cooling down" : "Ready") + "   Cost: " + _view.App.RunState.OverdriveCost.ToString(CultureInfo.InvariantCulture) + " " + _view.App.PrimaryCurrencyDisplayName + "\n" +
                "Persistent totals: " + _view.App._profile.LifetimeCredits.ToString(CultureInfo.InvariantCulture) + " " + _view.App.PrimaryCurrencyDisplayName.ToLowerInvariant() + ", " + _view.App._profile.LifetimeParts.ToString(CultureInfo.InvariantCulture) + " " + _view.App.SecondaryCurrencyDisplayName.ToLowerInvariant();
        }

        internal void RefreshSettingsControls()
        {
            if (_view._masterSlider == null) return;
            _view._masterSlider.SetValueWithoutNotify(_view.App._profile.MasterVolume);
            _view._uiSlider.SetValueWithoutNotify(_view.App._profile.UiVolume);
            _view._combatSlider.SetValueWithoutNotify(_view.App._profile.CombatVolume);
            _view._rewardSlider.SetValueWithoutNotify(_view.App._profile.RewardWarningVolume);
            _view._motionSlider.SetValueWithoutNotify(_view.App._profile.MotionIntensity);
        }
    }
}
