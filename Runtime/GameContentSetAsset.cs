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
        [SerializeField] private RunUpgradeDefinitionAsset[] _upgradePool = Array.Empty<RunUpgradeDefinitionAsset>();
        [SerializeField] private IdleAutoDefenseRewardCatalogAsset _rewardCatalog;
        [SerializeField] private IdleAutoDefenseEconomyAsset _economy;
        [SerializeField] private IdleAutoDefenseRunProfileAsset _runProfile;
        [SerializeField] private IdleAutoDefenseProgressionAsset _progression;
        [SerializeField] private IdleAutoDefenseOfflineProgressionAsset _offlineProgression;
        [SerializeField] private IdleAutoDefenseGameRulesAsset _gameRules;
        [SerializeField] private string[] _tags = Array.Empty<string>();
        [SerializeField] private IdleAutoDefenseContentSetRuntimeSettings _runtimeSettings = IdleAutoDefenseContentSetRuntimeSettings.CreateDefault();

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public string Description => _description ?? string.Empty;
        public Sprite Icon => _icon;
        public Texture2D Banner => _banner;
        public WeaponDefinitionAsset StartingWeapon => _startingWeapon;
        public IReadOnlyList<WeaponDefinitionAsset> AvailableWeapons => _availableWeapons ?? Array.Empty<WeaponDefinitionAsset>();
        public IReadOnlyList<EnemyDefinitionAsset> EnemyPool => _enemyPool ?? Array.Empty<EnemyDefinitionAsset>();
        public IReadOnlyList<WaveDefinitionAsset> WaveSet => _runProfile == null ? Array.Empty<WaveDefinitionAsset>() : _runProfile.Waves;
        public IReadOnlyList<RunUpgradeDefinitionAsset> UpgradePool => _upgradePool ?? Array.Empty<RunUpgradeDefinitionAsset>();
        public IdleAutoDefenseRewardCatalogAsset RewardCatalog => _rewardCatalog;
        public IdleAutoDefenseEconomyAsset Economy => _economy;
        public IdleAutoDefenseRunProfileAsset RunProfile => _runProfile;
        public IdleAutoDefenseProgressionAsset Progression => _progression;
        public IdleAutoDefenseOfflineProgressionAsset OfflineProgression => _offlineProgression;
        public IdleAutoDefenseGameRulesAsset GameRules => _gameRules;
        public int StartingCredits => (int)Math.Min(int.MaxValue, _economy == null ? 0L : _economy.StartingCredits);
        public int StartingParts => (int)Math.Min(int.MaxValue, _economy == null ? 0L : _economy.StartingParts);
        public float RewardMultiplier => _runProfile == null ? 0f : _runProfile.RewardMultiplier;
        public float DifficultyMultiplier => _runProfile == null ? 0f : _runProfile.DifficultyMultiplier;
        public int SessionLengthTicks => _runProfile == null ? 0 : _runProfile.SessionLengthTicks;
        public bool Endless => _runProfile != null && _runProfile.Endless;
        public IReadOnlyList<string> Tags => _tags ?? Array.Empty<string>();
        public IdleAutoDefenseContentSetRuntimeSettings RuntimeSettings => _runtimeSettings ??= IdleAutoDefenseContentSetRuntimeSettings.CreateDefault();

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
            IReadOnlyList<string> tags,
            IdleAutoDefenseContentSetRuntimeSettings runtimeSettings = null)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _description = description ?? string.Empty;
            _icon = icon;
            _banner = banner;
            _startingWeapon = startingWeapon;
            _availableWeapons = CopyAssets(availableWeapons);
            _enemyPool = CopyAssets(enemyPool);
            _upgradePool = CopyAssets(upgradePool);
            _tags = CopyTags(tags);
            _runtimeSettings = runtimeSettings == null ? IdleAutoDefenseContentSetRuntimeSettings.CreateDefault() : runtimeSettings.Clone();
            if (_rewardCatalog == null || IsTransient(_rewardCatalog))
                _rewardCatalog = IdleAutoDefenseRewardCatalogAsset.CreateTransient(
                    runtimeSettings == null ? null : runtimeSettings.RewardDraftSettings,
                    runtimeSettings == null ? null : runtimeSettings.RewardDraftCatalog);
            if (_economy == null || IsTransient(_economy))
                _economy = IdleAutoDefenseEconomyAsset.CreateTransient(startingCredits, startingParts);
            if (_runProfile == null || IsTransient(_runProfile))
                _runProfile = IdleAutoDefenseRunProfileAsset.CreateTransient(
                    waveSet,
                    difficultyMultiplier,
                    sessionLengthTicks,
                    endless,
                    rewardMultiplier);
            if (_progression == null || IsTransient(_progression))
                _progression = IdleAutoDefenseProgressionAsset.CreateTransient();
            if (_offlineProgression == null || IsTransient(_offlineProgression))
                _offlineProgression = IdleAutoDefenseOfflineProgressionAsset.CreateTransient();
            if (_gameRules == null || IsTransient(_gameRules))
                _gameRules = IdleAutoDefenseGameRulesAsset.CreateTransient(_availableWeapons, _enemyPool);
        }

        public void ConfigureAuthoredCore(
            IdleAutoDefenseRewardCatalogAsset rewardCatalog,
            IdleAutoDefenseEconomyAsset economy,
            IdleAutoDefenseRunProfileAsset runProfile,
            IdleAutoDefenseProgressionAsset progression,
            IdleAutoDefenseOfflineProgressionAsset offlineProgression,
            IdleAutoDefenseGameRulesAsset gameRules)
        {
            _rewardCatalog = rewardCatalog;
            _economy = economy;
            _runProfile = runProfile;
            _progression = progression;
            _offlineProgression = offlineProgression;
            _gameRules = gameRules;
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
            IReadOnlyList<string> tags = null,
            IdleAutoDefenseContentSetRuntimeSettings runtimeSettings = null)
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
                tags ?? Array.Empty<string>(),
                runtimeSettings ?? IdleAutoDefenseContentSetRuntimeSettings.CreateDefault());
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

        private static bool IsTransient(UnityEngine.Object asset)
        {
            return asset != null && (asset.hideFlags & HideFlags.HideAndDontSave) == HideFlags.HideAndDontSave;
        }
    }
}
