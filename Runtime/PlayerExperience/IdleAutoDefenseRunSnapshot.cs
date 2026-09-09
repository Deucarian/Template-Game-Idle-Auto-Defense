using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseRunSnapshot
    {
        // Captured once at run boundaries; presenters receive no mutation port.

        public int DirectOrCombatKillCount { get; }

        public int ProjectileAdapterKillCount { get; }

        public bool UsingAssignedContentPack { get; }

        public bool StartupBlocked { get; }

        public string StartupError { get; }

        public bool FallbackModeActive { get; }

        public bool UsingAuthoredCore { get; }

        public string ActiveContentPackId { get; }

        public double SessionLengthSeconds { get; }

        public int SimulationTicksPerSecond { get; }

        public int ObjectiveDamageEvents { get; }

        public int SelectedUpgradeCount { get; }

        public int RewardDraftOpenedCount { get; }

        public int RewardDraftSelectionCount { get; }

        public int EpicRewardSelectionCount { get; }

        public int LegendaryRewardSelectionCount { get; }

        public int EliteDefeatCount { get; }

        public int BossDefeatCount { get; }

        public int CommanderLevel { get; }

        public long CommanderExperience { get; }

        public long ExperienceToNextLevel { get; }

        public bool RewardDraftActive { get; }

        public IReadOnlyList<IdleAutoDefenseRewardDraftChoice> RewardDraftChoices { get; }

        public double ProjectileSpeedMultiplier { get; }

        public double RewardCreditMultiplierBonus { get; }

        public long RuntimeCurrency { get; }

        public long RuntimeCurrencyEarned { get; }

        public long RuntimeCurrencySpent { get; }

        public float SurvivalSeconds { get; }

        public int DamageUpgradeRank { get; }

        public int AttackSpeedUpgradeRank { get; }

        public int RangeUpgradeRank { get; }

        public int RepairUpgradeRank { get; }

        public long EncounterRewardCredits { get; }

        public long EncounterRewardParts { get; }

        public bool EncounterCompleted { get; }

        public bool EncounterFailed { get; }

        public bool EncounterRunning { get; }

        public double ObjectiveHealth { get; }

        public double ObjectiveMaximumHealth { get; }

        public string CurrentSpawnProfileName { get; }

        public int DamageUpgradeCost { get; }

        public int AttackSpeedUpgradeCost { get; }

        public int RangeUpgradeCost { get; }

        public bool PulseBeamUnlocked { get; }

        public bool ArcBurstUnlocked { get; }

        public bool HomingPulseUnlocked { get; }

        public int PulseBeamUnlockCost { get; }

        public int ArcBurstUnlockCost { get; }

        public int HomingPulseUnlockCost { get; }

        public int OverdriveCost { get; }

        public bool OverdriveActive { get; }

        public float OverdriveSecondsRemaining { get; }

        public float OverdriveCooldownSecondsRemaining { get; }

        public bool CanPurchaseOverdrive { get; }

        public int UnlockedModuleCount { get; }

        public IdleAutoDefenseEconomyAsset ActiveEconomy { get; }

        public IdleAutoDefenseRunProfileAsset ActiveRunProfile { get; }

        public IdleAutoDefenseProgressionAsset ActiveProgression { get; }

        public IdleAutoDefenseOfflineProgressionAsset ActiveOfflineProgression { get; }

        public IdleAutoDefenseGameRulesAsset ActiveGameRules { get; }

        public int TotalWaveCount { get; }

        public int CurrentWaveNumber { get; }

        public string StatusSummary { get; }

        private readonly IdleAutoDefenseMajorThreatSnapshot _threat;
        private readonly bool _hasThreat;
        private readonly Dictionary<string, int> _research = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, long> _currencies = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<IdleAutoDefenseRewardDraftChoice, int> _choiceRanks = new Dictionary<IdleAutoDefenseRewardDraftChoice, int>();

        internal IdleAutoDefenseRunSnapshot(IdleAutoDefenseTemplateController source)
        {
            DirectOrCombatKillCount = source.DirectOrCombatKillCount;
            ProjectileAdapterKillCount = source.ProjectileAdapterKillCount;
            UsingAssignedContentPack = source.UsingAssignedContentPack;
            StartupBlocked = source.StartupBlocked;
            StartupError = source.StartupError;
            FallbackModeActive = source.FallbackModeActive;
            UsingAuthoredCore = source.UsingAuthoredCore;
            ActiveContentPackId = source.ActiveContentPackId;
            SessionLengthSeconds = source.SessionLengthSeconds;
            SimulationTicksPerSecond = source.SimulationTicksPerSecond;
            ObjectiveDamageEvents = source.ObjectiveDamageEvents;
            SelectedUpgradeCount = source.SelectedUpgradeCount;
            RewardDraftOpenedCount = source.RewardDraftOpenedCount;
            RewardDraftSelectionCount = source.RewardDraftSelectionCount;
            EpicRewardSelectionCount = source.EpicRewardSelectionCount;
            LegendaryRewardSelectionCount = source.LegendaryRewardSelectionCount;
            EliteDefeatCount = source.EliteDefeatCount;
            BossDefeatCount = source.BossDefeatCount;
            CommanderLevel = source.CommanderLevel;
            CommanderExperience = source.CommanderExperience;
            ExperienceToNextLevel = source.ExperienceToNextLevel;
            RewardDraftActive = source.RewardDraftActive;
            RewardDraftChoices = source.RewardDraftChoices;
            ProjectileSpeedMultiplier = source.ProjectileSpeedMultiplier;
            RewardCreditMultiplierBonus = source.RewardCreditMultiplierBonus;
            RuntimeCurrency = source.RuntimeCurrency;
            RuntimeCurrencyEarned = source.RuntimeCurrencyEarned;
            RuntimeCurrencySpent = source.RuntimeCurrencySpent;
            SurvivalSeconds = source.SurvivalSeconds;
            DamageUpgradeRank = source.DamageUpgradeRank;
            AttackSpeedUpgradeRank = source.AttackSpeedUpgradeRank;
            RangeUpgradeRank = source.RangeUpgradeRank;
            RepairUpgradeRank = source.RepairUpgradeRank;
            EncounterRewardCredits = source.EncounterRewardCredits;
            EncounterRewardParts = source.EncounterRewardParts;
            EncounterCompleted = source.EncounterCompleted;
            EncounterFailed = source.EncounterFailed;
            EncounterRunning = source.EncounterRunning;
            ObjectiveHealth = source.ObjectiveHealth;
            ObjectiveMaximumHealth = source.ObjectiveMaximumHealth;
            CurrentSpawnProfileName = source.CurrentSpawnProfileName;
            DamageUpgradeCost = source.DamageUpgradeCost;
            AttackSpeedUpgradeCost = source.AttackSpeedUpgradeCost;
            RangeUpgradeCost = source.RangeUpgradeCost;
            PulseBeamUnlocked = source.PulseBeamUnlocked;
            ArcBurstUnlocked = source.ArcBurstUnlocked;
            HomingPulseUnlocked = source.HomingPulseUnlocked;
            PulseBeamUnlockCost = source.PulseBeamUnlockCost;
            ArcBurstUnlockCost = source.ArcBurstUnlockCost;
            HomingPulseUnlockCost = source.HomingPulseUnlockCost;
            OverdriveCost = source.OverdriveCost;
            OverdriveActive = source.OverdriveActive;
            OverdriveSecondsRemaining = source.OverdriveSecondsRemaining;
            OverdriveCooldownSecondsRemaining = source.OverdriveCooldownSecondsRemaining;
            CanPurchaseOverdrive = source.CanPurchaseOverdrive;
            UnlockedModuleCount = source.UnlockedModuleCount;
            ActiveEconomy = source.ActiveEconomy;
            ActiveRunProfile = source.ActiveRunProfile;
            ActiveProgression = source.ActiveProgression;
            ActiveOfflineProgression = source.ActiveOfflineProgression;
            ActiveGameRules = source.ActiveGameRules;
            TotalWaveCount = source.TotalWaveCount;
            CurrentWaveNumber = source.CurrentWaveNumber;
            StatusSummary = source.StatusSummary;
            _hasThreat = source.TryGetPrimaryMajorThreat(out _threat);
            foreach (var choice in RewardDraftChoices) _choiceRanks[choice] = source.GetRewardDraftChoiceCurrentRank(choice);
            if (ActiveProgression != null)
                foreach (var node in ActiveProgression.ResearchNodes)
                {
                    if (node == null) continue;
                    _research[node.Id] = source.GetPersistentResearchRank(node.Id);
                    _currencies[node.CostCurrencyId] = source.GetPersistentCurrencyBalance(node.CostCurrencyId);
                }
            if (ActiveEconomy != null)
            {
                _currencies[ActiveEconomy.PrimaryCurrencyId] = source.GetPersistentCurrencyBalance(ActiveEconomy.PrimaryCurrencyId);
                _currencies[ActiveEconomy.SecondaryCurrencyId] = source.GetPersistentCurrencyBalance(ActiveEconomy.SecondaryCurrencyId);
            }
        }

        public bool TryGetPrimaryMajorThreat(out IdleAutoDefenseMajorThreatSnapshot threat) { threat = _threat; return _hasThreat; }
        public int GetRewardDraftChoiceCurrentRank(IdleAutoDefenseRewardDraftChoice choice) => choice != null && _choiceRanks.TryGetValue(choice, out int rank) ? rank : 0;
        public int GetPersistentResearchRank(string id) => id != null && _research.TryGetValue(id, out int rank) ? rank : 0;
        public long GetPersistentCurrencyBalance(string id) => id != null && _currencies.TryGetValue(id, out long value) ? value : 0;
    }
}
