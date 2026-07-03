using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
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
                _weaponUnlocks = CopyRewards(_weaponUnlocks),
                _normalWeaponRewards = CopyRewards(_normalWeaponRewards),
                _epicWeaponRewards = CopyRewards(_epicWeaponRewards),
                _legendaryWeaponRewards = CopyRewards(_legendaryWeaponRewards),
                _baseRewards = CopyRewards(_baseRewards)
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

        private static TReward[] CopyRewards<TReward>(IReadOnlyList<TReward> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<TReward>();
            var copy = new TReward[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseWeaponUnlockReward
    {
        [SerializeField] private string _weaponId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _effectDescription;
        [SerializeField] private IdleAutoDefenseRewardRarity _levelUpRarity;
        [SerializeField] private IdleAutoDefenseRewardRarity _eliteRarity;
        [SerializeField] private IdleAutoDefenseRewardRarity _bossRarity;

        public IdleAutoDefenseWeaponUnlockReward()
        {
        }

        public IdleAutoDefenseWeaponUnlockReward(string weaponId, string displayName, string effectDescription, IdleAutoDefenseRewardRarity levelUpRarity, IdleAutoDefenseRewardRarity eliteRarity, IdleAutoDefenseRewardRarity bossRarity)
        {
            _weaponId = weaponId ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _effectDescription = effectDescription ?? string.Empty;
            _levelUpRarity = levelUpRarity;
            _eliteRarity = eliteRarity;
            _bossRarity = bossRarity;
        }

        public string WeaponId => _weaponId ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public string EffectDescription => _effectDescription ?? string.Empty;

        public IdleAutoDefenseRewardRarity GetRarity(IdleAutoDefenseRewardDraftKind kind)
        {
            if (kind == IdleAutoDefenseRewardDraftKind.BossDefeated) return _bossRarity;
            if (kind == IdleAutoDefenseRewardDraftKind.EliteDefeated) return _eliteRarity;
            return _levelUpRarity;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseWeaponRewardDefinition
    {
        [SerializeField] private string _weaponId;
        [SerializeField] private string _tierKey;
        [SerializeField] private string _displayName;
        [SerializeField] private IdleAutoDefenseRewardRarity _rarity;
        [SerializeField] private string _typeName;
        [SerializeField] private string _effectDescription;
        [SerializeField] private IdleAutoDefenseRewardEffectKind _effectKind;
        [SerializeField] private double _amount;

        public IdleAutoDefenseWeaponRewardDefinition()
        {
        }

        public IdleAutoDefenseWeaponRewardDefinition(string weaponId, string tierKey, string displayName, IdleAutoDefenseRewardRarity rarity, string typeName, string effectDescription, IdleAutoDefenseRewardEffectKind effectKind, double amount)
        {
            _weaponId = weaponId ?? string.Empty;
            _tierKey = tierKey ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _rarity = rarity;
            _typeName = typeName ?? string.Empty;
            _effectDescription = effectDescription ?? string.Empty;
            _effectKind = effectKind;
            _amount = amount;
        }

        public string WeaponId => _weaponId ?? string.Empty;
        public string TierKey => _tierKey ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public IdleAutoDefenseRewardRarity Rarity => _rarity;
        public string TypeName => _typeName ?? string.Empty;
        public string EffectDescription => _effectDescription ?? string.Empty;
        public IdleAutoDefenseRewardEffectKind EffectKind => _effectKind;
        public double Amount => _amount;
    }

    [Serializable]
    public sealed class IdleAutoDefenseBaseRewardDefinition
    {
        [SerializeField] private string _key;
        [SerializeField] private string _displayName;
        [SerializeField] private IdleAutoDefenseRewardRarity _rarity;
        [SerializeField] private string _typeName;
        [SerializeField] private string _targetName;
        [SerializeField] private string _effectDescription;
        [SerializeField] private IdleAutoDefenseRewardEffectKind _effectKind;
        [SerializeField] private double _amount;
        [SerializeField] private int _maxRank;

        public IdleAutoDefenseBaseRewardDefinition()
        {
        }

        public IdleAutoDefenseBaseRewardDefinition(string key, string displayName, IdleAutoDefenseRewardRarity rarity, string typeName, string targetName, string effectDescription, IdleAutoDefenseRewardEffectKind effectKind, double amount, int maxRank)
        {
            _key = key ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _rarity = rarity;
            _typeName = typeName ?? string.Empty;
            _targetName = targetName ?? string.Empty;
            _effectDescription = effectDescription ?? string.Empty;
            _effectKind = effectKind;
            _amount = amount;
            _maxRank = maxRank;
        }

        public string Key => _key ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public IdleAutoDefenseRewardRarity Rarity => _rarity;
        public string TypeName => _typeName ?? string.Empty;
        public string TargetName => _targetName ?? string.Empty;
        public string EffectDescription => _effectDescription ?? string.Empty;
        public IdleAutoDefenseRewardEffectKind EffectKind => _effectKind;
        public double Amount => _amount;
        public int MaxRank => Math.Max(1, _maxRank);
    }
}
