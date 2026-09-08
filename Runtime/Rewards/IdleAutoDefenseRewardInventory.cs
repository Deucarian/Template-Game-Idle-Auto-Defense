using System;
using System.Collections.Generic;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Narrow, live observations used by reward selection across public run commands.</summary>
    internal sealed class IdleAutoDefenseRewardInventory
    {
        private readonly Func<IdleAutoDefenseRewardDraftSettings> _settings;
        private readonly Func<IdleAutoDefenseRewardDraftCatalog> _catalog;
        private readonly Func<int> _level;
        internal readonly IReadOnlyDictionary<string, int> NormalRanks;
        internal readonly IReadOnlyDictionary<string, int> EpicRanks;
        internal readonly IReadOnlyDictionary<string, int> BaseRanks;
        internal readonly Func<string, bool> HasLegendaryWeapon;
        internal readonly Func<string, bool> HasSelectedReward;
        internal readonly Func<string, bool> IsWeaponUnlocked;
        internal readonly Func<string, string> ResolveWeaponDisplayName;

        internal IdleAutoDefenseRewardInventory(
            Func<IdleAutoDefenseRewardDraftSettings> settings, Func<IdleAutoDefenseRewardDraftCatalog> catalog,
            Func<int> level, IReadOnlyDictionary<string, int> normalRanks,
            IReadOnlyDictionary<string, int> epicRanks, IReadOnlyDictionary<string, int> baseRanks,
            ISet<string> legendaryWeapons, ISet<string> selectedRewards,
            Func<string, bool> isWeaponUnlocked, Func<string, string> resolveWeaponDisplayName)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _level = level ?? throw new ArgumentNullException(nameof(level));
            NormalRanks = normalRanks ?? throw new ArgumentNullException(nameof(normalRanks));
            EpicRanks = epicRanks ?? throw new ArgumentNullException(nameof(epicRanks));
            BaseRanks = baseRanks ?? throw new ArgumentNullException(nameof(baseRanks));
            if (legendaryWeapons == null) throw new ArgumentNullException(nameof(legendaryWeapons));
            if (selectedRewards == null) throw new ArgumentNullException(nameof(selectedRewards));
            HasLegendaryWeapon = legendaryWeapons.Contains;
            HasSelectedReward = selectedRewards.Contains;
            IsWeaponUnlocked = isWeaponUnlocked ?? throw new ArgumentNullException(nameof(isWeaponUnlocked));
            ResolveWeaponDisplayName = resolveWeaponDisplayName ?? throw new ArgumentNullException(nameof(resolveWeaponDisplayName));
        }

        internal IdleAutoDefenseRewardDraftSettings Settings => _settings();
        internal IdleAutoDefenseRewardDraftCatalog Catalog => _catalog();
        internal int CommanderLevel => _level();

        internal static int GetRank(IReadOnlyDictionary<string, int> ranks, string key)
        {
            return string.IsNullOrWhiteSpace(key) ? 0 : ranks.TryGetValue(key, out int rank) ? rank : 0;
        }
    }
}
