using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.RunUpgrades;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Owns deterministic reward selection sequence; inventory is observed, never mutated.</summary>
    internal sealed class IdleAutoDefenseRewardSelection
    {
        private readonly IdleAutoDefenseRewardInventory _inventory;
        private int _sequence;

        internal IdleAutoDefenseRewardSelection(IdleAutoDefenseRewardInventory inventory)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        internal int Sequence => _sequence;
        internal void Reset() => _sequence = 0;

        internal IdleAutoDefenseRewardDraftChoice[] Generate(IdleAutoDefenseRewardDraftKind kind)
        {
            var candidates = new List<IdleAutoDefenseRewardDraftChoice>();
            AddWeaponUnlockRewardCandidates(candidates, kind);
            AddOwnedWeaponRewardCandidates(candidates, kind);
            AddBaseRewardCandidates(candidates, kind);
            return SelectWeightedRewardChoices(candidates, kind);
        }

        private void AddWeaponUnlockRewardCandidates(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseRewardDraftKind kind)
        {
            IReadOnlyList<IdleAutoDefenseWeaponUnlockReward> unlocks = _inventory.Catalog.WeaponUnlocks;
            for (int i = 0; i < unlocks.Count; i++)
                AddWeaponUnlockRewardCandidate(candidates, unlocks[i], kind);
        }

        private void AddWeaponUnlockRewardCandidate(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseWeaponUnlockReward unlock, IdleAutoDefenseRewardDraftKind kind)
        {
            if (unlock == null) return;
            if (!unlock.IsEligible(kind) || !RewardPrerequisitesMet(unlock.PrerequisiteIds)) return;
            string weaponId = unlock.WeaponId;
            if (_inventory.IsWeaponUnlocked(weaponId)) return;
            candidates.Add(new IdleAutoDefenseRewardDraftChoice(
                unlock.Id,
                unlock.DisplayName,
                unlock.GetRarity(kind),
                "Unlock",
                _inventory.ResolveWeaponDisplayName(weaponId),
                unlock.EffectDescription,
                string.Empty,
                true,
                weaponId,
                IdleAutoDefenseRewardEffectKind.UnlockWeapon,
                1d,
                "unlock." + weaponId,
                unlock.Weight));
        }

        private void AddOwnedWeaponRewardCandidates(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseRewardDraftKind kind)
        {
            IReadOnlyList<string> weaponIds = _inventory.Catalog.GetWeaponIds();
            for (int i = 0; i < weaponIds.Count; i++)
            {
                if (!_inventory.IsWeaponUnlocked(weaponIds[i])) continue;
                IdleAutoDefenseRewardDraftChoice choice = CreateNextWeaponRewardChoice(weaponIds[i], kind);
                if (choice != null) candidates.Add(choice);
            }
        }

        private IdleAutoDefenseRewardDraftChoice CreateNextWeaponRewardChoice(string weaponId, IdleAutoDefenseRewardDraftKind kind)
        {
            string targetName = _inventory.ResolveWeaponDisplayName(weaponId);
            int normalRank = IdleAutoDefenseRewardInventory.GetRank(_inventory.NormalRanks, weaponId);
            if (normalRank < _inventory.Settings.NormalInvestmentsForEpic)
            {
                IdleAutoDefenseWeaponRewardDefinition reward = _inventory.Catalog.GetNormalWeaponReward(weaponId, normalRank);
                return reward == null || !reward.IsEligible(kind) || !reward.IsAvailableAt(normalRank, 0) || !RewardPrerequisitesMet(reward.PrerequisiteIds)
                    ? null
                    : CreateRewardChoice(weaponId, reward, targetName);
            }

            int epicRank = IdleAutoDefenseRewardInventory.GetRank(_inventory.EpicRanks, weaponId);
            if (epicRank < _inventory.Settings.EpicInvestmentsForLegendary)
            {
                IdleAutoDefenseWeaponRewardDefinition reward = _inventory.Catalog.GetEpicWeaponReward(weaponId, epicRank);
                return reward == null || !reward.IsEligible(kind) || !reward.IsAvailableAt(normalRank, epicRank) || !RewardPrerequisitesMet(reward.PrerequisiteIds)
                    ? null
                    : CreateRewardChoice(weaponId, reward, targetName);
            }

            if (!_inventory.HasLegendaryWeapon(weaponId))
            {
                IdleAutoDefenseWeaponRewardDefinition reward = _inventory.Catalog.GetLegendaryWeaponReward(weaponId);
                return reward == null || !reward.IsEligible(kind) || !reward.IsAvailableAt(normalRank, epicRank) || !RewardPrerequisitesMet(reward.PrerequisiteIds)
                    ? null
                    : CreateRewardChoice(weaponId, reward, targetName);
            }

            return null;
        }

        private IdleAutoDefenseRewardDraftChoice CreateRewardChoice(string weaponId, IdleAutoDefenseWeaponRewardDefinition reward, string targetName)
        {
            return new IdleAutoDefenseRewardDraftChoice(
                reward.Id,
                reward.DisplayName,
                reward.Rarity,
                reward.TypeName,
                targetName,
                reward.EffectDescription,
                string.Empty,
                false,
                weaponId,
                reward.EffectKind,
                reward.Amount,
                weaponId + "." + reward.TierKey,
                reward.Weight);
        }

        private void AddBaseRewardCandidates(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseRewardDraftKind kind)
        {
            IReadOnlyList<IdleAutoDefenseBaseRewardDefinition> rewards = _inventory.Catalog.BaseRewards;
            for (int i = 0; i < rewards.Count; i++)
                AddBaseRewardCandidate(candidates, rewards[i], kind);
        }

        private void AddBaseRewardCandidate(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseBaseRewardDefinition reward, IdleAutoDefenseRewardDraftKind kind)
        {
            if (reward == null) return;
            if (!reward.IsEligible(kind) || !RewardPrerequisitesMet(reward.PrerequisiteIds)) return;
            string key = reward.Key;
            int rank = IdleAutoDefenseRewardInventory.GetRank(_inventory.BaseRanks, key);
            if (rank >= reward.MaxRank) return;
            candidates.Add(new IdleAutoDefenseRewardDraftChoice(
                reward.Id,
                reward.DisplayName,
                reward.Rarity,
                reward.TypeName,
                reward.TargetName,
                reward.EffectDescription,
                string.Empty,
                false,
                string.Empty,
                reward.EffectKind,
                reward.Amount,
                key,
                reward.Weight));
        }

        private IdleAutoDefenseRewardDraftChoice[] SelectWeightedRewardChoices(List<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseRewardDraftKind kind)
        {
            if (candidates == null || candidates.Count == 0) return Array.Empty<IdleAutoDefenseRewardDraftChoice>();
            int choiceCount = _inventory.Settings.ChoiceCount;
            int seed = 20260623 + _inventory.CommanderLevel * 17 + (int)kind * 1009 + _sequence++ * 97;
            var preferredChoices = new List<IdleAutoDefenseRewardDraftChoice>(2);
            IdleAutoDefenseRewardDraftChoice earlyUnlock = FindPreferredEarlyUnlock(candidates, kind);
            if (earlyUnlock != null) preferredChoices.Add(earlyUnlock);
            IdleAutoDefenseRewardDraftChoice excitingChoice = FindExcitingReward(candidates, preferredChoices);
            if (excitingChoice != null) preferredChoices.Add(excitingChoice);

            var options = new RunUpgradeDraftOption[candidates.Count];
            var choicesById = new Dictionary<RunUpgradeId, IdleAutoDefenseRewardDraftChoice>();
            for (int i = 0; i < candidates.Count; i++)
            {
                IdleAutoDefenseRewardDraftChoice choice = candidates[i];
                var id = new RunUpgradeId(choice.Id);
                choicesById.Add(id, choice);
                options[i] = new RunUpgradeDraftOption(
                    id,
                    CalculateRewardChoiceWeight(choice, kind),
                    new RunUpgradeDraftGroupId(NormalizeRewardDedupeKey(choice)));
            }

            var lockedIds = new RunUpgradeId[preferredChoices.Count];
            for (int i = 0; i < preferredChoices.Count; i++)
                lockedIds[i] = new RunUpgradeId(preferredChoices[i].Id);

            RunUpgradeDraftSelection selection = RunUpgradeDraftSelector.Select(
                options,
                new RunUpgradeDraftRequest(choiceCount, seed, 0, lockedIds));
            var selected = new IdleAutoDefenseRewardDraftChoice[selection.ChoiceIds.Count];
            for (int i = 0; i < selection.ChoiceIds.Count; i++)
                selected[i] = WithRewardHotkey(choicesById[selection.ChoiceIds[i]], i + 1);
            return selected;
        }

        private IdleAutoDefenseRewardDraftChoice FindPreferredEarlyUnlock(IReadOnlyList<IdleAutoDefenseRewardDraftChoice> candidates, IdleAutoDefenseRewardDraftKind kind)
        {
            if (candidates == null || kind != IdleAutoDefenseRewardDraftKind.LevelUp || _inventory.CommanderLevel > 2) return null;
            for (int i = 0; i < candidates.Count; i++)
            {
                IdleAutoDefenseRewardDraftChoice choice = candidates[i];
                if (choice == null || !choice.IsUnlock) continue;
                return choice;
            }

            return null;
        }

        private static IdleAutoDefenseRewardDraftChoice FindExcitingReward(
            IReadOnlyList<IdleAutoDefenseRewardDraftChoice> candidates,
            IReadOnlyList<IdleAutoDefenseRewardDraftChoice> preferredChoices)
        {
            if (candidates == null || preferredChoices == null || preferredChoices.Count >= 3) return null;
            for (int i = 0; i < preferredChoices.Count; i++)
                if (IsExcitingRewardChoice(preferredChoices[i]))
                    return null;

            int bestIndex = -1;
            IdleAutoDefenseRewardRarity bestRarity = IdleAutoDefenseRewardRarity.Common;
            for (int i = 0; i < candidates.Count; i++)
            {
                IdleAutoDefenseRewardDraftChoice choice = candidates[i];
                if (!IsExcitingRewardChoice(choice)) continue;
                if (ConflictsWithPreferredDedupeGroup(choice, preferredChoices)) continue;
                if (bestIndex >= 0 && choice.Rarity < bestRarity) continue;
                bestIndex = i;
                bestRarity = choice.Rarity;
            }

            return bestIndex < 0 ? null : candidates[bestIndex];
        }

        private static bool IsExcitingRewardChoice(IdleAutoDefenseRewardDraftChoice choice)
        {
            if (choice == null) return false;
            if (choice.IsUnlock) return true;
            if (choice.Rarity >= IdleAutoDefenseRewardRarity.Epic) return true;
            switch (choice.EffectKind)
            {
                case IdleAutoDefenseRewardEffectKind.ExtraProjectile:
                case IdleAutoDefenseRewardEffectKind.PulsePower:
                case IdleAutoDefenseRewardEffectKind.ArcPower:
                case IdleAutoDefenseRewardEffectKind.HomingPower:
                    return true;
                case IdleAutoDefenseRewardEffectKind.GlobalDamageMultiplier:
                case IdleAutoDefenseRewardEffectKind.ProjectileSpeed:
                    return choice.Rarity >= IdleAutoDefenseRewardRarity.Rare;
                default:
                    return false;
            }
        }

        private static bool ConflictsWithPreferredDedupeGroup(
            IdleAutoDefenseRewardDraftChoice candidate,
            IReadOnlyList<IdleAutoDefenseRewardDraftChoice> preferredChoices)
        {
            string candidateGroup = NormalizeRewardDedupeKey(candidate);
            for (int i = 0; i < preferredChoices.Count; i++)
                if (string.Equals(candidateGroup, NormalizeRewardDedupeKey(preferredChoices[i]), StringComparison.Ordinal))
                    return true;
            return false;
        }

        private static string NormalizeRewardDedupeKey(IdleAutoDefenseRewardDraftChoice choice)
        {
            return choice.DedupeKey.Trim().ToLowerInvariant();
        }

        private static IdleAutoDefenseRewardDraftChoice WithRewardHotkey(IdleAutoDefenseRewardDraftChoice choice, int hotkey)
        {
            return new IdleAutoDefenseRewardDraftChoice(
                choice.Id,
                choice.DisplayName,
                choice.Rarity,
                choice.TypeName,
                choice.TargetName,
                choice.EffectDescription,
                hotkey.ToString(CultureInfo.InvariantCulture),
                choice.IsUnlock,
                choice.TargetWeaponId,
                choice.EffectKind,
                choice.Amount,
                choice.DedupeKey,
                choice.Weight);
        }

        private double CalculateRewardChoiceWeight(IdleAutoDefenseRewardDraftChoice choice, IdleAutoDefenseRewardDraftKind kind)
        {
            double rarityWeight = _inventory.Settings.GetRarityWeight(kind, choice.Rarity);
            if (choice.IsUnlock) rarityWeight *= _inventory.Settings.GetUnlockWeightMultiplier(kind);
            return Math.Max(0.001d, rarityWeight * Math.Max(0.001d, choice.Weight));
        }

        private bool RewardPrerequisitesMet(IReadOnlyList<string> prerequisiteIds)
        {
            for (int i = 0; i < prerequisiteIds.Count; i++)
                if (!_inventory.HasSelectedReward(prerequisiteIds[i]))
                    return false;
            return true;
        }
    }
}
