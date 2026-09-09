using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefensePlayerInput
    {
        private readonly IdleAutoDefensePlayerExperience _app;
        internal IdleAutoDefensePlayerInput(IdleAutoDefensePlayerExperience app) => _app = app;

        internal void Handle()
        {
            if (Input.GetKeyDown(KeyCode.F1)) _app.ToggleDebugUi();
            if (_app.PortraitMessageVisible) return;

            if (_app.RunState.RewardDraftActive && _app.CurrentFlowState == IdleAutoDefensePlayerFlowState.Running)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) _app.ChooseRewardCard(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) _app.ChooseRewardCard(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) _app.ChooseRewardCard(2);
                return;
            }

            if (_app.TutorialVisible)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) _app.CompleteTutorial();
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) _app.AdvanceTutorial();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape)) _app.TogglePause();
            if (_app.CurrentFlowState == IdleAutoDefensePlayerFlowState.Running &&
                (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.B))) _app.OpenBuildView();
            if (_app.CurrentFlowState == IdleAutoDefensePlayerFlowState.Running && Input.GetKeyDown(KeyCode.Space)) _app.TryActivateOverdriveFromUi();
        }
    }
}
