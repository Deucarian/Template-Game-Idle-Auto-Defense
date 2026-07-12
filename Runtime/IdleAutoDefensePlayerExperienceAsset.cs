using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [CreateAssetMenu(menuName = "Deucarian/Idle Auto Defense/Player Experience", fileName = "IdleAutoDefensePlayerExperience")]
    public sealed class IdleAutoDefensePlayerExperienceAsset : ScriptableObject
    {
        [SerializeField] private string _id = "player-experience.idle-auto-defense.playable";
        [SerializeField] private string _displayName = "Basic Idle Auto Defense Player Experience";
        [SerializeField] private IdleAutoDefenseUiSettingsAsset _uiSettings;
        [SerializeField] private IdleAutoDefenseTutorialAsset _tutorial;
        [SerializeField] private IdleAutoDefenseAudioPaletteAsset _audioPalette;
        [SerializeField] private IdleAutoDefenseThemeAsset[] _themes = Array.Empty<IdleAutoDefenseThemeAsset>();
        [SerializeField] private string _defaultThemeId = "theme.idle-auto-defense.default";

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public IdleAutoDefenseUiSettingsAsset UiSettings => _uiSettings;
        public IdleAutoDefenseTutorialAsset Tutorial => _tutorial;
        public IdleAutoDefenseAudioPaletteAsset AudioPalette => _audioPalette;
        public IReadOnlyList<IdleAutoDefenseThemeAsset> Themes => _themes ?? Array.Empty<IdleAutoDefenseThemeAsset>();
        public string DefaultThemeId => _defaultThemeId ?? string.Empty;

        public IdleAutoDefenseThemeAsset ResolveTheme(string themeId, out bool usedFallback)
        {
            usedFallback = false;
            IdleAutoDefenseThemeAsset defaultTheme = null;
            for (int i = 0; i < Themes.Count; i++)
            {
                IdleAutoDefenseThemeAsset theme = Themes[i];
                if (theme == null || !theme.IsValid(out _)) continue;
                if (string.Equals(theme.Id, DefaultThemeId, StringComparison.OrdinalIgnoreCase)) defaultTheme = theme;
                if (!string.IsNullOrWhiteSpace(themeId) && string.Equals(theme.Id, themeId, StringComparison.OrdinalIgnoreCase))
                    return theme;
            }

            usedFallback = true;
            if (defaultTheme != null) return defaultTheme;
            for (int i = 0; i < Themes.Count; i++)
                if (Themes[i] != null && Themes[i].IsValid(out _))
                    return Themes[i];
            return null;
        }

        public IReadOnlyList<string> Validate()
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) issues.Add("Player experience ID is required.");
            if (_uiSettings == null) issues.Add("UI settings are missing.");
            else if (!_uiSettings.IsValid(out string uiIssue)) issues.Add(uiIssue);
            if (_tutorial == null) issues.Add("Tutorial is missing.");
            else if (!_tutorial.IsValid(out string tutorialIssue)) issues.Add(tutorialIssue);
            if (_audioPalette == null) issues.Add("Audio palette is missing.");
            else if (!_audioPalette.IsValid(out string audioIssue)) issues.Add(audioIssue);
            if (Themes.Count < 2) issues.Add("Default and alternate authored themes are required.");

            var themeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasDefault = false;
            for (int i = 0; i < Themes.Count; i++)
            {
                IdleAutoDefenseThemeAsset theme = Themes[i];
                if (theme == null)
                {
                    issues.Add("Theme reference is missing.");
                    continue;
                }
                if (!theme.IsValid(out string themeIssue))
                {
                    issues.Add(themeIssue);
                    continue;
                }
                if (!themeIds.Add(theme.Id)) issues.Add("Theme ID '" + theme.Id + "' is duplicated.");
                if (string.Equals(theme.Id, DefaultThemeId, StringComparison.OrdinalIgnoreCase)) hasDefault = true;
            }
            if (!hasDefault) issues.Add("Default theme reference does not resolve.");
            return issues;
        }

        public void Configure(
            string id,
            string displayName,
            IdleAutoDefenseUiSettingsAsset uiSettings,
            IdleAutoDefenseTutorialAsset tutorial,
            IdleAutoDefenseAudioPaletteAsset audioPalette,
            IReadOnlyList<IdleAutoDefenseThemeAsset> themes,
            string defaultThemeId)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _uiSettings = uiSettings;
            _tutorial = tutorial;
            _audioPalette = audioPalette;
            _defaultThemeId = defaultThemeId ?? string.Empty;
            if (themes == null)
            {
                _themes = Array.Empty<IdleAutoDefenseThemeAsset>();
                return;
            }
            _themes = new IdleAutoDefenseThemeAsset[themes.Count];
            for (int i = 0; i < themes.Count; i++) _themes[i] = themes[i];
        }

        public static IdleAutoDefensePlayerExperienceAsset CreateTransient()
        {
            var asset = CreateInstance<IdleAutoDefensePlayerExperienceAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            IdleAutoDefenseThemeAsset defaultTheme = IdleAutoDefenseThemeAsset.CreateTransient();
            IdleAutoDefenseThemeAsset alternateTheme = IdleAutoDefenseThemeAsset.CreateTransient(true);
            asset.Configure(
                "player-experience.idle-auto-defense.playable",
                "Basic Idle Auto Defense Player Experience",
                IdleAutoDefenseUiSettingsAsset.CreateTransient(),
                IdleAutoDefenseTutorialAsset.CreateTransient(),
                IdleAutoDefenseAudioPaletteAsset.CreateTransient(),
                new[] { defaultTheme, alternateTheme },
                defaultTheme.Id);
            return asset;
        }
    }
}
