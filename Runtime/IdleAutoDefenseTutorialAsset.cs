using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [Serializable]
    public sealed class IdleAutoDefenseTutorialStep
    {
        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [SerializeField] private string _body;
        [SerializeField] private string _focusTarget;

        public IdleAutoDefenseTutorialStep() { }

        public IdleAutoDefenseTutorialStep(string id, string title, string body, string focusTarget)
        {
            _id = id ?? string.Empty;
            _title = title ?? string.Empty;
            _body = body ?? string.Empty;
            _focusTarget = focusTarget ?? string.Empty;
        }

        public string Id => _id ?? string.Empty;
        public string Title => _title ?? string.Empty;
        public string Body => _body ?? string.Empty;
        public string FocusTarget => _focusTarget ?? string.Empty;
    }

    [CreateAssetMenu(menuName = "Deucarian/Idle Auto Defense/Tutorial", fileName = "IdleAutoDefenseTutorial")]
    public sealed class IdleAutoDefenseTutorialAsset : ScriptableObject
    {
        [SerializeField] private string _id = "tutorial.idle-auto-defense.first-run";
        [SerializeField] private string _displayName = "First Defense Briefing";
        [SerializeField] private IdleAutoDefenseTutorialStep[] _steps = CreateDefaultSteps();

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public IReadOnlyList<IdleAutoDefenseTutorialStep> Steps => _steps ?? Array.Empty<IdleAutoDefenseTutorialStep>();

        public void Configure(string id, string displayName, IReadOnlyList<IdleAutoDefenseTutorialStep> steps)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            if (steps == null)
            {
                _steps = Array.Empty<IdleAutoDefenseTutorialStep>();
                return;
            }
            _steps = new IdleAutoDefenseTutorialStep[steps.Count];
            for (int i = 0; i < steps.Count; i++) _steps[i] = steps[i];
        }

        public bool IsValid(out string message)
        {
            if (string.IsNullOrWhiteSpace(Id) || Steps.Count < 10)
            {
                message = "Tutorial requires an ID and the ten core briefing steps.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Steps.Count; i++)
            {
                IdleAutoDefenseTutorialStep step = Steps[i];
                if (step == null || string.IsNullOrWhiteSpace(step.Id) || string.IsNullOrWhiteSpace(step.Title) || string.IsNullOrWhiteSpace(step.Body) || !ids.Add(step.Id))
                {
                    message = "Tutorial steps require unique IDs, titles, and body copy.";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        public static IdleAutoDefenseTutorialAsset CreateTransient()
        {
            var asset = CreateInstance<IdleAutoDefenseTutorialAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            return asset;
        }

        private static IdleAutoDefenseTutorialStep[] CreateDefaultSteps()
        {
            return new[]
            {
                new IdleAutoDefenseTutorialStep("tutorial.protect-core", "Protect the Core", "Keep the central objective alive until the authored defense run ends.", "objective-health"),
                new IdleAutoDefenseTutorialStep("tutorial.authored-waves", "Read the Wave", "Enemies arrive in authored waves. The timer and wave display show how far the defense has progressed.", "run-timer"),
                new IdleAutoDefenseTutorialStep("tutorial.automatic-defense", "Automatic Fire", "Mounted defense modules acquire targets and attack automatically.", "module-bar"),
                new IdleAutoDefenseTutorialStep("tutorial.earn-credits", "Earn Credits", "Defeated enemies and passive income provide credits for the current run.", "credits"),
                new IdleAutoDefenseTutorialStep("tutorial.modules", "Build the Mounts", "Unlock or improve all four mounted modules from the controls along the bottom edge.", "module-bar"),
                new IdleAutoDefenseTutorialStep("tutorial.reward-draft", "Choose One Reward", "When a draft opens, choose one of three eligible authored rewards. Combat waits for your decision.", "reward-draft"),
                new IdleAutoDefenseTutorialStep("tutorial.rarity-paths", "Shape the Build", "Normal rewards build ranks; Epic and Legendary rewards create stronger authored paths.", "reward-progress"),
                new IdleAutoDefenseTutorialStep("tutorial.overdrive", "Use Overdrive", "Spend the shown credits to empower mounted defenses for the authored duration. Press Space or tap the ability.", "overdrive"),
                new IdleAutoDefenseTutorialStep("tutorial.major-threats", "Major Threats", "Elite and boss health bars appear at the top. A marker keeps important threats readable offscreen.", "threat-bar"),
                new IdleAutoDefenseTutorialStep("tutorial.persistent-progress", "Return Stronger", "Run rewards and authored offline production support persistent progress between sessions.", "offline-claim")
            };
        }
    }
}
