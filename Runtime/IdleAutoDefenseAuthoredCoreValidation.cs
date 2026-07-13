using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.WeaponSystems.Authoring;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public static class IdleAutoDefenseAuthoredCoreValidator
    {
        public static void AddIssues(GameContentSetAsset contentSet, List<GameContentSetValidationIssue> issues)
        {
            if (contentSet == null || issues == null) return;
            ValidateRewardCatalog(contentSet, issues);
            ValidateEconomy(contentSet, issues);
            ValidateRunProfile(contentSet, issues);
            ValidateProgression(contentSet, issues);
            ValidateOfflineProgression(contentSet, issues);
            ValidateGameRules(contentSet, issues);
        }

        private static void ValidateRewardCatalog(GameContentSetAsset contentSet, List<GameContentSetValidationIssue> issues)
        {
            IdleAutoDefenseRewardCatalogAsset asset = contentSet.RewardCatalog;
            const string root = "AuthoredCore.RewardCatalog";
            if (asset == null)
            {
                issues.Add(GameContentSetValidationIssue.Error(root, "Assign the authored live reward catalog."));
                return;
            }

            RequireId(asset.Id, root + ".Id", issues);
            if (asset.Settings == null)
                issues.Add(GameContentSetValidationIssue.Error(root + ".Settings", "Reward draft cadence, rarity weights, and rank thresholds are required."));
            else
                ValidateRewardSettings(asset.Settings, root + ".Settings", issues);
            IdleAutoDefenseRewardDraftCatalog catalog = asset.Catalog;
            if (catalog == null)
            {
                issues.Add(GameContentSetValidationIssue.Error(root + ".Choices", "The live reward choice catalog is required."));
                return;
            }

            var knownWeapons = CollectWeaponIds(contentSet.AvailableWeapons);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var prerequisites = new List<PrerequisiteCheck>();
            bool requireAssetReferences = !IsTransient(asset);
            ValidateUnlockRewards(catalog.WeaponUnlocks, knownWeapons, ids, prerequisites, requireAssetReferences, root + ".Unlocks", issues);
            ValidateWeaponRewards(catalog.NormalWeaponRewards, IdleAutoDefenseRewardTrack.Normal, knownWeapons, ids, prerequisites, requireAssetReferences, root + ".Normal", issues);
            ValidateWeaponRewards(catalog.EpicWeaponRewards, IdleAutoDefenseRewardTrack.Epic, knownWeapons, ids, prerequisites, requireAssetReferences, root + ".Epic", issues);
            ValidateWeaponRewards(catalog.LegendaryWeaponRewards, IdleAutoDefenseRewardTrack.Legendary, knownWeapons, ids, prerequisites, requireAssetReferences, root + ".Legendary", issues);
            ValidateBaseRewards(catalog.BaseRewards, ids, prerequisites, root + ".Base", issues);
            ValidatePrerequisites(prerequisites, ids, issues);

            if (catalog.NormalWeaponRewards.Count == 0)
                issues.Add(GameContentSetValidationIssue.Error(root + ".Normal", "At least one authored normal reward is required."));
            if (catalog.EpicWeaponRewards.Count == 0)
                issues.Add(GameContentSetValidationIssue.Error(root + ".Epic", "At least one authored Epic reward is required."));
            if (catalog.LegendaryWeaponRewards.Count == 0)
                issues.Add(GameContentSetValidationIssue.Error(root + ".Legendary", "At least one authored Legendary reward is required."));
            if (catalog.BaseRewards.Count == 0)
                issues.Add(GameContentSetValidationIssue.Error(root + ".Base", "At least one authored base reward is required."));
        }

        private static void ValidateRewardSettings(IdleAutoDefenseRewardDraftSettings settings, string path, List<GameContentSetValidationIssue> issues)
        {
            if (settings.ChoiceCount <= 0) issues.Add(GameContentSetValidationIssue.Error(path + ".ChoiceCount", "Reward choice count must be positive."));
            if (settings.NormalEnemyExperience <= 0L) issues.Add(GameContentSetValidationIssue.Error(path + ".NormalEnemyExperience", "Normal enemies must grant authored draft experience."));
            if (settings.BaseExperienceToNextLevel <= 0L) issues.Add(GameContentSetValidationIssue.Error(path + ".BaseExperienceToNextLevel", "Base XP must be positive."));
            if (settings.NormalInvestmentsForEpic <= 0) issues.Add(GameContentSetValidationIssue.Error(path + ".NormalInvestmentsForEpic", "Epic rank threshold must be positive."));
            if (settings.EpicInvestmentsForLegendary <= 0) issues.Add(GameContentSetValidationIssue.Error(path + ".EpicInvestmentsForLegendary", "Legendary rank threshold must be positive."));
            if (settings.LevelUpRarityWeights.Common <= 0d && settings.LevelUpRarityWeights.Uncommon <= 0d && settings.LevelUpRarityWeights.Rare <= 0d)
                issues.Add(GameContentSetValidationIssue.Error(path + ".LevelUpRarityWeights", "Level-up drafts need at least one positive authored rarity weight."));
        }

        private static void ValidateUnlockRewards(
            IReadOnlyList<IdleAutoDefenseWeaponUnlockReward> rewards,
            HashSet<string> knownWeapons,
            HashSet<string> ids,
            List<PrerequisiteCheck> prerequisites,
            bool requireAssetReferences,
            string path,
            List<GameContentSetValidationIssue> issues)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                IdleAutoDefenseWeaponUnlockReward reward = rewards[i];
                string itemPath = path + "[" + i + "]";
                if (reward == null)
                {
                    issues.Add(GameContentSetValidationIssue.Error(itemPath, "Reward entry is empty."));
                    continue;
                }

                ValidateRewardIdentity(reward.Id, reward.Weight, reward.MaxRank, reward.EligibleSources, ids, itemPath, issues);
                ValidateWeaponReference(reward.Weapon, reward.WeaponId, knownWeapons, requireAssetReferences, itemPath, issues);
                AddPrerequisites(reward.Id, reward.PrerequisiteIds, itemPath, prerequisites);
            }
        }

        private static void ValidateWeaponRewards(
            IReadOnlyList<IdleAutoDefenseWeaponRewardDefinition> rewards,
            IdleAutoDefenseRewardTrack expectedTrack,
            HashSet<string> knownWeapons,
            HashSet<string> ids,
            List<PrerequisiteCheck> prerequisites,
            bool requireAssetReferences,
            string path,
            List<GameContentSetValidationIssue> issues)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                IdleAutoDefenseWeaponRewardDefinition reward = rewards[i];
                string itemPath = path + "[" + i + "]";
                if (reward == null)
                {
                    issues.Add(GameContentSetValidationIssue.Error(itemPath, "Reward entry is empty."));
                    continue;
                }

                ValidateRewardIdentity(reward.Id, reward.Weight, reward.MaxRank, reward.EligibleSources, ids, itemPath, issues);
                if (reward.Track != expectedTrack)
                    issues.Add(GameContentSetValidationIssue.Error(itemPath + ".Track", "Reward is in the wrong authored track."));
                if (string.IsNullOrWhiteSpace(reward.TierKey))
                    issues.Add(GameContentSetValidationIssue.Error(itemPath + ".TierKey", "Stable tier key is required."));
                if (reward.EffectKind == IdleAutoDefenseRewardEffectKind.None)
                    issues.Add(GameContentSetValidationIssue.Error(itemPath + ".Effect", "A gameplay effect is required."));
                if (!IsFinite(reward.Amount))
                    issues.Add(GameContentSetValidationIssue.Error(itemPath + ".Amount", "Reward amount must be finite."));
                ValidateWeaponReference(reward.Weapon, reward.WeaponId, knownWeapons, requireAssetReferences, itemPath, issues);
                AddPrerequisites(reward.Id, reward.PrerequisiteIds, itemPath, prerequisites);
            }
        }

        private static void ValidateBaseRewards(
            IReadOnlyList<IdleAutoDefenseBaseRewardDefinition> rewards,
            HashSet<string> ids,
            List<PrerequisiteCheck> prerequisites,
            string path,
            List<GameContentSetValidationIssue> issues)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                IdleAutoDefenseBaseRewardDefinition reward = rewards[i];
                string itemPath = path + "[" + i + "]";
                if (reward == null)
                {
                    issues.Add(GameContentSetValidationIssue.Error(itemPath, "Reward entry is empty."));
                    continue;
                }

                ValidateRewardIdentity(reward.Id, reward.Weight, reward.MaxRank, reward.EligibleSources, ids, itemPath, issues);
                if (string.IsNullOrWhiteSpace(reward.TargetId))
                    issues.Add(GameContentSetValidationIssue.Error(itemPath + ".Target", "Stable effect target ID is required."));
                if (reward.EffectKind == IdleAutoDefenseRewardEffectKind.None)
                    issues.Add(GameContentSetValidationIssue.Error(itemPath + ".Effect", "A gameplay effect is required."));
                if (!IsFinite(reward.Amount))
                    issues.Add(GameContentSetValidationIssue.Error(itemPath + ".Amount", "Reward amount must be finite."));
                AddPrerequisites(reward.Id, reward.PrerequisiteIds, itemPath, prerequisites);
            }
        }

        private static void ValidateRewardIdentity(
            string id,
            double weight,
            int maxRank,
            IdleAutoDefenseRewardSourceEligibility eligibility,
            HashSet<string> ids,
            string path,
            List<GameContentSetValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(id))
                issues.Add(GameContentSetValidationIssue.Error(path + ".Id", "Stable reward ID is required."));
            else if (!ids.Add(id.Trim()))
                issues.Add(GameContentSetValidationIssue.Error(path + ".Id", "Duplicate reward ID: " + id));
            if (!IsFinite(weight) || weight <= 0d)
                issues.Add(GameContentSetValidationIssue.Error(path + ".Weight", "Reward weight must be finite and greater than zero."));
            if (maxRank <= 0) issues.Add(GameContentSetValidationIssue.Error(path + ".MaxRank", "Max rank must be positive."));
            if (eligibility == IdleAutoDefenseRewardSourceEligibility.None)
                issues.Add(GameContentSetValidationIssue.Error(path + ".EligibleSources", "At least one reward source must be eligible."));
        }

        private static void ValidateWeaponReference(
            WeaponDefinitionAsset weapon,
            string weaponId,
            HashSet<string> knownWeapons,
            bool requireAssetReference,
            string path,
            List<GameContentSetValidationIssue> issues)
        {
            if (requireAssetReference && weapon == null)
                issues.Add(GameContentSetValidationIssue.Error(path + ".Weapon", "Persisted reward records must use a canonical weapon asset reference."));
            if (string.IsNullOrWhiteSpace(weaponId) || !knownWeapons.Contains(weaponId.Trim()))
                issues.Add(GameContentSetValidationIssue.Error(path + ".Weapon", "Reward target is missing or outside the authored weapon pool: " + weaponId));
        }

        private static void AddPrerequisites(string id, IReadOnlyList<string> requiredIds, string path, List<PrerequisiteCheck> target)
        {
            for (int i = 0; i < requiredIds.Count; i++)
                target.Add(new PrerequisiteCheck(id, requiredIds[i], path + ".Prerequisites[" + i + "]"));
        }

        private static void ValidatePrerequisites(List<PrerequisiteCheck> prerequisites, HashSet<string> knownIds, List<GameContentSetValidationIssue> issues)
        {
            for (int i = 0; i < prerequisites.Count; i++)
            {
                PrerequisiteCheck prerequisite = prerequisites[i];
                if (string.IsNullOrWhiteSpace(prerequisite.RequiredId) || !knownIds.Contains(prerequisite.RequiredId.Trim()))
                    issues.Add(GameContentSetValidationIssue.Error(prerequisite.Path, "Unknown reward prerequisite: " + prerequisite.RequiredId));
                if (string.Equals(prerequisite.OwnerId, prerequisite.RequiredId, StringComparison.OrdinalIgnoreCase))
                    issues.Add(GameContentSetValidationIssue.Error(prerequisite.Path, "Reward cannot require itself."));
            }
        }

        private static void ValidateEconomy(GameContentSetAsset contentSet, List<GameContentSetValidationIssue> issues)
        {
            IdleAutoDefenseEconomyAsset economy = contentSet.Economy;
            const string root = "AuthoredCore.Economy";
            if (economy == null)
            {
                issues.Add(GameContentSetValidationIssue.Error(root, "Assign the authored economy definition."));
                return;
            }

            RequireId(economy.Id, root + ".Id", issues);
            var currencyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < economy.Currencies.Count; i++)
            {
                IdleAutoDefenseCurrencyRecord currency = economy.Currencies[i];
                string path = root + ".Currencies[" + i + "]";
                if (currency == null || string.IsNullOrWhiteSpace(currency.Id))
                {
                    issues.Add(GameContentSetValidationIssue.Error(path, "Currency ID is required."));
                    continue;
                }

                if (!currencyIds.Add(currency.Id.Trim())) issues.Add(GameContentSetValidationIssue.Error(path + ".Id", "Duplicate currency ID: " + currency.Id));
                if (currency.Capacity < 0L) issues.Add(GameContentSetValidationIssue.Error(path + ".Capacity", "Currency capacity cannot be negative."));
                if (currency.StartingAmount < 0L) issues.Add(GameContentSetValidationIssue.Error(path + ".StartingAmount", "Starting currency cannot be negative."));
                if (currency.StartingAmount > currency.Capacity) issues.Add(GameContentSetValidationIssue.Error(path + ".StartingAmount", "Starting currency exceeds its capacity."));
            }

            RequireReference(economy.PrimaryCurrencyId, currencyIds, root + ".PrimaryCurrency", issues);
            RequireReference(economy.SecondaryCurrencyId, currencyIds, root + ".SecondaryCurrency", issues);
            RequireReference(economy.PassiveIncomeCurrencyId, currencyIds, root + ".PassiveIncome.Currency", issues);
            if (economy.PassiveIncomeAmount < 0L) issues.Add(GameContentSetValidationIssue.Error(root + ".PassiveIncome.Amount", "Passive income cannot be negative."));
            if (economy.PassiveIncomeIntervalTicks <= 0) issues.Add(GameContentSetValidationIssue.Error(root + ".PassiveIncome.IntervalTicks", "Passive income interval must be positive."));

            var costIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < economy.UpgradeCosts.Count; i++)
            {
                IdleAutoDefenseCostCurve cost = economy.UpgradeCosts[i];
                string path = root + ".UpgradeCosts[" + i + "]";
                if (cost == null || string.IsNullOrWhiteSpace(cost.Id))
                {
                    issues.Add(GameContentSetValidationIssue.Error(path, "Cost ID is required."));
                    continue;
                }

                if (!costIds.Add(cost.Id.Trim())) issues.Add(GameContentSetValidationIssue.Error(path + ".Id", "Duplicate cost ID: " + cost.Id));
                RequireReference(cost.CurrencyId, currencyIds, path + ".Currency", issues);
                if (cost.BaseCost < 0 || cost.CostPerRank < 0) issues.Add(GameContentSetValidationIssue.Error(path, "Upgrade costs cannot be negative."));
            }

            RequireCost(economy, economy.DamageUpgradeCostCurveId, root, issues);
            RequireCost(economy, economy.FireRateUpgradeCostCurveId, root, issues);
            RequireCost(economy, economy.RangeUpgradeCostCurveId, root, issues);
            RequireCost(economy, economy.RepairUpgradeCostCurveId, root, issues);
            RequireCost(economy, economy.OverdriveCostCurveId, root, issues);
            if (economy.EncounterCompletionCredits < 0L || economy.EncounterCompletionParts < 0L || economy.EncounterCompletionAccountXp < 0L)
                issues.Add(GameContentSetValidationIssue.Error(root + ".EncounterReward", "Encounter reward values cannot be negative."));
            if (!IsFinite(economy.RunRewardClaimMultiplier) || economy.RunRewardClaimMultiplier < 1d)
                issues.Add(GameContentSetValidationIssue.Error(root + ".RunRewardClaimMultiplier", "Claim multiplier must be finite and at least one."));
        }

        private static void ValidateRunProfile(GameContentSetAsset contentSet, List<GameContentSetValidationIssue> issues)
        {
            IdleAutoDefenseRunProfileAsset profile = contentSet.RunProfile;
            const string root = "AuthoredCore.RunProfile";
            if (profile == null)
            {
                issues.Add(GameContentSetValidationIssue.Error(root, "Assign the authored run/session profile."));
                return;
            }

            RequireId(profile.Id, root + ".Id", issues);
            if (profile.TimeUnit != IdleAutoDefenseRunTimeUnit.SimulationTicks || profile.TickSemantics != IdleAutoDefenseTickSemantics.FixedRate)
                issues.Add(GameContentSetValidationIssue.Error(root + ".Time", "The playable profile must declare fixed-rate simulation tick semantics."));
            if (profile.SimulationTicksPerSecond <= 0) issues.Add(GameContentSetValidationIssue.Error(root + ".SimulationTicksPerSecond", "Tick rate must be positive."));
            if (profile.SessionLengthTicks <= 0)
            {
                if (profile.Endless)
                    issues.Add(GameContentSetValidationIssue.Warning(root + ".SessionLengthTicks", "Endless mode has no finite victory duration; session length is preview-only."));
                else
                    issues.Add(GameContentSetValidationIssue.Error(root + ".SessionLengthTicks", "Session length must be positive."));
            }
            if (!IsFinite(profile.DifficultyMultiplier) || profile.DifficultyMultiplier <= 0f)
                issues.Add(GameContentSetValidationIssue.Error(root + ".DifficultyMultiplier", "Difficulty multiplier must be finite and positive."));
            if (!IsFinite(profile.RewardMultiplier) || profile.RewardMultiplier <= 0f)
                issues.Add(GameContentSetValidationIssue.Error(root + ".RewardMultiplier", "Reward multiplier must be finite and positive."));
            if (profile.PreparationTicks < 0) issues.Add(GameContentSetValidationIssue.Error(root + ".PreparationTicks", "Preparation time cannot be negative."));
            if (profile.Waves.Count == 0) issues.Add(GameContentSetValidationIssue.Error(root + ".Waves", "Run profile must reference the authored wave sequence."));
            var waveIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < profile.Waves.Count; i++)
            {
                WaveDefinitionAsset wave = profile.Waves[i];
                string path = root + ".Waves[" + i + "]";
                if (wave == null || string.IsNullOrWhiteSpace(wave.Id)) issues.Add(GameContentSetValidationIssue.Error(path, "Wave reference is missing."));
                else if (!waveIds.Add(wave.Id.Trim())) issues.Add(GameContentSetValidationIssue.Error(path, "Duplicate wave reference: " + wave.Id));
            }
        }

        private static void ValidateProgression(GameContentSetAsset contentSet, List<GameContentSetValidationIssue> issues)
        {
            IdleAutoDefenseProgressionAsset progression = contentSet.Progression;
            const string root = "AuthoredCore.Progression";
            if (progression == null)
            {
                issues.Add(GameContentSetValidationIssue.Error(root, "Assign the authored persistent progression catalog."));
                return;
            }

            RequireId(progression.Id, root + ".Id", issues);
            var currencyIds = CollectCurrencyIds(contentSet.Economy);
            var trackIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < progression.Tracks.Count; i++)
            {
                IdleAutoDefenseProgressionTrackRecord track = progression.Tracks[i];
                string path = root + ".Tracks[" + i + "]";
                if (track == null || string.IsNullOrWhiteSpace(track.Id))
                {
                    issues.Add(GameContentSetValidationIssue.Error(path, "Progression track ID is required."));
                    continue;
                }

                if (!trackIds.Add(track.Id.Trim())) issues.Add(GameContentSetValidationIssue.Error(path + ".Id", "Duplicate progression track ID: " + track.Id));
                long previous = 0L;
                for (int thresholdIndex = 0; thresholdIndex < track.CumulativeThresholds.Count; thresholdIndex++)
                {
                    long threshold = track.CumulativeThresholds[thresholdIndex];
                    if (threshold <= previous) issues.Add(GameContentSetValidationIssue.Error(path + ".Thresholds[" + thresholdIndex + "]", "Thresholds must be strictly increasing."));
                    previous = threshold;
                }
            }

            RequireReference(progression.AccountTrackId, trackIds, root + ".AccountTrack", issues);
            var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < progression.ResearchNodes.Count; i++)
            {
                IdleAutoDefenseResearchNodeRecord node = progression.ResearchNodes[i];
                string path = root + ".ResearchNodes[" + i + "]";
                if (node == null || string.IsNullOrWhiteSpace(node.Id))
                {
                    issues.Add(GameContentSetValidationIssue.Error(path, "Research node ID is required."));
                    continue;
                }

                if (!nodeIds.Add(node.Id.Trim())) issues.Add(GameContentSetValidationIssue.Error(path + ".Id", "Duplicate research node ID: " + node.Id));
                RequireReference(node.CostCurrencyId, currencyIds, path + ".CostCurrency", issues);
                if (node.RankCosts.Count != node.MaxRank) issues.Add(GameContentSetValidationIssue.Error(path + ".RankCosts", "Provide exactly one authored cost for each rank."));
                for (int rank = 0; rank < node.RankCosts.Count; rank++)
                    if (node.RankCosts[rank] < 0L) issues.Add(GameContentSetValidationIssue.Error(path + ".RankCosts[" + rank + "]", "Research cost cannot be negative."));
                if (node.EffectKind == IdleAutoDefenseProgressionEffectKind.None || string.IsNullOrWhiteSpace(node.EffectTargetId) || !IsFinite(node.EffectAmountPerRank))
                    issues.Add(GameContentSetValidationIssue.Error(path + ".Effect", "Research node needs a finite authored effect and stable target."));
            }

            for (int i = 0; i < progression.ResearchNodes.Count; i++)
            {
                IdleAutoDefenseResearchNodeRecord node = progression.ResearchNodes[i];
                if (node == null) continue;
                for (int j = 0; j < node.Prerequisites.Count; j++)
                {
                    IdleAutoDefenseResearchPrerequisiteRecord prerequisite = node.Prerequisites[j];
                    string path = root + ".ResearchNodes[" + i + "].Prerequisites[" + j + "]";
                    if (prerequisite == null || !nodeIds.Contains(prerequisite.NodeId))
                        issues.Add(GameContentSetValidationIssue.Error(path, "Research prerequisite does not resolve: " + (prerequisite == null ? string.Empty : prerequisite.NodeId)));
                    else
                    {
                        IdleAutoDefenseResearchNodeRecord requiredNode = progression.FindResearchNode(prerequisite.NodeId);
                        if (requiredNode != null && prerequisite.MinimumRank > requiredNode.MaxRank)
                            issues.Add(GameContentSetValidationIssue.Error(path, "Required rank exceeds the prerequisite node's max rank."));
                    }
                }
            }

            RequireId(progression.ProfileDocumentId, root + ".ProfileDocumentId", issues);
            RequireId(progression.RunDocumentId, root + ".RunDocumentId", issues);
            RequireId(progression.SettingsDocumentId, root + ".SettingsDocumentId", issues);
        }

        private static void ValidateOfflineProgression(GameContentSetAsset contentSet, List<GameContentSetValidationIssue> issues)
        {
            IdleAutoDefenseOfflineProgressionAsset offline = contentSet.OfflineProgression;
            const string root = "AuthoredCore.OfflineProgression";
            if (offline == null)
            {
                issues.Add(GameContentSetValidationIssue.Error(root, "Assign authored offline progression settings."));
                return;
            }

            RequireId(offline.Id, root + ".Id", issues);
            if (!offline.Enabled) return;
            var currencyIds = CollectCurrencyIds(contentSet.Economy);
            if (!IsFinite(offline.MaximumOfflineSeconds) || offline.MaximumOfflineSeconds <= 0d)
                issues.Add(GameContentSetValidationIssue.Error(root + ".MaximumOfflineSeconds", "Offline cap must be finite and positive."));
            if (!IsFinite(offline.MinimumEligibleSeconds) || offline.MinimumEligibleSeconds < 0d || offline.MinimumEligibleSeconds > offline.MaximumOfflineSeconds)
                issues.Add(GameContentSetValidationIssue.Error(root + ".MinimumEligibleSeconds", "Minimum duration must be finite, non-negative, and within the cap."));
            RequireReference(offline.ProductionCurrencyId, currencyIds, root + ".ProductionCurrency", issues);
            RequireReference(offline.CycleCurrencyId, currencyIds, root + ".CycleCurrency", issues);
            if (!IsFinite(offline.ProductionAmountPerSecond) || offline.ProductionAmountPerSecond < 0d)
                issues.Add(GameContentSetValidationIssue.Error(root + ".ProductionRate", "Offline production rate must be finite and non-negative."));
            if (offline.CycleRewardAmount < 0L || !IsFinite(offline.CycleDurationSeconds) || offline.CycleDurationSeconds <= 0d)
                issues.Add(GameContentSetValidationIssue.Error(root + ".CycleReward", "Cycle amount must be non-negative and duration must be finite and positive."));
            if (!IsFinite(offline.ClaimMultiplier) || offline.ClaimMultiplier < 1d)
                issues.Add(GameContentSetValidationIssue.Error(root + ".ClaimMultiplier", "Claim multiplier must be finite and at least one."));
            RequireId(offline.SaveTimestampKey, root + ".SaveTimestampKey", issues);
        }

        private static void ValidateGameRules(GameContentSetAsset contentSet, List<GameContentSetValidationIssue> issues)
        {
            IdleAutoDefenseGameRulesAsset rules = contentSet.GameRules;
            const string root = "AuthoredCore.GameRules";
            if (rules == null)
            {
                issues.Add(GameContentSetValidationIssue.Error(root, "Assign the authored objective, spawn, module, and combat rules."));
                return;
            }

            RequireId(rules.Id, root + ".Id", issues);
            RequireId(rules.ObjectiveId, root + ".ObjectiveId", issues);
            RequireId(rules.RunRewardTargetId, root + ".RunRewardTargetId", issues);
            RequireId(rules.OfflineRewardTargetId, root + ".OfflineRewardTargetId", issues);
            RequireId(rules.DamageTypeId, root + ".DamageTypeId", issues);
            if (rules.ObjectiveMaximumHealth <= 0d || rules.ObjectiveContactRadius <= 0f || rules.ObjectiveLives <= 0)
                issues.Add(GameContentSetValidationIssue.Error(root + ".Objective", "Objective health, contact radius, and lives must be positive."));
            if (rules.SpawnRingRadius <= rules.ObjectiveContactRadius)
                issues.Add(GameContentSetValidationIssue.Error(root + ".SpawnRingRadius", "Spawn ring must be outside the objective."));
            var channelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rules.SpawnChannels.Count; i++)
            {
                string channelId = rules.SpawnChannels[i] == null ? string.Empty : rules.SpawnChannels[i].Id;
                if (string.IsNullOrWhiteSpace(channelId) || !channelIds.Add(channelId.Trim()))
                    issues.Add(GameContentSetValidationIssue.Error(root + ".SpawnChannels[" + i + "]", "Spawn channel ID is missing or duplicated."));
            }

            var weaponIds = CollectWeaponIds(contentSet.AvailableWeapons);
            var roles = new HashSet<IdleAutoDefenseModuleRole>();
            var moduleWeaponIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rules.Modules.Count; i++)
            {
                IdleAutoDefenseModuleRule module = rules.Modules[i];
                string path = root + ".Modules[" + i + "]";
                if (module == null || module.Weapon == null)
                {
                    issues.Add(GameContentSetValidationIssue.Error(path, "Module must reference its canonical authored weapon."));
                    continue;
                }

                if (!roles.Add(module.Role)) issues.Add(GameContentSetValidationIssue.Error(path + ".Role", "Duplicate module role: " + module.Role));
                if (!weaponIds.Contains(module.WeaponId)) issues.Add(GameContentSetValidationIssue.Error(path + ".Weapon", "Module weapon is outside the content set: " + module.WeaponId));
                if (!moduleWeaponIds.Add(module.WeaponId)) issues.Add(GameContentSetValidationIssue.Error(path + ".Weapon", "Duplicate module weapon reference: " + module.WeaponId));
                if (string.IsNullOrWhiteSpace(module.AttackId)) issues.Add(GameContentSetValidationIssue.Error(path + ".Attack", "Module weapon must resolve an authored attack."));
                if (module.BaseDamage <= 0d || module.BaseRange <= 0d || module.BaseCooldownTicks <= 0 || module.MinimumCooldownTicks <= 0 || module.BaseTargetCount <= 0)
                    issues.Add(GameContentSetValidationIssue.Error(path + ".Tuning", "Module damage, range, cooldown, and target count must be positive."));
                if (!module.StartsUnlocked && module.BuildCost <= 0)
                    issues.Add(GameContentSetValidationIssue.Error(path + ".BuildCost", "Locked modules need a positive authored weapon build cost."));
            }

            foreach (IdleAutoDefenseModuleRole role in Enum.GetValues(typeof(IdleAutoDefenseModuleRole)))
                if (!roles.Contains(role)) issues.Add(GameContentSetValidationIssue.Error(root + ".Modules", "Missing authored module role: " + role));
            foreach (string weaponId in weaponIds)
                if (!moduleWeaponIds.Contains(weaponId)) issues.Add(GameContentSetValidationIssue.Error(root + ".Modules", "Missing authored module rule for weapon: " + weaponId));
            IdleAutoDefenseModuleRule startingModule = rules.GetModule(IdleAutoDefenseModuleRole.StartingProjectile);
            if (startingModule != null && contentSet.StartingWeapon != startingModule.Weapon)
                issues.Add(GameContentSetValidationIssue.Error(root + ".Modules", "The starting projectile module must reference the content set's starting weapon."));
            var enemyIds = CollectEnemyIds(contentSet.EnemyPool);
            if (rules.EliteEnemy == null || !enemyIds.Contains(rules.EliteEnemyId))
                issues.Add(GameContentSetValidationIssue.Error(root + ".EliteEnemy", "Elite role must reference an enemy in the authored pool."));
            if (rules.BossEnemy == null || !enemyIds.Contains(rules.BossEnemyId))
                issues.Add(GameContentSetValidationIssue.Error(root + ".BossEnemy", "Boss role must reference an enemy in the authored pool."));
        }

        private static HashSet<string> CollectWeaponIds(IReadOnlyList<WeaponDefinitionAsset> weapons)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < weapons.Count; i++)
                if (weapons[i] != null && !string.IsNullOrWhiteSpace(weapons[i].Id)) ids.Add(weapons[i].Id.Trim());
            return ids;
        }

        private static HashSet<string> CollectEnemyIds(IReadOnlyList<EnemyDefinitionAsset> enemies)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] != null && !string.IsNullOrWhiteSpace(enemies[i].Id)) ids.Add(enemies[i].Id.Trim());
            return ids;
        }

        private static HashSet<string> CollectCurrencyIds(IdleAutoDefenseEconomyAsset economy)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (economy == null) return ids;
            for (int i = 0; i < economy.Currencies.Count; i++)
                if (economy.Currencies[i] != null && !string.IsNullOrWhiteSpace(economy.Currencies[i].Id)) ids.Add(economy.Currencies[i].Id.Trim());
            return ids;
        }

        private static void RequireCost(IdleAutoDefenseEconomyAsset economy, string costId, string root, List<GameContentSetValidationIssue> issues)
        {
            if (economy.GetUpgradeCostCurve(costId) == null)
                issues.Add(GameContentSetValidationIssue.Error(root + ".UpgradeCosts", "Missing required upgrade cost: " + costId));
        }

        private static void RequireReference(string id, HashSet<string> knownIds, string path, List<GameContentSetValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(id) || !knownIds.Contains(id.Trim()))
                issues.Add(GameContentSetValidationIssue.Error(path, "Reference does not resolve: " + id));
        }

        private static void RequireId(string id, string path, List<GameContentSetValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(id)) issues.Add(GameContentSetValidationIssue.Error(path, "Stable ID is required."));
        }

        private static bool IsTransient(UnityEngine.Object asset)
        {
            return asset != null && (asset.hideFlags & HideFlags.HideAndDontSave) == HideFlags.HideAndDontSave;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private readonly struct PrerequisiteCheck
        {
            public PrerequisiteCheck(string ownerId, string requiredId, string path)
            {
                OwnerId = ownerId ?? string.Empty;
                RequiredId = requiredId ?? string.Empty;
                Path = path ?? string.Empty;
            }

            public string OwnerId { get; }
            public string RequiredId { get; }
            public string Path { get; }
        }
    }
}
