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
        private void BlockStrictStartup() => ContentBinding.BlockStrictStartup();
        private void BindExplicitFallbackCore() => ContentBinding.BindExplicitFallbackCore(RewardDraftSettings, RewardDraftCatalog, DefaultRuntimeStartingCredits);
        private bool TryUseAssignedContentSet() => ContentBinding.TryUseAssignedContentSet(_contentPack, _contentSet, _requireAuthoredContent);
        private WeaponDefinitionAsset[] ResolveActiveWeaponDefinitionsForRun() => ContentBinding.ResolveActiveWeaponDefinitionsForRun();
        private AttackDefinitionAsset[] ResolveAttackRecipes() => ContentBinding.ResolveAttackRecipes(_attackRecipes);
        private EnemyDefinitionAsset[] ResolveEnemyDefinitions() => ContentBinding.ResolveEnemyDefinitions(_enemyDefinitions);
        private WaveDefinitionAsset[] ResolveWaveDefinitions(IReadOnlyList<EnemyDefinitionAsset> enemies) => ContentBinding.ResolveWaveDefinitions(_waveDefinitions, enemies);
        private WeaponDefinitionAsset[] ResolveWeaponDefinitions(IReadOnlyList<AttackDefinitionAsset> attacks) => ContentBinding.ResolveWeaponDefinitions(_weaponDefinitions, attacks);
        private RunUpgradeDefinitionAsset[] ResolveUpgradeDefinitions() => ContentBinding.ResolveUpgradeDefinitions(_upgradeDefinitions);
        private void DisposeContentBinding() { _contentBinding?.Dispose(); _contentBinding = null; }

        private void ApplyContentSetRuntimeSettings(GameContentSetAsset contentSet)
        {
            if (contentSet == null || contentSet.RuntimeSettings == null) return;
            IdleAutoDefenseContentSetRuntimeSettings settings = contentSet.RuntimeSettings;
            if (contentSet.RewardCatalog != null)
            {
                _rewardDraftSettings = contentSet.RewardCatalog.Settings.Clone();
                _rewardDraftCatalog = contentSet.RewardCatalog.Catalog.Clone();
            }
            IdleAutoDefensePresentationDebugSettings debug = settings.PresentationDebug;
            _showDebugAimLines = debug != null && debug.ShowDebugAimLines;
            _showDebugRanges = debug != null && debug.ShowDebugRanges;
            _showDebugSpawnRing = debug != null && debug.ShowDebugSpawnRing;
            UsingContentSetRuntimeSettings = true;
        }
    }
}
