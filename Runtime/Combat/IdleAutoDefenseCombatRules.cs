using System;
using System.Collections.Generic;
using Deucarian.Attacks;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.Combat;
using Deucarian.Projectiles;
using Deucarian.WeaponSystems;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseCombatRules
    {
        private readonly IdleAutoDefenseRunBuild _build;
        private readonly Func<IdleAutoDefenseGameRulesAsset> _gameRules;
        private readonly Func<IdleAutoDefenseRunProfileAsset> _profile;
        private readonly Func<IdleAutoDefenseRewardDraftSettings> _draft;
        private IdleAutoDefenseGameRulesAsset _activeGameRules => _gameRules();
        private IdleAutoDefenseRunProfileAsset _activeRunProfile => _profile();
        internal IdleAutoDefenseGameRulesAsset GameRules => _gameRules();
        internal float ProjectileRetargetRadius => _draft().ProjectileRetargetRadius;

        internal IdleAutoDefenseCombatRules(IdleAutoDefenseRunBuild build, Func<IdleAutoDefenseGameRulesAsset> gameRules,
            Func<IdleAutoDefenseRunProfileAsset> profile, Func<IdleAutoDefenseRewardDraftSettings> draft)
        {
            _build = build ?? throw new ArgumentNullException(nameof(build));
            _gameRules = gameRules ?? throw new ArgumentNullException(nameof(gameRules));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _draft = draft ?? throw new ArgumentNullException(nameof(draft));
        }

        internal double ResolveManualTowerDamage()
        {
            IdleAutoDefenseModuleRule module = _build.ResolveModuleRule(IdleAutoDefenseModuleRole.StartingProjectile);
            double baseDamage = (module == null ? IdleAutoDefenseCombatDefaults.ManualTowerBaseDamage : module.BaseDamage) +
                _build.DamageUpgradeRank * (_activeGameRules == null ? IdleAutoDefenseCombatDefaults.ManualTowerDamageRankBonus : _activeGameRules.ManualDamageRankBonus) +
                _build.RangeUpgradeRank * (_activeGameRules == null ? IdleAutoDefenseCombatDefaults.ManualTowerRangeRankBonus : _activeGameRules.ManualRangeRankDamageBonus) +
                _build.DirectDamageBonus;
            return ResolveModuleDamage(baseDamage);
        }

        internal double ResolveManualTowerRange()
        {
            IdleAutoDefenseModuleRule module = _build.ResolveModuleRule(IdleAutoDefenseModuleRole.StartingProjectile);
            double maximumRange = _activeGameRules == null ? IdleAutoDefenseCombatDefaults.ManualTowerMaximumRange : _activeGameRules.ManualMaximumRange;
            double baseRange = module == null ? IdleAutoDefenseCombatDefaults.ManualTowerBaseRange : module.BaseRange;
            double rankBonus = _activeGameRules == null ? IdleAutoDefenseCombatDefaults.ManualTowerRangeRankBonus : _activeGameRules.ManualRangeRankBonus;
            return Math.Min(maximumRange, baseRange + _build.RangeUpgradeRank * rankBonus);
        }

        internal double ResolveModuleRange(double baseRange)
        {
            double maximumRange = (_activeGameRules == null ? IdleAutoDefenseCombatDefaults.ManualTowerMaximumRange : _activeGameRules.ManualMaximumRange) + 1d;
            double rankBonus = _activeGameRules == null ? IdleAutoDefenseCombatDefaults.ModuleRangeRankBonus : _activeGameRules.ModuleRangeRankBonus;
            return Math.Min(maximumRange, baseRange + _build.RangeUpgradeRank * rankBonus);
        }

        internal double ResolveModuleDamage(double baseDamage)
        {
            double multiplier = 1d + Math.Max(0d, _build.RewardDamageMultiplierBonus);
            if (_build.OverdriveActive)
                multiplier *= _activeGameRules == null ? IdleAutoDefenseCombatDefaults.OverdriveDamageMultiplier : _activeGameRules.OverdriveDamageMultiplier;
            return Math.Max(1d, baseDamage * multiplier);
        }

        internal bool IsEliteEnemy(AutoDefenseEnemySnapshot enemy)
        {
            string eliteId = _activeGameRules == null || string.IsNullOrWhiteSpace(_activeGameRules.EliteEnemyId)
                ? BasicIdleAutoDefenseGame.EliteEnemySpawnableId.Value
                : _activeGameRules.EliteEnemyId;
            return string.Equals(enemy.SpawnableId.Value, eliteId, StringComparison.OrdinalIgnoreCase);
        }

        internal bool IsBossEnemy(AutoDefenseEnemySnapshot enemy)
        {
            string bossId = _activeGameRules == null || string.IsNullOrWhiteSpace(_activeGameRules.BossEnemyId)
                ? BasicIdleAutoDefenseGame.BossEnemySpawnableId.Value
                : _activeGameRules.BossEnemyId;
            return string.Equals(enemy.SpawnableId.Value, bossId, StringComparison.OrdinalIgnoreCase);
        }

        internal int CalculateProjectileImpactDelayTicks(Vector3 origin, Vector3 destination, float speed)
        {
            float safeSpeed = Mathf.Max(0.5f, speed);
            float secondsPerTick = _activeRunProfile == null ? 0.05f : _activeRunProfile.SecondsPerSimulationTick;
            int ticks = Mathf.CeilToInt(Vector3.Distance(origin, destination) / safeSpeed / secondsPerTick);
            int minimum = _activeGameRules == null ? IdleAutoDefenseCombatDefaults.MinimumProjectileImpactDelayTicks : _activeGameRules.MinimumProjectileImpactDelayTicks;
            int maximum = _activeGameRules == null ? IdleAutoDefenseCombatDefaults.MaximumProjectileImpactDelayTicks : _activeGameRules.MaximumProjectileImpactDelayTicks;
            return Mathf.Clamp(ticks, minimum, maximum);
        }

        internal static float ResolveProjectileSpeed(AttackDefinitionAsset attack)
        {
            return attack != null && attack.Delivery != null
                ? Mathf.Max(0.5f, attack.Delivery.ProjectileSpeed)
                : 8f;
        }

        internal double ResolveAttackDamage(AttackDefinitionAsset attack)
        {
            double threshold = _activeGameRules == null ? IdleAutoDefenseCombatDefaults.SampleProjectileFinishThreshold : _activeGameRules.ProjectileFinishDamageThreshold;
            return attack != null && attack.Mechanics != null
                ? Math.Max(threshold, attack.Mechanics.DamageAmount)
                : threshold;
        }

        internal double ResolvePresentationRange(AttackDefinitionAsset attack)
        {
            if (attack == null) return IdleAutoDefenseCombatDefaults.ManualTowerBaseRange;
            IdleAutoDefenseModuleRule module = _activeGameRules == null ? null : _activeGameRules.GetModuleByAttackId(attack.Id);
            if (module != null)
            {
                return module.Role == IdleAutoDefenseModuleRole.StartingProjectile
                    ? ResolveManualTowerRange()
                    : ResolveModuleRange(module.BaseRange);
            }
            if (_activeGameRules == null)
            {
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.ShardAttackId.Value, StringComparison.OrdinalIgnoreCase)) return ResolveManualTowerRange();
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.PulseAttackId.Value, StringComparison.OrdinalIgnoreCase)) return ResolveModuleRange(IdleAutoDefenseCombatDefaults.PulseBeamModuleBaseRange);
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.ArcBurstAttackId.Value, StringComparison.OrdinalIgnoreCase)) return ResolveModuleRange(IdleAutoDefenseCombatDefaults.ArcBurstModuleBaseRange);
                if (string.Equals(attack.Id, BasicIdleAutoDefenseGame.HomingPulseAttackId.Value, StringComparison.OrdinalIgnoreCase)) return ResolveModuleRange(IdleAutoDefenseCombatDefaults.HomingPulseModuleBaseRange);
            }
            return attack.Mechanics == null ? IdleAutoDefenseCombatDefaults.ManualTowerBaseRange : attack.Mechanics.Range;
        }
    }
}
