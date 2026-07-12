using System;
using System.Collections.Generic;
using Deucarian.WeaponSystems.Authoring;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    [Flags]
    public enum IdleAutoDefenseRewardSourceEligibility
    {
        None = 0,
        LevelUp = 1 << 0,
        WaveComplete = 1 << 1,
        EliteDefeated = 1 << 2,
        BossDefeated = 1 << 3,
        All = LevelUp | WaveComplete | EliteDefeated | BossDefeated
    }

    public enum IdleAutoDefenseRewardTrack
    {
        Unlock = 0,
        Normal = 1,
        Epic = 2,
        Legendary = 3,
        Base = 4
    }

    [Serializable]
    public sealed class IdleAutoDefenseRewardDraftCatalog
    {
        [SerializeField] private IdleAutoDefenseWeaponUnlockReward[] _weaponUnlocks = Array.Empty<IdleAutoDefenseWeaponUnlockReward>();
        [SerializeField] private IdleAutoDefenseWeaponRewardDefinition[] _normalWeaponRewards = Array.Empty<IdleAutoDefenseWeaponRewardDefinition>();
        [SerializeField] private IdleAutoDefenseWeaponRewardDefinition[] _epicWeaponRewards = Array.Empty<IdleAutoDefenseWeaponRewardDefinition>();
        [SerializeField] private IdleAutoDefenseWeaponRewardDefinition[] _legendaryWeaponRewards = Array.Empty<IdleAutoDefenseWeaponRewardDefinition>();
        [SerializeField] private IdleAutoDefenseBaseRewardDefinition[] _baseRewards = Array.Empty<IdleAutoDefenseBaseRewardDefinition>();

        public static IdleAutoDefenseRewardDraftCatalog CreateDefault()
        {
            return new IdleAutoDefenseRewardDraftCatalog
            {
                _weaponUnlocks = new[]
                {
                    new IdleAutoDefenseWeaponUnlockReward(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, "Pulse Beam Module", "Unlocks a precise beam module that burns down the leading enemy.", IdleAutoDefenseRewardRarity.Uncommon, IdleAutoDefenseRewardRarity.Rare, IdleAutoDefenseRewardRarity.Epic),
                    new IdleAutoDefenseWeaponUnlockReward(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, "Arc Cannon Module", "Unlocks a splash module that detonates around clustered enemies.", IdleAutoDefenseRewardRarity.Uncommon, IdleAutoDefenseRewardRarity.Rare, IdleAutoDefenseRewardRarity.Epic),
                    new IdleAutoDefenseWeaponUnlockReward(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, "Homing Spire Module", "Unlocks a seeker module for cleanup shots and pressure relief.", IdleAutoDefenseRewardRarity.Uncommon, IdleAutoDefenseRewardRarity.Rare, IdleAutoDefenseRewardRarity.Epic)
                },
                _normalWeaponRewards = CreateNormalWeaponRewards(),
                _epicWeaponRewards = new[]
                {
                    WeaponReward(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, "epic.0", "Fracture Burst", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "Shard Launcher fires two extra visible shards per volley.", IdleAutoDefenseRewardEffectKind.ExtraProjectile, 2d),
                    WeaponReward(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, "epic.1", "Ricochet Shards", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "Shard volleys travel faster and retarget more cleanly after impact.", IdleAutoDefenseRewardEffectKind.ProjectileSpeed, 0.25d),
                    WeaponReward(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, "epic.2", "Shard Storm", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "Visible shard hits punch much harder across all weapons.", IdleAutoDefenseRewardEffectKind.GlobalDamageMultiplier, 0.30d),
                    WeaponReward(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, "epic.0", "Refracting Beam", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "Pulse Beam hits one extra target with authored beam VFX.", IdleAutoDefenseRewardEffectKind.PulsePower, 1d),
                    WeaponReward(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, "epic.1", "Overcharged Pulse", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "Pulse Beam cycles faster for more visible beam shots.", IdleAutoDefenseRewardEffectKind.FireRateRank, 1d),
                    WeaponReward(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, "epic.2", "Ion Burn", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "Beam impacts deal a large visible damage spike.", IdleAutoDefenseRewardEffectKind.GlobalDamageMultiplier, 0.30d),
                    WeaponReward(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, "epic.0", "Cluster Shells", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "Arc Cannon blasts one extra enemy in the detonation.", IdleAutoDefenseRewardEffectKind.ArcPower, 1d),
                    WeaponReward(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, "epic.1", "Burning Ground", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "Arc Cannon holds pressure farther from the core.", IdleAutoDefenseRewardEffectKind.RangeRank, 1d),
                    WeaponReward(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, "epic.2", "Shockwave Impact", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "Area impacts hit harder and produce larger damage-number spikes.", IdleAutoDefenseRewardEffectKind.GlobalDamageMultiplier, 0.30d),
                    WeaponReward(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, "epic.0", "Extra Seeker", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "Homing Pulse fires one extra authored seeker projectile.", IdleAutoDefenseRewardEffectKind.HomingPower, 1d),
                    WeaponReward(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, "epic.1", "Target Painter", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "Marked targets take a large visible damage spike.", IdleAutoDefenseRewardEffectKind.GlobalDamageMultiplier, 0.30d),
                    WeaponReward(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, "epic.2", "Overclocked Guidance", IdleAutoDefenseRewardRarity.Epic, "Epic Weapon", "The seeker module fires more often during pressure waves.", IdleAutoDefenseRewardEffectKind.FireRateRank, 1d)
                },
                _legendaryWeaponRewards = new[]
                {
                    WeaponReward(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, "legendary", "Crystal Tempest", IdleAutoDefenseRewardRarity.Legendary, "Legendary Weapon", "Shard Launcher becomes a storm of three extra visible shards per volley.", IdleAutoDefenseRewardEffectKind.ExtraProjectile, 3d),
                    WeaponReward(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, "legendary", "Orbital Lance", IdleAutoDefenseRewardRarity.Legendary, "Legendary Weapon", "Pulse Beam becomes a build-defining beam that hits two extra targets.", IdleAutoDefenseRewardEffectKind.PulsePower, 2d),
                    WeaponReward(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, "legendary", "Siege Barrage", IdleAutoDefenseRewardRarity.Legendary, "Legendary Weapon", "Arc Cannon detonations hit two extra enemies during pressure spikes.", IdleAutoDefenseRewardEffectKind.ArcPower, 2d),
                    WeaponReward(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, "legendary", "Carrier Hive", IdleAutoDefenseRewardRarity.Legendary, "Legendary Weapon", "Homing Spire launches two extra authored seeker projectiles.", IdleAutoDefenseRewardEffectKind.HomingPower, 2d)
                },
                _baseRewards = new[]
                {
                    new IdleAutoDefenseBaseRewardDefinition("base.damage", "Targeting Drill", IdleAutoDefenseRewardRarity.Common, "Base Upgrade", "Tower", "+2 damage ranks for immediate relief.", IdleAutoDefenseRewardEffectKind.DamageRank, 2d, 8),
                    new IdleAutoDefenseBaseRewardDefinition("base.fire-rate", "Tactical Overclock", IdleAutoDefenseRewardRarity.Common, "Base Upgrade", "Tower", "+2 fire-rate ranks so weapons visibly fire faster.", IdleAutoDefenseRewardEffectKind.FireRateRank, 2d, 6),
                    new IdleAutoDefenseBaseRewardDefinition("base.range", "Sensor Array", IdleAutoDefenseRewardRarity.Uncommon, "Base Upgrade", "Tower", "+1 range rank to catch enemies before they leak.", IdleAutoDefenseRewardEffectKind.RangeRank, 1d, 6),
                    new IdleAutoDefenseBaseRewardDefinition("base.repair", "Emergency Repairs", IdleAutoDefenseRewardRarity.Common, "Base Upgrade", "Tower", "Repair and increase maximum HP.", IdleAutoDefenseRewardEffectKind.Repair, 1d, 5),
                    new IdleAutoDefenseBaseRewardDefinition("base.velocity", "Velocity Matrix", IdleAutoDefenseRewardRarity.Uncommon, "Base Upgrade", "Projectiles", "+25% projectile travel speed for snappier impacts.", IdleAutoDefenseRewardEffectKind.ProjectileSpeed, 0.25d, 4),
                    new IdleAutoDefenseBaseRewardDefinition("base.credits", "Scrap Collector", IdleAutoDefenseRewardRarity.Rare, "Economy", "Tower", "+25% credits from kills for more live purchases.", IdleAutoDefenseRewardEffectKind.RewardMultiplier, 0.25d, 5)
                }
            };
        }

        public IdleAutoDefenseRewardDraftCatalog Clone()
        {
            return new IdleAutoDefenseRewardDraftCatalog
            {
                _weaponUnlocks = CloneRewards(_weaponUnlocks, reward => reward == null ? null : reward.Clone()),
                _normalWeaponRewards = CloneRewards(_normalWeaponRewards, reward => reward == null ? null : reward.Clone()),
                _epicWeaponRewards = CloneRewards(_epicWeaponRewards, reward => reward == null ? null : reward.Clone()),
                _legendaryWeaponRewards = CloneRewards(_legendaryWeaponRewards, reward => reward == null ? null : reward.Clone()),
                _baseRewards = CloneRewards(_baseRewards, reward => reward == null ? null : reward.Clone())
            };
        }

        public IReadOnlyList<IdleAutoDefenseWeaponUnlockReward> WeaponUnlocks => _weaponUnlocks ?? Array.Empty<IdleAutoDefenseWeaponUnlockReward>();
        public IReadOnlyList<IdleAutoDefenseBaseRewardDefinition> BaseRewards => _baseRewards ?? Array.Empty<IdleAutoDefenseBaseRewardDefinition>();
        public IReadOnlyList<IdleAutoDefenseWeaponRewardDefinition> NormalWeaponRewards => _normalWeaponRewards ?? Array.Empty<IdleAutoDefenseWeaponRewardDefinition>();
        public IReadOnlyList<IdleAutoDefenseWeaponRewardDefinition> EpicWeaponRewards => _epicWeaponRewards ?? Array.Empty<IdleAutoDefenseWeaponRewardDefinition>();
        public IReadOnlyList<IdleAutoDefenseWeaponRewardDefinition> LegendaryWeaponRewards => _legendaryWeaponRewards ?? Array.Empty<IdleAutoDefenseWeaponRewardDefinition>();

        public IReadOnlyList<string> GetWeaponIds()
        {
            var weaponIds = new List<string>();
            AddWeaponIds(weaponIds, NormalWeaponRewards);
            AddWeaponIds(weaponIds, EpicWeaponRewards);
            AddWeaponIds(weaponIds, LegendaryWeaponRewards);
            return weaponIds;
        }

        public IdleAutoDefenseWeaponRewardDefinition GetNormalWeaponReward(string weaponId, int rank)
        {
            return GetWeaponReward(NormalWeaponRewards, weaponId, rank);
        }

        public IdleAutoDefenseWeaponRewardDefinition GetEpicWeaponReward(string weaponId, int rank)
        {
            return GetWeaponReward(EpicWeaponRewards, weaponId, rank);
        }

        public IdleAutoDefenseWeaponRewardDefinition GetLegendaryWeaponReward(string weaponId)
        {
            IReadOnlyList<IdleAutoDefenseWeaponRewardDefinition> rewards = LegendaryWeaponRewards;
            for (int i = 0; i < rewards.Count; i++)
                if (string.Equals(rewards[i].WeaponId, weaponId, StringComparison.OrdinalIgnoreCase))
                    return rewards[i];
            return null;
        }

        public int CountWeaponRewards(string weaponId, IdleAutoDefenseRewardRarity rarity)
        {
            return CountWeaponRewards(NormalWeaponRewards, weaponId, rarity) +
                CountWeaponRewards(EpicWeaponRewards, weaponId, rarity) +
                CountWeaponRewards(LegendaryWeaponRewards, weaponId, rarity);
        }

        private static IdleAutoDefenseWeaponRewardDefinition[] CreateNormalWeaponRewards()
        {
            return new[]
            {
                WeaponReward(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, "normal.0", "Sharper Shards", IdleAutoDefenseRewardRarity.Common, "Weapon Upgrade", "+2 damage ranks for shard impacts.", IdleAutoDefenseRewardEffectKind.DamageRank, 2d),
                WeaponReward(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, "normal.1", "Quick Chisel", IdleAutoDefenseRewardRarity.Uncommon, "Weapon Upgrade", "+2 fire-rate ranks for faster shard volleys.", IdleAutoDefenseRewardEffectKind.FireRateRank, 2d),
                WeaponReward(BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, "normal.2", "Split Tip", IdleAutoDefenseRewardRarity.Rare, "Weapon Upgrade", "Adds one extra shard to each visible volley.", IdleAutoDefenseRewardEffectKind.ExtraProjectile, 1d),
                WeaponReward(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, "normal.0", "Focused Lens", IdleAutoDefenseRewardRarity.Common, "Weapon Upgrade", "+2 damage ranks for beam impacts.", IdleAutoDefenseRewardEffectKind.DamageRank, 2d),
                WeaponReward(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, "normal.1", "Faster Capacitors", IdleAutoDefenseRewardRarity.Uncommon, "Weapon Upgrade", "+2 fire-rate ranks for more beam pulses.", IdleAutoDefenseRewardEffectKind.FireRateRank, 2d),
                WeaponReward(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value, "normal.2", "Piercing Beam", IdleAutoDefenseRewardRarity.Rare, "Weapon Upgrade", "Pulse Beam hits one extra enemy with authored beam VFX.", IdleAutoDefenseRewardEffectKind.PulsePower, 1d),
                WeaponReward(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, "normal.0", "Packed Powder", IdleAutoDefenseRewardRarity.Common, "Weapon Upgrade", "+2 damage ranks for area detonations.", IdleAutoDefenseRewardEffectKind.DamageRank, 2d),
                WeaponReward(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, "normal.1", "Fast Loader", IdleAutoDefenseRewardRarity.Uncommon, "Weapon Upgrade", "+2 fire-rate ranks for more splash bursts.", IdleAutoDefenseRewardEffectKind.FireRateRank, 2d),
                WeaponReward(BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value, "normal.2", "Forked Burst", IdleAutoDefenseRewardRarity.Rare, "Weapon Upgrade", "Arc Burst detonates into one extra nearby enemy.", IdleAutoDefenseRewardEffectKind.ArcPower, 1d),
                WeaponReward(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, "normal.0", "Sharper Signal", IdleAutoDefenseRewardRarity.Common, "Weapon Upgrade", "+2 damage ranks for seeker impacts.", IdleAutoDefenseRewardEffectKind.DamageRank, 2d),
                WeaponReward(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, "normal.1", "Efficient Engines", IdleAutoDefenseRewardRarity.Uncommon, "Weapon Upgrade", "+2 fire-rate ranks for more seeker launches.", IdleAutoDefenseRewardEffectKind.FireRateRank, 2d),
                WeaponReward(BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value, "normal.2", "Second Lock", IdleAutoDefenseRewardRarity.Rare, "Weapon Upgrade", "Homing Pulse launches one extra authored seeker.", IdleAutoDefenseRewardEffectKind.HomingPower, 1d)
            };
        }

        private static IdleAutoDefenseWeaponRewardDefinition WeaponReward(
            string weaponId,
            string tierKey,
            string displayName,
            IdleAutoDefenseRewardRarity rarity,
            string typeName,
            string description,
            IdleAutoDefenseRewardEffectKind effectKind,
            double amount)
        {
            return new IdleAutoDefenseWeaponRewardDefinition(weaponId, tierKey, displayName, rarity, typeName, description, effectKind, amount);
        }

        private static IdleAutoDefenseWeaponRewardDefinition GetWeaponReward(IReadOnlyList<IdleAutoDefenseWeaponRewardDefinition> rewards, string weaponId, int rank)
        {
            int matchingIndex = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                if (!string.Equals(rewards[i].WeaponId, weaponId, StringComparison.OrdinalIgnoreCase)) continue;
                if (matchingIndex == rank) return rewards[i];
                matchingIndex++;
            }

            return null;
        }

        private static void AddWeaponIds(List<string> weaponIds, IReadOnlyList<IdleAutoDefenseWeaponRewardDefinition> rewards)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                string weaponId = rewards[i].WeaponId;
                if (string.IsNullOrWhiteSpace(weaponId) || ContainsWeaponId(weaponIds, weaponId)) continue;
                weaponIds.Add(weaponId);
            }
        }

        private static int CountWeaponRewards(IReadOnlyList<IdleAutoDefenseWeaponRewardDefinition> rewards, string weaponId, IdleAutoDefenseRewardRarity rarity)
        {
            int count = 0;
            for (int i = 0; i < rewards.Count; i++)
                if (string.Equals(rewards[i].WeaponId, weaponId, StringComparison.OrdinalIgnoreCase) && rewards[i].Rarity == rarity)
                    count++;
            return count;
        }

        private static bool ContainsWeaponId(List<string> weaponIds, string weaponId)
        {
            for (int i = 0; i < weaponIds.Count; i++)
                if (string.Equals(weaponIds[i], weaponId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static TReward[] CloneRewards<TReward>(IReadOnlyList<TReward> source, Func<TReward, TReward> clone)
        {
            if (source == null || source.Count == 0) return Array.Empty<TReward>();
            var copy = new TReward[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = clone(source[i]);
            return copy;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseWeaponUnlockReward
    {
        [SerializeField] private string _id;
        [SerializeField] private WeaponDefinitionAsset _weapon;
        [SerializeField] private string _weaponId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _effectDescription;
        [SerializeField] private IdleAutoDefenseRewardRarity _levelUpRarity;
        [SerializeField] private IdleAutoDefenseRewardRarity _eliteRarity;
        [SerializeField] private IdleAutoDefenseRewardRarity _bossRarity;
        [SerializeField] private double _weight = 1d;
        [SerializeField] private int _maxRank = 1;
        [SerializeField] private string[] _prerequisiteIds = Array.Empty<string>();
        [SerializeField] private IdleAutoDefenseRewardSourceEligibility _eligibleSources = IdleAutoDefenseRewardSourceEligibility.All;

        public IdleAutoDefenseWeaponUnlockReward()
        {
        }

        public IdleAutoDefenseWeaponUnlockReward(string weaponId, string displayName, string effectDescription, IdleAutoDefenseRewardRarity levelUpRarity, IdleAutoDefenseRewardRarity eliteRarity, IdleAutoDefenseRewardRarity bossRarity)
        {
            _id = "reward.unlock." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(weaponId);
            _weaponId = weaponId ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _effectDescription = effectDescription ?? string.Empty;
            _levelUpRarity = levelUpRarity;
            _eliteRarity = eliteRarity;
            _bossRarity = bossRarity;
            _weight = 1d;
            _maxRank = 1;
            _eligibleSources = IdleAutoDefenseRewardSourceEligibility.All;
        }

        public string Id => _id ?? string.Empty;
        public WeaponDefinitionAsset Weapon => _weapon;
        public string WeaponId => _weapon == null ? _weaponId ?? string.Empty : _weapon.Id;
        public string DisplayName => _displayName ?? string.Empty;
        public string EffectDescription => _effectDescription ?? string.Empty;
        public double Weight => IsFinitePositive(_weight) ? _weight : 0d;
        public int MaxRank => Math.Max(1, _maxRank);
        public IReadOnlyList<string> PrerequisiteIds => _prerequisiteIds ?? Array.Empty<string>();
        public IdleAutoDefenseRewardSourceEligibility EligibleSources => _eligibleSources;
        public IdleAutoDefenseRewardTrack Track => IdleAutoDefenseRewardTrack.Unlock;

        public IdleAutoDefenseRewardRarity GetRarity(IdleAutoDefenseRewardDraftKind kind)
        {
            if (kind == IdleAutoDefenseRewardDraftKind.BossDefeated) return _bossRarity;
            if (kind == IdleAutoDefenseRewardDraftKind.EliteDefeated) return _eliteRarity;
            return _levelUpRarity;
        }

        public bool IsEligible(IdleAutoDefenseRewardDraftKind kind)
        {
            return (EligibleSources & IdleAutoDefenseRewardEligibility.For(kind)) != 0;
        }

        public IdleAutoDefenseWeaponUnlockReward Clone()
        {
            return new IdleAutoDefenseWeaponUnlockReward
            {
                _id = Id,
                _weapon = _weapon,
                _weaponId = _weaponId,
                _displayName = DisplayName,
                _effectDescription = EffectDescription,
                _levelUpRarity = _levelUpRarity,
                _eliteRarity = _eliteRarity,
                _bossRarity = _bossRarity,
                _weight = _weight,
                _maxRank = _maxRank,
                _prerequisiteIds = CopyStrings(PrerequisiteIds),
                _eligibleSources = _eligibleSources
            };
        }

        private static bool IsFinitePositive(double value)
        {
            return value > 0d && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static string[] CopyStrings(IReadOnlyList<string> source)
        {
            var copy = new string[source.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = source[i] ?? string.Empty;
            return copy;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseWeaponRewardDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private WeaponDefinitionAsset _weapon;
        [SerializeField] private string _weaponId;
        [SerializeField] private string _tierKey;
        [SerializeField] private IdleAutoDefenseRewardTrack _track;
        [SerializeField] private string _displayName;
        [SerializeField] private IdleAutoDefenseRewardRarity _rarity;
        [SerializeField] private double _weight = 1d;
        [SerializeField] private string _typeName;
        [SerializeField] private string _effectDescription;
        [SerializeField] private IdleAutoDefenseRewardEffectKind _effectKind;
        [SerializeField] private double _amount;
        [SerializeField] private int _maxRank = 1;
        [SerializeField] private int _requiredNormalRank;
        [SerializeField] private int _requiredEpicRank;
        [SerializeField] private string[] _prerequisiteIds = Array.Empty<string>();
        [SerializeField] private IdleAutoDefenseRewardSourceEligibility _eligibleSources = IdleAutoDefenseRewardSourceEligibility.All;

        public IdleAutoDefenseWeaponRewardDefinition()
        {
        }

        public IdleAutoDefenseWeaponRewardDefinition(string weaponId, string tierKey, string displayName, IdleAutoDefenseRewardRarity rarity, string typeName, string effectDescription, IdleAutoDefenseRewardEffectKind effectKind, double amount)
        {
            _id = "reward." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(weaponId) + "." + BasicIdleAutoDefenseGame.SanitizeContentSetOperationSegment(tierKey);
            _weaponId = weaponId ?? string.Empty;
            _tierKey = tierKey ?? string.Empty;
            _track = rarity == IdleAutoDefenseRewardRarity.Legendary
                ? IdleAutoDefenseRewardTrack.Legendary
                : rarity == IdleAutoDefenseRewardRarity.Epic
                    ? IdleAutoDefenseRewardTrack.Epic
                    : IdleAutoDefenseRewardTrack.Normal;
            _displayName = displayName ?? string.Empty;
            _rarity = rarity;
            _weight = 1d;
            _typeName = typeName ?? string.Empty;
            _effectDescription = effectDescription ?? string.Empty;
            _effectKind = effectKind;
            _amount = amount;
            _maxRank = 1;
            if (_track == IdleAutoDefenseRewardTrack.Epic) _requiredNormalRank = 3;
            if (_track == IdleAutoDefenseRewardTrack.Legendary)
            {
                _requiredNormalRank = 3;
                _requiredEpicRank = 3;
            }
            _eligibleSources = IdleAutoDefenseRewardSourceEligibility.All;
        }

        public string Id => _id ?? string.Empty;
        public WeaponDefinitionAsset Weapon => _weapon;
        public string WeaponId => _weapon == null ? _weaponId ?? string.Empty : _weapon.Id;
        public string TierKey => _tierKey ?? string.Empty;
        public IdleAutoDefenseRewardTrack Track => _track;
        public string DisplayName => _displayName ?? string.Empty;
        public IdleAutoDefenseRewardRarity Rarity => _rarity;
        public double Weight
        {
            get => IsFinitePositive(_weight) ? _weight : 0d;
            set => _weight = value;
        }
        public string TypeName => _typeName ?? string.Empty;
        public string EffectDescription => _effectDescription ?? string.Empty;
        public IdleAutoDefenseRewardEffectKind EffectKind => _effectKind;
        public double Amount
        {
            get => _amount;
            set => _amount = value;
        }
        public int MaxRank => Math.Max(1, _maxRank);
        public int RequiredNormalRank => Math.Max(0, _requiredNormalRank);
        public int RequiredEpicRank => Math.Max(0, _requiredEpicRank);
        public IReadOnlyList<string> PrerequisiteIds => _prerequisiteIds ?? Array.Empty<string>();
        public IdleAutoDefenseRewardSourceEligibility EligibleSources => _eligibleSources;

        public bool IsEligible(IdleAutoDefenseRewardDraftKind kind)
        {
            return (EligibleSources & IdleAutoDefenseRewardEligibility.For(kind)) != 0;
        }

        public bool IsAvailableAt(int normalRank, int epicRank)
        {
            return normalRank >= RequiredNormalRank && epicRank >= RequiredEpicRank;
        }

        public IdleAutoDefenseWeaponRewardDefinition Clone()
        {
            return new IdleAutoDefenseWeaponRewardDefinition
            {
                _id = Id,
                _weapon = _weapon,
                _weaponId = _weaponId,
                _tierKey = TierKey,
                _track = _track,
                _displayName = DisplayName,
                _rarity = _rarity,
                _weight = _weight,
                _typeName = TypeName,
                _effectDescription = EffectDescription,
                _effectKind = _effectKind,
                _amount = _amount,
                _maxRank = _maxRank,
                _requiredNormalRank = _requiredNormalRank,
                _requiredEpicRank = _requiredEpicRank,
                _prerequisiteIds = CopyStrings(PrerequisiteIds),
                _eligibleSources = _eligibleSources
            };
        }

        private static bool IsFinitePositive(double value)
        {
            return value > 0d && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static string[] CopyStrings(IReadOnlyList<string> source)
        {
            var copy = new string[source.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = source[i] ?? string.Empty;
            return copy;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseBaseRewardDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private IdleAutoDefenseRewardRarity _rarity;
        [SerializeField] private double _weight = 1d;
        [SerializeField] private string _typeName;
        [SerializeField] private string _targetId;
        [SerializeField] private string _targetName;
        [SerializeField] private string _effectDescription;
        [SerializeField] private IdleAutoDefenseRewardEffectKind _effectKind;
        [SerializeField] private double _amount;
        [SerializeField] private int _maxRank;
        [SerializeField] private string[] _prerequisiteIds = Array.Empty<string>();
        [SerializeField] private IdleAutoDefenseRewardSourceEligibility _eligibleSources = IdleAutoDefenseRewardSourceEligibility.All;

        public IdleAutoDefenseBaseRewardDefinition()
        {
        }

        public IdleAutoDefenseBaseRewardDefinition(string key, string displayName, IdleAutoDefenseRewardRarity rarity, string typeName, string targetName, string effectDescription, IdleAutoDefenseRewardEffectKind effectKind, double amount, int maxRank)
        {
            _id = key ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _rarity = rarity;
            _weight = 1d;
            _typeName = typeName ?? string.Empty;
            _targetId = "objective.idle-auto-defense.core";
            _targetName = targetName ?? string.Empty;
            _effectDescription = effectDescription ?? string.Empty;
            _effectKind = effectKind;
            _amount = amount;
            _maxRank = maxRank;
            _eligibleSources = IdleAutoDefenseRewardSourceEligibility.All;
        }

        public string Id => _id ?? string.Empty;
        public string Key => Id;
        public string DisplayName => _displayName ?? string.Empty;
        public IdleAutoDefenseRewardRarity Rarity => _rarity;
        public double Weight
        {
            get => IsFinitePositive(_weight) ? _weight : 0d;
            set => _weight = value;
        }
        public string TypeName => _typeName ?? string.Empty;
        public string TargetId => _targetId ?? string.Empty;
        public string TargetName => _targetName ?? string.Empty;
        public string EffectDescription => _effectDescription ?? string.Empty;
        public IdleAutoDefenseRewardEffectKind EffectKind => _effectKind;
        public double Amount
        {
            get => _amount;
            set => _amount = value;
        }
        public int MaxRank => Math.Max(1, _maxRank);
        public IReadOnlyList<string> PrerequisiteIds => _prerequisiteIds ?? Array.Empty<string>();
        public IdleAutoDefenseRewardSourceEligibility EligibleSources => _eligibleSources;
        public IdleAutoDefenseRewardTrack Track => IdleAutoDefenseRewardTrack.Base;

        public bool IsEligible(IdleAutoDefenseRewardDraftKind kind)
        {
            return (EligibleSources & IdleAutoDefenseRewardEligibility.For(kind)) != 0;
        }

        public IdleAutoDefenseBaseRewardDefinition Clone()
        {
            return new IdleAutoDefenseBaseRewardDefinition
            {
                _id = Id,
                _displayName = DisplayName,
                _rarity = _rarity,
                _weight = _weight,
                _typeName = TypeName,
                _targetId = TargetId,
                _targetName = TargetName,
                _effectDescription = EffectDescription,
                _effectKind = _effectKind,
                _amount = _amount,
                _maxRank = _maxRank,
                _prerequisiteIds = CopyStrings(PrerequisiteIds),
                _eligibleSources = _eligibleSources
            };
        }

        private static bool IsFinitePositive(double value)
        {
            return value > 0d && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static string[] CopyStrings(IReadOnlyList<string> source)
        {
            var copy = new string[source.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = source[i] ?? string.Empty;
            return copy;
        }
    }

    internal static class IdleAutoDefenseRewardEligibility
    {
        public static IdleAutoDefenseRewardSourceEligibility For(IdleAutoDefenseRewardDraftKind kind)
        {
            if (kind == IdleAutoDefenseRewardDraftKind.BossDefeated) return IdleAutoDefenseRewardSourceEligibility.BossDefeated;
            if (kind == IdleAutoDefenseRewardDraftKind.EliteDefeated) return IdleAutoDefenseRewardSourceEligibility.EliteDefeated;
            if (kind == IdleAutoDefenseRewardDraftKind.WaveComplete) return IdleAutoDefenseRewardSourceEligibility.WaveComplete;
            return IdleAutoDefenseRewardSourceEligibility.LevelUp;
        }
    }
}
