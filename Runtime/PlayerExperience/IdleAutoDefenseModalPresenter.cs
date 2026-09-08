using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseModalPresenter
    {
        private readonly IdleAutoDefensePlayerView _view;
        internal IdleAutoDefenseModalPresenter(IdleAutoDefensePlayerView view) => _view = view;

        internal void BuildRewardDraft()
        {
            _view._rewardOverlay = _view.Style.Overlay("reward-draft-overlay", 0.87f);
            _view._rewardOverlay.style.display = DisplayStyle.None;
            _view._safeAreaRoot.Add(_view._rewardOverlay);
            Label title = _view.Style.AddLabel(_view._rewardOverlay, "Choose an Authored Reward", 31, FontStyle.Bold);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 4;
            Label hint = _view.Style.AddLabel(_view._rewardOverlay, "Combat paused  |  Tap a card or press 1, 2, 3", 14, FontStyle.Bold);
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            hint.style.marginBottom = 12;
            _view._rewardCardRow = new VisualElement { name = "reward-card-row" };
            _view._rewardCardRow.style.width = Length.Percent(94);
            _view._rewardCardRow.style.maxWidth = 1180;
            _view._rewardCardRow.style.flexDirection = FlexDirection.Row;
            _view._rewardCardRow.style.flexGrow = 1;
            _view._rewardCardRow.style.maxHeight = 430;
            _view._rewardOverlay.Add(_view._rewardCardRow);
            for (int i = 0; i < _view._rewardButtons.Length; i++)
            {
                int index = i;
                Button card = _view.Style.AddButton(_view._rewardCardRow, string.Empty, () => _view.App.ChooseRewardCard(index), 250, 300);
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
                _view._rewardButtons[i] = card;
            }
        }

        internal void BuildTutorial()
        {
            _view._tutorialOverlay = _view.Style.Overlay("tutorial-overlay", 0.84f);
            _view._tutorialOverlay.style.display = DisplayStyle.None;
            _view._safeAreaRoot.Add(_view._tutorialOverlay);
            VisualElement panel = _view.Style.ModalPanel(_view._tutorialOverlay, 680, 500);
            _view._tutorialProgressLabel = _view.Style.AddLabel(panel, string.Empty, 13, FontStyle.Bold);
            _view._tutorialTitleLabel = _view.Style.AddLabel(panel, string.Empty, 29, FontStyle.Bold);
            _view._tutorialTitleLabel.style.marginTop = 10;
            _view._tutorialBodyLabel = _view.Style.AddLabel(panel, string.Empty, 18);
            _view._tutorialBodyLabel.style.flexGrow = 1;
            _view._tutorialBodyLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            VisualElement row = _view.Style.ButtonRow(panel);
            _view.Style.AddButton(row, "Skip", _view.App.CompleteTutorial, 120, 48);
            _view.Style.AddButton(row, "Next", _view.App.AdvanceTutorial, 170, 48);
        }

        internal void BuildOfflineClaim()
        {
            _view._offlineOverlay = _view.Style.Overlay("offline-claim-overlay", 0.86f);
            _view._offlineOverlay.style.display = DisplayStyle.None;
            _view._safeAreaRoot.Add(_view._offlineOverlay);
            VisualElement panel = _view.Style.ModalPanel(_view._offlineOverlay, 620, 520);
            _view._offlineTitleLabel = _view.Style.AddLabel(panel, "Welcome Back", 30, FontStyle.Bold);
            _view._offlineTitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _view._offlineBodyLabel = _view.Style.AddLabel(panel, string.Empty, 17);
            _view._offlineBodyLabel.style.flexGrow = 1;
            _view._offlineBodyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            Button claim = _view.Style.AddButton(panel, "Claim Resources", _view.App.ClaimOfflineReward, 260, 52);
            claim.name = "offline-claim-button";
        }

        internal void BuildRunSummary()
        {
            _view._summaryOverlay = _view.Style.Overlay("run-summary-overlay", 0.9f);
            _view._summaryOverlay.style.display = DisplayStyle.None;
            _view._safeAreaRoot.Add(_view._summaryOverlay);
            VisualElement panel = _view.Style.ModalPanel(_view._summaryOverlay, 760, 680);
            _view._summaryTitleLabel = _view.Style.AddLabel(panel, string.Empty, 32, FontStyle.Bold);
            _view._summaryTitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            _view._summaryBodyLabel = _view.Style.AddLabel(scroll, string.Empty, 15);
            panel.Add(scroll);
            VisualElement row = _view.Style.ButtonRow(panel);
            _view.Style.AddButton(row, "Restart Run", _view.App.RestartCurrentRun, 180, 50);
            _view.Style.AddButton(row, "Main Menu", _view.App.ReturnToMainMenu, 180, 50);
        }

        internal void BuildResetConfirmation()
        {
            _view._resetConfirmationOverlay = _view.Style.Overlay("reset-confirmation-overlay", 0.9f);
            _view._resetConfirmationOverlay.style.display = DisplayStyle.None;
            _view._safeAreaRoot.Add(_view._resetConfirmationOverlay);
            VisualElement panel = _view.Style.ModalPanel(_view._resetConfirmationOverlay, 500, 320);
            Label title = _view.Style.AddLabel(panel, "Reset Progress?", 27, FontStyle.Bold);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            Label body = _view.Style.AddLabel(panel, "This clears tutorial, settings, theme choice, offline timestamp, and template progression totals.", 16);
            body.style.flexGrow = 1;
            body.style.unityTextAlign = TextAnchor.MiddleCenter;
            VisualElement row = _view.Style.ButtonRow(panel);
            _view.Style.AddButton(row, "Cancel", _view.App.CancelResetProgress, 140, 48);
            _view.Style.AddButton(row, "Reset", _view.App.ConfirmResetProgress, 140, 48);
        }

        internal void BuildPortraitMessage()
        {
            _view._portraitOverlay = _view.Style.Overlay("portrait-rotate-overlay", 0.96f);
            _view._portraitOverlay.style.display = DisplayStyle.None;
            _view._playerUiRoot.Add(_view._portraitOverlay);
            Label title = _view.Style.AddLabel(_view._portraitOverlay, "Rotate Device", 31, FontStyle.Bold);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            Label body = _view.Style.AddLabel(_view._portraitOverlay, _view.App._effectiveExperience.UiSettings.PortraitMessage, 17);
            body.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        internal void BuildDebugPanel()
        {
            _view._debugPanel = new VisualElement { name = "debug-panel" };
            _view._debugPanel.style.display = DisplayStyle.None;
            _view._debugPanel.style.position = Position.Absolute;
            _view._debugPanel.style.left = 12;
            _view._debugPanel.style.top = 132;
            _view._debugPanel.style.width = 390;
            _view._debugPanel.style.maxHeight = 310;
            _view._debugPanel.style.paddingLeft = 9;
            _view._debugPanel.style.paddingRight = 9;
            _view._debugPanel.style.paddingTop = 8;
            _view._debugPanel.style.paddingBottom = 8;
            _view.Style.SetBorder(_view._debugPanel, 1, Color.white, 5);
            _view._debugLabel = _view.Style.AddLabel(_view._debugPanel, string.Empty, 11);
            _view._safeAreaRoot.Add(_view._debugPanel);
        }

        internal void RefreshRewardCards()
        {
            bool visible = _view.App.RunState.RewardDraftActive && _view.App.CurrentFlowState == IdleAutoDefensePlayerFlowState.Running;
            IdleAutoDefensePlayerView.SetVisible(_view._rewardOverlay, visible);
            if (!visible) return;
            for (int i = 0; i < _view._rewardButtons.Length; i++)
            {
                Button button = _view._rewardButtons[i];
                if (i >= _view.App.RunState.RewardDraftChoices.Count)
                {
                    button.style.display = DisplayStyle.None;
                    continue;
                }
                IdleAutoDefenseRewardDraftChoice choice = _view.App.RunState.RewardDraftChoices[i];
                int rank = _view.App.RunState.GetRewardDraftChoiceCurrentRank(choice);
                button.style.display = DisplayStyle.Flex;
                button.text = (i + 1).ToString(CultureInfo.InvariantCulture) + "  " + choice.RarityName.ToUpperInvariant() + "\n" +
                    choice.TypeName + "  |  " + choice.TargetName + "\n\n" +
                    choice.DisplayName + "\n\n" + choice.EffectDescription + "\n\n" +
                    (choice.IsUnlock ? "UNLOCK  |  Eligible now" : "RANK " + rank.ToString(CultureInfo.InvariantCulture) + " -> " + (rank + 1).ToString(CultureInfo.InvariantCulture) + "  |  Eligible now");
                Color rarity = _view.App._activeTheme.GetRarityColor(choice.Rarity);
                button.style.backgroundColor = Color.Lerp(_view.App._activeTheme.Panel, rarity, 0.18f);
                _view.Style.SetBorder(button, 3, rarity, 6);
            }
        }

        internal void RefreshTutorial()
        {
            if (_view.App._effectiveExperience.Tutorial == null || _view.App._effectiveExperience.Tutorial.Steps.Count == 0) return;
            int stepIndex = Mathf.Clamp(_view.App._tutorialStepIndex, 0, _view.App._effectiveExperience.Tutorial.Steps.Count - 1);
            IdleAutoDefenseTutorialStep step = _view.App._effectiveExperience.Tutorial.Steps[stepIndex];
            _view._tutorialProgressLabel.text = "BRIEFING " + (_view.App._tutorialStepIndex + 1).ToString(CultureInfo.InvariantCulture) + " / " + _view.App._effectiveExperience.Tutorial.Steps.Count.ToString(CultureInfo.InvariantCulture);
            _view._tutorialTitleLabel.text = step.Title;
            _view._tutorialBodyLabel.text = step.Body;
        }

        internal void RefreshOfflineClaim()
        {
            if (!_view.App.HasOfflineRewardPreview || _view.App.RunState.ActiveOfflineProgression == null) return;
            long credits = IdleAutoDefenseProfileSession.GetRewardAmount(_view.App._offlinePreview, _view.App.RunState.ActiveOfflineProgression.ProductionCurrencyId);
            long parts = IdleAutoDefenseProfileSession.GetRewardAmount(_view.App._offlinePreview, _view.App.RunState.ActiveOfflineProgression.CycleCurrencyId);
            _view._offlineTitleLabel.text = "Welcome Back";
            _view._offlineBodyLabel.text =
                "Time away: " + _view.Style.FormatDuration(_view.App._offlinePreview.RawElapsed.TotalSeconds) + "\n\n" +
                credits.ToString(CultureInfo.InvariantCulture) + " " + _view.App.PrimaryCurrencyDisplayName.ToLowerInvariant() + "\n" + parts.ToString(CultureInfo.InvariantCulture) + " " + _view.App.SecondaryCurrencyDisplayName.ToLowerInvariant() + "\n\n" +
                "Authored rate: " + _view.App.RunState.ActiveOfflineProgression.ProductionAmountPerSecond.ToString("0.##", CultureInfo.InvariantCulture) + " " + _view.App.PrimaryCurrencyDisplayName.ToLowerInvariant() + " / second\n" +
                "Effective time: " + _view.Style.FormatDuration(_view.App._offlinePreview.EffectiveElapsed.TotalSeconds) + (_view.App._offlinePreview.Capped ? " (cap applied)" : string.Empty) + "\n" +
                "Rounding: " + _view.App.RunState.ActiveOfflineProgression.Rounding + "\n\n" +
                "The optional multiplier is omitted because no rewarded placement is currently available.";
        }

        internal void RefreshRunSummary()
        {
            _view._summaryTitleLabel.text = _view.App.RunState.EncounterCompleted ? _view.App._effectiveExperience.UiSettings.VictoryTitle : _view.App._effectiveExperience.UiSettings.DefeatTitle;
            _view._summaryTitleLabel.style.color = _view.App.RunState.EncounterCompleted ? _view.App._activeTheme.Success : _view.App._activeTheme.Danger;
            _view._summaryBodyLabel.text =
                "Run profile: " + (_view.App.RunState.ActiveRunProfile == null ? "Authored Run" : _view.App.RunState.ActiveRunProfile.DisplayName) + "\n" +
                "Run time: " + _view.Style.FormatDuration(_view.App.RunState.SurvivalSeconds) + "\n" +
                "Wave reached: " + _view.App.RunState.CurrentWaveNumber.ToString(CultureInfo.InvariantCulture) + " / " + _view.App.RunState.TotalWaveCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Enemies defeated: " + (_view.App.RunState.DirectOrCombatKillCount + _view.App.RunState.ProjectileAdapterKillCount).ToString(CultureInfo.InvariantCulture) + "\n" +
                "Elites defeated: " + _view.App.RunState.EliteDefeatCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Boss defeated: " + (_view.App.RunState.BossDefeatCount > 0 ? "Yes" : "No") + "\n\n" +
                _view.App.PrimaryCurrencyDisplayName + " earned / spent: " + _view.App.RunState.RuntimeCurrencyEarned.ToString(CultureInfo.InvariantCulture) + " / " + _view.App.RunState.RuntimeCurrencySpent.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Run reward: " + _view.App.RunState.EncounterRewardCredits.ToString(CultureInfo.InvariantCulture) + " " + _view.App.PrimaryCurrencyDisplayName.ToLowerInvariant() + ", " + _view.App.RunState.EncounterRewardParts.ToString(CultureInfo.InvariantCulture) + " " + _view.App.SecondaryCurrencyDisplayName.ToLowerInvariant() + "\n" +
                "Upgrades acquired: " + _view.App.RunState.SelectedUpgradeCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Epic / Legendary: " + _view.App.RunState.EpicRewardSelectionCount.ToString(CultureInfo.InvariantCulture) + " / " + _view.App.RunState.LegendaryRewardSelectionCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Module ranks: Damage " + _view.App.RunState.DamageUpgradeRank.ToString(CultureInfo.InvariantCulture) + ", Cadence " + _view.App.RunState.AttackSpeedUpgradeRank.ToString(CultureInfo.InvariantCulture) + ", Range " + _view.App.RunState.RangeUpgradeRank.ToString(CultureInfo.InvariantCulture) + ", Repair " + _view.App.RunState.RepairUpgradeRank.ToString(CultureInfo.InvariantCulture) + "\n" +
                _view.App._effectiveExperience.UiSettings.ObjectiveLabel + " damage taken: " + Math.Max(0d, _view.App.RunState.ObjectiveMaximumHealth - _view.App.RunState.ObjectiveHealth).ToString("0", CultureInfo.InvariantCulture) + "\n\n" +
                "Per-module damage attribution is not exposed by the current combat runtime, so no fabricated top-module statistic is shown.";
        }
    }
}
