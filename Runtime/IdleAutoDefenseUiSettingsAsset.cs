using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [Serializable]
    public sealed class IdleAutoDefenseModulePresentationToken
    {
        [SerializeField] private IdleAutoDefenseModuleRole _role;
        [SerializeField] private string _iconToken;
        [SerializeField] private string _playerDescription;

        public IdleAutoDefenseModulePresentationToken() { }

        public IdleAutoDefenseModulePresentationToken(IdleAutoDefenseModuleRole role, string iconToken, string playerDescription)
        {
            _role = role;
            _iconToken = iconToken ?? string.Empty;
            _playerDescription = playerDescription ?? string.Empty;
        }

        public IdleAutoDefenseModuleRole Role => _role;
        public string IconToken => _iconToken ?? string.Empty;
        public string PlayerDescription => _playerDescription ?? string.Empty;
    }

    [CreateAssetMenu(menuName = "Deucarian/Idle Auto Defense/UI Settings", fileName = "IdleAutoDefenseUiSettings")]
    public sealed class IdleAutoDefenseUiSettingsAsset : ScriptableObject
    {
        [SerializeField] private string _id = "ui-settings.idle-auto-defense.mobile-landscape";
        [SerializeField] private string _gameTitle = "Bastion: Idle Defense";
        [SerializeField] private string _gameDescription = "Command four mounted defenses through a short authored siege.";
        [SerializeField] private string _overdriveName = "Overdrive";
        [SerializeField] private string _overdriveDescription = "Temporarily boosts mounted defense damage and cadence.";
        [SerializeField] private bool _respectSafeArea = true;
        [SerializeField] private float _minimumTouchTarget = 44f;
        [SerializeField] private float _compactWidthThreshold = 1000f;
        [SerializeField] private float _compactHeightThreshold = 560f;
        [SerializeField] private float _portraitAspectThreshold = 0.95f;
        [SerializeField] private IdleAutoDefenseModulePresentationToken[] _moduleTokens = CreateDefaultModuleTokens();

        public string Id => _id ?? string.Empty;
        public string GameTitle => _gameTitle ?? string.Empty;
        public string GameDescription => _gameDescription ?? string.Empty;
        public string OverdriveName => _overdriveName ?? string.Empty;
        public string OverdriveDescription => _overdriveDescription ?? string.Empty;
        public bool RespectSafeArea => _respectSafeArea;
        public float MinimumTouchTarget => Mathf.Max(36f, _minimumTouchTarget);
        public float CompactWidthThreshold => Mathf.Max(640f, _compactWidthThreshold);
        public float CompactHeightThreshold => Mathf.Max(360f, _compactHeightThreshold);
        public float PortraitAspectThreshold => Mathf.Clamp(_portraitAspectThreshold, 0.7f, 1.1f);
        public IReadOnlyList<IdleAutoDefenseModulePresentationToken> ModuleTokens => _moduleTokens ?? Array.Empty<IdleAutoDefenseModulePresentationToken>();

        public IdleAutoDefenseModulePresentationToken GetModuleToken(IdleAutoDefenseModuleRole role)
        {
            for (int i = 0; i < ModuleTokens.Count; i++)
                if (ModuleTokens[i] != null && ModuleTokens[i].Role == role)
                    return ModuleTokens[i];
            return null;
        }

        public bool IsValid(out string message)
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(GameTitle) || string.IsNullOrWhiteSpace(GameDescription))
            {
                message = "UI settings require stable identity and player-facing title copy.";
                return false;
            }
            if (ModuleTokens.Count != 4 || MinimumTouchTarget < 44f)
            {
                message = "UI settings require four module tokens and a 44px minimum touch target.";
                return false;
            }
            message = string.Empty;
            return true;
        }

        public static IdleAutoDefenseUiSettingsAsset CreateTransient()
        {
            var asset = CreateInstance<IdleAutoDefenseUiSettingsAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            return asset;
        }

        private static IdleAutoDefenseModulePresentationToken[] CreateDefaultModuleTokens()
        {
            return new[]
            {
                new IdleAutoDefenseModulePresentationToken(IdleAutoDefenseModuleRole.StartingProjectile, "SHARD", "Reliable projectile mount with balanced damage and range."),
                new IdleAutoDefenseModulePresentationToken(IdleAutoDefenseModuleRole.PrecisionBeam, "BEAM", "Precision beam for durable priority targets."),
                new IdleAutoDefenseModulePresentationToken(IdleAutoDefenseModuleRole.AreaBurst, "BURST", "Area burst mount for clustered pressure."),
                new IdleAutoDefenseModulePresentationToken(IdleAutoDefenseModuleRole.HomingProjectile, "SEEK", "Homing projectiles that track distant targets.")
            };
        }
    }
}
