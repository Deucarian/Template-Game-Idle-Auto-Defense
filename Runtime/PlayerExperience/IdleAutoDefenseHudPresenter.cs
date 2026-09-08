using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseHudPresenter
    {
        private readonly IdleAutoDefensePlayerView _view;
        internal IdleAutoDefenseHudPresenter(IdleAutoDefensePlayerView view) => _view = view;

        internal void BuildHud()
        {
            _view._hudLayer = new VisualElement { name = "player-hud" };
            _view.Style.FillAbsolute(_view._hudLayer);
            _view._hudLayer.pickingMode = PickingMode.Ignore;
            _view._safeAreaRoot.Add(_view._hudLayer);

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
            _view._hudLayer.Add(top);

            VisualElement left = _view.Style.HudBlock(250);
            _view._profileLabel = _view.Style.AddLabel(left, string.Empty, 13, FontStyle.Bold);
            _view._waveLabel = _view.Style.AddLabel(left, string.Empty, 15, FontStyle.Bold);
            top.Add(left);

            VisualElement center = _view.Style.HudBlock(330);
            center.style.alignItems = Align.Center;
            _view._timerLabel = _view.Style.AddLabel(center, "04:40", 31, FontStyle.Bold);
            _view._timerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _view._threatBar = new VisualElement { name = "major-threat-bar" };
            _view._threatBar.style.display = DisplayStyle.None;
            _view._threatBar.style.width = Length.Percent(100);
            _view._threatBar.style.height = 18;
            _view._threatBar.style.marginTop = 2;
            _view._threatBar.style.backgroundColor = new Color(0f, 0f, 0f, 0.65f);
            _view.Style.SetBorder(_view._threatBar, 1, Color.white, 4);
            _view._threatFill = new VisualElement();
            _view._threatFill.style.position = Position.Absolute;
            _view._threatFill.style.left = 0;
            _view._threatFill.style.top = 0;
            _view._threatFill.style.bottom = 0;
            _view._threatFill.style.width = Length.Percent(100);
            _view._threatBar.Add(_view._threatFill);
            _view._threatLabel = _view.Style.AddLabel(_view._threatBar, string.Empty, 11, FontStyle.Bold);
            _view._threatLabel.style.position = Position.Absolute;
            _view._threatLabel.style.left = 5;
            _view._threatLabel.style.right = 5;
            _view._threatLabel.style.top = 1;
            _view._threatLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            center.Add(_view._threatBar);
            top.Add(center);

            VisualElement right = _view.Style.HudBlock(250);
            right.style.alignItems = Align.FlexEnd;
            _view._currencyLabel = _view.Style.AddLabel(right, string.Empty, 16, FontStyle.Bold);
            _view._levelLabel = _view.Style.AddLabel(right, string.Empty, 13, FontStyle.Bold);
            VisualElement xpTrack = _view.Style.BarTrack(220, 7);
            _view._xpFill = _view.Style.BarFill(xpTrack);
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
            _view.Style.SetBorder(objective, 1, Color.white, 6);
            _view._healthLabel = _view.Style.AddLabel(objective, string.Empty, 13, FontStyle.Bold);
            VisualElement healthTrack = _view.Style.BarTrack(228, 10);
            _view._healthFill = _view.Style.BarFill(healthTrack);
            objective.Add(healthTrack);
            _view._hudLayer.Add(objective);

            Button menuButton = _view.Style.AddButton(_view._hudLayer, "Menu", _view.App.TogglePause, 92, 46);
            menuButton.name = "touch-menu-button";
            menuButton.style.position = Position.Absolute;
            menuButton.style.right = 12;
            menuButton.style.top = 82;

            _view._overdriveStatusLabel = _view.Style.AddLabel(_view._hudLayer, string.Empty, 13, FontStyle.Bold);
            _view._overdriveStatusLabel.style.position = Position.Absolute;
            _view._overdriveStatusLabel.style.right = 116;
            _view._overdriveStatusLabel.style.top = 90;
            _view._overdriveStatusLabel.style.unityTextAlign = TextAnchor.MiddleRight;

            _view._threatMarker = _view.Style.AddLabel(_view._hudLayer, string.Empty, 14, FontStyle.Bold);
            _view._threatMarker.name = "major-threat-offscreen-marker";
            _view._threatMarker.style.display = DisplayStyle.None;
            _view._threatMarker.style.position = Position.Absolute;
            _view._threatMarker.style.paddingLeft = 8;
            _view._threatMarker.style.paddingRight = 8;
            _view._threatMarker.style.paddingTop = 5;
            _view._threatMarker.style.paddingBottom = 5;
            _view.Style.SetBorder(_view._threatMarker, 2, Color.white, 5);

            _view._toastLabel = _view.Style.AddLabel(_view._hudLayer, string.Empty, 15, FontStyle.Bold);
            _view._toastLabel.name = "player-toast";
            _view._toastLabel.style.position = Position.Absolute;
            _view._toastLabel.style.left = Length.Percent(30);
            _view._toastLabel.style.right = Length.Percent(30);
            _view._toastLabel.style.bottom = 160;
            _view._toastLabel.style.paddingTop = 7;
            _view._toastLabel.style.paddingBottom = 7;
            _view._toastLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _view._toastLabel.style.display = DisplayStyle.None;

            _view._themeFallbackLabel = _view.Style.AddLabel(_view._hudLayer, "Theme fallback active", 11, FontStyle.Bold);
            _view._themeFallbackLabel.style.position = Position.Absolute;
            _view._themeFallbackLabel.style.left = 12;
            _view._themeFallbackLabel.style.bottom = 156;

            BuildModuleBar();
        }

        internal void BuildModuleBar()
        {
            _view._moduleBar = new VisualElement { name = "module-bar" };
            _view._moduleBar.style.position = Position.Absolute;
            _view._moduleBar.style.left = 10;
            _view._moduleBar.style.right = 10;
            _view._moduleBar.style.bottom = 8;
            _view._moduleBar.style.height = 142;
            _view._moduleBar.style.flexDirection = FlexDirection.Row;
            _view._moduleBar.style.alignItems = Align.Stretch;
            _view._moduleBar.pickingMode = PickingMode.Position;
            _view._safeAreaRoot.Add(_view._moduleBar);

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
                Button card = _view.Style.AddButton(_view._moduleBar, string.Empty, () => _view.App.TryUseModuleAction(role), 150, 132);
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
                _view._moduleButtons[index] = card;
                _view._moduleLabels[index] = null;
            }

            VisualElement overdrive = new VisualElement { name = "overdrive-panel" };
            overdrive.style.width = 184;
            overdrive.style.minWidth = 156;
            overdrive.style.marginLeft = 4;
            overdrive.style.paddingLeft = 7;
            overdrive.style.paddingRight = 7;
            overdrive.style.paddingTop = 7;
            overdrive.style.paddingBottom = 7;
            _view.Style.SetBorder(overdrive, 2, Color.white, 6);
            Label ability = _view.Style.AddLabel(overdrive, _view.App._effectiveExperience.UiSettings.OverdriveName, 16, FontStyle.Bold);
            ability.style.unityTextAlign = TextAnchor.MiddleCenter;
            Label description = _view.Style.AddLabel(overdrive, _view.App._effectiveExperience.UiSettings.OverdriveDescription, 11);
            description.style.flexGrow = 1;
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            _view._overdriveButton = _view.Style.AddButton(overdrive, "Activate", () => _view.App.TryActivateOverdriveFromUi(), 150, 46);
            _view._overdriveButton.name = "overdrive-button";
            _view._moduleBar.Add(overdrive);
        }

        internal void RefreshPlayerUi()
        {
            if (_view._timerLabel == null) return;
            double remaining = Math.Max(0d, _view.App.RunState.SessionLengthSeconds - _view.App.RunState.SurvivalSeconds);
            _view._timerLabel.text = _view.Style.FormatDuration(remaining);
            _view._profileLabel.text = _view.App.RunState.ActiveRunProfile == null ? "Authored Run" : _view.App.RunState.ActiveRunProfile.DisplayName;
            _view._waveLabel.text = "Wave " + _view.App.RunState.CurrentWaveNumber.ToString(CultureInfo.InvariantCulture) + " / " + _view.App.RunState.TotalWaveCount.ToString(CultureInfo.InvariantCulture) +
                (_view.App.RunState.CurrentWaveNumber > 0 ? "  " + _view.App.RunState.CurrentSpawnProfileName : string.Empty);
            _view._healthLabel.text = _view.App._effectiveExperience.UiSettings.ObjectiveLabel.ToUpperInvariant() + "  " + _view.App.RunState.ObjectiveHealth.ToString("0", CultureInfo.InvariantCulture) + " / " + _view.App.RunState.ObjectiveMaximumHealth.ToString("0", CultureInfo.InvariantCulture);
            _view.Style.SetPercentWidth(_view._healthFill, _view.App.RunState.ObjectiveMaximumHealth <= 0d ? 0f : (float)(_view.App.RunState.ObjectiveHealth / _view.App.RunState.ObjectiveMaximumHealth));
            _view._healthFill.style.backgroundColor = _view.App.RunState.ObjectiveMaximumHealth > 0d && _view.App.RunState.ObjectiveHealth / _view.App.RunState.ObjectiveMaximumHealth <= 0.25d ? _view.App._activeTheme.Danger : _view.App._activeTheme.Success;
            _view._currencyLabel.text = _view.App.RunState.RuntimeCurrency.ToString(CultureInfo.InvariantCulture) + " " + _view.App.PrimaryCurrencyDisplayName.ToUpperInvariant();
            _view._currencyLabel.style.color = _view.App._activeTheme.Currency;
            _view._levelLabel.text = _view.App._effectiveExperience.UiSettings.PlayerRankLabel + " " + _view.App.RunState.CommanderLevel.ToString(CultureInfo.InvariantCulture) + "  XP " + _view.App.RunState.CommanderExperience.ToString(CultureInfo.InvariantCulture) + " / " + _view.App.RunState.ExperienceToNextLevel.ToString(CultureInfo.InvariantCulture);
            _view.Style.SetPercentWidth(_view._xpFill, _view.App.RunState.ExperienceToNextLevel <= 0 ? 0f : (float)_view.App.RunState.CommanderExperience / _view.App.RunState.ExperienceToNextLevel);
            _view._xpFill.style.backgroundColor = _view.App._activeTheme.Accent;
            string overdriveName = _view.App._effectiveExperience.UiSettings.OverdriveName;
            _view._overdriveStatusLabel.text = _view.App.RunState.OverdriveActive
                ? overdriveName.ToUpperInvariant() + " " + _view.App.RunState.OverdriveSecondsRemaining.ToString("0.0", CultureInfo.InvariantCulture) + "s"
                : _view.App.RunState.OverdriveCooldownSecondsRemaining > 0f
                    ? "Cooldown " + _view.App.RunState.OverdriveCooldownSecondsRemaining.ToString("0.0", CultureInfo.InvariantCulture) + "s"
                    : overdriveName + " ready";
            _view._overdriveStatusLabel.style.color = _view.App.RunState.OverdriveActive ? _view.App._activeTheme.Warning : _view.App._activeTheme.PrimaryText;
            _view._overdriveButton.text = _view.App.RunState.OverdriveActive
                ? _view.App.RunState.OverdriveSecondsRemaining.ToString("0.0", CultureInfo.InvariantCulture) + "s ACTIVE"
                : _view.App.RunState.OverdriveCooldownSecondsRemaining > 0f
                    ? _view.App.RunState.OverdriveCooldownSecondsRemaining.ToString("0.0", CultureInfo.InvariantCulture) + "s"
                    : "Activate  " + _view.App.RunState.OverdriveCost.ToString(CultureInfo.InvariantCulture) + " " + _view.App.PrimaryCurrencyToken;
            _view._overdriveButton.SetEnabled(_view.App.RunState.CanPurchaseOverdrive);
            RefreshModuleCards();
            _view.Modal.RefreshRewardCards();
            RefreshThreatUi();
            _view.Menu.RefreshBuildStats();
            _view._themeFallbackLabel.style.display = _view.App._themeFallbackVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _view._toastLabel.style.display = !string.IsNullOrWhiteSpace(_view.App._toastText) && Time.unscaledTime < _view.App._toastUntil ? DisplayStyle.Flex : DisplayStyle.None;
            _view._toastLabel.text = _view.App._toastText;
            if (_view.App._debugVisible)
                _view._debugLabel.text = _view.App.RunState.StatusSummary + "\nCore=" + _view.App.RunState.UsingAuthoredCore + "  Fallback=" + _view.App.RunState.FallbackModeActive + "  Pack=" + _view.App.RunState.UsingAssignedContentPack;
        }

        internal void RefreshModuleCards()
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
                IdleAutoDefenseModuleRule module = _view.App.RunState.ActiveGameRules == null ? null : _view.App.RunState.ActiveGameRules.GetModule(role);
                Button button = _view._moduleButtons[i];
                if (button == null) continue;
                bool unlocked = IsModuleUnlocked(role);
                string name = module == null || module.Weapon == null || string.IsNullOrWhiteSpace(module.Weapon.DisplayName)
                    ? _view.Style.Nicify(role.ToString())
                    : module.Weapon.DisplayName;
                IdleAutoDefenseModulePresentationToken token = _view.App._effectiveExperience.UiSettings.GetModuleToken(role);
                string icon = token == null ? "MODULE" : token.IconToken;
                string description = token == null ? string.Empty : token.PlayerDescription;
                int rank = ResolveModuleRank(role);
                int cost = ResolveModuleActionCost(role, unlocked);
                double damage = module == null ? 0d : module.BaseDamage + _view.App.RunState.DamageUpgradeRank * module.DamagePerDamageRank + _view.App.RunState.RangeUpgradeRank * module.DamagePerRangeRank;
                double range = module == null ? 0d : module.BaseRange + _view.App.RunState.RangeUpgradeRank * (_view.App.RunState.ActiveGameRules == null ? 0d : _view.App.RunState.ActiveGameRules.ModuleRangeRankBonus);
                int cooldownTicks = module == null ? 0 : Math.Max(module.MinimumCooldownTicks, module.BaseCooldownTicks - _view.App.RunState.AttackSpeedUpgradeRank * module.CooldownReductionPerFireRateRank);
                double cadence = _view.App.RunState.SimulationTicksPerSecond <= 0 ? 0d : (double)cooldownTicks / _view.App.RunState.SimulationTicksPerSecond;
                string action = unlocked ? ResolveModuleUpgradeLabel(role) : "UNLOCK";
                button.text = icon + "  " + name + "\n" + (unlocked ? "ONLINE" : "LOCKED") + "  |  " + _view.Style.Nicify(role.ToString()) +
                    "\nDMG " + damage.ToString("0.#", CultureInfo.InvariantCulture) + "  CAD " + cadence.ToString("0.00", CultureInfo.InvariantCulture) + "s  RNG " + range.ToString("0.#", CultureInfo.InvariantCulture) +
                    "\nRank " + rank.ToString(CultureInfo.InvariantCulture) + " -> " + (rank + 1).ToString(CultureInfo.InvariantCulture) + "  |  " + action + " " + cost.ToString(CultureInfo.InvariantCulture) + " " + _view.App.PrimaryCurrencyToken +
                    (_view._compactLayout ? string.Empty : "\n" + description);
                bool affordable = _view.App.RunState.EncounterRunning && _view.App.RunState.RuntimeCurrency >= cost;
                button.SetEnabled(unlocked || cost > 0);
                button.style.borderTopColor = affordable ? _view.App._activeTheme.Success : unlocked ? _view.App._activeTheme.Accent : _view.App._activeTheme.Warning;
                button.style.borderBottomColor = button.style.borderTopColor;
                button.style.borderLeftColor = button.style.borderTopColor;
                button.style.borderRightColor = button.style.borderTopColor;
                button.style.opacity = unlocked ? 1f : 0.88f;
            }
        }

        internal void RefreshThreatUi()
        {
            if (!_view.App.RunState.TryGetPrimaryMajorThreat(out IdleAutoDefenseMajorThreatSnapshot threat))
            {
                IdleAutoDefensePlayerView.SetVisible(_view._threatBar, false);
                IdleAutoDefensePlayerView.SetVisible(_view._threatMarker, false);
                return;
            }
            IdleAutoDefensePlayerView.SetVisible(_view._threatBar, true);
            _view._threatLabel.text = (threat.Boss ? "BOSS  " : "ELITE  ") + threat.DisplayName + "  " + threat.Health.ToString("0", CultureInfo.InvariantCulture) + " / " + threat.MaximumHealth.ToString("0", CultureInfo.InvariantCulture);
            _view.Style.SetPercentWidth(_view._threatFill, threat.HealthNormalized);
            Color color = threat.Boss ? _view.App._activeTheme.Danger : _view.App._activeTheme.Warning;
            _view._threatFill.style.backgroundColor = color;
            _view.Style.SetBorder(_view._threatBar, 1, color, 4);

            Camera camera = Camera.main;
            if (camera == null)
            {
                IdleAutoDefensePlayerView.SetVisible(_view._threatMarker, false);
                return;
            }
            Vector3 screen = camera.WorldToScreenPoint(threat.WorldPosition);
            bool offscreen = screen.z <= 0f || screen.x < 40f || screen.x > Screen.width - 40f || screen.y < 40f || screen.y > Screen.height - 40f;
            IdleAutoDefensePlayerView.SetVisible(_view._threatMarker, offscreen);
            if (!offscreen) return;
            float panelWidth = Mathf.Max(1f, _view._safeAreaRoot.resolvedStyle.width);
            float panelHeight = Mathf.Max(1f, _view._safeAreaRoot.resolvedStyle.height);
            float x = screen.z <= 0f ? panelWidth - screen.x / Mathf.Max(1f, Screen.width) * panelWidth : screen.x / Mathf.Max(1f, Screen.width) * panelWidth;
            float y = panelHeight - screen.y / Mathf.Max(1f, Screen.height) * panelHeight;
            _view._threatMarker.style.left = Mathf.Clamp(x - 55f, 12f, panelWidth - 122f);
            _view._threatMarker.style.top = Mathf.Clamp(y - 20f, 132f, panelHeight - 190f);
            _view._threatMarker.text = (threat.Boss ? "BOSS" : "ELITE") + "  >";
            _view._threatMarker.style.color = color;
            _view.Style.SetBorder(_view._threatMarker, 2, color, 5);
        }

        internal bool IsModuleUnlocked(IdleAutoDefenseModuleRole role)
        {
            if (role == IdleAutoDefenseModuleRole.PrecisionBeam) return _view.App.RunState.PulseBeamUnlocked;
            if (role == IdleAutoDefenseModuleRole.AreaBurst) return _view.App.RunState.ArcBurstUnlocked;
            if (role == IdleAutoDefenseModuleRole.HomingProjectile) return _view.App.RunState.HomingPulseUnlocked;
            return true;
        }

        internal int ResolveModuleRank(IdleAutoDefenseModuleRole role)
        {
            if (role == IdleAutoDefenseModuleRole.AreaBurst) return _view.App.RunState.RangeUpgradeRank;
            if (role == IdleAutoDefenseModuleRole.HomingProjectile) return _view.App.RunState.AttackSpeedUpgradeRank;
            return _view.App.RunState.DamageUpgradeRank;
        }

        internal int ResolveModuleActionCost(IdleAutoDefenseModuleRole role, bool unlocked)
        {
            if (!unlocked)
            {
                if (role == IdleAutoDefenseModuleRole.PrecisionBeam) return _view.App.RunState.PulseBeamUnlockCost;
                if (role == IdleAutoDefenseModuleRole.AreaBurst) return _view.App.RunState.ArcBurstUnlockCost;
                if (role == IdleAutoDefenseModuleRole.HomingProjectile) return _view.App.RunState.HomingPulseUnlockCost;
            }
            if (role == IdleAutoDefenseModuleRole.AreaBurst) return _view.App.RunState.RangeUpgradeCost;
            if (role == IdleAutoDefenseModuleRole.HomingProjectile) return _view.App.RunState.AttackSpeedUpgradeCost;
            return _view.App.RunState.DamageUpgradeCost;
        }

        internal string ResolveModuleUpgradeLabel(IdleAutoDefenseModuleRole role)
        {
            if (role == IdleAutoDefenseModuleRole.AreaBurst) return "RANGE";
            if (role == IdleAutoDefenseModuleRole.HomingProjectile) return "CADENCE";
            return "DAMAGE";
        }
    }
}
