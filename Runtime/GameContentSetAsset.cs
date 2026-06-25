using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public sealed class GameContentSetAsset : ScriptableObject
    {
        [SerializeField] private string _id = "contentset.example.idle-auto-defense";
        [SerializeField] private string _displayName = "Example Idle Auto Defense Content Set";
        [SerializeField] private string _description = "A playable authored recipe for one idle auto-defense run.";
        [SerializeField] private Sprite _icon;
        [SerializeField] private Texture2D _banner;
        [SerializeField] private WeaponDefinitionAsset _startingWeapon;
        [SerializeField] private WeaponDefinitionAsset[] _availableWeapons = Array.Empty<WeaponDefinitionAsset>();
        [SerializeField] private EnemyDefinitionAsset[] _enemyPool = Array.Empty<EnemyDefinitionAsset>();
        [SerializeField] private WaveDefinitionAsset[] _waveSet = Array.Empty<WaveDefinitionAsset>();
        [SerializeField] private RunUpgradeDefinitionAsset[] _upgradePool = Array.Empty<RunUpgradeDefinitionAsset>();
        [SerializeField] private int _startingCredits = 60;
        [SerializeField] private int _startingParts;
        [SerializeField] private float _rewardMultiplier = 1f;
        [SerializeField] private float _difficultyMultiplier = 1f;
        [SerializeField] private int _sessionLengthTicks = 180;
        [SerializeField] private bool _endless;
        [SerializeField] private string[] _tags = Array.Empty<string>();

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public string Description => _description ?? string.Empty;
        public Sprite Icon => _icon;
        public Texture2D Banner => _banner;
        public WeaponDefinitionAsset StartingWeapon => _startingWeapon;
        public IReadOnlyList<WeaponDefinitionAsset> AvailableWeapons => _availableWeapons ?? Array.Empty<WeaponDefinitionAsset>();
        public IReadOnlyList<EnemyDefinitionAsset> EnemyPool => _enemyPool ?? Array.Empty<EnemyDefinitionAsset>();
        public IReadOnlyList<WaveDefinitionAsset> WaveSet => _waveSet ?? Array.Empty<WaveDefinitionAsset>();
        public IReadOnlyList<RunUpgradeDefinitionAsset> UpgradePool => _upgradePool ?? Array.Empty<RunUpgradeDefinitionAsset>();
        public int StartingCredits => _startingCredits;
        public int StartingParts => _startingParts;
        public float RewardMultiplier => _rewardMultiplier;
        public float DifficultyMultiplier => _difficultyMultiplier;
        public int SessionLengthTicks => _sessionLengthTicks;
        public bool Endless => _endless;
        public IReadOnlyList<string> Tags => _tags ?? Array.Empty<string>();

        public void Configure(
            string id,
            string displayName,
            string description,
            Sprite icon,
            Texture2D banner,
            WeaponDefinitionAsset startingWeapon,
            IReadOnlyList<WeaponDefinitionAsset> availableWeapons,
            IReadOnlyList<EnemyDefinitionAsset> enemyPool,
            IReadOnlyList<WaveDefinitionAsset> waveSet,
            IReadOnlyList<RunUpgradeDefinitionAsset> upgradePool,
            int startingCredits,
            int startingParts,
            float rewardMultiplier,
            float difficultyMultiplier,
            int sessionLengthTicks,
            bool endless,
            IReadOnlyList<string> tags)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _description = description ?? string.Empty;
            _icon = icon;
            _banner = banner;
            _startingWeapon = startingWeapon;
            _availableWeapons = CopyAssets(availableWeapons);
            _enemyPool = CopyAssets(enemyPool);
            _waveSet = CopyAssets(waveSet);
            _upgradePool = CopyAssets(upgradePool);
            _startingCredits = startingCredits;
            _startingParts = startingParts;
            _rewardMultiplier = rewardMultiplier;
            _difficultyMultiplier = difficultyMultiplier;
            _sessionLengthTicks = sessionLengthTicks;
            _endless = endless;
            _tags = CopyTags(tags);
        }

        public static GameContentSetAsset CreateTransient(
            string id,
            string displayName,
            WeaponDefinitionAsset startingWeapon,
            IReadOnlyList<WeaponDefinitionAsset> availableWeapons,
            IReadOnlyList<EnemyDefinitionAsset> enemyPool,
            IReadOnlyList<WaveDefinitionAsset> waveSet,
            IReadOnlyList<RunUpgradeDefinitionAsset> upgradePool,
            int startingCredits = 60,
            int startingParts = 0,
            float rewardMultiplier = 1f,
            float difficultyMultiplier = 1f,
            int sessionLengthTicks = 180,
            bool endless = false,
            string description = "",
            IReadOnlyList<string> tags = null)
        {
            var asset = CreateInstance<GameContentSetAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            asset.Configure(
                id,
                displayName,
                description,
                null,
                null,
                startingWeapon,
                availableWeapons,
                enemyPool,
                waveSet,
                upgradePool,
                startingCredits,
                startingParts,
                rewardMultiplier,
                difficultyMultiplier,
                sessionLengthTicks,
                endless,
                tags ?? Array.Empty<string>());
            return asset;
        }

        private static TAsset[] CopyAssets<TAsset>(IReadOnlyList<TAsset> source) where TAsset : UnityEngine.Object
        {
            if (source == null || source.Count == 0) return Array.Empty<TAsset>();
            var copy = new TAsset[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }

        private static string[] CopyTags(IReadOnlyList<string> tags)
        {
            if (tags == null || tags.Count == 0) return Array.Empty<string>();
            var copy = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (!string.IsNullOrWhiteSpace(tag)) copy.Add(tag.Trim());
            }

            return copy.ToArray();
        }
    }
}
