using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Common;
using Deucarian.DefenseGames;
using Deucarian.Encounters;
using Deucarian.IdleProgression;
using Deucarian.Monetization;
using Deucarian.Persistence;
using Deucarian.Progression;
using Deucarian.Projectiles;
using Deucarian.RunUpgrades;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems;
using Deucarian.WeaponSystems.Authoring;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public partial class IdleAutoDefenseTemplateController
    {
        private IdleAutoDefenseContentBinding _contentBinding;
        private IdleAutoDefenseContentBinding ContentBinding => _contentBinding ??= new IdleAutoDefenseContentBinding(ReportTemplateWarning, ReportTemplateError, ApplyContentSetRuntimeSettings);
        [SerializeField] protected GameContentPackAsset _contentPack;

        [SerializeField] protected GameContentSetAsset _contentSet;

        [SerializeField] protected AttackDefinitionAsset[] _attackRecipes = Array.Empty<AttackDefinitionAsset>();

        [SerializeField] protected EnemyDefinitionAsset[] _enemyDefinitions = Array.Empty<EnemyDefinitionAsset>();

        [SerializeField] protected WaveDefinitionAsset[] _waveDefinitions = Array.Empty<WaveDefinitionAsset>();

        [SerializeField] protected WeaponDefinitionAsset[] _weaponDefinitions = Array.Empty<WeaponDefinitionAsset>();

        [SerializeField] protected RunUpgradeDefinitionAsset[] _upgradeDefinitions = Array.Empty<RunUpgradeDefinitionAsset>();

        [SerializeField] private bool _requireAuthoredContent;

        private AttackDefinitionAsset[] _resolvedAttackRecipes { get => ContentBinding.Attacks; set => ContentBinding.Attacks = value; }

        private EnemyDefinitionAsset[] _resolvedEnemyDefinitions { get => ContentBinding.Enemies; set => ContentBinding.Enemies = value; }

        private WaveDefinitionAsset[] _resolvedWaveDefinitions { get => ContentBinding.Waves; set => ContentBinding.Waves = value; }

        private WeaponDefinitionAsset[] _resolvedWeaponDefinitions { get => ContentBinding.Weapons; set => ContentBinding.Weapons = value; }

        private RunUpgradeDefinitionAsset[] _resolvedUpgradeDefinitions { get => ContentBinding.Upgrades; set => ContentBinding.Upgrades = value; }

        private GameContentSetResolution _resolvedContentSet { get => ContentBinding.ContentSet; set => ContentBinding.ContentSet = value; }

        private IdleAutoDefenseRewardCatalogAsset _activeRewardCatalog { get => ContentBinding.RewardCatalog; set => ContentBinding.RewardCatalog = value; }

        private IdleAutoDefenseEconomyAsset _activeEconomy { get => ContentBinding.Economy; set => ContentBinding.Economy = value; }

        private IdleAutoDefenseRunProfileAsset _activeRunProfile { get => ContentBinding.RunProfile; set => ContentBinding.RunProfile = value; }

        private IdleAutoDefenseProgressionAsset _activeProgression { get => ContentBinding.Progression; set => ContentBinding.Progression = value; }

        private IdleAutoDefenseOfflineProgressionAsset _activeOfflineProgression { get => ContentBinding.OfflineProgression; set => ContentBinding.OfflineProgression = value; }

        private IdleAutoDefenseGameRulesAsset _activeGameRules { get => ContentBinding.GameRules; set => ContentBinding.GameRules = value; }

        public bool UsingContentSetRuntimeSettings { get => ContentBinding.UsingContentSetRuntimeSettings; private set => ContentBinding.UsingContentSetRuntimeSettings = value; }

        public int InvalidAssignedRecipeCount { get => ContentBinding.InvalidAssignedRecipeCount; private set => ContentBinding.InvalidAssignedRecipeCount = value; }

        public int InvalidAssignedEnemyCount { get => ContentBinding.InvalidAssignedEnemyCount; private set => ContentBinding.InvalidAssignedEnemyCount = value; }

        public int InvalidAssignedWaveCount { get => ContentBinding.InvalidAssignedWaveCount; private set => ContentBinding.InvalidAssignedWaveCount = value; }

        public int InvalidAssignedWeaponCount { get => ContentBinding.InvalidAssignedWeaponCount; private set => ContentBinding.InvalidAssignedWeaponCount = value; }

        public int InvalidAssignedUpgradeCount { get => ContentBinding.InvalidAssignedUpgradeCount; private set => ContentBinding.InvalidAssignedUpgradeCount = value; }

        public int InvalidAssignedContentPackIssueCount { get => ContentBinding.InvalidAssignedContentPackIssueCount; private set => ContentBinding.InvalidAssignedContentPackIssueCount = value; }

        public int InvalidAssignedContentSetIssueCount { get => ContentBinding.InvalidAssignedContentSetIssueCount; private set => ContentBinding.InvalidAssignedContentSetIssueCount = value; }

        public bool UsingAssignedContentPack { get => ContentBinding.UsingAssignedContentPack; private set => ContentBinding.UsingAssignedContentPack = value; }

        public bool UsingAssignedContentSet { get => ContentBinding.UsingAssignedContentSet; private set => ContentBinding.UsingAssignedContentSet = value; }

        public bool StrictAuthoredStartup => _requireAuthoredContent;

        public bool StartupBlocked { get => ContentBinding.StartupBlocked; private set => ContentBinding.StartupBlocked = value; }

        public string StartupError { get => ContentBinding.StartupError; private set => ContentBinding.StartupError = value; }

        public bool FallbackModeActive { get => ContentBinding.FallbackModeActive; private set => ContentBinding.FallbackModeActive = value; }

        public bool UsingAuthoredCore => UsingAssignedContentSet && !FallbackModeActive &&
            _activeRewardCatalog != null && _activeEconomy != null && _activeRunProfile != null &&
            _activeProgression != null && _activeOfflineProgression != null && _activeGameRules != null;

        public string ActiveContentPackId => _contentPack == null ? string.Empty : _contentPack.Id;

        public string ActiveContentPackDisplayName => _contentPack == null ? string.Empty : _contentPack.DisplayName;

        public string ActiveContentSetId => _resolvedContentSet != null && _resolvedContentSet.IsValid && _resolvedContentSet.ContentSet != null
            ? _resolvedContentSet.ContentSet.Id
            : string.Empty;

        public string ActiveRewardCatalogId => _activeRewardCatalog == null ? string.Empty : _activeRewardCatalog.Id;

        public string ActiveEconomyId => _activeEconomy == null ? string.Empty : _activeEconomy.Id;

        public string ActiveRunProfileId => _activeRunProfile == null ? string.Empty : _activeRunProfile.Id;

        public string ActiveProgressionId => _activeProgression == null ? string.Empty : _activeProgression.Id;

        public string ActiveOfflineProgressionId => _activeOfflineProgression == null ? string.Empty : _activeOfflineProgression.Id;

        public string ActiveGameRulesId => _activeGameRules == null ? string.Empty : _activeGameRules.Id;

        public string AssignedContentPackStatus { get => ContentBinding.AssignedContentPackStatus; private set => ContentBinding.AssignedContentPackStatus = value; }

        public string AssignedContentSetStatus { get => ContentBinding.AssignedContentSetStatus; private set => ContentBinding.AssignedContentSetStatus = value; }

        private IdleAutoDefenseContentSetRuntimeSettings ContentSetRuntimeSettings => _resolvedContentSet != null && _resolvedContentSet.IsValid && _resolvedContentSet.ContentSet != null
            ? _resolvedContentSet.ContentSet.RuntimeSettings
            : null;

        protected void ConfigureContentPack(GameContentPackAsset contentPack, GameContentSetAsset contentSet)
        {
            _contentPack = contentPack;
            _contentSet = contentSet;
        }

        protected void RequireAuthoredContentOnStartup()
        {
            _requireAuthoredContent = true;
        }

        public void ConfigureStrictAuthoredStartup(bool required)
        {
            if (_runtime != null) throw new InvalidOperationException("Strict authored startup must be configured before the run is built.");
            _requireAuthoredContent = required;
        }
    }
}
