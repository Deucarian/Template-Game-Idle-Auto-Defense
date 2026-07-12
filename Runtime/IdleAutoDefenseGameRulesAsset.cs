using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.DefenseGames;
using Deucarian.WeaponSystems.Authoring;
using Deucarian.WorldSpawning;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public enum IdleAutoDefenseModuleRole
    {
        StartingProjectile = 0,
        PrecisionBeam = 1,
        AreaBurst = 2,
        HomingProjectile = 3
    }

    [CreateAssetMenu(menuName = "Deucarian/Idle Auto Defense/Game Rules", fileName = "IdleAutoDefenseGameRules")]
    public sealed class IdleAutoDefenseGameRulesAsset : ScriptableObject
    {
        [SerializeField] private string _id = "game-rules.idle-auto-defense.playable";
        [SerializeField] private string _displayName = "Basic Idle Auto Defense Game Rules";
        [SerializeField] private string _objectiveId = "objective.idle-auto-defense.core";
        [SerializeField] private string _runRewardTargetId = "reward.idle-auto-defense.run";
        [SerializeField] private string _offlineRewardTargetId = "offline.idle-auto-defense.credits";
        [SerializeField] private string _damageTypeId = "damage.idle-auto-defense.basic";
        [SerializeField] private double _objectiveMaximumHealth = 240d;
        [SerializeField] private float _objectiveContactRadius = 0.45f;
        [SerializeField] private int _objectiveLives = 60;
        [SerializeField] private int _objectiveLifeRestoreAmount = 2;
        [SerializeField] private float _spawnRingRadius = 18.5f;
        [SerializeField] private IdleAutoDefenseSpawnChannelRule[] _spawnChannels = CreateDefaultSpawnChannels();
        [SerializeField] private EnemyDefinitionAsset _eliteEnemy;
        [SerializeField] private EnemyDefinitionAsset _bossEnemy;
        [SerializeField] private IdleAutoDefenseModuleRule[] _modules = Array.Empty<IdleAutoDefenseModuleRule>();
        [SerializeField] private double _manualDamageRankBonus = 1.6d;
        [SerializeField] private double _manualRangeRankDamageBonus = 0.4d;
        [SerializeField] private double _manualRangeRankBonus = 0.4d;
        [SerializeField] private double _moduleRangeRankBonus = 0.35d;
        [SerializeField] private double _manualMaximumRange = 10.6d;
        [SerializeField] private int _minimumProjectileImpactDelayTicks = 12;
        [SerializeField] private int _maximumProjectileImpactDelayTicks = 52;
        [SerializeField] private double _projectileFinishDamageThreshold = 3d;
        [SerializeField] private double _purchaseRangeDamageBonus = 0.5d;
        [SerializeField] private double _purchaseRepairMaximumHealth = 8d;
        [SerializeField] private double _purchaseRepairBaseHeal = 34d;
        [SerializeField] private double _purchaseRepairHealPerRank = 6d;
        [SerializeField] private double _draftRepairMaximumHealth = 8d;
        [SerializeField] private double _draftRepairBaseHeal = 28d;
        [SerializeField] private double _draftRepairHealPerRank = 4d;
        [SerializeField] private float _visibleArenaRadius = 14.75f;
        [SerializeField] private float _overdriveDurationSeconds = 7f;
        [SerializeField] private float _overdriveCooldownSeconds = 18f;
        [SerializeField] private int _overdriveCooldownBonusTicks = 10;
        [SerializeField] private double _overdriveDamageMultiplier = 1.55d;

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public string ObjectiveId => _objectiveId ?? string.Empty;
        public string RunRewardTargetId => _runRewardTargetId ?? string.Empty;
        public string OfflineRewardTargetId => _offlineRewardTargetId ?? string.Empty;
        public string DamageTypeId => _damageTypeId ?? string.Empty;
        public double ObjectiveMaximumHealth => Math.Max(1d, _objectiveMaximumHealth);
        public float ObjectiveContactRadius => Mathf.Max(0.01f, _objectiveContactRadius);
        public int ObjectiveLives => Math.Max(1, _objectiveLives);
        public int ObjectiveLifeRestoreAmount => Math.Max(0, _objectiveLifeRestoreAmount);
        public float SpawnRingRadius => Mathf.Max(0.1f, _spawnRingRadius);
        public IReadOnlyList<IdleAutoDefenseSpawnChannelRule> SpawnChannels => _spawnChannels ?? Array.Empty<IdleAutoDefenseSpawnChannelRule>();
        public EnemyDefinitionAsset EliteEnemy => _eliteEnemy;
        public EnemyDefinitionAsset BossEnemy => _bossEnemy;
        public string EliteEnemyId => _eliteEnemy == null ? string.Empty : _eliteEnemy.Id;
        public string BossEnemyId => _bossEnemy == null ? string.Empty : _bossEnemy.Id;
        public IReadOnlyList<IdleAutoDefenseModuleRule> Modules => _modules ?? Array.Empty<IdleAutoDefenseModuleRule>();
        public double ManualDamageRankBonus => _manualDamageRankBonus;
        public double ManualRangeRankDamageBonus => _manualRangeRankDamageBonus;
        public double ManualRangeRankBonus => _manualRangeRankBonus;
        public double ModuleRangeRankBonus => _moduleRangeRankBonus;
        public double ManualMaximumRange => Math.Max(0.1d, _manualMaximumRange);
        public int MinimumProjectileImpactDelayTicks => Math.Max(1, _minimumProjectileImpactDelayTicks);
        public int MaximumProjectileImpactDelayTicks => Math.Max(MinimumProjectileImpactDelayTicks, _maximumProjectileImpactDelayTicks);
        public double ProjectileFinishDamageThreshold => Math.Max(0.1d, _projectileFinishDamageThreshold);
        public double PurchaseRangeDamageBonus => _purchaseRangeDamageBonus;
        public double PurchaseRepairMaximumHealth => Math.Max(0d, _purchaseRepairMaximumHealth);
        public double PurchaseRepairBaseHeal => Math.Max(0d, _purchaseRepairBaseHeal);
        public double PurchaseRepairHealPerRank => Math.Max(0d, _purchaseRepairHealPerRank);
        public double DraftRepairMaximumHealth => Math.Max(0d, _draftRepairMaximumHealth);
        public double DraftRepairBaseHeal => Math.Max(0d, _draftRepairBaseHeal);
        public double DraftRepairHealPerRank => Math.Max(0d, _draftRepairHealPerRank);
        public float VisibleArenaRadius => Mathf.Max(0.1f, _visibleArenaRadius);
        public float OverdriveDurationSeconds => Mathf.Max(0.1f, _overdriveDurationSeconds);
        public float OverdriveCooldownSeconds => Mathf.Max(0f, _overdriveCooldownSeconds);
        public int OverdriveCooldownBonusTicks => Math.Max(0, _overdriveCooldownBonusTicks);
        public double OverdriveDamageMultiplier => Math.Max(1d, _overdriveDamageMultiplier);

        public AutoDefenseObjectiveDefinition CreateObjectiveDefinition()
        {
            return new AutoDefenseObjectiveDefinition(
                new DefenseObjectiveId(ObjectiveId),
                Vector3.zero,
                ObjectiveMaximumHealth,
                new DamageTypeId(DamageTypeId),
                ObjectiveContactRadius,
                ObjectiveLives,
                ObjectiveLifeRestoreAmount);
        }

        public AutoDefenseSpawnRingDefinition CreateSpawnRingDefinition()
        {
            IReadOnlyList<IdleAutoDefenseSpawnChannelRule> source = SpawnChannels.Count == 0 ? CreateDefaultSpawnChannels() : SpawnChannels;
            var channels = new AutoDefenseSpawnChannelDefinition[source.Count];
            for (int i = 0; i < channels.Length; i++)
                channels[i] = new AutoDefenseSpawnChannelDefinition(new WorldSpawnChannelId(source[i].Id), source[i].AngleDegrees);
            return new AutoDefenseSpawnRingDefinition(SpawnRingRadius, channels);
        }

        public IdleAutoDefenseModuleRule GetModule(IdleAutoDefenseModuleRole role)
        {
            for (int i = 0; i < Modules.Count; i++)
            {
                IdleAutoDefenseModuleRule module = Modules[i];
                if (module != null && module.Role == role) return module;
            }

            return null;
        }

        public IdleAutoDefenseModuleRule GetModuleByWeaponId(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId)) return null;
            for (int i = 0; i < Modules.Count; i++)
            {
                IdleAutoDefenseModuleRule module = Modules[i];
                if (module != null && string.Equals(module.WeaponId, weaponId, StringComparison.OrdinalIgnoreCase)) return module;
            }

            return null;
        }

        public IdleAutoDefenseModuleRule GetModuleByAttackId(string attackId)
        {
            if (string.IsNullOrWhiteSpace(attackId)) return null;
            for (int i = 0; i < Modules.Count; i++)
            {
                IdleAutoDefenseModuleRule module = Modules[i];
                if (module != null && string.Equals(module.AttackId, attackId, StringComparison.OrdinalIgnoreCase)) return module;
            }

            return null;
        }

        public static IdleAutoDefenseGameRulesAsset CreateTransient(
            IReadOnlyList<WeaponDefinitionAsset> weapons,
            IReadOnlyList<EnemyDefinitionAsset> enemies)
        {
            var asset = CreateInstance<IdleAutoDefenseGameRulesAsset>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            WeaponDefinitionAsset shard = FindWeapon(weapons, BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value);
            WeaponDefinitionAsset pulse = FindWeapon(weapons, BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value);
            WeaponDefinitionAsset arc = FindWeapon(weapons, BasicIdleAutoDefenseGame.ArcBurstTowerWeaponId.Value);
            WeaponDefinitionAsset homing = FindWeapon(weapons, BasicIdleAutoDefenseGame.HomingSpireWeaponId.Value);
            asset._modules = new[]
            {
                new IdleAutoDefenseModuleRule(IdleAutoDefenseModuleRole.StartingProjectile, shard, true, 3.2d, 0d, 0d, 8.4d, 18, 3, 1),
                new IdleAutoDefenseModuleRule(IdleAutoDefenseModuleRole.PrecisionBeam, pulse, false, 5d, 1.25d, 0.45d, 7.4d, 28, 2, 1),
                new IdleAutoDefenseModuleRule(IdleAutoDefenseModuleRole.AreaBurst, arc, false, 8d, 1.55d, 0d, 6.2d, 52, 3, 2),
                new IdleAutoDefenseModuleRule(IdleAutoDefenseModuleRole.HomingProjectile, homing, false, 8d, 1.45d, 0.45d, 8.8d, 42, 2, 1)
            };
            asset._eliteEnemy = FindEnemy(enemies, BasicIdleAutoDefenseGame.EliteEnemySpawnableId.Value);
            asset._bossEnemy = FindEnemy(enemies, BasicIdleAutoDefenseGame.BossEnemySpawnableId.Value);
            return asset;
        }

        private static IdleAutoDefenseSpawnChannelRule[] CreateDefaultSpawnChannels()
        {
            return new[]
            {
                new IdleAutoDefenseSpawnChannelRule("perimeter-north", 0f),
                new IdleAutoDefenseSpawnChannelRule("perimeter-northeast", 45f),
                new IdleAutoDefenseSpawnChannelRule("perimeter-east", 90f),
                new IdleAutoDefenseSpawnChannelRule("perimeter-southeast", 135f),
                new IdleAutoDefenseSpawnChannelRule("perimeter-south", 180f),
                new IdleAutoDefenseSpawnChannelRule("perimeter-southwest", 225f),
                new IdleAutoDefenseSpawnChannelRule("perimeter-west", 270f),
                new IdleAutoDefenseSpawnChannelRule("perimeter-northwest", 315f)
            };
        }

        private static WeaponDefinitionAsset FindWeapon(IReadOnlyList<WeaponDefinitionAsset> weapons, string id)
        {
            if (weapons == null) return null;
            for (int i = 0; i < weapons.Count; i++)
                if (weapons[i] != null && string.Equals(weapons[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return weapons[i];
            return null;
        }

        private static EnemyDefinitionAsset FindEnemy(IReadOnlyList<EnemyDefinitionAsset> enemies, string id)
        {
            if (enemies == null) return null;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] != null && string.Equals(enemies[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return enemies[i];
            return null;
        }
    }

    [Serializable]
    public sealed class IdleAutoDefenseSpawnChannelRule
    {
        [SerializeField] private string _id;
        [SerializeField] private float _angleDegrees;

        public IdleAutoDefenseSpawnChannelRule()
        {
        }

        public IdleAutoDefenseSpawnChannelRule(string id, float angleDegrees)
        {
            _id = id ?? string.Empty;
            _angleDegrees = angleDegrees;
        }

        public string Id => _id ?? string.Empty;
        public float AngleDegrees => _angleDegrees;
    }

    [Serializable]
    public sealed class IdleAutoDefenseModuleRule
    {
        [SerializeField] private IdleAutoDefenseModuleRole _role;
        [SerializeField] private WeaponDefinitionAsset _weapon;
        [SerializeField] private bool _startsUnlocked;
        [SerializeField] private double _baseDamage;
        [SerializeField] private double _damagePerDamageRank;
        [SerializeField] private double _damagePerRangeRank;
        [SerializeField] private double _baseRange;
        [SerializeField] private int _minimumCooldownTicks;
        [SerializeField] private int _cooldownReductionPerFireRateRank;
        [SerializeField] private int _baseTargetCount;

        public IdleAutoDefenseModuleRule()
        {
        }

        public IdleAutoDefenseModuleRule(
            IdleAutoDefenseModuleRole role,
            WeaponDefinitionAsset weapon,
            bool startsUnlocked,
            double baseDamage,
            double damagePerDamageRank,
            double damagePerRangeRank,
            double baseRange,
            int minimumCooldownTicks,
            int cooldownReductionPerFireRateRank,
            int baseTargetCount)
        {
            _role = role;
            _weapon = weapon;
            _startsUnlocked = startsUnlocked;
            _baseDamage = baseDamage;
            _damagePerDamageRank = damagePerDamageRank;
            _damagePerRangeRank = damagePerRangeRank;
            _baseRange = baseRange;
            _minimumCooldownTicks = minimumCooldownTicks;
            _cooldownReductionPerFireRateRank = cooldownReductionPerFireRateRank;
            _baseTargetCount = baseTargetCount;
        }

        public IdleAutoDefenseModuleRole Role => _role;
        public WeaponDefinitionAsset Weapon => _weapon;
        public string WeaponId => _weapon == null ? string.Empty : _weapon.Id;
        public string AttackId => _weapon == null || _weapon.Stats == null || _weapon.Stats.Attack == null ? string.Empty : _weapon.Stats.Attack.Id;
        public bool StartsUnlocked => _startsUnlocked;
        public double BaseDamage => Math.Max(0.1d, _baseDamage);
        public double DamagePerDamageRank => _damagePerDamageRank;
        public double DamagePerRangeRank => _damagePerRangeRank;
        public double BaseRange => Math.Max(0.1d, _baseRange);
        public int BaseCooldownTicks => _weapon == null || _weapon.Stats == null ? 1 : Math.Max(1, _weapon.Stats.CooldownTicks);
        public int MinimumCooldownTicks => Math.Max(1, _minimumCooldownTicks);
        public int CooldownReductionPerFireRateRank => Math.Max(0, _cooldownReductionPerFireRateRank);
        public int BaseTargetCount => Math.Max(1, _baseTargetCount);
        public int BuildCost => _weapon == null || _weapon.Stats == null ? 0 : Math.Max(0, _weapon.Stats.BuildCost);
    }
}
