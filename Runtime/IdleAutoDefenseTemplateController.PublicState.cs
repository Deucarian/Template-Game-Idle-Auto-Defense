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
    // Source-compatible component observations and settings; state lives in composed run owners.
    public partial class IdleAutoDefenseTemplateController
    {
        public AutoDefenseRuntime Runtime => _runtime;

        public MonetizationSession MonetizationSession
        {
            get => RewardOffers.Session;
            set => RewardOffers.Session = value;
        }

        public string RuntimeStateName => RunProfileVictoryReached ? AutoDefenseRuntimeState.Completed.ToString() : RuntimeState.ToString();

        public int SpawnedCount => RunSession.SpawnedCount;

        public int DamageNumberSpawnCount => _runtimeUi?.DamageNumberSpawnCount ?? 0;

        public bool RuntimeUiDocumentReady => _runtimeUi?.RuntimeUiDocumentReady ?? false;

        public bool RuntimeUiThemeAssigned => _runtimeUi?.RuntimeUiThemeAssigned ?? false;

        public bool RuntimeUiDirectStylesApplied => _runtimeUi?.RuntimeUiDirectStylesApplied ?? false;

        public int RuntimeDamageNumberVisibleCount => _runtimeUi?.VisibleCount ?? 0;

        public float RuntimeUiRootResolvedWidth => ResolveRuntimePanelSize().x;

        public float RuntimeUiRootResolvedHeight => ResolveRuntimePanelSize().y;

        public bool ShowDebugAimLines
        {
            get => _showDebugAimLines;
            set => _showDebugAimLines = value;
        }

        public bool ShowDebugRanges
        {
            get => _showDebugRanges;
            set => _showDebugRanges = value;
        }

        public bool ShowDebugSpawnRing
        {
            get => _showDebugSpawnRing;
            set => _showDebugSpawnRing = value;
        }

        public int SessionElapsedTicks => RunSession.ElapsedTicks;

        public int SessionLengthTicks => _activeRunProfile == null ? 0 : _activeRunProfile.SessionLengthTicks;

        public double SessionLengthSeconds => _activeRunProfile == null ? 0d : _activeRunProfile.SessionLengthSeconds;

        public int SimulationTicksPerSecond => _activeRunProfile == null ? 0 : _activeRunProfile.SimulationTicksPerSecond;

        public bool EndlessEnabled => _activeRunProfile != null && _activeRunProfile.Endless;

        public int ObjectiveReachCount => RunSession.ObjectiveReachCount;

        public int ObjectiveDamageEvents => RunSession.ObjectiveDamageEvents;

        public int DraftTickCount => LegacyDraft.TickCount;

        public int SelectedUpgradeCount => RunBuild.SelectedUpgradeCount;

        public int RewardDraftOpenedCount => RewardProgression.RewardDraftOpenedCount;

        public int RewardDraftSelectionCount => RewardProgression.RewardDraftSelectionCount;

        public float FirstRewardDraftSeconds => RewardProgression.FirstRewardDraftSeconds;

        public int LevelUpRewardDraftCount => RewardProgression.LevelUpRewardDraftCount;

        public int EliteRewardDraftCount => RewardProgression.EliteRewardDraftCount;

        public int BossRewardDraftCount => RewardProgression.BossRewardDraftCount;

        public int WaveRewardExperienceCount => RewardProgression.WaveRewardExperienceCount;

        public int EpicRewardSelectionCount => RewardProgression.EpicRewardSelectionCount;

        public int LegendaryRewardSelectionCount => RewardProgression.LegendaryRewardSelectionCount;

        public int EliteDefeatCount => RewardProgression.EliteDefeatCount;

        public int BossDefeatCount => RewardProgression.BossDefeatCount;

        public int UpgradeFeedbackSpawnCount => _runtimeUi?.UpgradeFeedbackSpawnCount ?? 0;

        public int CommanderLevel => RewardProgression.CommanderLevel;

        public long CommanderExperience => RewardProgression.CommanderExperience;

        public long ExperienceToNextLevel => RewardProgression.ExperienceToNextLevel;

        public bool RewardDraftPausesCombat { get => RunSession.RewardDraftPausesCombat; set => RunSession.RewardDraftPausesCombat = value; }

        public bool RewardDraftActive => RewardProgression.RewardDraftActive;

        public IReadOnlyList<IdleAutoDefenseRewardDraftChoice> RewardDraftChoices => RewardProgression.RewardDraftChoices;

        public int RewardDraftChoiceCount => RewardProgression.RewardDraftChoiceCount;

        public string ActiveRewardDraftKindName => RewardProgression.ActiveRewardDraftKindName;

        public double DirectDamageBonus => RunBuild.DirectDamageBonus;

        public double ProjectileSpeedMultiplier => RunBuild.ProjectileSpeedMultiplier;

        public int EnemySpawnDelayTicks => RunBuild.EnemySpawnDelayTicks;

        public double RewardCreditMultiplierBonus => RunBuild.RewardCreditMultiplierBonus;

        public double OfflineRewardMultiplierBonus => RunBuild.OfflineRewardMultiplierBonus;

        public long RuntimeCurrency => RunWallet.Balance;

        public long RuntimeCurrencyEarned => RunWallet.Earned;

        public long RuntimeCurrencySpent => RunWallet.Spent;

        public float SurvivalSeconds => RunSession.SurvivalSeconds;

        public int DamageUpgradeRank => RunBuild.DamageUpgradeRank;

        public int AttackSpeedUpgradeRank => RunBuild.AttackSpeedUpgradeRank;

        public int RangeUpgradeRank => RunBuild.RangeUpgradeRank;

        public int RepairUpgradeRank => RunBuild.RepairUpgradeRank;

        public int UnsupportedUpgradeIntentCount => RunBuild.UnsupportedUpgradeIntentCount;

        public long OfflineRewardCredits => PersistentProgression.OfflineRewardCredits;

        public long OfflineRewardParts => PersistentProgression.OfflineRewardParts;

        public long EncounterRewardCredits => PersistentProgression.EncounterRewardCredits;

        public long EncounterRewardParts => PersistentProgression.EncounterRewardParts;

        public IdleProgressionResultCode LastOfflineRewardCode => PersistentProgression.LastOfflineRewardCode;

        public bool ReviveOfferAccepted => _rewardOffers?.ReviveOfferAccepted ?? false;

        public bool RunProfileVictoryReached => RunSession.VictoryReached;

        public bool EncounterCompleted => RunProfileVictoryReached || _runtime != null && _runtime.State == AutoDefenseRuntimeState.Completed;

        public bool EncounterFailed => _runtime != null && _runtime.State == AutoDefenseRuntimeState.Failed;

        public bool EncounterRunning => _runtime != null && _runtime.State == AutoDefenseRuntimeState.Running;

        public int ActiveEnemyCount => _runtime == null ? 0 : _runtime.ActiveEnemyCount;

        public int ObjectiveLivesRemaining => _runtime == null ? 0 : _runtime.Objective.LivesRemaining;

        public double ObjectiveHealth => _runtime == null ? 0d : _runtime.Objective.Health.CurrentHealth;

        public double ObjectiveMaximumHealth => _runtime == null ? 0d : _runtime.Objective.Health.MaximumHealth;

        public string ObjectiveHealthText => ObjectiveMaximumHealth <= 0d
            ? "--"
            : ObjectiveHealth.ToString("0", CultureInfo.InvariantCulture) + "/" + ObjectiveMaximumHealth.ToString("0", CultureInfo.InvariantCulture);

        public string CurrentSpawnProfileName => ResolveCurrentSpawnProfileName();

        public int DamageUpgradeCost => RunBuild.DamageUpgradeCost;

        public int AttackSpeedUpgradeCost => RunBuild.AttackSpeedUpgradeCost;

        public int RangeUpgradeCost => RunBuild.RangeUpgradeCost;

        public int RepairUpgradeCost => RunBuild.RepairUpgradeCost;

        public bool PulseBeamUnlocked => RunBuild.PulseBeamUnlocked;

        public bool ArcBurstUnlocked => RunBuild.ArcBurstUnlocked;

        public bool HomingPulseUnlocked => RunBuild.HomingPulseUnlocked;

        public int PulseBeamUnlockCost => RunBuild.PulseBeamUnlockCost;

        public int ArcBurstUnlockCost => RunBuild.ArcBurstUnlockCost;

        public int HomingPulseUnlockCost => RunBuild.HomingPulseUnlockCost;

        public int OverdriveCost => RunBuild.OverdriveCost;

        public bool OverdriveActive => RunBuild.OverdriveActive;

        public float OverdriveSecondsRemaining => RunBuild.OverdriveSecondsRemaining;

        public float OverdriveCooldownSecondsRemaining => RunBuild.OverdriveCooldownSecondsRemaining;

        public bool CanPurchasePulseBeamModule => RunBuild.CanPurchasePulseBeamModule;

        public bool CanPurchaseArcBurstModule => RunBuild.CanPurchaseArcBurstModule;

        public bool CanPurchaseHomingPulseModule => RunBuild.CanPurchaseHomingPulseModule;

        public bool CanPurchaseOverdrive => RunBuild.CanPurchaseOverdrive;

        public int UnlockedModuleCount => RunBuild.UnlockedModuleCount;

        public IdleAutoDefenseRewardCatalogAsset ActiveRewardCatalog => _activeRewardCatalog;

        public IdleAutoDefenseEconomyAsset ActiveEconomy => _activeEconomy;

        public IdleAutoDefenseRunProfileAsset ActiveRunProfile => _activeRunProfile;

        public IdleAutoDefenseProgressionAsset ActiveProgression => _activeProgression;

        public IdleAutoDefenseOfflineProgressionAsset ActiveOfflineProgression => _activeOfflineProgression;

        public IdleAutoDefenseGameRulesAsset ActiveGameRules => _activeGameRules;

        public int TotalWaveCount => _resolvedWaveDefinitions.Length;

        public int CurrentWaveNumber => ResolveCurrentWaveNumber();

        public int OverdriveActivationCount => RunBuild.OverdriveActivationCount;

        public bool CanPurchaseDamageUpgrade => RunBuild.CanPurchaseDamageUpgrade;

        public bool CanPurchaseAttackSpeedUpgrade => RunBuild.CanPurchaseAttackSpeedUpgrade;

        public bool CanPurchaseRangeUpgrade => RunBuild.CanPurchaseRangeUpgrade;

        public bool CanPurchaseRepairUpgrade => RunBuild.CanPurchaseRepairUpgrade;

        public string StatusSummary => "State=" + RuntimeState +
            " Spawned=" + SpawnedCount +
            " Kills=" + (DirectOrCombatKillCount + ProjectileAdapterKillCount) +
            " Projectiles=" + ProjectileLaunchCount +
            " ProjectileImpacts=" + ProjectileImpactCallbackCount +
            " BeamVisuals=" + BeamVisualSpawnCount +
            " Upgrades=" + SelectedUpgradeCount +
            " Drafts=" + RewardDraftOpenedCount +
            " Modules=" + UnlockedModuleCount +
            " ObjectiveHits=" + ObjectiveDamageEvents +
            " RangeRejects=" + RangeRejectedTargetCount +
            " ClosestEnemy=" + ClosestEnemyDistanceToObjective.ToString("0.0", CultureInfo.InvariantCulture) +
            " Kenney3D=" + Kenney3DModelSpawnCount +
            " Aim=" + TurretAimUpdateCount +
            " MuzzleProjectiles=" + MuzzleProjectileLaunchCount +
            " MuzzleFlash=" + MuzzleFlashSpawnCount +
            " Recoil=" + RecoilEventCount +
            " AuthoredWeapons=" + AuthoredWeaponPresentationSpawnCount +
            " FallbackWeapons=" + FallbackWeaponPresentationSpawnCount +
            " AuthoredBindings=" + AuthoredWeaponPresentationBindingCount +
            " FallbackBindings=" + FallbackWeaponPresentationBindingCount +
            " AuthoredObjective=" + AuthoredObjectivePresentationBindingCount +
            " FallbackObjective=" + FallbackObjectivePresentationBindingCount +
            " AuthoredSlots=" + AuthoredModuleSlotPresentationBindingCount +
            " FallbackSlots=" + FallbackModuleSlotPresentationBindingCount +
            " DebugAimLines=" + DebugAimTracerSpawnCount +
            " EnemyFacing=" + EnemyFacingUpdateCount +
            " EnemyHitFlash=" + EnemyHitFlashCount +
            " EnemyDeathPop=" + EnemyDeathPopCount +
            " AuthoredStamps=" + AuthoredVisibleInstanceStampCount +
            " FallbackVisible=" + FallbackVisibleGameplaySpawnCount +
            " Currency=" + RuntimeCurrency +
            " Level=" + CommanderLevel +
            " Overdrive=" + (OverdriveActive ? "on" : "off") +
            " Time=" + SurvivalSeconds.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
