using System;
using System.Collections.Generic;
using Deucarian.Progression;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public enum IdleAutoDefenseProgressionEffectKind
    {
        None = 0,
        ObjectiveMaximumHealth = 1,
        DamageRank = 2,
        ExtraProjectile = 3,
        OfflineRewardMultiplier = 4
    }

    [CreateAssetMenu(menuName = "Deucarian/Idle Auto Defense/Persistent Progression", fileName = "IdleAutoDefenseProgression")]
    public sealed class IdleAutoDefenseProgressionAsset : ScriptableObject
    {
        [SerializeField] private string _id = "progression.idle-auto-defense.playable";
        [SerializeField] private string _displayName = "Basic Idle Auto Defense Progression";
        [SerializeField] private IdleAutoDefenseProgressionTrackRecord[] _tracks = CreateDefaultTracks();
        [SerializeField] private IdleAutoDefenseResearchNodeRecord[] _researchNodes = CreateDefaultResearchNodes();
        [SerializeField] private string[] _encounterCompletionUnlockIds =
        {
            "unlock.idle-auto-defense.starter",
            "unlock.idle-auto-defense.stage.pressure-ring",
            "unlock.idle-auto-defense.module.pulse-cannon",
            "unlock.idle-auto-defense.module.shard-launcher"
        };
        [SerializeField] private string _accountTrackId = "track.idle-auto-defense.account";
        [SerializeField] private string _profileDocumentId = "idle-auto-defense-template-profile";
        [SerializeField] private string _runDocumentId = "idle-auto-defense-template-run";
        [SerializeField] private string _settingsDocumentId = "idle-auto-defense-template-settings";
        [SerializeField] private int _saveVersion = 2;

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public IReadOnlyList<IdleAutoDefenseProgressionTrackRecord> Tracks => _tracks ?? Array.Empty<IdleAutoDefenseProgressionTrackRecord>();
        public IReadOnlyList<IdleAutoDefenseResearchNodeRecord> ResearchNodes => _researchNodes ?? Array.Empty<IdleAutoDefenseResearchNodeRecord>();
        public IReadOnlyList<string> EncounterCompletionUnlockIds => _encounterCompletionUnlockIds ?? Array.Empty<string>();
        public string AccountTrackId => _accountTrackId ?? string.Empty;
        public string ProfileDocumentId => _profileDocumentId ?? string.Empty;
        public string RunDocumentId => _runDocumentId ?? string.Empty;
        public string SettingsDocumentId => _settingsDocumentId ?? string.Empty;
        public int SaveVersion => Math.Max(1, _saveVersion);

        public ProgressionCatalog CreateRuntimeCatalog(IdleAutoDefenseEconomyAsset economy)
        {
            if (economy == null) throw new ArgumentNullException(nameof(economy));
            var currencies = new CurrencyDefinition[economy.Currencies.Count];
            for (int i = 0; i < currencies.Length; i++)
            {
                IdleAutoDefenseCurrencyRecord currency = economy.Currencies[i];
                currencies[i] = new CurrencyDefinition(new CurrencyId(currency.Id), new ProgressionAmount(currency.Capacity));
            }

            var tracks = new ProgressionTrackDefinition[Tracks.Count];
            for (int i = 0; i < tracks.Length; i++) tracks[i] = Tracks[i].ToRuntimeDefinition();
            var research = new ResearchNodeDefinition[ResearchNodes.Count];
            for (int i = 0; i < research.Length; i++) research[i] = ResearchNodes[i].ToRuntimeDefinition();
            return new ProgressionCatalog(currencies, tracks, research);
        }

        public RewardBundle CreateEncounterCompletionReward(IdleAutoDefenseEconomyAsset economy)
        {
            if (economy == null) throw new ArgumentNullException(nameof(economy));
            var currencies = new List<CurrencyLine>();
            if (economy.EncounterCompletionCredits > 0L)
                currencies.Add(new CurrencyLine(economy.PrimaryCurrency, new ProgressionAmount(economy.EncounterCompletionCredits), true));
            if (economy.EncounterCompletionParts > 0L)
                currencies.Add(new CurrencyLine(economy.SecondaryCurrency, new ProgressionAmount(economy.EncounterCompletionParts), true));

            var unlocks = new List<UnlockId>();
            for (int i = 0; i < EncounterCompletionUnlockIds.Count; i++)
                if (!string.IsNullOrWhiteSpace(EncounterCompletionUnlockIds[i]))
                    unlocks.Add(new UnlockId(EncounterCompletionUnlockIds[i]));
            XpGrant[] xp = economy.EncounterCompletionAccountXp <= 0L
                ? Array.Empty<XpGrant>()
                : new[] { new XpGrant(new TrackId(AccountTrackId), new ProgressionAmount(economy.EncounterCompletionAccountXp)) };
            return new RewardBundle(currencies, xp, unlocks);
        }

        public IdleAutoDefenseResearchNodeRecord FindResearchNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) return null;
            for (int i = 0; i < ResearchNodes.Count; i++)
            {
                IdleAutoDefenseResearchNodeRecord node = ResearchNodes[i];
                if (node != null && string.Equals(node.Id, nodeId, StringComparison.OrdinalIgnoreCase))
                    return node;
            }

            return null;
        }

        public void Configure(
            string id,
            string displayName,
            IReadOnlyList<IdleAutoDefenseProgressionTrackRecord> tracks,
            IReadOnlyList<IdleAutoDefenseResearchNodeRecord> researchNodes,
            IReadOnlyList<string> encounterCompletionUnlockIds,
            string accountTrackId,
            string profileDocumentId,
            string runDocumentId,
            string settingsDocumentId,
            int saveVersion)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _tracks = Copy(tracks);
            _researchNodes = Copy(researchNodes);
            _encounterCompletionUnlockIds = CopyStrings(encounterCompletionUnlockIds);
            _accountTrackId = accountTrackId ?? string.Empty;
            _profileDocumentId = profileDocumentId ?? string.Empty;
            _runDocumentId = runDocumentId ?? string.Empty;
            _settingsDocumentId = settingsDocumentId ?? string.Empty;
            _saveVersion = saveVersion;
        }

        public static IdleAutoDefenseProgressionAsset CreateTransient()
        {
            var asset = CreateInstance<IdleAutoDefenseProgressionAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            return asset;
        }

        private static IdleAutoDefenseProgressionTrackRecord[] CreateDefaultTracks()
        {
            return new[]
            {
                new IdleAutoDefenseProgressionTrackRecord(
                    "track.idle-auto-defense.account",
                    "Account XP",
                    0,
                    new long[] { 100, 250, 500, 900 })
            };
        }

        private static IdleAutoDefenseResearchNodeRecord[] CreateDefaultResearchNodes()
        {
            return new[]
            {
                new IdleAutoDefenseResearchNodeRecord(
                    "research.idle-auto-defense.core-plating", "Core Plating", 3,
                    "currency.idle-auto-defense.credits", new long[] { 25, 75, 160 },
                    Array.Empty<IdleAutoDefenseResearchPrerequisiteRecord>(), Array.Empty<string>(),
                    IdleAutoDefenseProgressionEffectKind.ObjectiveMaximumHealth, "objective.idle-auto-defense.core", 8d),
                new IdleAutoDefenseResearchNodeRecord(
                    "research.idle-auto-defense.pulse-capacitor", "Pulse Capacitor", 2,
                    "currency.idle-auto-defense.parts", new long[] { 2, 5 },
                    Array.Empty<IdleAutoDefenseResearchPrerequisiteRecord>(), new[] { "unlock.idle-auto-defense.module.pulse-cannon" },
                    IdleAutoDefenseProgressionEffectKind.DamageRank, "weapon.idle-auto-defense.pulse-beam", 1d),
                new IdleAutoDefenseResearchNodeRecord(
                    "research.idle-auto-defense.shard-loader", "Shard Loader", 2,
                    "currency.idle-auto-defense.parts", new long[] { 2, 5 },
                    Array.Empty<IdleAutoDefenseResearchPrerequisiteRecord>(), new[] { "unlock.idle-auto-defense.module.shard-launcher" },
                    IdleAutoDefenseProgressionEffectKind.ExtraProjectile, "weapon.idle-auto-defense.shard-launcher", 1d),
                new IdleAutoDefenseResearchNodeRecord(
                    "research.idle-auto-defense.offline-routing", "Offline Routing", 2,
                    "currency.idle-auto-defense.credits", new long[] { 40, 120 },
                    new[] { new IdleAutoDefenseResearchPrerequisiteRecord("research.idle-auto-defense.core-plating", 1) }, Array.Empty<string>(),
                    IdleAutoDefenseProgressionEffectKind.OfflineRewardMultiplier, "offline-progression.idle-auto-defense.playable", 0.1d)
            };
        }

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }

        private static string[] CopyStrings(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<string>();
            var copy = new string[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i] ?? string.Empty;
            return copy;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseProgressionTrackRecord
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private int _startingLevel;
        [SerializeField] private long[] _cumulativeThresholds = Array.Empty<long>();

        public IdleAutoDefenseProgressionTrackRecord()
        {
        }

        public IdleAutoDefenseProgressionTrackRecord(string id, string displayName, int startingLevel, IReadOnlyList<long> cumulativeThresholds)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _startingLevel = startingLevel;
            _cumulativeThresholds = Copy(cumulativeThresholds);
        }

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public int StartingLevel => Math.Max(0, _startingLevel);
        public IReadOnlyList<long> CumulativeThresholds => _cumulativeThresholds ?? Array.Empty<long>();

        public ProgressionTrackDefinition ToRuntimeDefinition()
        {
            var thresholds = new ProgressionAmount[CumulativeThresholds.Count];
            for (int i = 0; i < thresholds.Length; i++) thresholds[i] = new ProgressionAmount(Math.Max(0L, CumulativeThresholds[i]));
            return new ProgressionTrackDefinition(new TrackId(Id), StartingLevel, thresholds);
        }

        private static long[] Copy(IReadOnlyList<long> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<long>();
            var copy = new long[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseResearchPrerequisiteRecord
    {
        [SerializeField] private string _nodeId;
        [SerializeField] private int _minimumRank;

        public IdleAutoDefenseResearchPrerequisiteRecord()
        {
        }

        public IdleAutoDefenseResearchPrerequisiteRecord(string nodeId, int minimumRank)
        {
            _nodeId = nodeId ?? string.Empty;
            _minimumRank = minimumRank;
        }

        public string NodeId => _nodeId ?? string.Empty;
        public int MinimumRank => Math.Max(1, _minimumRank);
    }

    [Serializable]
    public sealed class IdleAutoDefenseResearchNodeRecord
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private int _maxRank;
        [SerializeField] private string _costCurrencyId;
        [SerializeField] private long[] _rankCosts = Array.Empty<long>();
        [SerializeField] private IdleAutoDefenseResearchPrerequisiteRecord[] _prerequisites = Array.Empty<IdleAutoDefenseResearchPrerequisiteRecord>();
        [SerializeField] private string[] _requiredUnlockIds = Array.Empty<string>();
        [SerializeField] private IdleAutoDefenseProgressionEffectKind _effectKind;
        [SerializeField] private string _effectTargetId;
        [SerializeField] private double _effectAmountPerRank;

        public IdleAutoDefenseResearchNodeRecord()
        {
        }

        public IdleAutoDefenseResearchNodeRecord(
            string id,
            string displayName,
            int maxRank,
            string costCurrencyId,
            IReadOnlyList<long> rankCosts,
            IReadOnlyList<IdleAutoDefenseResearchPrerequisiteRecord> prerequisites,
            IReadOnlyList<string> requiredUnlockIds,
            IdleAutoDefenseProgressionEffectKind effectKind,
            string effectTargetId,
            double effectAmountPerRank)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _maxRank = maxRank;
            _costCurrencyId = costCurrencyId ?? string.Empty;
            _rankCosts = Copy(rankCosts);
            _prerequisites = Copy(prerequisites);
            _requiredUnlockIds = CopyStrings(requiredUnlockIds);
            _effectKind = effectKind;
            _effectTargetId = effectTargetId ?? string.Empty;
            _effectAmountPerRank = effectAmountPerRank;
        }

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public int MaxRank => Math.Max(1, _maxRank);
        public string CostCurrencyId => _costCurrencyId ?? string.Empty;
        public IReadOnlyList<long> RankCosts => _rankCosts ?? Array.Empty<long>();
        public IReadOnlyList<IdleAutoDefenseResearchPrerequisiteRecord> Prerequisites => _prerequisites ?? Array.Empty<IdleAutoDefenseResearchPrerequisiteRecord>();
        public IReadOnlyList<string> RequiredUnlockIds => _requiredUnlockIds ?? Array.Empty<string>();
        public IdleAutoDefenseProgressionEffectKind EffectKind => _effectKind;
        public string EffectTargetId => _effectTargetId ?? string.Empty;
        public double EffectAmountPerRank => _effectAmountPerRank;

        public ResearchNodeDefinition ToRuntimeDefinition()
        {
            var costs = new CurrencyLine[RankCosts.Count];
            for (int i = 0; i < costs.Length; i++)
                costs[i] = new CurrencyLine(new CurrencyId(CostCurrencyId), new ProgressionAmount(Math.Max(0L, RankCosts[i])), false);
            var prerequisites = new ResearchPrerequisite[Prerequisites.Count];
            for (int i = 0; i < prerequisites.Length; i++)
                prerequisites[i] = new ResearchPrerequisite(new ResearchNodeId(Prerequisites[i].NodeId), Prerequisites[i].MinimumRank);
            var requiredUnlocks = new UnlockId[RequiredUnlockIds.Count];
            for (int i = 0; i < requiredUnlocks.Length; i++) requiredUnlocks[i] = new UnlockId(RequiredUnlockIds[i]);
            return new ResearchNodeDefinition(new ResearchNodeId(Id), MaxRank, costs, prerequisites, requiredUnlocks);
        }

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }

        private static string[] CopyStrings(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<string>();
            var copy = new string[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i] ?? string.Empty;
            return copy;
        }
    }
}
