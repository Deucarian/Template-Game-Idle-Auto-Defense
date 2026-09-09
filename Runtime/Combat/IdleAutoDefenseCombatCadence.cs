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
    internal sealed class IdleAutoDefenseCombatCadence
    {
        private readonly IIdleAutoDefenseCombatEnemies _enemies;
        private readonly IdleAutoDefenseCombatStatistics _stats;
        private readonly IdleAutoDefenseRunBuild _build;
        private readonly IdleAutoDefenseCombatContent _content;
        private readonly IdleAutoDefenseCombatRules _rules;
        private readonly IdleAutoDefenseCombatTargets _targets;
        private readonly IdleAutoDefenseVisibleDamage _damage;
        private readonly IdleAutoDefenseCombatProjectiles _projectiles;
        private int ManualCooldown;
        private int PulseCooldown;
        private int ArcCooldown;
        private int HomingCooldown;

        internal IdleAutoDefenseCombatCadence(IIdleAutoDefenseCombatEnemies enemies, IdleAutoDefenseCombatStatistics stats,
            IdleAutoDefenseRunBuild build, IdleAutoDefenseCombatContent content, IdleAutoDefenseCombatRules rules,
            IdleAutoDefenseCombatTargets targets, IdleAutoDefenseVisibleDamage damage, IdleAutoDefenseCombatProjectiles projectiles)
        {
            _enemies = enemies;
            _stats = stats;
            _build = build;
            _content = content;
            _rules = rules;
            _targets = targets;
            _damage = damage;
            _projectiles = projectiles;
        }

        internal void Reset()
        {
            ManualCooldown = 0;
            PulseCooldown = 0;
            ArcCooldown = 0;
            HomingCooldown = 0;
        }

        internal int ApplyDirectDamageBonusIfReady()
        {
            // _build.DirectDamageBonus feeds visible tower/module damage. It must not silently delete enemies.
            return 0;
        }

        internal int FireManualTowerShotIfReady(int ticks)
        {
            IdleAutoDefenseModuleRule module = _build.ResolveModuleRule(IdleAutoDefenseModuleRole.StartingProjectile);
            ManualCooldown += Math.Max(1, ticks);
            int baseCooldown = module == null ? IdleAutoDefenseCombatDefaults.ManualTowerBaseCooldownTicks : module.BaseCooldownTicks;
            int minimumCooldown = module == null ? IdleAutoDefenseCombatDefaults.ManualTowerMinimumCooldownTicks : module.MinimumCooldownTicks;
            int rankReduction = module == null ? 3 : module.CooldownReductionPerFireRateRank;
            int overdriveReduction = _rules.GameRules == null ? IdleAutoDefenseCombatDefaults.OverdriveCooldownBonusTicks : _rules.GameRules.OverdriveCooldownBonusTicks;
            int cooldownTicks = Math.Max(minimumCooldown, baseCooldown - _build.AttackSpeedUpgradeRank * rankReduction - (_build.OverdriveActive ? overdriveReduction : 0));
            if (ManualCooldown < cooldownTicks) return 0;
            ManualCooldown = 0;

            int kills = 0;
            int shotCount = Math.Max(1, (module == null ? 1 : module.BaseTargetCount) + _build.ShardVolleyBonus);
            double damageAmount = _rules.ResolveManualTowerDamage();
            AttackDefinitionAsset attack = _content.FindAttackRecipeForPresentation(module == null ? BasicIdleAutoDefenseGame.ShardAttackId.Value : module.AttackId);
            for (int shot = 0; shot < shotCount; shot++)
            {
                AutoDefenseRuntimeSnapshot snapshot = _enemies.CreateSnapshot();
                if (!_targets.TrySelectPriorityEnemyWithinRange(snapshot, _rules.ResolveManualTowerRange(), out AutoDefenseEnemySnapshot selected)) break;
                if (_projectiles.TryLaunchVisibleProjectileAtEnemy(selected, attack, damageAmount)) continue;
                if (!_damage.TryDamageEnemyWithPresentation(selected, attack, damageAmount, out bool killed)) continue;
                if (!killed) continue;
                _stats.DirectOrCombatKillCount++;
                kills++;
            }

            return kills;
        }

        internal int FireUnlockedModulesIfReady(int ticks)
        {
            int kills = 0;
            if (_build.PulseBeamUnlocked)
            {
                IdleAutoDefenseModuleRule module = _build.ResolveModuleRule(IdleAutoDefenseModuleRole.PrecisionBeam);
                PulseCooldown += Math.Max(1, ticks);
                int overdriveReduction = _rules.GameRules == null ? IdleAutoDefenseCombatDefaults.OverdriveCooldownBonusTicks : _rules.GameRules.OverdriveCooldownBonusTicks;
                int cooldown = module == null
                    ? Math.Max(28, IdleAutoDefenseCombatDefaults.PulseBeamModuleCooldownTicks - _build.AttackSpeedUpgradeRank * 2 - (_build.OverdriveActive ? overdriveReduction : 0))
                    : Math.Max(module.MinimumCooldownTicks, module.BaseCooldownTicks - _build.AttackSpeedUpgradeRank * module.CooldownReductionPerFireRateRank - (_build.OverdriveActive ? overdriveReduction : 0));
                if (PulseCooldown >= cooldown)
                {
                    PulseCooldown = 0;
                    _stats.ModuleActivationCount++;
                    kills += TryKillPriorityEnemies(
                        _rules.ResolveModuleDamage((module == null ? 5d : module.BaseDamage) + _build.DamageUpgradeRank * (module == null ? 1.25d : module.DamagePerDamageRank) + _build.RangeUpgradeRank * (module == null ? 0.45d : module.DamagePerRangeRank)),
                        (module == null ? 1 : module.BaseTargetCount) + _build.PulseBeamBonus,
                        module == null ? BasicIdleAutoDefenseGame.PulseAttackId.Value : module.AttackId,
                        _rules.ResolveModuleRange(module == null ? IdleAutoDefenseCombatDefaults.PulseBeamModuleBaseRange : module.BaseRange));
                }
            }

            if (_build.ArcBurstUnlocked)
            {
                IdleAutoDefenseModuleRule module = _build.ResolveModuleRule(IdleAutoDefenseModuleRole.AreaBurst);
                ArcCooldown += Math.Max(1, ticks);
                int overdriveReduction = _rules.GameRules == null ? IdleAutoDefenseCombatDefaults.OverdriveCooldownBonusTicks : _rules.GameRules.OverdriveCooldownBonusTicks;
                int cooldown = module == null
                    ? Math.Max(52, IdleAutoDefenseCombatDefaults.ArcBurstModuleCooldownTicks - _build.AttackSpeedUpgradeRank * 3 - (_build.OverdriveActive ? overdriveReduction : 0))
                    : Math.Max(module.MinimumCooldownTicks, module.BaseCooldownTicks - _build.AttackSpeedUpgradeRank * module.CooldownReductionPerFireRateRank - (_build.OverdriveActive ? overdriveReduction : 0));
                if (ArcCooldown >= cooldown)
                {
                    ArcCooldown = 0;
                    _stats.ModuleActivationCount++;
                    kills += TryKillPriorityEnemies(
                        _rules.ResolveModuleDamage((module == null ? 8d : module.BaseDamage) + _build.DamageUpgradeRank * (module == null ? 1.55d : module.DamagePerDamageRank) + _build.RangeUpgradeRank * (module == null ? 0d : module.DamagePerRangeRank)),
                        (module == null ? 2 : module.BaseTargetCount) + _build.ArcBurstBonus,
                        module == null ? BasicIdleAutoDefenseGame.ArcBurstAttackId.Value : module.AttackId,
                        _rules.ResolveModuleRange(module == null ? IdleAutoDefenseCombatDefaults.ArcBurstModuleBaseRange : module.BaseRange));
                }
            }

            if (_build.HomingPulseUnlocked)
            {
                IdleAutoDefenseModuleRule module = _build.ResolveModuleRule(IdleAutoDefenseModuleRole.HomingProjectile);
                HomingCooldown += Math.Max(1, ticks);
                int overdriveReduction = _rules.GameRules == null ? IdleAutoDefenseCombatDefaults.OverdriveCooldownBonusTicks : _rules.GameRules.OverdriveCooldownBonusTicks;
                int cooldown = module == null
                    ? Math.Max(42, IdleAutoDefenseCombatDefaults.HomingPulseModuleCooldownTicks - _build.AttackSpeedUpgradeRank * 2 - (_build.OverdriveActive ? overdriveReduction : 0))
                    : Math.Max(module.MinimumCooldownTicks, module.BaseCooldownTicks - _build.AttackSpeedUpgradeRank * module.CooldownReductionPerFireRateRank - (_build.OverdriveActive ? overdriveReduction : 0));
                if (HomingCooldown >= cooldown)
                {
                    HomingCooldown = 0;
                    _stats.ModuleActivationCount++;
                    kills += TryKillPriorityEnemies(
                        _rules.ResolveModuleDamage((module == null ? 8d : module.BaseDamage) + _build.DamageUpgradeRank * (module == null ? 1.45d : module.DamagePerDamageRank) + _build.RangeUpgradeRank * (module == null ? 0.45d : module.DamagePerRangeRank)),
                        (module == null ? 1 : module.BaseTargetCount) + _build.HomingPulseBonus,
                        module == null ? BasicIdleAutoDefenseGame.HomingPulseAttackId.Value : module.AttackId,
                        _rules.ResolveModuleRange(module == null ? IdleAutoDefenseCombatDefaults.HomingPulseModuleBaseRange : module.BaseRange),
                        preferProjectileVisual: true);
                }
            }

            return kills;
        }

        internal int TryKillPriorityEnemies(double damageThreshold, int maxKills, string attackId, double range, bool preferProjectileVisual = false)
        {
            if (!_enemies.IsAvailable || maxKills <= 0) return 0;
            int kills = 0;
            AttackDefinitionAsset attack = _content.FindAttackRecipeForPresentation(attackId);
            for (int attempt = 0; attempt < maxKills; attempt++)
            {
                AutoDefenseRuntimeSnapshot snapshot = _enemies.CreateSnapshot();
                if (!_targets.TrySelectPriorityEnemyWithinRange(snapshot, range, out AutoDefenseEnemySnapshot selected)) break;
                if (preferProjectileVisual && _projectiles.TryLaunchVisibleProjectileAtEnemy(selected, attack, damageThreshold)) continue;
                if (!_damage.TryDamageEnemyWithPresentation(selected, attack, damageThreshold, out bool killed)) break;
                if (killed)
                {
                    _stats.DirectOrCombatKillCount++;
                    kills++;
                }
            }

            return kills;
        }
    }
}
