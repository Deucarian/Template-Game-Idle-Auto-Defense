using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.AutoDefense;
using Deucarian.IdleProgression;
using Deucarian.Progression;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Owns persistent progression, restoration and reward claims across run restarts.</summary>
    internal sealed class IdleAutoDefensePersistentProgression
    {
        private readonly Action<IdleAutoDefenseResearchNodeRecord> _applyEffect;
        private ProgressionCatalog _progressionCatalog;
        private ProgressionState _progressionState;
        private IdleProgressionDefinition _offlineDefinition;
        private IdleAutoDefenseProgressionAsset _activeProgression;
        private IdleAutoDefenseEconomyAsset _activeEconomy;
        private IdleAutoDefenseOfflineProgressionAsset _activeOfflineProgression;
        private GameContentSetResolution _resolvedContentSet;
        private bool _completionRewardApplied;
        private CurrencyId RuntimeCredits => _activeEconomy == null ? BasicIdleAutoDefenseGame.Credits : _activeEconomy.PrimaryCurrency;
        private CurrencyId RuntimeParts => _activeEconomy == null ? BasicIdleAutoDefenseGame.Parts : _activeEconomy.SecondaryCurrency;

        internal IdleAutoDefensePersistentProgression(Action<IdleAutoDefenseResearchNodeRecord> applyEffect)
        {
            _applyEffect = applyEffect ?? throw new ArgumentNullException(nameof(applyEffect));
        }

        internal long OfflineRewardCredits { get; private set; }
        internal long OfflineRewardParts { get; private set; }
        internal long EncounterRewardCredits { get; private set; }
        internal long EncounterRewardParts { get; private set; }
        internal IdleProgressionResultCode LastOfflineRewardCode { get; private set; } = IdleProgressionResultCode.NoElapsedTime;

        internal void Bind(IdleAutoDefenseProgressionAsset progression, IdleAutoDefenseEconomyAsset economy,
            IdleAutoDefenseOfflineProgressionAsset offline, GameContentSetResolution contentSet, bool authored)
        {
            _activeProgression = progression;
            _activeEconomy = economy;
            _activeOfflineProgression = offline;
            _resolvedContentSet = contentSet;
            _progressionCatalog ??= progression == null || economy == null
                ? BasicIdleAutoDefenseGame.CreateProgressionCatalog() : progression.CreateRuntimeCatalog(economy);
            bool created = _progressionState == null;
            _progressionState ??= new ProgressionState();
            if (created && authored) ApplyContentSetStartingResources(contentSet);
            _offlineDefinition = offline == null ? BasicIdleAutoDefenseGame.CreateOfflineProgressionDefinition() : offline.CreateRuntimeDefinition();
        }

        internal void ResetRunRewards()
        {
            EncounterRewardCredits = 0;
            EncounterRewardParts = 0;
            _completionRewardApplied = false;
        }

        internal void MultiplyOfflineClaim(double multiplier)
        {
            OfflineRewardCredits = (long)Math.Ceiling(OfflineRewardCredits * multiplier);
            OfflineRewardParts = (long)Math.Ceiling(OfflineRewardParts * multiplier);
        }

        internal void MultiplyEncounterClaim(double multiplier)
        {
            EncounterRewardCredits = (long)Math.Ceiling(EncounterRewardCredits * multiplier);
            EncounterRewardParts = (long)Math.Ceiling(EncounterRewardParts * multiplier);
        }

        internal void AddCurrencyClaim(long amount) => EncounterRewardCredits += amount;

        internal int GetPersistentResearchRank(string nodeId)
        {
            return _progressionState == null || string.IsNullOrWhiteSpace(nodeId)
                ? 0
                : _progressionState.GetResearchRank(new ResearchNodeId(nodeId));
        }

        internal long GetPersistentCurrencyBalance(string currencyId)
        {
            return _progressionState == null || string.IsNullOrWhiteSpace(currencyId)
                ? 0L
                : _progressionState.GetBalance(new CurrencyId(currencyId)).Value;
        }

        internal IdleAutoDefensePersistentProgressionData CapturePersistentProgression()
        {
            var data = new IdleAutoDefensePersistentProgressionData();
            if (_progressionState == null) return data;
            ProgressionSnapshot snapshot = _progressionState.CreateSnapshot();
            for (int i = 0; i < snapshot.Balances.Count; i++)
                data.Balances.Add(new IdleAutoDefensePersistentLongValue { Id = snapshot.Balances[i].Id.Value, Value = snapshot.Balances[i].Value });
            for (int i = 0; i < snapshot.Tracks.Count; i++)
                data.Tracks.Add(new IdleAutoDefensePersistentLongValue { Id = snapshot.Tracks[i].Id.Value, Value = snapshot.Tracks[i].Value });
            for (int i = 0; i < snapshot.Research.Count; i++)
                data.ResearchRanks.Add(new IdleAutoDefensePersistentIntValue { Id = snapshot.Research[i].Id.Value, Value = snapshot.Research[i].Value });
            for (int i = 0; i < snapshot.Unlocks.Count; i++) data.UnlockIds.Add(snapshot.Unlocks[i].Value);
            return data;
        }

        internal bool RestorePersistentProgression(IdleAutoDefensePersistentProgressionData data)
        {
            if (data == null || !data.HasData || _progressionCatalog == null || _activeProgression == null) return false;
            var restored = new ProgressionState();
            var balances = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var researchFunds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < data.Balances.Count; i++)
            {
                IdleAutoDefensePersistentLongValue value = data.Balances[i];
                if (value != null && !string.IsNullOrWhiteSpace(value.Id)) balances[value.Id] = Math.Max(0L, value.Value);
            }

            var targetRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < data.ResearchRanks.Count; i++)
            {
                IdleAutoDefensePersistentIntValue value = data.ResearchRanks[i];
                if (value != null && !string.IsNullOrWhiteSpace(value.Id)) targetRanks[value.Id] = Math.Max(0, value.Value);
            }
            for (int i = 0; i < _activeProgression.ResearchNodes.Count; i++)
            {
                IdleAutoDefenseResearchNodeRecord node = _activeProgression.ResearchNodes[i];
                if (node == null || !targetRanks.TryGetValue(node.Id, out int targetRank)) continue;
                long cost = 0L;
                for (int rank = 0; rank < Math.Min(targetRank, node.RankCosts.Count); rank++) cost += Math.Max(0L, node.RankCosts[rank]);
                researchFunds.TryGetValue(node.CostCurrencyId, out long current);
                researchFunds[node.CostCurrencyId] = current + cost;
            }

            var researchCurrencyLines = new List<CurrencyLine>();
            foreach (KeyValuePair<string, long> balance in researchFunds)
                if (balance.Value > 0L) researchCurrencyLines.Add(new CurrencyLine(new CurrencyId(balance.Key), new ProgressionAmount(balance.Value), true));
            var finalCurrencyLines = new List<CurrencyLine>();
            foreach (KeyValuePair<string, long> balance in balances)
                if (balance.Value > 0L) finalCurrencyLines.Add(new CurrencyLine(new CurrencyId(balance.Key), new ProgressionAmount(balance.Value), true));
            var xp = new List<XpGrant>();
            for (int i = 0; i < data.Tracks.Count; i++)
            {
                IdleAutoDefensePersistentLongValue value = data.Tracks[i];
                if (value != null && !string.IsNullOrWhiteSpace(value.Id) && value.Value > 0L)
                    xp.Add(new XpGrant(new TrackId(value.Id), new ProgressionAmount(value.Value)));
            }
            var unlocks = new List<UnlockId>();
            for (int i = 0; i < data.UnlockIds.Count; i++)
                if (!string.IsNullOrWhiteSpace(data.UnlockIds[i])) unlocks.Add(new UnlockId(data.UnlockIds[i]));
            ProgressionResult seed = restored.ApplyReward(
                _progressionCatalog,
                new ProgressionOperationId("idle-auto-defense.profile.restore.seed"),
                new RewardBundle(researchCurrencyLines, xp, unlocks));
            if (!seed.Succeeded) return false;

            int remaining = 0;
            foreach (int target in targetRanks.Values) remaining += target;
            for (int pass = 0; pass < remaining + 1 && remaining > 0; pass++)
            {
                bool progressed = false;
                for (int i = 0; i < _activeProgression.ResearchNodes.Count; i++)
                {
                    IdleAutoDefenseResearchNodeRecord node = _activeProgression.ResearchNodes[i];
                    if (node == null || !targetRanks.TryGetValue(node.Id, out int targetRank)) continue;
                    int currentRank = restored.GetResearchRank(new ResearchNodeId(node.Id));
                    if (currentRank >= targetRank) continue;
                    ProgressionResult purchase = restored.PurchaseResearch(
                        _progressionCatalog,
                        new ProgressionOperationId("idle-auto-defense.profile.restore." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(node.Id) + "." + (currentRank + 1).ToString(CultureInfo.InvariantCulture)),
                        new ResearchNodeId(node.Id));
                    if (!purchase.Succeeded) continue;
                    remaining--;
                    progressed = true;
                }
                if (!progressed) break;
            }
            if (remaining > 0) return false;

            ProgressionResult finalBalances = restored.ApplyReward(
                _progressionCatalog,
                new ProgressionOperationId("idle-auto-defense.profile.restore.balances"),
                new RewardBundle(finalCurrencyLines));
            if (!finalBalances.Succeeded) return false;

            _progressionState = restored;
            ApplyAllPersistentProgressionEffects();
            OfflineRewardCredits = GetPersistentCurrencyBalance(_activeEconomy.PrimaryCurrencyId);
            OfflineRewardParts = GetPersistentCurrencyBalance(_activeEconomy.SecondaryCurrencyId);
            return true;
        }

        internal void ResetPersistentProgression()
        {
            _progressionState = new ProgressionState();
            if (_progressionCatalog == null && _activeProgression != null && _activeEconomy != null)
                _progressionCatalog = _activeProgression.CreateRuntimeCatalog(_activeEconomy);
            ApplyContentSetStartingResources(_resolvedContentSet);
            OfflineRewardCredits = GetPersistentCurrencyBalance(_activeEconomy == null ? string.Empty : _activeEconomy.PrimaryCurrencyId);
            OfflineRewardParts = GetPersistentCurrencyBalance(_activeEconomy == null ? string.Empty : _activeEconomy.SecondaryCurrencyId);
        }

        internal bool TryPurchasePersistentUpgrade(string nodeId)
        {
            if (_progressionState == null || _progressionCatalog == null || _activeProgression == null || string.IsNullOrWhiteSpace(nodeId))
                return false;
            IdleAutoDefenseResearchNodeRecord node = _activeProgression.FindResearchNode(nodeId);
            if (node == null) return false;
            ProgressionResult result = _progressionState.PurchaseResearch(
                _progressionCatalog,
                new ProgressionOperationId("idle-auto-defense.research." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(node.Id) + "." + (GetPersistentResearchRank(node.Id) + 1).ToString(CultureInfo.InvariantCulture)),
                new ResearchNodeId(node.Id));
            if (!result.Succeeded) return false;
            _applyEffect(node);
            return true;
        }

        internal void ApplyAllPersistentProgressionEffects()
        {
            if (_activeProgression == null || _progressionState == null) return;
            for (int i = 0; i < _activeProgression.ResearchNodes.Count; i++)
            {
                IdleAutoDefenseResearchNodeRecord node = _activeProgression.ResearchNodes[i];
                if (node == null) continue;
                int rank = GetPersistentResearchRank(node.Id);
                for (int applied = 0; applied < rank; applied++) _applyEffect(node);
            }
        }

        internal IdleProgressionResult SimulateOfflineReward(DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc, double offlineMultiplier)
        {
            if (_offlineDefinition == null || _progressionState == null || _progressionCatalog == null)
                throw new InvalidOperationException("Offline rewards require a successfully bound authored core or an explicit fallback host.");
            IdleProgressionResult result = _activeOfflineProgression == null
                ? IdleProgressionCalculator.Calculate(lastSeenUtc, nowUtc, _offlineDefinition)
                : _activeOfflineProgression.Calculate(lastSeenUtc, nowUtc);
            LastOfflineRewardCode = result.Code;
            if (result.Reward.CurrencyLines.Count > 0)
            {
                _progressionState.ApplyReward(_progressionCatalog, new ProgressionOperationId("idle-auto-defense.offline." + nowUtc.UtcTicks), result.Reward);
            }

            long bonusCredits = CalculateOfflineBonusCredits(result, offlineMultiplier);
            if (bonusCredits > 0)
            {
                _progressionState.ApplyReward(
                    _progressionCatalog,
                    new ProgressionOperationId("idle-auto-defense.offline.bonus." + nowUtc.UtcTicks),
                    new RewardBundle(new[] { new CurrencyLine(RuntimeCredits, new ProgressionAmount(bonusCredits), true) }));
            }

            OfflineRewardCredits = _progressionState.GetBalance(RuntimeCredits).Value;
            OfflineRewardParts = _progressionState.GetBalance(RuntimeParts).Value;
            return result;
        }

        private long CalculateOfflineBonusCredits(IdleProgressionResult result, double offlineMultiplier)
        {
            if (offlineMultiplier <= 0d || result == null || result.Reward == null) return 0;
            for (int i = 0; i < result.Reward.CurrencyLines.Count; i++)
            {
                CurrencyLine line = result.Reward.CurrencyLines[i];
                if (line.CurrencyId.Equals(RuntimeCredits))
                    return (long)Math.Ceiling(line.Amount.Value * offlineMultiplier);
            }

            return 0;
        }

        internal void ApplyEncounterRewardIfTerminal(AutoDefenseRuntimeState state, int runSequence, double rewardMultiplier)
        {
            if (_completionRewardApplied || _progressionState == null || state == AutoDefenseRuntimeState.Running) return;
            RewardBundle authoredReward = _activeProgression == null || _activeEconomy == null
                ? BasicIdleAutoDefenseGame.CreateEncounterCompletionReward()
                : _activeProgression.CreateEncounterCompletionReward(_activeEconomy);
            ProgressionResult result = _progressionState.ApplyReward(
                _progressionCatalog,
                new ProgressionOperationId("idle-auto-defense.encounter.terminal." + runSequence.ToString(CultureInfo.InvariantCulture)),
                authoredReward);
            if (!result.Succeeded) return;
            long baseCredits = _activeEconomy == null ? 60L : Math.Max(0L, _activeEconomy.EncounterCompletionCredits);
            long bonusCredits = (long)Math.Ceiling(baseCredits * rewardMultiplier);
            if (bonusCredits > 0)
            {
                _progressionState.ApplyReward(
                    _progressionCatalog,
                    new ProgressionOperationId("idle-auto-defense.encounter.terminal.1.reward-bonus"),
                    new RewardBundle(new[] { new CurrencyLine(RuntimeCredits, new ProgressionAmount(bonusCredits), true) }));
            }

            _completionRewardApplied = true;
            EncounterRewardCredits = _progressionState.GetBalance(RuntimeCredits).Value;
            EncounterRewardParts = _progressionState.GetBalance(RuntimeParts).Value;
        }

        private void ApplyContentSetStartingResources(GameContentSetResolution resolution)
        {
            if (resolution == null || !resolution.IsValid || _progressionState == null || _progressionCatalog == null) return;
            var currencies = new List<CurrencyLine>();
            if (_activeEconomy != null && _activeEconomy.StartingCredits > 0)
                currencies.Add(new CurrencyLine(RuntimeCredits, new ProgressionAmount(_activeEconomy.StartingCredits), true));
            if (_activeEconomy != null && _activeEconomy.StartingParts > 0)
                currencies.Add(new CurrencyLine(RuntimeParts, new ProgressionAmount(_activeEconomy.StartingParts), true));
            if (currencies.Count == 0) return;

            _progressionState.ApplyReward(
                _progressionCatalog,
                new ProgressionOperationId("idle-auto-defense.content-set.starting-resources." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(resolution.ContentSet.Id)),
                new RewardBundle(currencies));
        }
    }
}
