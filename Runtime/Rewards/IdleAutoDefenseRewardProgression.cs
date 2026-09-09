using System;
using System.Collections.Generic;
using Deucarian.Encounters;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Owns commander XP, reward queues, selection progress and defeat/wave deduplication.</summary>
    internal sealed class IdleAutoDefenseRewardProgression
    {
        private readonly IdleAutoDefenseRunBuild _build;
        private readonly Func<IdleAutoDefenseRewardDraftSettings> _settings;
        private readonly Func<float> _survivalSeconds;
        private readonly Action<IdleAutoDefenseRewardDraftChoice> _choiceFeedback;
        private readonly Action<Vector3> _creditFeedback;
        private readonly Action _readyFeedback;
        private readonly IdleAutoDefenseRewardSelection _selection;
        private IdleAutoDefenseRewardDraftSettings RewardDraftSettings => _settings();

        internal IdleAutoDefenseRewardProgression(IdleAutoDefenseRunBuild build,
            Func<IdleAutoDefenseRewardDraftSettings> settings, Func<IdleAutoDefenseRewardDraftCatalog> catalog,
            Func<float> survivalSeconds, Func<string, string> weaponName,
            Action<IdleAutoDefenseRewardDraftChoice> choiceFeedback, Action<Vector3> creditFeedback, Action readyFeedback)
        {
            _build = build ?? throw new ArgumentNullException(nameof(build));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _survivalSeconds = survivalSeconds ?? throw new ArgumentNullException(nameof(survivalSeconds));
            _choiceFeedback = choiceFeedback ?? throw new ArgumentNullException(nameof(choiceFeedback));
            _creditFeedback = creditFeedback ?? throw new ArgumentNullException(nameof(creditFeedback));
            _readyFeedback = readyFeedback ?? throw new ArgumentNullException(nameof(readyFeedback));
            _selection = new IdleAutoDefenseRewardSelection(new IdleAutoDefenseRewardInventory(settings, catalog, () => CommanderLevel,
                _weaponNormalUpgradeRanks, _weaponEpicUpgradeRanks, _baseRewardRanks, _weaponLegendaryUnlocks, _selectedRewardIds,
                build.IsWeaponUnlocked, weaponName));
        }

        internal long ConsumeKillCredits()
        {
            long credits = _pendingAuthoredKillCredits;
            _pendingAuthoredKillCredits = 0L;
            return credits;
        }

        internal void Reset()
        {
            _rewardedEnemyDefeatIds.Clear();
            _rewardedCompletedWaveIds.Clear();
            _queuedRewardDrafts.Clear();
            _weaponNormalUpgradeRanks.Clear();
            _weaponEpicUpgradeRanks.Clear();
            _weaponLegendaryUnlocks.Clear();
            _selectedRewardIds.Clear();
            _baseRewardRanks.Clear();
            _rewardDraftChoices = Array.Empty<IdleAutoDefenseRewardDraftChoice>();
            _activeRewardDraftKind = IdleAutoDefenseRewardDraftKind.LevelUp;
            _pendingAuthoredKillCredits = 0L;
            _starterRewardDraftOffered = false;
            RewardDraftOpenedCount = 0;
            RewardDraftSelectionCount = 0;
            FirstRewardDraftSeconds = -1f;
            LevelUpRewardDraftCount = 0;
            EliteRewardDraftCount = 0;
            BossRewardDraftCount = 0;
            WaveRewardExperienceCount = 0;
            EpicRewardSelectionCount = 0;
            LegendaryRewardSelectionCount = 0;
            EliteDefeatCount = 0;
            BossDefeatCount = 0;
            CommanderLevel = 1;
            CommanderExperience = 0;
            _selection.Reset();
        }

        internal void Release()
        {
            _rewardedEnemyDefeatIds.Clear();
            _rewardedCompletedWaveIds.Clear();
            _queuedRewardDrafts.Clear();
            _weaponNormalUpgradeRanks.Clear();
            _weaponEpicUpgradeRanks.Clear();
            _weaponLegendaryUnlocks.Clear();
            _baseRewardRanks.Clear();
            _rewardDraftChoices = Array.Empty<IdleAutoDefenseRewardDraftChoice>();
            _starterRewardDraftOffered = false;
        }

        private readonly HashSet<long> _rewardedEnemyDefeatIds = new HashSet<long>();

        private readonly HashSet<string> _rewardedCompletedWaveIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Queue<IdleAutoDefenseRewardDraftKind> _queuedRewardDrafts = new Queue<IdleAutoDefenseRewardDraftKind>();

        private readonly Dictionary<string, int> _weaponNormalUpgradeRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, int> _weaponEpicUpgradeRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _weaponLegendaryUnlocks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _selectedRewardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, int> _baseRewardRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private IdleAutoDefenseRewardDraftChoice[] _rewardDraftChoices = Array.Empty<IdleAutoDefenseRewardDraftChoice>();

        private IdleAutoDefenseRewardDraftKind _activeRewardDraftKind;

        private long _pendingAuthoredKillCredits;

        private bool _starterRewardDraftOffered;

        internal int RewardDraftOpenedCount { get; private set; }

        internal int RewardDraftSelectionCount { get; private set; }

        internal float FirstRewardDraftSeconds { get; private set; } = -1f;

        internal int LevelUpRewardDraftCount { get; private set; }

        internal int EliteRewardDraftCount { get; private set; }

        internal int BossRewardDraftCount { get; private set; }

        internal int WaveRewardExperienceCount { get; private set; }

        internal int EpicRewardSelectionCount { get; private set; }

        internal int LegendaryRewardSelectionCount { get; private set; }

        internal int EliteDefeatCount { get; private set; }

        internal int BossDefeatCount { get; private set; }

        internal int CommanderLevel { get; private set; } = 1;

        internal long CommanderExperience { get; private set; }

        internal long ExperienceToNextLevel => RewardDraftSettings.CalculateExperienceToNextLevel(CommanderLevel);

        internal bool RewardDraftActive => _rewardDraftChoices.Length > 0;

        internal IReadOnlyList<IdleAutoDefenseRewardDraftChoice> RewardDraftChoices => _rewardDraftChoices;

        internal int RewardDraftChoiceCount => _rewardDraftChoices.Length;

        internal string ActiveRewardDraftKindName => RewardDraftActive ? _activeRewardDraftKind.ToString() : string.Empty;

        internal bool TryChooseRewardDraftChoice(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= _rewardDraftChoices.Length) return false;
            IdleAutoDefenseRewardDraftChoice choice = _rewardDraftChoices[choiceIndex];
            if (!ApplyRewardDraftChoice(choice)) return false;
            _selectedRewardIds.Add(choice.Id);
            _choiceFeedback(choice);

            _build.RecordSelection();
            RewardDraftSelectionCount++;
            if (choice.Rarity == IdleAutoDefenseRewardRarity.Epic) EpicRewardSelectionCount++;
            if (choice.Rarity == IdleAutoDefenseRewardRarity.Legendary) LegendaryRewardSelectionCount++;

            _rewardDraftChoices = Array.Empty<IdleAutoDefenseRewardDraftChoice>();
            OpenNextQueuedRewardDraft();
            return true;
        }

        internal bool TryChooseRewardDraftHotkey(int hotkey)
        {
            return TryChooseRewardDraftChoice(hotkey - 1);
        }

        internal void RequestRewardDraft(IdleAutoDefenseRewardDraftKind kind)
        {
            QueueOrOpenRewardDraft(kind);
        }

        internal bool ApplyRewardDraftChoice(IdleAutoDefenseRewardDraftChoice choice)
        {
            if (choice == null) return false;
            if (!string.IsNullOrEmpty(choice.TargetWeaponId) && !choice.IsUnlock)
                RecordWeaponRewardProgress(choice.TargetWeaponId, choice.Rarity);
            else if (!choice.IsUnlock)
                IncrementRank(_baseRewardRanks, choice.DedupeKey);

            return _build.ApplyRewardDraftChoice(choice);
        }

        internal void RecordWeaponRewardProgress(string weaponId, IdleAutoDefenseRewardRarity rarity)
        {
            if (string.IsNullOrWhiteSpace(weaponId)) return;
            if (rarity == IdleAutoDefenseRewardRarity.Epic)
                IncrementRank(_weaponEpicUpgradeRanks, weaponId);
            else if (rarity == IdleAutoDefenseRewardRarity.Legendary)
                _weaponLegendaryUnlocks.Add(weaponId);
            else
                IncrementRank(_weaponNormalUpgradeRanks, weaponId);
        }

        internal static void IncrementRank(Dictionary<string, int> ranks, string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            ranks[key] = ranks.TryGetValue(key, out int current) ? current + 1 : 1;
        }

        internal void AddCommanderExperience(long amount)
        {
            if (amount <= 0) return;
            CommanderExperience += amount;
            while (CommanderExperience >= ExperienceToNextLevel)
            {
                CommanderExperience -= ExperienceToNextLevel;
                CommanderLevel++;
                QueueOrOpenRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
            }
        }

        internal void QueueOrOpenRewardDraft(IdleAutoDefenseRewardDraftKind kind)
        {
            if (RewardDraftActive)
            {
                _queuedRewardDrafts.Enqueue(kind);
                return;
            }

            OpenRewardDraft(kind);
        }

        internal void OpenNextQueuedRewardDraft()
        {
            while (_queuedRewardDrafts.Count > 0 && !RewardDraftActive)
                OpenRewardDraft(_queuedRewardDrafts.Dequeue());
        }

        internal void OpenRewardDraft(IdleAutoDefenseRewardDraftKind kind)
        {
            IdleAutoDefenseRewardDraftChoice[] choices = GenerateRewardDraftChoices(kind);
            if (choices.Length == 0) return;
            _activeRewardDraftKind = kind;
            _rewardDraftChoices = choices;
            if (FirstRewardDraftSeconds < 0f)
                FirstRewardDraftSeconds = _survivalSeconds();
            RewardDraftOpenedCount++;
            if (kind == IdleAutoDefenseRewardDraftKind.LevelUp) LevelUpRewardDraftCount++;
            else if (kind == IdleAutoDefenseRewardDraftKind.EliteDefeated) EliteRewardDraftCount++;
            else if (kind == IdleAutoDefenseRewardDraftKind.BossDefeated) BossRewardDraftCount++;
        }

        internal IdleAutoDefenseRewardDraftChoice[] GenerateRewardDraftChoices(IdleAutoDefenseRewardDraftKind kind) => _selection.Generate(kind);

        internal static int GetRank(Dictionary<string, int> ranks, string key)
        {
            return ranks != null && !string.IsNullOrWhiteSpace(key) && ranks.TryGetValue(key, out int rank) ? rank : 0;
        }

        internal int GetRewardDraftChoiceCurrentRank(IdleAutoDefenseRewardDraftChoice choice)
        {
            if (choice == null || choice.IsUnlock) return 0;
            if (!string.IsNullOrWhiteSpace(choice.TargetWeaponId))
            {
                if (choice.Rarity == IdleAutoDefenseRewardRarity.Epic)
                    return GetRank(_weaponEpicUpgradeRanks, choice.TargetWeaponId);
                if (choice.Rarity == IdleAutoDefenseRewardRarity.Legendary)
                    return _weaponLegendaryUnlocks.Contains(choice.TargetWeaponId) ? 1 : 0;
                return GetRank(_weaponNormalUpgradeRanks, choice.TargetWeaponId);
            }

            switch (choice.EffectKind)
            {
                case IdleAutoDefenseRewardEffectKind.DamageRank: return _build.DamageUpgradeRank;
                case IdleAutoDefenseRewardEffectKind.FireRateRank: return _build.AttackSpeedUpgradeRank;
                case IdleAutoDefenseRewardEffectKind.RangeRank: return _build.RangeUpgradeRank;
                case IdleAutoDefenseRewardEffectKind.Repair: return _build.RepairUpgradeRank;
                default: return GetRank(_baseRewardRanks, choice.DedupeKey);
            }
        }

        internal void AwardExperienceForCompletedWaves(EncounterSnapshot snapshot)
        {
            if (snapshot == null) return;
            for (int i = 0; i < snapshot.Waves.Count; i++)
            {
                WaveProgressSnapshot wave = snapshot.Waves[i];
                if (!wave.Emitted || string.IsNullOrWhiteSpace(wave.WaveId.Value)) continue;
                if (!_rewardedCompletedWaveIds.Add(wave.WaveId.Value)) continue;
                WaveRewardExperienceCount++;
                AddCommanderExperience(RewardDraftSettings.WaveCompletionExperience);
            }
        }

        internal void RecordEnemyDefeated(long enemyId, long authoredReward, bool boss, bool elite, Vector3 position)
        {
            if (enemyId <= 0 || !_rewardedEnemyDefeatIds.Add(enemyId)) return;
            _pendingAuthoredKillCredits += authoredReward;
            _creditFeedback(position);
            if (boss)
            {
                BossDefeatCount++;
                AddCommanderExperience(RewardDraftSettings.BossEnemyExperience);
                QueueOrOpenRewardDraft(IdleAutoDefenseRewardDraftKind.BossDefeated);
            }
            else if (elite)
            {
                EliteDefeatCount++;
                AddCommanderExperience(RewardDraftSettings.EliteEnemyExperience);
                QueueOrOpenRewardDraft(IdleAutoDefenseRewardDraftKind.EliteDefeated);
            }
            else
            {
                AddCommanderExperience(RewardDraftSettings.NormalEnemyExperience);
            }
        }

        internal void OfferFirstRewardDraftIfReady(float targetSeconds)
        {
            if (_starterRewardDraftOffered || FirstRewardDraftSeconds >= 0f || _survivalSeconds() < targetSeconds) return;
            _starterRewardDraftOffered = true;
            QueueOrOpenRewardDraft(IdleAutoDefenseRewardDraftKind.LevelUp);
            _readyFeedback();
        }
    }
}
