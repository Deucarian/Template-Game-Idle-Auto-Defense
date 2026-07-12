using System;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [CreateAssetMenu(menuName = "Deucarian/Idle Auto Defense/Presentation Theme", fileName = "IdleAutoDefenseTheme")]
    public sealed class IdleAutoDefenseThemeAsset : ScriptableObject
    {
        [SerializeField] private string _id = "theme.idle-auto-defense.default";
        [SerializeField] private string _displayName = "Bastion Command";
        [SerializeField] private Color _background = new Color(0.018f, 0.025f, 0.035f, 0.96f);
        [SerializeField] private Color _panel = new Color(0.045f, 0.065f, 0.085f, 0.97f);
        [SerializeField] private Color _panelRaised = new Color(0.075f, 0.105f, 0.13f, 0.98f);
        [SerializeField] private Color _primaryText = new Color(0.94f, 0.97f, 1f, 1f);
        [SerializeField] private Color _secondaryText = new Color(0.68f, 0.76f, 0.82f, 1f);
        [SerializeField] private Color _accent = new Color(0.18f, 0.82f, 0.92f, 1f);
        [SerializeField] private Color _warning = new Color(1f, 0.72f, 0.18f, 1f);
        [SerializeField] private Color _danger = new Color(0.96f, 0.25f, 0.22f, 1f);
        [SerializeField] private Color _success = new Color(0.24f, 0.88f, 0.5f, 1f);
        [SerializeField] private Color _currency = new Color(1f, 0.86f, 0.28f, 1f);
        [SerializeField] private Color _common = new Color(0.72f, 0.76f, 0.8f, 1f);
        [SerializeField] private Color _uncommon = new Color(0.32f, 0.84f, 0.48f, 1f);
        [SerializeField] private Color _rare = new Color(0.26f, 0.62f, 1f, 1f);
        [SerializeField] private Color _epic = new Color(0.72f, 0.38f, 0.98f, 1f);
        [SerializeField] private Color _legendary = new Color(1f, 0.62f, 0.12f, 1f);
        [SerializeField] private string _fontStyleToken = "compact-command";

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public Color Background => _background;
        public Color Panel => _panel;
        public Color PanelRaised => _panelRaised;
        public Color PrimaryText => _primaryText;
        public Color SecondaryText => _secondaryText;
        public Color Accent => _accent;
        public Color Warning => _warning;
        public Color Danger => _danger;
        public Color Success => _success;
        public Color Currency => _currency;
        public string FontStyleToken => _fontStyleToken ?? string.Empty;

        public Color GetRarityColor(IdleAutoDefenseRewardRarity rarity)
        {
            switch (rarity)
            {
                case IdleAutoDefenseRewardRarity.Uncommon: return _uncommon;
                case IdleAutoDefenseRewardRarity.Rare: return _rare;
                case IdleAutoDefenseRewardRarity.Epic: return _epic;
                case IdleAutoDefenseRewardRarity.Legendary: return _legendary;
                default: return _common;
            }
        }

        public void Configure(
            string id,
            string displayName,
            Color background,
            Color panel,
            Color panelRaised,
            Color primaryText,
            Color secondaryText,
            Color accent,
            Color warning,
            Color danger,
            Color success,
            Color currency,
            Color common,
            Color uncommon,
            Color rare,
            Color epic,
            Color legendary,
            string fontStyleToken)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _background = background;
            _panel = panel;
            _panelRaised = panelRaised;
            _primaryText = primaryText;
            _secondaryText = secondaryText;
            _accent = accent;
            _warning = warning;
            _danger = danger;
            _success = success;
            _currency = currency;
            _common = common;
            _uncommon = uncommon;
            _rare = rare;
            _epic = epic;
            _legendary = legendary;
            _fontStyleToken = fontStyleToken ?? string.Empty;
        }

        public bool IsValid(out string message)
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(DisplayName))
            {
                message = "Theme identity is incomplete.";
                return false;
            }

            if (Background.a <= 0f || Panel.a <= 0f || PrimaryText.a <= 0f || Accent.a <= 0f)
            {
                message = "Theme colors must be visible.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        public static IdleAutoDefenseThemeAsset CreateTransient(bool alternate = false)
        {
            var asset = CreateInstance<IdleAutoDefenseThemeAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            if (alternate)
            {
                asset.Configure(
                    "theme.idle-auto-defense.neon-bastion",
                    "Neon Bastion",
                    new Color(0.025f, 0.018f, 0.055f, 0.96f),
                    new Color(0.065f, 0.04f, 0.11f, 0.97f),
                    new Color(0.11f, 0.055f, 0.17f, 0.98f),
                    new Color(0.98f, 0.95f, 1f, 1f),
                    new Color(0.78f, 0.7f, 0.86f, 1f),
                    new Color(0.25f, 0.95f, 0.82f, 1f),
                    new Color(1f, 0.78f, 0.2f, 1f),
                    new Color(1f, 0.22f, 0.45f, 1f),
                    new Color(0.35f, 1f, 0.55f, 1f),
                    new Color(1f, 0.84f, 0.24f, 1f),
                    new Color(0.78f, 0.76f, 0.85f, 1f),
                    new Color(0.3f, 0.92f, 0.5f, 1f),
                    new Color(0.22f, 0.7f, 1f, 1f),
                    new Color(0.86f, 0.34f, 1f, 1f),
                    new Color(1f, 0.55f, 0.14f, 1f),
                    "neon-command");
            }
            return asset;
        }
    }
}
